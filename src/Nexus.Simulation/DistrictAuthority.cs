using Nexus.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nexus.Simulation;

public static class DistrictAuthorityProtocol
{
    public const string Version = "Nexus.DistrictAuthority.v1";
}

public enum DistrictPhase { Calm, Warning, Incident, Aftermath }
public enum DistrictCrisisStatus { Pending, Active, Resolved, Failed }
public enum DistrictInfrastructureState { Stable, Damaged, Stabilized }
public enum DistrictRouteState { Open, Blocked }
public enum DistrictCivilianState { Normal, Sheltered, Incapacitated, PostCrisis }
public enum DistrictCommandType
{
    StartCrisis1,
    ResolveCrisis1,
    FailCrisis1,
    StartCrisis2,
    ResolveCrisis2,
    FailCrisis2,
    ObserveZone,
    ResetSession
}

public sealed record DistrictZoneSnapshot(
    string Id,
    bool Visited,
    DistrictCivilianState CivilianState);

public sealed record DistrictSnapshot(
    string SessionId,
    ulong Seed,
    ulong Tick,
    ulong ServerSequence,
    DistrictPhase Phase,
    DistrictCrisisStatus Crisis1,
    DistrictCrisisStatus Crisis2,
    DistrictInfrastructureState Infrastructure,
    DistrictRouteState Route,
    bool Crisis2InheritedDamage,
    ulong Crisis2DeadlineTick,
    IReadOnlyList<DistrictZoneSnapshot> Zones,
    string StateHash);

public sealed record DistrictCommand(
    string ProtocolVersion,
    string SessionId,
    string ClientId,
    ulong ClientSequence,
    string CommandId,
    DistrictCommandType Type,
    string EntityId = "",
    ulong? TargetTick = null);

public sealed record DistrictEvent(
    string ProtocolVersion,
    string SessionId,
    ulong ServerSequence,
    ulong SimulationTick,
    string Type,
    string EntityId = "",
    string Value = "");

public sealed record DistrictCommandResult(
    bool Accepted,
    bool Duplicate,
    string Code,
    string Message,
    DistrictEvent? Event,
    DistrictSnapshot Snapshot);

/// <summary>Deterministic, semantic District01 authority. Unity physical state is deliberately absent.</summary>
public sealed class DistrictAuthoritySession
{
    public const ulong Crisis1WarningTicks = 30;
    public const ulong Crisis1IncidentTicks = 180;
    public const ulong AftermathTicks = 60;
    public const ulong Crisis2SuccessDeadlineTicks = 240;
    public const ulong Crisis2DamagedDeadlineTicks = 120;

    private readonly Dictionary<string, DistrictZoneSnapshot> _zones = new(StringComparer.Ordinal)
    {
        ["district01.zone.a"] = new("district01.zone.a", false, DistrictCivilianState.Normal),
        ["district01.zone.b"] = new("district01.zone.b", false, DistrictCivilianState.Normal),
        ["district01.zone.c"] = new("district01.zone.c", false, DistrictCivilianState.Normal)
    };
    private readonly Dictionary<string, (ulong Tick, DistrictCommandResult Result)> _commands = new(StringComparer.Ordinal);
    private readonly List<DistrictCommand> _acceptedCommands = [];
    private readonly List<DistrictEvent> _events = [];
    private ulong _phaseEnteredTick;

    public DistrictAuthoritySession(ulong seed, string? sessionId = null)
    {
        Seed = seed;
        SessionId = string.IsNullOrWhiteSpace(sessionId) ? $"district01-{seed:X16}" : sessionId;
    }

    public string SessionId { get; }
    public ulong Seed { get; }
    public ulong Tick { get; private set; }
    public ulong ServerSequence { get; private set; }
    public DistrictPhase Phase { get; private set; } = DistrictPhase.Calm;
    public DistrictCrisisStatus Crisis1 { get; private set; } = DistrictCrisisStatus.Pending;
    public DistrictCrisisStatus Crisis2 { get; private set; } = DistrictCrisisStatus.Pending;
    public DistrictInfrastructureState Infrastructure { get; private set; } = DistrictInfrastructureState.Stable;
    public DistrictRouteState Route { get; private set; } = DistrictRouteState.Open;
    public bool Crisis2InheritedDamage { get; private set; }
    public ulong Crisis2DeadlineTick { get; private set; }
    public IReadOnlyList<DistrictCommand> AcceptedCommands => _acceptedCommands;
    public IReadOnlyList<DistrictEvent> Events => _events;
    public DistrictReplayDocument CreateReplayDocument() =>
        new(DistrictAuthorityProtocol.Version, Seed, _acceptedCommands.ToArray(), Tick);

