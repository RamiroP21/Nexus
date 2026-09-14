using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    public enum DistrictAuthorityConnectionState { Disconnected, Connecting, Handshaking, Connected, Failed }

    // Unity lifecycle adapter for the loopback Simulation Host. It owns no
    // semantic state: all transitions come from snapshots/events.
    public sealed class DistrictAuthorityBridge : MonoBehaviour
    {
        [SerializeField] private bool connectOnStart = true;
        [SerializeField] private string host = "127.0.0.1";
        [SerializeField, Min(1)] private int port = 43101;
        [SerializeField, Min(100)] private int connectTimeoutMilliseconds = 3000;
        [SerializeField] private ulong seed = 123456789;
        [SerializeField] private string clientId = "unity-district01";
        [SerializeField, Min(1)] private int maxMessagesPerFrame = 32;
        private readonly ConcurrentQueue<string> faults = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<DistrictAuthorityConnectionState> pendingStates = new ConcurrentQueue<DistrictAuthorityConnectionState>();
        private CancellationTokenSource lifetime;
        private IDistrictAuthorityTransport transport;
        private bool helloSent;
        private bool helloPending;
        private bool sessionRequestSent;
        private bool resetPending;
        private ulong clientSequence;
        private int reconnectAttempts;
        private float nextReconnectTime;
        private int snapshotPollFrames;
        private DistrictAuthorityConnectionState connectionState;
        private const int MaxReconnectAttempts = 20;
        public DistrictAuthorityReplica Replica { get; private set; }
        public DistrictAuthorityConnectionState ConnectionState => connectionState;
        public bool IsAuthorityReady => connectionState == DistrictAuthorityConnectionState.Connected && Replica.HasSnapshot;
        public string LastDiagnostic { get; private set; } = string.Empty;
        public int PendingInboundCount { get; private set; }
        public int RejectedCommandCount { get; private set; }
        public event Action<DistrictAuthorityConnectionState> ConnectionChanged;
        public event Action<DistrictAuthorityWireEvent> AuthoritativeEvent;
        public event Action<DistrictAuthorityWireSnapshot> AuthoritativeSnapshot;

        public void ConfigureForTests(IDistrictAuthorityTransport fakeTransport, string testClientId = "unity-test", ulong testSeed = 1)
        {
            if (isActiveAndEnabled) Disconnect();
            transport = fakeTransport ?? throw new ArgumentNullException(nameof(fakeTransport));
            transport.Faulted += OnTransportFault;
            clientId = testClientId; seed = testSeed; connectOnStart = false;
            Replica = new DistrictAuthorityReplica();
        }

        private void Awake()
        {
            Replica = new DistrictAuthorityReplica();
            SetConnectionState(DistrictAuthorityConnectionState.Disconnected);
        }

        private void Start()
        {
            if (connectOnStart) Connect();
        }

        public void Connect()
        {
            if (connectionState == DistrictAuthorityConnectionState.Connecting || IsAuthorityReady) return;
            if (transport == null)
            {
                transport = new DistrictAuthorityTcpTransport();
                transport.Faulted += OnTransportFault;
            }
            lifetime = new CancellationTokenSource();
            reconnectAttempts++;
            SetConnectionState(DistrictAuthorityConnectionState.Connecting);
            _ = ConnectAsync(lifetime.Token);
        }

        private async Task ConnectAsync(CancellationToken token)
        {
            try
            {
                await transport.ConnectAsync(host, port, connectTimeoutMilliseconds, token).ConfigureAwait(false);
                QueueConnectionState(DistrictAuthorityConnectionState.Handshaking);
                helloPending = true;
            }
            catch (Exception ex)
            {
                faults.Enqueue("Connect failed: " + ex.Message);
                QueueConnectionState(DistrictAuthorityConnectionState.Failed);
            }
        }

        private void Update()
        {
            while (pendingStates.TryDequeue(out DistrictAuthorityConnectionState pending))
                SetConnectionState(pending);
            if (transport != null)
            {
                int drained = transport.Drain(HandleMessage, Math.Max(1, maxMessagesPerFrame));
                PendingInboundCount = Math.Max(0, PendingInboundCount - drained);
            }
            if (IsAuthorityReady && ++snapshotPollFrames >= 6)
            {
                snapshotPollFrames = 0;
                RequestSnapshot();
            }
            if (helloPending)
            {
                helloPending = false;
                Send(new DistrictAuthorityWireMessage { protocolVersion = DistrictAuthorityReplica.ProtocolVersion, messageType = "ClientHello" });
            }
            while (faults.TryDequeue(out string fault))
            {
                LastDiagnostic = fault;
                Debug.LogWarning(fault, this);
                SetConnectionState(DistrictAuthorityConnectionState.Failed);
            }
            if (connectionState == DistrictAuthorityConnectionState.Failed && reconnectAttempts < MaxReconnectAttempts
                && Time.unscaledTime >= nextReconnectTime)
            {
                nextReconnectTime = Time.unscaledTime + 0.25f;
                Connect();
            }
        }

        private void OnTransportFault(string message) => faults.Enqueue("Authority transport fault: " + message);

        private void QueueConnectionState(DistrictAuthorityConnectionState value) => pendingStates.Enqueue(value);

        public bool TryObserveZone(string stableZoneId) => TryCommand("ObserveZone", stableZoneId);
        public bool TryStartCrisis1() => TryCommand("StartCrisis1", "district01.crisis.1");
        public bool TryResolveCrisis1() => TryCommand("ResolveCrisis1", "district01.crisis.1");
        public bool TryFailCrisis1() => TryCommand("FailCrisis1", "district01.crisis.1");
        public bool TryStartCrisis2() => TryCommand("StartCrisis2", "district01.crisis.2");
        public bool TryResolveCrisis2() => TryCommand("ResolveCrisis2", "district01.crisis.2");
        public bool TryFailCrisis2() => TryCommand("FailCrisis2", "district01.crisis.2");

        public bool ContinueCampaign()
        {
            if (transport == null || !transport.IsConnected) return false;
            return Send(new DistrictAuthorityWireMessage
            {
                protocolVersion = DistrictAuthorityReplica.ProtocolVersion,
                messageType = "ContinueCampaign", seed = seed, clientId = clientId
            });
        }

        public bool NewCampaign()
        {
            if (transport == null || !transport.IsConnected) return false;
            resetPending = true;
            return Send(new DistrictAuthorityWireMessage
            {
                protocolVersion = DistrictAuthorityReplica.ProtocolVersion,
                messageType = "NewCampaign", seed = seed, clientId = clientId
            });
        }

        public bool SaveCampaign() => TryLifecycleCommand("SaveCampaign");

        public bool ShutdownHost() => TryLifecycleCommand("ShutdownHost");

        public bool ResetAuthoritativeSession()
        {
            resetPending = true;
            return TryCommand("ResetSession", "district01");
        }

        public void RequestSnapshot()
        {
            if (!IsAuthorityReady) return;
            Send(new DistrictAuthorityWireMessage
            {
                protocolVersion = DistrictAuthorityReplica.ProtocolVersion,
                messageType = "SnapshotRequest", sessionId = Replica.SessionId, clientId = clientId
            });
        }

        private bool TryCommand(string commandType, string entityId)
        {
            if (!IsAuthorityReady)
            {
                LastDiagnostic = "Authority unavailable; semantic command was not committed locally.";
                return false;
            }
            DistrictAuthorityWireMessage command = new DistrictAuthorityWireMessage
            {
                protocolVersion = DistrictAuthorityReplica.ProtocolVersion,
                messageType = "ClientCommand", sessionId = Replica.SessionId, clientId = clientId,
                clientSequence = ++clientSequence,
                // Session-scoped IDs avoid colliding with durable receipts after a Host restart.
                commandId = clientId + ":" + Replica.SessionId + ":" + clientSequence.ToString(),
                commandType = commandType, entityId = entityId
            };
            return Send(command);
        }

        private bool TryLifecycleCommand(string messageType)
        {
            if (!IsAuthorityReady)
            {
                LastDiagnostic = "Authority unavailable; lifecycle request was not sent.";
                return false;
            }
            return Send(new DistrictAuthorityWireMessage
            {
                protocolVersion = DistrictAuthorityReplica.ProtocolVersion,
                messageType = messageType, sessionId = Replica.SessionId, clientId = clientId
            });
        }

        private bool Send(DistrictAuthorityWireMessage message)
        {
            if (transport == null || !transport.IsConnected || !transport.TrySend(message))
            {
                LastDiagnostic = "Authority transport rejected outbound message.";
                return false;
            }
            PendingInboundCount++;
            return true;
        }

        private void HandleMessage(DistrictAuthorityWireMessage message)
        {
            if (message == null) return;
            if (!string.Equals(message.protocolVersion, DistrictAuthorityReplica.ProtocolVersion, StringComparison.Ordinal)
                && !string.Equals(message.messageType, "ServerHello", StringComparison.Ordinal))
            { LastDiagnostic = "Authority protocol version mismatch."; SetConnectionState(DistrictAuthorityConnectionState.Failed); return; }

            switch (message.messageType)
            {
                case "ServerHello":
                    if (!helloSent) { helloSent = true; sessionRequestSent = ContinueCampaign(); }
                    break;
                case "SessionStarted":
                    ApplySnapshot(message.snapshot, true);
                    sessionRequestSent = true;
                    reconnectAttempts = 0;
                    SetConnectionState(DistrictAuthorityConnectionState.Connected);
                    break;
                case "Snapshot":
                case "ServerEvent":
                    bool fresh = message.@event != null && Replica.ApplyEvent(message.@event);
                    ApplySnapshot(message.snapshot, resetPending);
                    if (fresh) AuthoritativeEvent?.Invoke(message.@event);
                    resetPending = false;
                    SetConnectionState(DistrictAuthorityConnectionState.Connected);
                    break;
                case "CommandRejected":
                case "ProtocolError":
                    RejectedCommandCount++;
                    LastDiagnostic = (message.errorCode ?? "rejected") + ": " + (message.errorMessage ?? "Authority rejected message.");
                    if (message.snapshot != null) ApplySnapshot(message.snapshot, resetPending);
                    resetPending = false;
                    break;
                case "HostShuttingDown":
                    ApplySnapshot(message.snapshot, true);
                    LastDiagnostic = "Authority host flushed campaign and is shutting down.";
                    SetConnectionState(DistrictAuthorityConnectionState.Disconnected);
                    break;
            }
        }

        private void ApplySnapshot(DistrictAuthorityWireSnapshot snapshot, bool force)
        {
            if (snapshot == null || !Replica.ApplySnapshot(snapshot, force)) return;
            AuthoritativeSnapshot?.Invoke(snapshot);
        }

        private void SetConnectionState(DistrictAuthorityConnectionState value)
        {
            if (connectionState == value) return;
            connectionState = value;
            ConnectionChanged?.Invoke(value);
        }

        public void Disconnect()
        {
            lifetime?.Cancel(); lifetime?.Dispose(); lifetime = null;
            if (transport != null) transport.Faulted -= OnTransportFault;
            transport?.Disconnect();
            helloSent = sessionRequestSent = resetPending = helloPending = false;
            reconnectAttempts = 0;
            SetConnectionState(DistrictAuthorityConnectionState.Disconnected);
        }

        private void OnDestroy()
        {
            Disconnect();
            transport?.Dispose();
        }

    }
}
