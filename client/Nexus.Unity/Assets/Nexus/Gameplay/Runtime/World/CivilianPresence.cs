using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    public enum CivilianState { Calm, Fleeing, Blocked, Sheltered, Injured, Depleted }

    [RequireComponent(typeof(PhysicalTarget), typeof(DamageReceiver), typeof(CapsuleCollider))]
    public sealed class CivilianPresence : MonoBehaviour
    {
        [SerializeField] private Transform refuge, visual;
        [SerializeField] private TextMesh stateLabel;
        [SerializeField, Min(.1f)] private float perceptionRadius = 10, moveSpeed = 2.4f;
        private Rigidbody body;
        private DamageReceiver receiver;
        private PhysicalTarget physical;
        private Vector3 spawn;
        private Quaternion rotation;
        private bool alarmed;
        private readonly Collider[] nearby = new Collider[32];
        private readonly RaycastHit[] obstacles = new RaycastHit[16];
        public CivilianState State { get; private set; }
        public bool Harmed => receiver.Health.Current < receiver.Health.Maximum;
        public Transform Refuge => refuge;

        private void Awake()
        {
            physical = GetComponent<PhysicalTarget>(); body = physical.Body; receiver = GetComponent<DamageReceiver>();
            spawn = body.position; rotation = body.rotation;
            if (!refuge || !visual || !stateLabel)
            { Debug.LogError("CivilianPresence requires refuge and presentation.", this); enabled = false; }
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
                State = receiver.Health.IsDepleted ? CivilianState.Depleted : CivilianState.Injured;
                Present(); return;
            }
            if (!alarmed) Perceive();
            if (!alarmed) { State = CivilianState.Calm; Present(); return; }
            Vector3 delta = Vector3.ProjectOnPlane(refuge.position - body.position, Vector3.up);
            if (delta.magnitude < .45f)
            { State = CivilianState.Sheltered; Stop(); Present(); return; }
            Vector3 direction = delta.normalized;
            int count = Physics.SphereCastNonAlloc(body.position + Vector3.up, .38f, direction, obstacles, .7f, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = count == obstacles.Length;
            for (int i = 0; i < count; i++) if (!obstacles[i].transform.IsChildOf(transform)) blocked = true;
            if (!Physics.Raycast(body.position + direction * .65f + Vector3.up * .3f, Vector3.down, .8f, ~0, QueryTriggerInteraction.Ignore)) blocked = true;
            State = blocked ? CivilianState.Blocked : CivilianState.Fleeing;
            Vector3 velocity = Vector3.MoveTowards(Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up), blocked ? Vector3.zero : direction * moveSpeed, 8 * dt);
            body.linearVelocity = velocity + Vector3.up * body.linearVelocity.y;
            if (!blocked) body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), 250 * dt));
            Present();
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
            stateLabel.text = State == CivilianState.Blocked ? "RESIDENT\nEXIT BLOCKED" : "RESIDENT\n" + State.ToString().ToUpperInvariant();
            stateLabel.color = Harmed ? new Color(1, .3f, .15f) : State == CivilianState.Sheltered ? Color.green : Color.white;
            visual.localRotation = Quaternion.Euler(State == CivilianState.Depleted ? 80 : Harmed ? 35 : 0, 0, 0);
        }
        public void ResetCivilian()
        {
            transform.SetPositionAndRotation(spawn, rotation); body.position = spawn; body.rotation = rotation;
            body.linearVelocity = body.angularVelocity = Vector3.zero; receiver.ResetHealth(); alarmed = false; State = CivilianState.Calm; Present();
        }
    }
}