    public DistrictCommandResult Submit(DistrictCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!string.Equals(command.ProtocolVersion, DistrictAuthorityProtocol.Version, StringComparison.Ordinal))
            return Reject("protocol_version", "Incompatible protocol version.");
        if (!string.Equals(command.SessionId, SessionId, StringComparison.Ordinal))
            return Reject("stale_session", "Command belongs to another session.");
        if (string.IsNullOrWhiteSpace(command.ClientId) || string.IsNullOrWhiteSpace(command.CommandId))
            return Reject("malformed_command", "ClientId and CommandId are required.");
        if (_commands.TryGetValue(command.CommandId, out var prior))
            return prior.Result with { Duplicate = true, Code = "duplicate", Snapshot = Snapshot() };
        if (command.TargetTick is ulong target && target < Tick)
            return Reject("stale_tick", "Target tick is already complete.");

        DistrictCommandResult result = Apply(command);
        _commands[command.CommandId] = (Tick, result);
        if (result.Accepted)
            _acceptedCommands.Add(command);
        return result;
    }

    public void Advance(ulong ticks)
    {
        for (ulong i = 0; i < ticks; i++)
        {
            Tick++;
            if (Phase == DistrictPhase.Warning && Tick - _phaseEnteredTick >= Crisis1WarningTicks)
                Transition(DistrictPhase.Incident, "Crisis1Activated", "district01.crisis.1");
            if (Phase == DistrictPhase.Incident && Crisis1 == DistrictCrisisStatus.Active && Tick - _phaseEnteredTick >= Crisis1IncidentTicks)
                AutoFailCrisis1();
            if (Phase == DistrictPhase.Aftermath && Crisis2 == DistrictCrisisStatus.Pending && Tick - _phaseEnteredTick >= AftermathTicks)
                AutoStartCrisis2();
            if (Crisis2 == DistrictCrisisStatus.Active && Tick >= Crisis2DeadlineTick)
                AutoFailCrisis2();
        }
    }

    public DistrictSnapshot Snapshot() => new(
        SessionId, Seed, Tick, ServerSequence, Phase, Crisis1, Crisis2, Infrastructure, Route,
        Crisis2InheritedDamage, Crisis2DeadlineTick, _zones.Values.OrderBy(static z => z.Id).ToArray(), ComputeStateHash());

    public string ComputeStateHash()
    {
        var h = new StableHasher64();
        h.Add("Nexus.DistrictAuthority.State.v1"); h.Add(Seed); h.Add(Tick); h.Add((int)Phase);
        h.Add((int)Crisis1); h.Add((int)Crisis2); h.Add((int)Infrastructure); h.Add((int)Route);
        h.Add(Crisis2InheritedDamage); h.Add(Crisis2DeadlineTick);
        foreach (DistrictZoneSnapshot zone in _zones.Values.OrderBy(static z => z.Id))
        { h.Add(zone.Id); h.Add(zone.Visited); h.Add((int)zone.CivilianState); }
        return h.ToHexString();
    }

    public void Reset()
    {
        Tick = 0; ServerSequence = 0; Phase = DistrictPhase.Calm; Crisis1 = DistrictCrisisStatus.Pending;
        Crisis2 = DistrictCrisisStatus.Pending; Infrastructure = DistrictInfrastructureState.Stable; Route = DistrictRouteState.Open;
        Crisis2InheritedDamage = false; Crisis2DeadlineTick = 0; _phaseEnteredTick = 0;
        _commands.Clear(); _acceptedCommands.Clear(); _events.Clear();
        foreach (string id in _zones.Keys.ToArray()) _zones[id] = new(id, false, DistrictCivilianState.Normal);
    }

    private DistrictCommandResult Apply(DistrictCommand command)
    {
        if (command.Type == DistrictCommandType.ResetSession) { Reset(); return Accept("SessionReset", "district01", "reset"); }
        if (command.Type == DistrictCommandType.ObserveZone)
        {
            if (!_zones.TryGetValue(command.EntityId, out DistrictZoneSnapshot? zone)) return Reject("unknown_entity", "Unknown zone ID.");
            _zones[command.EntityId] = zone with { Visited = true };
            return Accept("ZoneObserved", command.EntityId, "visited");
        }
        return command.Type switch
        {
            DistrictCommandType.StartCrisis1 when Phase == DistrictPhase.Calm && Crisis1 == DistrictCrisisStatus.Pending => StartCrisis1(),
            DistrictCommandType.ResolveCrisis1 when (Phase == DistrictPhase.Warning || Phase == DistrictPhase.Incident) && Crisis1 == DistrictCrisisStatus.Active => ResolveCrisis1(),
            DistrictCommandType.FailCrisis1 when (Phase == DistrictPhase.Warning || Phase == DistrictPhase.Incident) && Crisis1 == DistrictCrisisStatus.Active => FailCrisis1(),
            DistrictCommandType.StartCrisis2 when Phase == DistrictPhase.Aftermath && Crisis2 == DistrictCrisisStatus.Pending => StartCrisis2(),
            DistrictCommandType.ResolveCrisis2 when Phase == DistrictPhase.Incident && Crisis2 == DistrictCrisisStatus.Active => ResolveCrisis2(),
            DistrictCommandType.FailCrisis2 when Phase == DistrictPhase.Incident && Crisis2 == DistrictCrisisStatus.Active => FailCrisis2(),
            _ => Reject("illegal_transition", "Command is not legal in the current district phase.")
        };
    }

    private DistrictCommandResult StartCrisis1() { Crisis1 = DistrictCrisisStatus.Active; Transition(DistrictPhase.Warning, "Crisis1Warning", "district01.crisis.1"); return LastResult(); }
    private DistrictCommandResult ResolveCrisis1() { Crisis1 = DistrictCrisisStatus.Resolved; Infrastructure = DistrictInfrastructureState.Stabilized; Route = DistrictRouteState.Open; SetZones(DistrictCivilianState.Sheltered); Transition(DistrictPhase.Aftermath, "Crisis1Resolved", "district01.crisis.1"); return LastResult(); }
    private DistrictCommandResult FailCrisis1() { Crisis1 = DistrictCrisisStatus.Failed; Infrastructure = DistrictInfrastructureState.Damaged; Route = DistrictRouteState.Blocked; SetZones(DistrictCivilianState.Incapacitated); Transition(DistrictPhase.Aftermath, "Crisis1Failed", "district01.crisis.1"); return LastResult(); }
    private DistrictCommandResult StartCrisis2() { Crisis2 = DistrictCrisisStatus.Active; Crisis2InheritedDamage = Infrastructure == DistrictInfrastructureState.Damaged; Crisis2DeadlineTick = Tick + (Crisis2InheritedDamage ? Crisis2DamagedDeadlineTicks : Crisis2SuccessDeadlineTicks); Transition(DistrictPhase.Incident, "Crisis2Started", "district01.crisis.2"); return LastResult(); }
    private DistrictCommandResult ResolveCrisis2() { Crisis2 = DistrictCrisisStatus.Resolved; Infrastructure = Infrastructure == DistrictInfrastructureState.Damaged ? DistrictInfrastructureState.Stabilized : Infrastructure; Route = DistrictRouteState.Open; SetZones(DistrictCivilianState.PostCrisis); Transition(DistrictPhase.Aftermath, "Crisis2Resolved", "district01.crisis.2"); return LastResult(); }
    private DistrictCommandResult FailCrisis2() { Crisis2 = DistrictCrisisStatus.Failed; Route = DistrictRouteState.Blocked; Transition(DistrictPhase.Aftermath, "Crisis2Failed", "district01.crisis.2"); return LastResult(); }
    private void SetZones(DistrictCivilianState state) { foreach (string id in _zones.Keys.ToArray()) _zones[id] = _zones[id] with { CivilianState = state }; }
    private void Transition(DistrictPhase phase, string eventType, string entityId) { Phase = phase; _phaseEnteredTick = Tick; Publish(eventType, entityId, phase.ToString()); }
    private void AutoStartCrisis2() { _ = StartCrisis2(); }
    private void AutoFailCrisis1() { _ = FailCrisis1(); }
    private void AutoFailCrisis2() { _ = FailCrisis2(); }
    private void Publish(string type, string entityId, string value) { _events.Add(new(DistrictAuthorityProtocol.Version, SessionId, ++ServerSequence, Tick, type, entityId, value)); }
    private DistrictCommandResult LastResult() => new(true, false, "accepted", "Command accepted.", _events.Count > 0 ? _events[^1] : null, Snapshot());
    private DistrictCommandResult Accept(string type, string entityId, string value) { Publish(type, entityId, value); return LastResult(); }
    private DistrictCommandResult Reject(string code, string message) => new(false, false, code, message, null, Snapshot());
}

