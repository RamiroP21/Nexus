using Nexus.Gameplay.Combat;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    public enum PressureOutcome { Pending, Active, Resolved, Failed }

    // B and C keep independent clocks and outcomes. A remains owned by HostileCombatant.
    public sealed class CompoundCrisisPressure : MonoBehaviour
    {
        [SerializeField] private CivilianPresence resident;
        [SerializeField] private Rigidbody exitObstruction, infrastructureLoad;
        [SerializeField] private Transform loadAnchor, emergencySignal, infrastructureSignal;
        [SerializeField] private GameObject damagedInfrastructure;
        [SerializeField, Min(.1f)] private float civilianSeconds = 24, infrastructureSeconds = 38;
        [SerializeField, Min(.1f)] private float clearance = 2.5f;
        private float civilianElapsed, infrastructureElapsed;
        public PressureOutcome Civilian { get; private set; }
        public PressureOutcome Infrastructure { get; private set; }
        public float CivilianProgress => Mathf.Clamp01(civilianElapsed / civilianSeconds);
        public float InfrastructureProgress => Mathf.Clamp01(infrastructureElapsed / infrastructureSeconds);
        public bool Complete => Terminal(Civilian) && Terminal(Infrastructure);
        public bool InfrastructureDamaged => Infrastructure == PressureOutcome.Failed;
        public bool HasLoss => Civilian == PressureOutcome.Failed || InfrastructureDamaged;
        public Rigidbody ExitObstruction => exitObstruction;
        public Rigidbody InfrastructureLoad => infrastructureLoad;
        public CivilianPresence Resident => resident;
        private static bool Terminal(PressureOutcome outcome) => outcome == PressureOutcome.Resolved || outcome == PressureOutcome.Failed;
        private void Awake()
        {
            if (!resident || !exitObstruction || !infrastructureLoad || !loadAnchor || !emergencySignal || !infrastructureSignal || !damagedInfrastructure
                || !float.IsFinite(civilianSeconds) || civilianSeconds <= 0 || !float.IsFinite(infrastructureSeconds) || infrastructureSeconds <= 0 || !float.IsFinite(clearance) || clearance <= 0)
            { Debug.LogError("Compound crisis requires explicit pressure actors, anchors and presentation.", this); enabled = false; return; }
            ResetPressure();
        }
        public void Warn() { emergencySignal.gameObject.SetActive(true); infrastructureSignal.gameObject.SetActive(true); Present(); }
        public void Begin()
        {
            if (Civilian != PressureOutcome.Pending || Infrastructure != PressureOutcome.Pending) return;
            Civilian = Infrastructure = PressureOutcome.Active; Present();
        }
        public void Tick(float dt)
        {
            if (!isActiveAndEnabled || dt <= 0 || !float.IsFinite(dt)) return;
            if (Civilian == PressureOutcome.Active)
            {
                if (resident.Harmed) Civilian = PressureOutcome.Failed;
                else if (resident.State == CivilianState.Sheltered) Civilian = PressureOutcome.Resolved;
                else
                {
                    civilianElapsed += dt;
                    if (civilianElapsed >= civilianSeconds)
                    { resident.GetComponent<DamageReceiver>().Receive(new Damage(12)); Civilian = PressureOutcome.Failed; }
                }
            }
            if (Infrastructure == PressureOutcome.Active)
            {
                // Clearing the physical load unloads the failing service unit. No interaction verb is added.
                if (Vector3.ProjectOnPlane(infrastructureLoad.position - loadAnchor.position, Vector3.up).magnitude >= clearance)
                    Infrastructure = PressureOutcome.Resolved;
                else
                {
                    infrastructureElapsed += dt;
                    if (infrastructureElapsed >= infrastructureSeconds) Infrastructure = PressureOutcome.Failed;
                }
            }
            Present();
        }
        public void ResetPressure()
        {
            Civilian = Infrastructure = PressureOutcome.Pending;
            civilianElapsed = infrastructureElapsed = 0;
            emergencySignal.gameObject.SetActive(false); infrastructureSignal.gameObject.SetActive(false);
            damagedInfrastructure.SetActive(false); Present();
        }
        private void Present()
        {
            emergencySignal.localScale = Vector3.one * (1 + CivilianProgress * 1.5f);
            infrastructureSignal.localScale = Vector3.one * (1 + InfrastructureProgress * 2);
            emergencySignal.gameObject.SetActive(Civilian == PressureOutcome.Active || Civilian == PressureOutcome.Failed || (Civilian == PressureOutcome.Pending && emergencySignal.gameObject.activeSelf));
            infrastructureSignal.gameObject.SetActive(Infrastructure == PressureOutcome.Active || (Infrastructure == PressureOutcome.Pending && infrastructureSignal.gameObject.activeSelf));
            damagedInfrastructure.SetActive(InfrastructureDamaged);
        }
    }
}
