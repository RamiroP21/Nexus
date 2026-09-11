using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.Combat
{
    public enum SentinelState { Ready, Telegraph, Recovery, Staggered, Disabled }
    [RequireComponent(typeof(PhysicalTarget), typeof(DamageReceiver))]
    public sealed class TrainingSentinel : MonoBehaviour
    {
        [SerializeField] private PlayerVitality player;
        [SerializeField] private TrainingHazard hazardTemplate;
        [SerializeField] private Transform muzzle, visual;
        [SerializeField] private LineRenderer telegraph;
        [SerializeField] private TextMesh stateLabel;
        [SerializeField, Min(.1f)] private float range = 10, telegraphSeconds = 1.2f, recoverySeconds = 2, staggerSeconds = .8f, projectileSpeed = 8;
        private PhysicalTarget physical;
        private DamageReceiver receiver;
        private float remaining;
        private Vector3 aim;
        private Quaternion rest;
        public SentinelState State { get; private set; }
        public int AttackCount { get; private set; }
        public TrainingHazard LastHazard { get; private set; }
        public float TelegraphSeconds => telegraphSeconds;
        private void Awake()
        {
            physical = GetComponent<PhysicalTarget>(); receiver = GetComponent<DamageReceiver>();
            if (!player || !hazardTemplate || !muzzle || !visual || !telegraph || !stateLabel)
            { Debug.LogError("TrainingSentinel requires player, hazard, muzzle and presentation references.", this); enabled = false; return; }
            rest = visual.localRotation;
        }
        private void OnEnable()
        { if (!physical) return; physical.Impacted += OnImpact; receiver.Changed += OnHealth; OnHealth(receiver.Health); Present(); }
        private void OnDisable()
        { if (physical) physical.Impacted -= OnImpact; if (receiver) receiver.Changed -= OnHealth; if (telegraph) telegraph.enabled = false; }
        private void OnImpact(ForceImpact impact)
        {
            if (State == SentinelState.Disabled || impact.Impulse.magnitude < 2) return;
            State = SentinelState.Staggered; remaining = staggerSeconds; Present();
        }
        private void OnHealth(Health health)
        { if (health.IsDepleted) { State = SentinelState.Disabled; Present(); } }
        private void Update() => Tick(Time.deltaTime);
        public void Tick(float dt)
        {
            if (!isActiveAndEnabled || dt <= 0 || State == SentinelState.Disabled) return;
            if (!player || player.Depleted) { State = SentinelState.Ready; Present(); return; }
            if (State == SentinelState.Ready)
            {
                Vector3 target = player.transform.position + Vector3.up;
                Vector3 delta = target - muzzle.position;
                if (delta.magnitude > range || !Physics.Raycast(muzzle.position, delta.normalized, out var hit, delta.magnitude + .1f, ~0, QueryTriggerInteraction.Ignore)
                    || !hit.transform.IsChildOf(player.transform)) return;
                aim = target; State = SentinelState.Telegraph; remaining = telegraphSeconds;
            }
            else
            {
                remaining -= dt;
                if (remaining <= 0)
                {
                    if (State == SentinelState.Telegraph)
                    {
                        LastHazard = Instantiate(hazardTemplate, muzzle.position, Quaternion.identity);
                        LastHazard.gameObject.SetActive(true);
                        LastHazard.Launch(gameObject, (aim - muzzle.position).normalized * projectileSpeed);
                        AttackCount++; State = SentinelState.Recovery; remaining = recoverySeconds;
                    }
                    else State = SentinelState.Ready;
                }
            }
            Present();
        }
        private void Present()
        {
            if (!telegraph || !visual || !stateLabel) return;
            telegraph.enabled = isActiveAndEnabled && State == SentinelState.Telegraph;
            if (telegraph.enabled) { telegraph.SetPosition(0, muzzle.position); telegraph.SetPosition(1, aim); }
            stateLabel.text = State == SentinelState.Telegraph ? "INCOMING" : State.ToString().ToUpperInvariant();
            stateLabel.color = State == SentinelState.Telegraph ? new Color(1, .35f, .1f) : Color.white;
            visual.localRotation = rest * Quaternion.Euler(State == SentinelState.Disabled ? 65 : State == SentinelState.Staggered ? -25 : 0, 0, 0);
        }
    }
}
