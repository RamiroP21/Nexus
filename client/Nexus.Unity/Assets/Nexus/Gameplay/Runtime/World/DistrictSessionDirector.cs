using System;
using Nexus.Gameplay.Combat;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    // Local presentation orchestrator. DistrictSessionState is the Host-backed
    // read model; this component observes authored actors and reconciles presentation.
    public sealed class DistrictSessionDirector : MonoBehaviour
    {
        [SerializeField] private UrbanBlockSituation crisis1;
        [SerializeField] private DistrictCargoEmergency crisis2;
        [SerializeField] private PlayerVitality player;
        [SerializeField] private CivilianPresence[] resetCivilians;
        [SerializeField] private Transform marketZone, residentialZone, serviceZone;
        [SerializeField, Min(1)] private float crisis2ActivationRadius = 12;
        [SerializeField] private DistrictAuthorityBridge authority;
        private DistrictSessionState state;
        private readonly int[] injuredByZone = new int[3];
        private readonly int[] shelteredByZone = new int[3];
        private readonly bool[] zoneIntentsSent = new bool[3];
        private bool crisis1StartSent, crisis1OutcomeSent, crisis2StartSent, crisis2OutcomeSent;
        public DistrictSessionState State => state;
        public DistrictCargoEmergency Crisis2 => crisis2;
        public bool AuthorityReady => authority && authority.IsAuthorityReady;
        private void Awake()
        {
            if (!crisis1 || !crisis2 || !player || !marketZone || !residentialZone || !serviceZone)
            { Debug.LogError("DistrictSessionDirector requires explicit crisis, player and zone references.", this); enabled = false; return; }
            if (!authority)
            { Debug.LogError("DistrictSessionDirector requires a DistrictAuthorityBridge; local semantic fallback is disabled.", this); enabled = false; return; }
            state = new DistrictSessionState();
            state.Reset();
        }
        private void Update()
        {
            if (!isActiveAndEnabled) return;
            UpdateAuthoritative();
        }

        private void UpdateAuthoritative()
        {
            if (!authority.IsAuthorityReady) return;
            state.ApplyAuthoritative(authority.Replica);
            ObserveAuthorityFacts();
            ReconcileCrisis2();
            bool nearby = crisis2.State == DistrictCrisisState.Active && Vector3.Distance(player.transform.position, crisis2.transform.position) <= crisis2ActivationRadius;
            crisis2.Tick(Time.deltaTime, nearby);
            if (!crisis2OutcomeSent && crisis2.State == DistrictCrisisState.Resolved)
                crisis2OutcomeSent = authority.TryResolveCrisis2();
            else if (!crisis2OutcomeSent && crisis2.State == DistrictCrisisState.Failed)
                crisis2OutcomeSent = authority.TryFailCrisis2();
            if (!string.Equals(authority.Replica.Crisis2, "Active", StringComparison.Ordinal)) crisis2OutcomeSent = false;
            state.ApplyAuthoritative(authority.Replica);
        }

        private void ObserveAuthorityFacts()
        {
            UrbanBlockPhase phase = crisis1.Phase;
            if ((phase == UrbanBlockPhase.Warning || phase == UrbanBlockPhase.Incident || phase == UrbanBlockPhase.Aftermath)
                && string.Equals(authority.Replica.Crisis1, "Pending", StringComparison.Ordinal) && !crisis1StartSent)
                crisis1StartSent = authority.TryStartCrisis1();
            if (!string.Equals(authority.Replica.Crisis1, "Pending", StringComparison.Ordinal)) crisis1StartSent = false;
            if (phase == UrbanBlockPhase.Aftermath
                && (string.Equals(authority.Replica.Phase, "Warning", StringComparison.Ordinal)
                    || string.Equals(authority.Replica.Phase, "Incident", StringComparison.Ordinal))
                && string.Equals(authority.Replica.Crisis1, "Active", StringComparison.Ordinal))
            {
                var pressure = crisis1.CompoundPressure;
                bool failed = pressure && (pressure.Civilian == PressureOutcome.Failed || pressure.Infrastructure == PressureOutcome.Failed);
                foreach (var civilian in crisis1.Civilians) if (civilian && civilian.Harmed) failed = true;
                if (!crisis1OutcomeSent)
                {
                    crisis1OutcomeSent = failed ? authority.TryFailCrisis1() : authority.TryResolveCrisis1();
                }
            }
            if (!string.Equals(authority.Replica.Crisis1, "Active", StringComparison.Ordinal)) crisis1OutcomeSent = false;
            float[] distances = { DistanceOnPlane(player.transform.position, marketZone.position), DistanceOnPlane(player.transform.position, residentialZone.position), DistanceOnPlane(player.transform.position, serviceZone.position) };
            for (int i = 0; i < distances.Length; i++)
            {
                if (zoneIntentsSent[i] || distances[i] > crisis2ActivationRadius * 1.5f) continue;
                zoneIntentsSent[i] = authority.TryObserveZone(DistrictSessionState.StableZoneId((DistrictZone)i));
            }
            if (string.Equals(authority.Replica.Phase, "Aftermath", StringComparison.Ordinal)
                && string.Equals(authority.Replica.Crisis2, "Pending", StringComparison.Ordinal)
                && Vector3.Distance(player.transform.position, crisis2.transform.position) <= crisis2ActivationRadius && !crisis2StartSent)
                crisis2StartSent = authority.TryStartCrisis2();
            if (!string.Equals(authority.Replica.Crisis2, "Pending", StringComparison.Ordinal)) crisis2StartSent = false;
        }

        private void ReconcileCrisis2()
        {
            if (string.Equals(authority.Replica.Crisis2, "Active", StringComparison.Ordinal) && crisis2.State == DistrictCrisisState.Dormant)
                crisis2.Begin(authority.Replica.Crisis2InheritedDamage);
        }
        private void ObserveCrisis1()
        {
            int injured = 0, sheltered = 0;
            foreach (var civilian in crisis1.Civilians)
            {
                if (!civilian) continue;
                if (civilian.Harmed) injured++;
                if (civilian.State == CivilianState.Sheltered) sheltered++;
            }
            var pressure = crisis1.CompoundPressure;
            state.ObserveMarket(crisis1.Phase,
                pressure ? pressure.Civilian : PressureOutcome.Pending,
                pressure ? pressure.Infrastructure : PressureOutcome.Pending,
                injured, sheltered);
        }
        private void MarkVisitedZone()
        {
            float market = DistanceOnPlane(player.transform.position, marketZone.position);
            float residential = DistanceOnPlane(player.transform.position, residentialZone.position);
            float service = DistanceOnPlane(player.transform.position, serviceZone.position);
            float best = Mathf.Min(market, residential, service);
            if (best > crisis2ActivationRadius * 1.5f) return;
            if (best == market) state.Visit(DistrictZone.Market);
            else if (best == residential) state.Visit(DistrictZone.Residential);
            else state.Visit(DistrictZone.Service);
        }
        private void ObserveDistrictPopulation()
        {
            if (resetCivilians == null) return;
            for (int i = 0; i < 3; i++) injuredByZone[i] = shelteredByZone[i] = 0;
            foreach (var civilian in resetCivilians)
            {
                if (!civilian) continue;
                int zone = (int)NearestZone(civilian.transform.position);
                if (civilian.Harmed) injuredByZone[zone]++;
                if (civilian.State == CivilianState.Sheltered) shelteredByZone[zone]++;
            }
            for (int i = 0; i < 3; i++) state.ObservePopulation((DistrictZone)i, injuredByZone[i], shelteredByZone[i]);
        }
        private DistrictZone NearestZone(Vector3 position)
        {
            float market = DistanceOnPlane(position, marketZone.position);
            float residential = DistanceOnPlane(position, residentialZone.position);
            float service = DistanceOnPlane(position, serviceZone.position);
            return market <= residential && market <= service ? DistrictZone.Market : residential <= service ? DistrictZone.Residential : DistrictZone.Service;
        }
        private static float DistanceOnPlane(Vector3 a, Vector3 b) => Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude;
        public void ResetDistrictState()
        {
            crisis2.ResetEmergency();
            if (resetCivilians != null)
                foreach (var civilian in resetCivilians) if (civilian) civilian.ResetCivilian();
            state.Reset();
            for (int i = 0; i < zoneIntentsSent.Length; i++) zoneIntentsSent[i] = false;
            crisis1StartSent = crisis1OutcomeSent = crisis2StartSent = crisis2OutcomeSent = false;
            if (authority) authority.ResetAuthoritativeSession();
        }
        private void OnGUI()
        {
            if (!crisis1 || !crisis1.DebugVisible || state is null) return;
            var market = state.Zone(DistrictZone.Market);
            var residential = state.Zone(DistrictZone.Residential);
            var service = state.Zone(DistrictZone.Service);
            GUI.Label(new Rect(20, Screen.height - 205, 850, 90),
                $"DISTRICT SESSION | C1 {state.Crisis1} | C2 {state.Crisis2} | aftermath {state.AftermathSeconds:0.0}s\n" +
                $"ZONES M:{market.Visited}/{market.RouteBlocked} R:{residential.Visited} S:{service.Visited}/{service.RouteBlocked}\n" +
                $"MEMORY resident:{state.ResidentOutcome} infrastructure:{state.InfrastructureOutcome} | C2 emergency:{crisis2.State} elapsed:{crisis2.Elapsed:0.0}/{crisis2.Deadline:0.0} inherited:{crisis2.InheritedDamage}");
            if (authority)
                GUI.Label(new Rect(20, Screen.height - 340, 1100, 120),
                    $"AUTHORITY {authority.ConnectionState} | campaign:{authority.Replica.CampaignId ?? "-"} session:{authority.Replica.SessionId ?? "-"}\n" +
                    $"format:{authority.Replica.SaveFormatVersion} revision:{authority.Replica.SaveRevision} persistence:{authority.Replica.SaveStatus} | seed:{authority.Replica.Seed} tick:{authority.Replica.Tick} seq:{authority.Replica.LastServerSequence}\n" +
                    $"phase:{authority.Replica.Phase} c1:{authority.Replica.Crisis1} c2:{authority.Replica.Crisis2} route:{authority.Replica.Route} infra:{authority.Replica.Infrastructure}\n" +
                    $"hash:{authority.Replica.StateHash ?? "-"} | memories:{authority.Replica.Memories.Count} | rejected:{authority.RejectedCommandCount} | {authority.LastDiagnostic}");
        }
    }
}
