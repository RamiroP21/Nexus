using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    public enum ReactiveState { Ready, Impacted, Depleted }
    [RequireComponent(typeof(PhysicalTarget), typeof(DamageReceiver))]
    public sealed class ReactiveEntity : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        [SerializeField, Min(.01f)] private float recoverySeconds = .65f;
        private PhysicalTarget target;
        private DamageReceiver damage;
        private Quaternion restRotation;
        private float until;
        public ReactiveState State { get; private set; }
        public int ImpactCount { get; private set; }
        private void Awake()
        {
            target = GetComponent<PhysicalTarget>(); damage = GetComponent<DamageReceiver>();
            if (!visual || recoverySeconds <= 0) { Debug.LogError("ReactiveEntity requires visual root and positive recovery time.", this); enabled = false; return; }
            restRotation = visual.localRotation;
        }
        private void OnEnable()
        {
            if (!target || !damage) return;
            target.Impacted += OnImpact; damage.Changed += OnHealth;
            OnHealth(damage.Health);
        }
        private void OnDisable()
        { if (target) target.Impacted -= OnImpact; if (damage) damage.Changed -= OnHealth; }
        private void OnImpact(ForceImpact impact)
        {
            ImpactCount++;
            if (State == ReactiveState.Depleted) return;
            State = ReactiveState.Impacted; until = Time.time + recoverySeconds;
            visual.localRotation = restRotation * Quaternion.Euler(-22, 0, 12);
        }
        private void OnHealth(Health health)
        {
            if (health.IsDepleted)
            { State = ReactiveState.Depleted; visual.localRotation = restRotation * Quaternion.Euler(65, 0, 12); }
        }
        private void Update()
        {
            if (State != ReactiveState.Impacted || Time.time < until) return;
            State = ReactiveState.Ready; visual.localRotation = restRotation;
        }
    }
}
