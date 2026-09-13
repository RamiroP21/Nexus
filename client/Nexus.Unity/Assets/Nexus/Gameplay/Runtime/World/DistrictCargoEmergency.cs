using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    public enum CargoOutcome { Waiting, Moving, Diverted, Impacted }

    // Two runaway service loads: redirect them off the marked lane, or lift/carry them out.
    // The district owns the clock. No Update, scheduler, scene lookup or power-specific mode.
    public sealed class DistrictCargoEmergency : MonoBehaviour
    {
        [SerializeField] private Rigidbody[] cargo;
        [SerializeField] private Transform laneStart, laneEnd;
        [SerializeField] private CivilianPresence[] workers;
        [SerializeField] private GameObject warningVisual, damagedUtility, consequenceObstruction;
        [SerializeField, Min(1)] private float duration = 42;
        [SerializeField, Min(.1f)] private float laneHalfWidth = 3, safeLift = 3, speed = 2.2f, acceleration = 1.5f;
        private Vector3[] starts;
        private Quaternion[] rotations;
        private CargoOutcome[] outcomes;
        private bool initialized;
        public DistrictCrisisState State { get; private set; }
        public float Elapsed { get; private set; }
        public float Deadline { get; private set; }
        public bool InheritedDamage { get; private set; }
        public bool RouteBlocked => InheritedDamage || State == DistrictCrisisState.Failed;
        public Rigidbody[] Cargo => (Rigidbody[])cargo.Clone();
        public int CargoCount => cargo == null ? 0 : cargo.Length;
        public CargoOutcome Outcome(int index) => outcomes[index];
        public bool Nearby { get; private set; }
        public event System.Action Resetting;
        private void Awake() => Initialize();
        private bool Initialize()
        {
            if (initialized) return true;
            if (cargo == null || cargo.Length == 0 || !laneStart || !laneEnd || workers == null || !warningVisual || !damagedUtility || !consequenceObstruction
                || !float.IsFinite(duration) || duration <= 0 || (laneEnd.position - laneStart.position).sqrMagnitude < 1)
            { Debug.LogError("District cargo emergency requires explicit cargo, lane, workers and consequence presentation.", this); enabled = false; return false; }
            starts = new Vector3[cargo.Length]; rotations = new Quaternion[cargo.Length]; outcomes = new CargoOutcome[cargo.Length];
            for (int i = 0; i < cargo.Length; i++)
            {
                if (!cargo[i] || !cargo[i].GetComponent<PhysicalTarget>() || !cargo[i].GetComponent<Graspable>())
                { Debug.LogError("District cargo must use PhysicalTarget and explicit Graspable.", this); enabled = false; return false; }
                starts[i] = cargo[i].position; rotations[i] = cargo[i].rotation;
            }
            initialized = true; ResetEmergency(); return true;
        }
        public bool Begin(bool infrastructureDamaged)
        {
            if (!isActiveAndEnabled || !Initialize() || State != DistrictCrisisState.Dormant) return false;
            InheritedDamage = infrastructureDamaged; Deadline = duration * (infrastructureDamaged ? .7f : 1);
            State = DistrictCrisisState.Active;
            for (int i = 0; i < outcomes.Length; i++) { outcomes[i] = CargoOutcome.Moving; cargo[i].WakeUp(); }
            foreach (var worker in workers) if (worker) worker.SetBlockPhase(UrbanBlockPhase.Incident);
            Present(); return true;
        }
        public void Tick(float seconds, bool nearby)
        {
            if (!isActiveAndEnabled || !initialized || seconds <= 0 || !float.IsFinite(seconds)) return;
            Nearby = nearby;
            if (State != DistrictCrisisState.Active) return;
            Elapsed = Mathf.Min(Deadline, Elapsed + seconds);
            Vector3 direction = Vector3.ProjectOnPlane(laneEnd.position - laneStart.position, Vector3.up).normalized;
            float length = Vector3.Distance(laneEnd.position, laneStart.position);
            bool moving = false, failed = false;
            for (int i = 0; i < outcomes.Length; i++)
            {
                if (outcomes[i] == CargoOutcome.Moving)
                {
                    Vector3 delta = cargo[i].position - laneStart.position;
                    float along = Vector3.Dot(delta, direction);
                    float lateral = Vector3.ProjectOnPlane(delta - direction * along, Vector3.up).magnitude;
                    // Both paths are world geometry: sideways Vector deflection or a sustained lift/carry.
                    if (lateral > laneHalfWidth || cargo[i].position.y > starts[i].y + safeLift || along < -2)
                        outcomes[i] = CargoOutcome.Diverted;
                    else if (along >= length || Elapsed >= Deadline)
                        outcomes[i] = CargoOutcome.Impacted;
                }
                moving |= outcomes[i] == CargoOutcome.Moving;
                failed |= outcomes[i] == CargoOutcome.Impacted;
            }
            if (!moving)
            {
                State = failed ? DistrictCrisisState.Failed : DistrictCrisisState.Resolved;
                foreach (var worker in workers)
                {
                    if (!worker) continue;
                    if (failed && worker.State != CivilianState.Sheltered && !worker.Harmed)
                        worker.GetComponent<DamageReceiver>().Receive(new Damage(12));
                    worker.SetBlockPhase(UrbanBlockPhase.Aftermath);
                }
            }
            Present();
        }
        private void FixedUpdate()
        {
            if (!initialized || State != DistrictCrisisState.Active || !Nearby) return;
            Vector3 direction = Vector3.ProjectOnPlane(laneEnd.position - laneStart.position, Vector3.up).normalized;
            for (int i = 0; i < cargo.Length; i++)
                if (outcomes[i] == CargoOutcome.Moving && !cargo[i].isKinematic && Vector3.Dot(cargo[i].linearVelocity, direction) < speed)
                    cargo[i].AddForce(direction * acceleration, ForceMode.Acceleration);
        }
        public void ResetEmergency()
        {
            if (!initialized) return;
            Resetting?.Invoke();
            State = DistrictCrisisState.Dormant; Elapsed = 0; Deadline = duration; InheritedDamage = Nearby = false;
            for (int i = 0; i < cargo.Length; i++)
            {
                cargo[i].position = starts[i]; cargo[i].rotation = rotations[i];
                cargo[i].linearVelocity = cargo[i].angularVelocity = Vector3.zero; outcomes[i] = CargoOutcome.Waiting;
            }
            foreach (var worker in workers) if (worker) worker.ResetCivilian();
            Present();
        }
        private void Present()
        {
            warningVisual.SetActive(State == DistrictCrisisState.Active);
            damagedUtility.SetActive(State == DistrictCrisisState.Failed);
            consequenceObstruction.SetActive(RouteBlocked);
        }
    }
}