public sealed record DistrictReplayDocument(string ProtocolVersion, ulong Seed, IReadOnlyList<DistrictCommand> Commands, ulong? FinalTick = null);

public static class DistrictReplay
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void Save(string path, DistrictAuthoritySession session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(session);
        File.WriteAllText(path, JsonSerializer.Serialize(session.CreateReplayDocument(), JsonOptions));
    }

    public static DistrictReplayDocument Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return JsonSerializer.Deserialize<DistrictReplayDocument>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("Replay is empty.");
    }

    public static DistrictAuthoritySession Run(DistrictReplayDocument replay)
    {
        if (!string.Equals(replay.ProtocolVersion, DistrictAuthorityProtocol.Version, StringComparison.Ordinal))
            throw new InvalidDataException("Incompatible replay protocol version.");
        var session = new DistrictAuthoritySession(replay.Seed, "replay");
        // A replay is an accepted input stream; preserve its recorded order.
        // TargetTick only advances the deterministic clock before that command.
        foreach (DistrictCommand command in replay.Commands)
        {
            if (command.TargetTick is ulong tick && tick > session.Tick) session.Advance(tick - session.Tick);
            if (command.TargetTick is ulong pastTick && pastTick < session.Tick)
                throw new InvalidDataException("Replay command target tick is earlier than the current tick.");
            DistrictCommandResult result = session.Submit(command with { SessionId = session.SessionId });
            if (!result.Accepted && !result.Duplicate) throw new InvalidDataException($"Replay command rejected: {result.Code}.");
        }
        if (replay.FinalTick is ulong finalTick && finalTick > session.Tick) session.Advance(finalTick - session.Tick);
        return session;
    }
}
