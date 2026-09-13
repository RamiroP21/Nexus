using Nexus.Gameplay.Combat;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    // Local presentation orchestrator. DistrictSessionState owns session memory;
    // this component only observes authored actors and reconciles their presentation.
    public sealed class DistrictSessionDirector : MonoBehaviour
    {
        [SerializeField] private UrbanBlockSituation crisis1;
        [SerializeField] private DistrictCargoEmergency crisis2;
        [SerializeField] private PlayerVitality player;
        [SerializeField] private CivilianPresence[] resetCivilians;
        [SerializeField] private Transform marketZone, residentialZone, serviceZone;
        [SerializeField, Min(0)] private float crisis2AftermathSeconds = 6;
        [SerializeField, Min(1)] private float crisis2ActivationRadius = 12;
        private DistrictSessionState state;
        private readonly int[] injuredByZone = new int[3];
        private readonly int[] shelteredByZone = new int[3];
        public DistrictSessionState State => state;
        public DistrictCargoEmergency Crisis2 => crisis2;
        private void Awake()
        {
            if (!crisis1 || !crisis2 || !player || !marketZone || !residentialZone || !serviceZone)
            { Debug.LogError("DistrictSessionDirector requires explicit crisis, player and zone references.", this); enabled = false; return; }
            state = new DistrictSessionState();
            state.Reset();
        }
        private void Update()
        {
            if (!isActiveAndEnabled) return;
            ObserveCrisis1();
            MarkVisitedZone();
            ObserveDistrictPopulation();
            state.Tick(Time.deltaTime);
            if (state.CanStartCrisis2(crisis2AftermathSeconds) && Vector3.Distance(player.transform.position, crisis2.transform.position) <= crisis2ActivationRadius)
            {
                crisis2.Begin(state.ServiceDegraded);
                state.ObserveCrisis2(DistrictCrisisState.Active, crisis2.RouteBlocked);
            }
            bool nearby = crisis2.State == DistrictCrisisState.Active && Vector3.Distance(player.transform.position, crisis2.transform.position) <= crisis2ActivationRadius;
            crisis2.Tick(Time.deltaTime, nearby);
            if (crisis2.State != DistrictCrisisState.Dormant) state.ObserveCrisis2(crisis2.State, crisis2.RouteBlocked);
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
        }
    }
}
