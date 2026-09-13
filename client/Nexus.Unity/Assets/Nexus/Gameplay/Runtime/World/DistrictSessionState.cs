namespace Nexus.Gameplay.World
{
    public enum DistrictZone { Market, Residential, Service }
    public enum DistrictCrisisState { Dormant, Active, Resolved, Failed }

    // Session authority contains no Unity objects. Scene presentation reads this state.
    public sealed class DistrictSessionState
    {
        public sealed class ZoneState
        {
            public bool Visited { get; internal set; }
            public int Injured { get; internal set; }
            public int Sheltered { get; internal set; }
            public bool RouteBlocked { get; internal set; }
        }
        private readonly ZoneState[] zones = { new ZoneState(), new ZoneState(), new ZoneState() };
        public DistrictCrisisState Crisis1 { get; private set; }
        public DistrictCrisisState Crisis2 { get; private set; }
        public PressureOutcome ResidentOutcome { get; private set; }
        public PressureOutcome InfrastructureOutcome { get; private set; }
        public bool ServiceDegraded => InfrastructureOutcome == PressureOutcome.Failed;
        public float AftermathSeconds { get; private set; }
        public ZoneState Zone(DistrictZone zone) => zones[(int)zone];
        public void Visit(DistrictZone zone) => Zone(zone).Visited = true;
        public void ObserveMarket(UrbanBlockPhase phase, PressureOutcome resident, PressureOutcome infrastructure, int injured, int sheltered)
        {
            if (phase == UrbanBlockPhase.Warning || phase == UrbanBlockPhase.Incident) Crisis1 = DistrictCrisisState.Active;
            if (phase == UrbanBlockPhase.Aftermath)
                Crisis1 = resident == PressureOutcome.Failed || infrastructure == PressureOutcome.Failed || injured > 0
                    ? DistrictCrisisState.Failed : DistrictCrisisState.Resolved;
            ResidentOutcome = resident; InfrastructureOutcome = infrastructure;
            ObservePopulation(DistrictZone.Market, injured, sheltered);
        }
        public void ObservePopulation(DistrictZone zone, int injured, int sheltered)
        {
            Zone(zone).Injured = System.Math.Max(Zone(zone).Injured, injured);
            Zone(zone).Sheltered = System.Math.Max(Zone(zone).Sheltered, sheltered);
        }
        public void Tick(float seconds)
        {
            if (seconds > 0 && float.IsFinite(seconds) && (Crisis1 == DistrictCrisisState.Resolved || Crisis1 == DistrictCrisisState.Failed))
                AftermathSeconds = System.Math.Min(3600, AftermathSeconds + seconds);
        }
        public bool CanStartCrisis2(float interval) => Crisis2 == DistrictCrisisState.Dormant &&
            (Crisis1 == DistrictCrisisState.Resolved || Crisis1 == DistrictCrisisState.Failed) && AftermathSeconds >= interval;
        public void ObserveCrisis2(DistrictCrisisState state, bool routeBlocked)
        { Crisis2 = state; Zone(DistrictZone.Service).RouteBlocked = routeBlocked; }

        // Applies a server snapshot to the client read model. This is a
        // presentation reconciliation hook; it never advances a clock or
        // derives a new outcome locally.
        public void ApplyAuthoritative(DistrictAuthorityReplica authority)
        {
            if (authority == null || !authority.HasSnapshot) return;
            Crisis1 = ParseCrisis(authority.Crisis1);
            Crisis2 = ParseCrisis(authority.Crisis2);
            InfrastructureOutcome = string.Equals(authority.Infrastructure, "Damaged", System.StringComparison.Ordinal)
                ? PressureOutcome.Failed : string.Equals(authority.Infrastructure, "Stable", System.StringComparison.Ordinal)
                    ? PressureOutcome.Pending : PressureOutcome.Resolved;
            ResidentOutcome = ParseCivilianOutcome(authority);
            foreach (DistrictZone zone in System.Enum.GetValues(typeof(DistrictZone)))
            {
                string id = StableZoneId(zone);
                if (authority.TryGetZone(id, out DistrictAuthorityWireZone snapshot))
                {
                    Zone(zone).Visited = snapshot.visited;
                    Zone(zone).RouteBlocked = string.Equals(authority.Route, "Blocked", System.StringComparison.Ordinal)
                        && zone == DistrictZone.Service;
                    Zone(zone).Injured = string.Equals(snapshot.civilianState, "Incapacitated", System.StringComparison.Ordinal) ? 1 : 0;
                    Zone(zone).Sheltered = string.Equals(snapshot.civilianState, "Sheltered", System.StringComparison.Ordinal) ? 1 : 0;
                }
            }
        }

        public static string StableZoneId(DistrictZone zone) => zone == DistrictZone.Market
            ? "district01.zone.a" : zone == DistrictZone.Residential ? "district01.zone.b" : "district01.zone.c";

        private static DistrictCrisisState ParseCrisis(string value) =>
            string.Equals(value, "Active", System.StringComparison.Ordinal) ? DistrictCrisisState.Active :
            string.Equals(value, "Resolved", System.StringComparison.Ordinal) ? DistrictCrisisState.Resolved :
            string.Equals(value, "Failed", System.StringComparison.Ordinal) ? DistrictCrisisState.Failed : DistrictCrisisState.Dormant;

        private static PressureOutcome ParseCivilianOutcome(DistrictAuthorityReplica authority)
        {
            foreach (DistrictAuthorityWireZone zone in authority.Zones)
            {
                if (string.Equals(zone.civilianState, "Incapacitated", System.StringComparison.Ordinal)) return PressureOutcome.Failed;
                if (string.Equals(zone.civilianState, "Sheltered", System.StringComparison.Ordinal)) return PressureOutcome.Resolved;
            }
            return PressureOutcome.Pending;
        }
        public void Reset()
        {
            Crisis1 = Crisis2 = DistrictCrisisState.Dormant;
            ResidentOutcome = InfrastructureOutcome = PressureOutcome.Pending; AftermathSeconds = 0;
            foreach (var zone in zones) { zone.Visited = zone.RouteBlocked = false; zone.Injured = zone.Sheltered = 0; }
        }
    }
}
