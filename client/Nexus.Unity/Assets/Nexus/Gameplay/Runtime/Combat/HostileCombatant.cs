using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.Combat
{
    public enum HostileState { Idle, Position, Telegraph, Attack, Recover, Staggered, Depleted }

    [RequireComponent(typeof(PhysicalTarget), typeof(DamageReceiver), typeof(CapsuleCollider))]
    public sealed class HostileCombatant : MonoBehaviour
    {
        [SerializeField] private PlayerVitality target;
        [SerializeField] private TrainingHazard hazardTemplate;
        [SerializeField] private Transform muzzle, visual;
        [SerializeField] private LineRenderer telegraph;
        [SerializeField] private TextMesh stateLabel;
        [SerializeField, Min(.1f)] private float detectionRange = 14, attackRange = 11, preferredRange = 7;
        [SerializeField, Min(.1f)] private float moveSpeed = 2.8f, acceleration = 9, telegraphSeconds = 1.15f, recoverySeconds = 1.6f;
        [SerializeField, Min(.1f)] private float projectileSpeed = 10, staggerSeconds = .65f, staggerProtectionSeconds = 3;
        private PhysicalTarget physical;
        private DamageReceiver receiver;
        private Rigidbody body;
        private Vector3 spawn, aim;
        private Quaternion spawnRotation, visualRest;
        private float remaining, staggerProtection;
        private bool acquired, halted;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        public HostileState State { get; private set; }
        public PlayerVitality Target => acquired ? target : null;
        public bool HasLineOfSight { get; private set; }
        public int AttackCount { get; private set; }
        public int StaggerCount { get; private set; }
        public TrainingHazard LastHazard { get; private set; }
        public float TelegraphSeconds => telegraphSeconds;
        public float RecoverySeconds => recoverySeconds;

        private void Awake()
        {
            physical = GetComponent<PhysicalTarget>(); receiver = GetComponent<DamageReceiver>(); body = physical.Body;
            spawn = body.position; spawnRotation = body.rotation;
            if (!target || !hazardTemplate || !muzzle || !visual || !telegraph || !stateLabel)
            { Debug.LogError("HostileCombatant requires target, hazard, muzzle and presentation references.", this); enabled = false; return; }
            visualRest = visual.localRotation;
        }
        private void OnEnable()
        { if (!physical) return; physical.Impacted += OnImpact; receiver.Changed += OnHealth; OnHealth(receiver.Health); Present(); }
        private void OnDisable()
        { if (physical) physical.Impacted -= OnImpact; if (receiver) receiver.Changed -= OnHealth; if (telegraph) telegraph.enabled = false; }
        private void OnHealth(Health health)
        { if (health.IsDepleted) { SetState(HostileState.Depleted); StopHorizontalMotion(); } }
        private void OnImpact(ForceImpact impact)
        {
            if (halted || State == HostileState.Depleted || staggerProtection > 0 || impact.Impulse.sqrMagnitude < 16) return;
            StaggerCount++; staggerProtection = staggerSeconds + staggerProtectionSeconds;
            SetState(HostileState.Staggered, staggerSeconds);
        }
        private void FixedUpdate() => Tick(Time.fixedDeltaTime);
        public void Tick(float dt)
        {
            if (!isActiveAndEnabled || !float.IsFinite(dt) || dt <= 0 || halted || State == HostileState.Depleted) return;
            if (body.position.y < -8) { receiver.Receive(new Damage(receiver.Health.Current)); return; }
            staggerProtection = Mathf.Max(0, staggerProtection - dt);
            if (!target || target.Depleted) { acquired = false; SetState(HostileState.Idle); StopHorizontalMotion(); return; }
            Vector3 targetPoint = target.transform.position + Vector3.up;
            float distance = Vector3.Distance(muzzle.position, targetPoint);
            HasLineOfSight = distance <= detectionRange && ClearSight(targetPoint);
            if (!acquired)
            {
                if (!HasLineOfSight) return;
                acquired = true; SetState(HostileState.Position, .45f);
            }
            if (distance > detectionRange * 1.6f || Vector3.Distance(body.position, spawn) > 18)
            { acquired = false; SetState(HostileState.Idle); StopHorizontalMotion(); return; }
            remaining -= dt;
            switch (State)
            {
                case HostileState.Position:
                    Orient(targetPoint, dt); Steer(targetPoint, distance, dt);
                    if (remaining <= 0 && HasLineOfSight && distance <= attackRange)
                    { aim = targetPoint; StopHorizontalMotion(); SetState(HostileState.Telegraph, telegraphSeconds); }
                    break;
                case HostileState.Telegraph:
                    // Committed aim: moving sideways during the warning remains a valid response.
                    if (remaining <= 0)
                    {
                        LastHazard = Instantiate(hazardTemplate, muzzle.position, Quaternion.identity);
                        LastHazard.gameObject.SetActive(true);
                        LastHazard.Launch(gameObject, (aim - muzzle.position).normalized * projectileSpeed);
                        AttackCount++; SetState(HostileState.Attack, .12f);
                    }
                    break;
                case HostileState.Attack:
                    if (remaining <= 0) SetState(HostileState.Recover, recoverySeconds);
                    break;
                case HostileState.Recover:
                case HostileState.Staggered:
                    if (remaining <= 0) SetState(HostileState.Position, .25f);
                    break;
            }
            Present();
        }
        private bool ClearSight(Vector3 point)
        {
            Vector3 delta = point - muzzle.position;
            int count = Physics.RaycastNonAlloc(muzzle.position, delta.normalized, hits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return false;
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(transform) && !hits[i].transform.IsChildOf(target.transform)) return false;
            return true;
        }
        private void Orient(Vector3 point, float dt)
        {
            Vector3 direction = Vector3.ProjectOnPlane(point - body.position, Vector3.up);
            if (direction.sqrMagnitude > .01f) body.MoveRotation(Quaternion.RotateTowards(body.rotation, Quaternion.LookRotation(direction), 200 * dt));
        }
        private void Steer(Vector3 point, float distance, float dt)
        {
            Vector3 toward = Vector3.ProjectOnPlane(point - body.position, Vector3.up).normalized;
            Vector3 direction = !HasLineOfSight ? Vector3.Cross(Vector3.up, toward)
                : distance > preferredRange + 1 ? toward : distance < preferredRange - 1 ? -toward : Vector3.Cross(Vector3.up, toward) * .45f;
            Vector3 desired = direction * moveSpeed;
            // Bounded local steering, not navigation: stop at blocked steps and unsupported ground.
            Vector3 origin = body.position + Vector3.up;
            int count = Physics.SphereCastNonAlloc(origin, .46f, direction.normalized, hits, .65f, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = count == hits.Length;
            for (int i = 0; i < count; i++) if (!hits[i].transform.IsChildOf(transform)) blocked = true;
            if (blocked || !Physics.Raycast(body.position + direction.normalized * .65f + Vector3.up * .3f, Vector3.down, .8f, ~0, QueryTriggerInteraction.Ignore)) desired = Vector3.zero;
            Vector3 horizontal = Vector3.MoveTowards(Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up), desired, acceleration * dt);
            body.linearVelocity = horizontal + Vector3.up * body.linearVelocity.y;
        }
        private void StopHorizontalMotion()
        { if (body) body.linearVelocity = Vector3.up * body.linearVelocity.y; }
        private void SetState(HostileState state, float seconds = 0) { State = state; remaining = seconds; Present(); }
        public void Halt()
        { halted = true; acquired = false; if (State != HostileState.Depleted) SetState(HostileState.Idle); StopHorizontalMotion(); }
        public void ResetCombatant()
        {
            halted = acquired = false; HasLineOfSight = false; remaining = staggerProtection = 0; AttackCount = StaggerCount = 0;
            transform.SetPositionAndRotation(spawn, spawnRotation);
            body.position = spawn; body.rotation = spawnRotation; body.linearVelocity = body.angularVelocity = Vector3.zero;
            receiver.ResetHealth(); SetState(HostileState.Idle);
        }
        private void Present()
        {
            if (!telegraph || !visual || !stateLabel) return;
            telegraph.enabled = isActiveAndEnabled && !halted && State == HostileState.Telegraph;
            if (telegraph.enabled) { telegraph.SetPosition(0, muzzle.position); telegraph.SetPosition(1, aim); }
            stateLabel.text = $"HOSTILE  {receiver.Health.Current:0}\n{State.ToString().ToUpperInvariant()}";
            stateLabel.color = State == HostileState.Telegraph || State == HostileState.Attack ? new Color(1, .3f, .08f)
                : State == HostileState.Staggered ? Color.cyan : Color.white;
            visual.localRotation = visualRest * Quaternion.Euler(State == HostileState.Depleted ? 75 : State == HostileState.Staggered ? -25 : State == HostileState.Telegraph ? -10 : 0, 0, 0);
        }
    }
}
