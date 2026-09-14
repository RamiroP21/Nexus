using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Nexus.Simulation;

namespace Nexus.Host;

/// <summary>Small loopback-only process host for semantic District01 authority.</summary>
public sealed class DistrictAuthorityHost : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly int _requestedPort;
    private TcpListener? _listener;
    private DistrictAuthoritySession? _session;
    private CancellationTokenSource? _stop;
    private readonly CampaignSaveRepository? _saves;
    private ulong _saveRevision;
    private string _saveStatus = "ephemeral";
    private string? _saveError;
    private string? _savedHash;
    private bool _connected;
    private bool _shutdownRequested;

    public DistrictAuthorityHost(int port = 0, string? saveDirectory = null)
    {
        _requestedPort = port;
        if (saveDirectory is not null) _saves = new(saveDirectory);
    }
    public int Port { get; private set; }
    public DistrictAuthoritySession? Session { get { lock (_gate) return _session; } }

    public void Start()
    {
        if (_listener is not null) throw new InvalidOperationException("Host is already started.");
        _listener = new TcpListener(IPAddress.Loopback, _requestedPort);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _stop = new CancellationTokenSource();
        Console.WriteLine($"NEXUS DISTRICT HOST READY port={Port}");
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_listener is null) Start();
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stop!.Token);
        Task ticker = TickLoopAsync(linked.Token);
        try
        {
            while (!linked.IsCancellationRequested)
            {
                TcpClient client;
                try { client = await _listener!.AcceptTcpClientAsync(linked.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) when (linked.IsCancellationRequested) { break; }
                await HandleClientAsync(client, linked.Token).ConfigureAwait(false);
            }
        }
        finally
        {
            linked.Cancel();
            lock (_gate) SaveIfDirty();
            _listener?.Stop();
            try { await ticker.ConfigureAwait(false); } catch (OperationCanceledException) { }
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_gate) SaveIfDirty();
        CancellationTokenSource? stop = Interlocked.Exchange(ref _stop, null);
        stop?.Cancel();
        _listener?.Stop();
        _listener = null;
        return ValueTask.CompletedTask;
    }

    private async Task TickLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(1000d / 60d));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                lock (_gate)
                {
                    if (!_connected || _saveError is not null || _shutdownRequested) continue;
                    ulong sequence = _session?.ServerSequence ?? 0;
                    _session?.Advance(1);
                    if (_session?.ServerSequence != sequence) SaveIfDirty();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    DistrictWireMessage? message = await DistrictProtocolCodec.ReadAsync(stream, cancellationToken).ConfigureAwait(false);
                    if (message is null) break;
                    DistrictWireMessage response = Handle(message);
                    await DistrictProtocolCodec.WriteAsync(stream, response, cancellationToken).ConfigureAwait(false);
                    if (_shutdownRequested) { _stop?.Cancel(); break; }
                }
            }
            catch (InvalidDataException ex)
            {
                await TryWriteAsync(stream, Error(DistrictMessageTypes.ProtocolError, "malformed", ex.Message), cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                await TryWriteAsync(stream, Error(DistrictMessageTypes.ProtocolError, "malformed", ex.Message), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (IOException) { }
            catch (SocketException) { }
            finally { lock (_gate) { _connected = false; SaveIfDirty(); } }
        }
    }

    private DistrictWireMessage Handle(DistrictWireMessage message)
    {
        if (message.MessageType == DistrictMessageTypes.ClientHello)
        {
            return string.Equals(message.ProtocolVersion, DistrictAuthorityProtocol.Version, StringComparison.Ordinal)
                ? new() { MessageType = DistrictMessageTypes.ServerHello }
                : Error(DistrictMessageTypes.ProtocolError, "protocol_version", "Incompatible protocol version.");
        }
        if (!string.Equals(message.ProtocolVersion, DistrictAuthorityProtocol.Version, StringComparison.Ordinal))
            return Error(DistrictMessageTypes.ProtocolError, "protocol_version", "Incompatible protocol version.");

        lock (_gate)
        {
            switch (message.MessageType)
            {
                case DistrictMessageTypes.CreateSession:
                case DistrictMessageTypes.ContinueCampaign:
                    if (_saveError is not null) return Error(DistrictMessageTypes.CommandRejected, "save_error", _saveError);
                    if (_session is null)
                    {
                        try
                        {
                            CampaignLoadResult? loaded = _saves?.Load();
                            if (loaded is not null)
                            {
                                _session = DistrictAuthoritySession.RestoreCampaign(loaded.Envelope.State, Guid.NewGuid().ToString("N"));
                                _saveRevision = loaded.Envelope.SaveRevision;
                                _savedHash = loaded.Envelope.StateHash;
                                _saveStatus = loaded.Recovered ? "recovered_backup" : "loaded";
                            }
                            else { NewCampaign(message.Seed); }
                        }
                        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
                        { _saveError = ex.Message; _saveStatus = "error"; return Error(DistrictMessageTypes.CommandRejected, "save_error", ex.Message); }
                    }
                    _connected = true;
                    return new() { MessageType = DistrictMessageTypes.SessionStarted, SessionId = _session!.SessionId, Snapshot = Snapshot() };
                case DistrictMessageTypes.NewCampaign:
                    // An explicit development reset is the sole operation allowed to replace campaign history.
                    _saveError = null;
                    NewCampaign(message.Seed);
                    _connected = _saveError is null;
                    return _saveError is null
                        ? new() { MessageType = DistrictMessageTypes.SessionStarted, SessionId = _session!.SessionId, Snapshot = Snapshot() }
                        : Error(DistrictMessageTypes.CommandRejected, "save_error", _saveError);
                case DistrictMessageTypes.SaveCampaign:
                    SaveIfDirty();
                    return _saveError is null ? new() { MessageType = DistrictMessageTypes.Snapshot, Snapshot = Snapshot() }
                        : Error(DistrictMessageTypes.CommandRejected, "save_error", _saveError);
                case DistrictMessageTypes.ShutdownHost:
                    _connected = false;
                    SaveIfDirty();
                    _shutdownRequested = _saveError is null;
                    return _saveError is null ? new() { MessageType = DistrictMessageTypes.HostShuttingDown, Snapshot = Snapshot() }
                        : Error(DistrictMessageTypes.CommandRejected, "save_error", _saveError);
                case DistrictMessageTypes.ResumeSession:
                case DistrictMessageTypes.SnapshotRequest:
                    return _session is not null && string.Equals(message.SessionId, _session.SessionId, StringComparison.Ordinal)
                        ? new() { MessageType = DistrictMessageTypes.Snapshot, SessionId = _session.SessionId, Snapshot = Snapshot() }
                        : Error(DistrictMessageTypes.CommandRejected, "stale_session", "Session is not available.");
                case DistrictMessageTypes.Ping:
                    return new() { MessageType = DistrictMessageTypes.Pong };
                case DistrictMessageTypes.ClientCommand:
                    return HandleCommand(message);
                default:
                    return Error(DistrictMessageTypes.ProtocolError, "unknown_message", "Unknown message type.");
            }
        }
    }

    private DistrictWireMessage HandleCommand(DistrictWireMessage message)
    {
        if (_session is null) return Error(DistrictMessageTypes.CommandRejected, "no_session", "Create a session first.");
        if (_saveError is not null) return Error(DistrictMessageTypes.CommandRejected, "save_error", _saveError);
        if (message.CommandType is not DistrictCommandType type || message.SessionId is null || message.ClientId is null || message.CommandId is null)
            return Error(DistrictMessageTypes.CommandRejected, "malformed_command", "Command fields are incomplete.");
        // Unity JsonUtility emits nullable numeric fields as zero. Treat zero
        // as an omitted target tick so a command is validated at current time.
        ulong? targetTick = message.TargetTick is 0 ? null : message.TargetTick;
        DistrictCommandResult result = _session.Submit(new(DistrictAuthorityProtocol.Version, message.SessionId, message.ClientId, message.ClientSequence, message.CommandId, type, message.EntityId ?? "", targetTick));
        if (result.Accepted && !result.Duplicate) SaveIfDirty();
        if (_saveError is not null) return Error(DistrictMessageTypes.CommandRejected, "save_error", _saveError);
        return result.Accepted
            ? new() { MessageType = DistrictMessageTypes.ServerEvent, SessionId = _session.SessionId, Event = result.Event, Snapshot = Snapshot() }
            : Error(DistrictMessageTypes.CommandRejected, result.Code, result.Message) with { SessionId = _session.SessionId, Snapshot = Snapshot() };
    }

    private DistrictSnapshot? Snapshot() => _session is null ? null : _session.Snapshot() with { SaveRevision = _saveRevision, SaveStatus = _saveStatus };

    private void NewCampaign(ulong seed)
    {
        _session = new(seed, Guid.NewGuid().ToString("N"));
        _session.SetCampaignIdentity(Guid.NewGuid().ToString("N"));
        _savedHash = null;
        SaveIfDirty();
    }

    private void SaveIfDirty()
    {
        if (_session is null || _saves is null || _saveError is not null || _session.ComputeStateHash() == _savedHash) return;
        try
        {
            CampaignSaveEnvelope saved = _saves.Save(_session, checked(_saveRevision + 1));
            _saveRevision = saved.SaveRevision; _savedHash = saved.StateHash; _saveStatus = "saved";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { _saveError = ex.Message; _saveStatus = "error"; Console.Error.WriteLine($"CAMPAIGN SAVE FAILED: {ex.Message}"); }
    }

    private static DistrictWireMessage Error(string type, string code, string message) => new() { MessageType = type, ErrorCode = code, ErrorMessage = message };
    private static async Task TryWriteAsync(Stream stream, DistrictWireMessage message, CancellationToken token)
    { try { await DistrictProtocolCodec.WriteAsync(stream, message, token).ConfigureAwait(false); } catch (IOException) { } }
}
