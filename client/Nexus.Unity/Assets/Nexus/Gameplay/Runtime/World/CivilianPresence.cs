using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    public enum CivilianState { Calm, Fleeing, Blocked, Sheltered, Injured, Depleted }
    public enum CivilianActivity { Waiting, Walking, Interrupted, Cautious }

    [RequireComponent(typeof(PhysicalTarget), typeof(DamageReceiver), typeof(CapsuleCollider))]
    public sealed class CivilianPresence : MonoBehaviour
    {
        [SerializeField] private Transform refuge, visual;
        [SerializeField] private Transform[] activityAnchors;
        [SerializeField] private Transform aftermathAnchor;
        [SerializeField, Min(.1f)] private float activitySpeed = .7f, activityPause = 2.5f;
        [SerializeField] private TextMesh stateLabel;
        [SerializeField, Min(.1f)] private float perceptionRadius = 10, moveSpeed = 2.4f;
        private Rigidbody body;
        private DamageReceiver receiver;
        private PhysicalTarget physical;
        private Vector3 spawn;
        private Quaternion rotation;
        private bool alarmed;
        private UrbanBlockPhase phase;
        private int activityIndex;
        private float activityWait, aftermathWait;
        private bool atRefuge;
        private Transform[] presentationParts;
        public Transform Visual => visual;
        private readonly Collider[] nearby = new Collider[32];
        private readonly RaycastHit[] obstacles = new RaycastHit[16];
        public CivilianState State { get; private set; }
        public bool Harmed => receiver.Health.Current < receiver.Health.Maximum;
        public bool Alerted => alarmed;
        public Transform Refuge => refuge;
        public CivilianActivity Activity { get; private set; }
        public int ActivityAnchorCount => activityAnchors == null ? 0 : activityAnchors.Length;

        private void Awake()
        {
            physical = GetComponent<PhysicalTarget>(); body = physical.Body; receiver = GetComponent<DamageReceiver>();
            spawn = body.position; rotation = body.rotation;
            activityWait = activityPause;
            if (!refuge || !visual || !stateLabel)
            { Debug.LogError("CivilianPresence requires refuge and presentation.", this); enabled = false; }
            if (visual) presentationParts = visual.GetComponentsInChildren<Transform>();
        }
        private void OnEnable()
        { if (physical) { physical.Impacted += OnImpact; receiver.Changed += OnHealth; } }
        private void OnDisable()
        { if (physical) { physical.Impacted -= OnImpact; receiver.Changed -= OnHealth; } }
        private void OnImpact(ForceImpact impact) { if (impact.Impulse.sqrMagnitude > 4) alarmed = true; }
        private void OnHealth(Health health) { alarmed = true; Present(); }
        private void FixedUpdate() => Tick(Time.fixedDeltaTime);
        public void Tick(float dt)
        {
            if (!isActiveAndEnabled || dt <= 0 || !float.IsFinite(dt)) return;
            if (body.position.y < -8) receiver.Receive(new Damage(receiver.Health.Current));
            if (Harmed)
            {
                Activity = CivilianActivity.Interrupted;
                State = receiver.Health.IsDepleted ? CivilianState.Depleted : CivilianState.Injured;
                Present(); return;
            }
            if (!alarmed) Perceive();
            if (!alarmed) { State = CivilianState.Calm; TickActivity(dt); Present(); return; }
            Activity = CivilianActivity.Interrupted;
            if (phase == UrbanBlockPhase.Aftermath && atRefuge)
            {
                State = CivilianState.Sheltered;
                aftermathWait = Mathf.Max(0, aftermathWait - dt);
                if (aftermathWait > 0 || !aftermathAnchor) Stop();
                else
                {
                    Activity = CivilianActivity.Cautious;
                    MoveLocally(aftermathAnchor.position, .45f, dt);
                }
                Present(); return;
            }
            Vector3 delta = Vector3.ProjectOnPlane(refuge.position - body.position, Vector3.up);
            if (delta.magnitude < .45f)
            { atRefuge = true; State = CivilianState.Sheltered; Stop(); Present(); return; }
            bool blocked = !MoveLocally(refuge.position, moveSpeed, dt);
            State = blocked ? CivilianState.Blocked : CivilianState.Fleeing;
            Present();
        }
        public void SetBlockPhase(UrbanBlockPhase value)
        {
            if (phase == value) return;
            phase = value;
            if (phase != UrbanBlockPhase.Calm)
            { alarmed = true; Activity = CivilianActivity.Interrupted; }
            if (phase == UrbanBlockPhase.Aftermath) aftermathWait = 6;
        }
        private void TickActivity(float dt)
        {
            Activity = CivilianActivity.Waiting;
            if (activityAnchors == null || activityAnchors.Length == 0) { Stop(); return; }
            if (activityWait > 0) { activityWait = Mathf.Max(0, activityWait - dt); Stop(); return; }
            var destination = activityAnchors[activityIndex];
            if (!destination) { Stop(); return; }
            if (Vector3.ProjectOnPlane(destination.position - body.position, Vector3.up).magnitude < .18f)
            { activityIndex = (activityIndex + 1) % activityAnchors.Length; activityWait = activityPause; Stop(); return; }
            if (MoveLocally(destination.position, activitySpeed, dt)) Activity = CivilianActivity.Walking;
        }
        private bool MoveLocally(Vector3 destination, float speed, float dt)
        {
            Vector3 delta = Vector3.ProjectOnPlane(destination - body.position, Vector3.up);
            if (delta.magnitude < .18f) { Stop(); return true; }
            Vector3 direction = delta.normalized;
            int count = Physics.SphereCastNonAlloc(body.position + Vector3.up, .38f, direction, obstacles, .7f, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = count == obstacles.Length;
            for (int i = 0; i < count; i++) if (!obstacles[i].transform.IsChildOf(transform)) blocked = true;
            if (!Physics.Raycast(body.position + direction * .65f + Vector3.up * .3f, Vector3.down, .8f, ~0, QueryTriggerInteraction.Ignore)) blocked = true;
            Vector3 velocity = Vector3.MoveTowards(Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up), blocked ? Vector3.zero : direction * Mathf.Min(speed, delta.magnitude / dt), 8 * dt);
            body.linearVelocity = velocity + Vector3.up * body.linearVelocity.y;
            if (!blocked) body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), 250 * dt));
            return !blocked;
        }
        private void Perceive()
        {
            int count = Physics.OverlapSphereNonAlloc(body.position, perceptionRadius, nearby, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var other = nearby[i];
                if (other.transform.IsChildOf(transform)) continue;
                bool danger = other.TryGetComponent<HostileCombatant>(out var hostile) && hostile.Target && hostile.State != HostileState.Depleted;
                danger |= other.TryGetComponent<TrainingHazard>(out var hazard) && !hazard.Spent;
                danger |= other.attachedRigidbody && other.attachedRigidbody.linearVelocity.sqrMagnitude > 16;
                if (!danger) continue;
                Vector3 point = other.bounds.center;
                if (Physics.Linecast(body.position + Vector3.up, point, out var hit, ~0, QueryTriggerInteraction.Ignore)
                    && !hit.transform.IsChildOf(other.transform)) continue;
                alarmed = true; return;
            }
        }
        private void Stop() { body.linearVelocity = Vector3.up * body.linearVelocity.y; }
        private void Present()
        {
            if (!stateLabel || !visual) return;
            if (Harmed) State = receiver.Health.IsDepleted ? CivilianState.Depleted : CivilianState.Injured;
            stateLabel.text = State == CivilianState.Blocked ? "RESIDENT\nEXIT BLOCKED" : "RESIDENT\n" + State.ToString().ToUpperInvariant() + " / " + Activity.ToString().ToUpperInvariant();
            stateLabel.color = Harmed ? new Color(1, .3f, .15f) : State == CivilianState.Sheltered ? Color.green : Color.white;
            bool fallen = State == CivilianState.Depleted;
            bool injured = State == CivilianState.Injured;
            bool shelter = State == CivilianState.Sheltered && Activity != CivilianActivity.Cautious;
            bool blocked = State == CivilianState.Blocked;
            float stride = State == CivilianState.Fleeing ? Mathf.Sin(Time.time * 13) * 30
                : Activity == CivilianActivity.Walking || (Activity == CivilianActivity.Cautious && body.linearVelocity.sqrMagnitude > .04f) ? Mathf.Sin(Time.time * 6) * 14 : 0;
            visual.localPosition = new Vector3(0, fallen ? .22f : injured ? -.48f : shelter ? -.38f : 0, 0);
            visual.localRotation = Quaternion.Euler(fallen ? 88 : injured ? 32 : shelter ? 18 : State == CivilianState.Fleeing ? 12 : 0, 0, 0);
            foreach (var part in presentationParts)
            {
                float side = Mathf.Sign(part.localPosition.x);
                if (part.name == "Arm") part.localRotation = Quaternion.Euler(blocked ? -130 : shelter ? -85 : injured ? -65 : -stride * side, 0, blocked ? side * -30 : side * -8);
                if (part.name == "Leg") part.localRotation = Quaternion.Euler(shelter || injured ? -35 : stride * side, 0, 0);
            }
        }
        public void ResetCivilian()
        {
            transform.SetPositionAndRotation(spawn, rotation); body.position = spawn; body.rotation = rotation;
            body.linearVelocity = body.angularVelocity = Vector3.zero; receiver.ResetHealth(); alarmed = atRefuge = false;
            phase = UrbanBlockPhase.Calm; activityIndex = 0; activityWait = activityPause; aftermathWait = 0;
            Activity = CivilianActivity.Waiting; State = CivilianState.Calm; Present();
        }
    }
}
