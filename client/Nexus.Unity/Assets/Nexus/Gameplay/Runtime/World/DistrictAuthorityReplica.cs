using System;
using System.Collections.Generic;

namespace Nexus.Gameplay.World
{
    // Read-only semantic state received from the Simulation Host. The client
    // can replace it with a snapshot, but cannot advance or resolve it locally.
    public sealed class DistrictAuthorityReplica
    {
        public const string ProtocolVersion = "Nexus.DistrictAuthority.v1";

        private readonly Dictionary<string, DistrictAuthorityWireZone> zones =
            new Dictionary<string, DistrictAuthorityWireZone>(StringComparer.Ordinal);
        private ulong lastEventSequence;

        public bool HasSnapshot { get; private set; }
        public string SessionId { get; private set; }
        public ulong Seed { get; private set; }
        public ulong Tick { get; private set; }
        public ulong LastServerSequence { get; private set; }
        public string Phase { get; private set; } = "Calm";
        public string Crisis1 { get; private set; } = "Pending";
        public string Crisis2 { get; private set; } = "Pending";
        public string Infrastructure { get; private set; } = "Stable";
        public string Route { get; private set; } = "Open";
        public bool Crisis2InheritedDamage { get; private set; }
        public ulong Crisis2DeadlineTick { get; private set; }
        public string StateHash { get; private set; } = string.Empty;
        public string LastEventType { get; private set; } = string.Empty;
        public string LastError { get; private set; } = string.Empty;

        public IReadOnlyCollection<DistrictAuthorityWireZone> Zones => zones.Values;

        public bool ApplySnapshot(DistrictAuthorityWireSnapshot snapshot, bool force = false)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.sessionId)) return false;
            if (!force && HasSnapshot && string.Equals(SessionId, snapshot.sessionId, StringComparison.Ordinal)
                && snapshot.serverSequence < LastServerSequence) return false;

            bool newSession = !HasSnapshot || !string.Equals(SessionId, snapshot.sessionId, StringComparison.Ordinal) || force;
            SessionId = snapshot.sessionId;
            Seed = snapshot.seed;
            Tick = snapshot.tick;
            LastServerSequence = snapshot.serverSequence;
            Phase = ValueOr(snapshot.phase, "Calm");
            Crisis1 = ValueOr(snapshot.crisis1, "Pending");
            Crisis2 = ValueOr(snapshot.crisis2, "Pending");
            Infrastructure = ValueOr(snapshot.infrastructure, "Stable");
            Route = ValueOr(snapshot.route, "Open");
            Crisis2InheritedDamage = snapshot.crisis2InheritedDamage;
            Crisis2DeadlineTick = snapshot.crisis2DeadlineTick;
            StateHash = ValueOr(snapshot.stateHash, string.Empty);
            zones.Clear();
            if (snapshot.zones != null)
                foreach (DistrictAuthorityWireZone zone in snapshot.zones)
                    if (zone != null && !string.IsNullOrEmpty(zone.id)) zones[zone.id] = zone;
            HasSnapshot = true;
            if (newSession) lastEventSequence = 0;
            return true;
        }

        public bool ApplyEvent(DistrictAuthorityWireEvent authorityEvent)
        {
            if (authorityEvent == null || !string.Equals(authorityEvent.sessionId, SessionId, StringComparison.Ordinal)) return false;
            if (authorityEvent.serverSequence <= lastEventSequence) return false;
            lastEventSequence = authorityEvent.serverSequence;
            LastServerSequence = Math.Max(LastServerSequence, authorityEvent.serverSequence);
            Tick = Math.Max(Tick, authorityEvent.simulationTick);
            LastEventType = ValueOr(authorityEvent.type, string.Empty);
            return true;
        }

        public bool TryGetZone(string id, out DistrictAuthorityWireZone zone) => zones.TryGetValue(id, out zone);

        public void RecordError(string message) => LastError = ValueOr(message, "Unknown authority error.");

        private static string ValueOr(string value, string fallback) => string.IsNullOrEmpty(value) ? fallback : value;
    }
}
