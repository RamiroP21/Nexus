using System;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.Abilities
{
    public enum GraspReleaseReason { Released, InvalidTarget, OutOfRange, Occluded, Unavailable, Reset, Vector }
    public sealed class KineticGraspAbility : MonoBehaviour
    {
        [SerializeField] private KineticGraspSettings settings;
        [SerializeField] private ContextualTargeting targeting;
        [SerializeField] private PlayerVitality vitality;
        private Graspable held;
        public KineticGraspSettings Settings => settings;
        public PhysicalTarget HeldTarget => held ? held.Target : null;
        public bool IsHolding => held;
        public Vector3 HoldPoint => targeting && settings ? targeting.Origin + targeting.AimDirection * settings.HoldDistance : default;
        public GraspReleaseReason LastReleaseReason { get; private set; }
        public event Action<PhysicalTarget> Acquired;
        public event Action<GraspReleaseReason> Released;
        private void Awake()
        {
            if (!settings || !settings.IsValid || !targeting)
            { Debug.LogError("KineticGraspAbility requires valid settings and targeting.", this); enabled = false; }
        }
        private void OnEnable()
        {
            if (targeting) targeting.Cleared += OnUnavailable;
            if (vitality) vitality.Resetting += ResetGrasp;
        }
        private void OnDisable()
        {
            if (targeting) targeting.Cleared -= OnUnavailable;
            if (vitality) vitality.Resetting -= ResetGrasp;
            Release(GraspReleaseReason.Unavailable);
        }
        private bool Available => isActiveAndEnabled && settings && settings.IsValid && targeting
            && targeting.isActiveAndEnabled && !targeting.Suspended && Time.timeScale > 0 && (!vitality || !vitality.Depleted);
        public bool TryAcquire()
        {
            if (!Available || IsHolding || !targeting.TrySelect(out var selected)) return false;
            return TryAcquire(selected.Target);
        }
        public bool TryAcquire(PhysicalTarget target)
        {
            if (!Available || IsHolding || !target || !target.TryGetComponent<Graspable>(out var graspable)
                || !graspable.CanGrasp || target.Body.mass > settings.MaximumMass
                || !targeting.TryValidate(target, out _) || Vector3.Distance(targeting.Origin, target.AimPoint) > settings.AcquireRange) return false;
            held = graspable;
            Acquired?.Invoke(target);
            return true;
        }
        private void Update()
        {
            if (!ReferenceEquals(held, null) && (!held || !Available)) Release(GraspReleaseReason.Unavailable);
        }
        private void FixedUpdate()
        {
            if (ReferenceEquals(held, null)) return;
            if (!Available) { Release(GraspReleaseReason.Unavailable); return; }
            if (!held || !held.CanGrasp || held.Target.Body.mass > settings.MaximumMass)
            { Release(GraspReleaseReason.InvalidTarget); return; }
            var target = held.Target;
            if (Vector3.Distance(targeting.Origin, target.AimPoint) > settings.BreakDistance)
            { Release(GraspReleaseReason.OutOfRange); return; }
            if (settings.RequireLineOfSight && !targeting.HasLineOfSight(target))
            { Release(GraspReleaseReason.Occluded); return; }
            var body = target.Body;
            // Preserve physics/collision ownership. No transform, velocity, gravity or constraint overrides.
            Vector3 acceleration = (HoldPoint - body.worldCenterOfMass) * settings.Spring - body.linearVelocity * settings.Damping;
            if (body.useGravity) acceleration -= Physics.gravity;
            body.AddForce(Vector3.ClampMagnitude(acceleration, settings.MaximumAcceleration), ForceMode.Acceleration);
        }
        public void Release(GraspReleaseReason reason = GraspReleaseReason.Released)
        {
            if (ReferenceEquals(held, null)) return;
            held = null; LastReleaseReason = reason; Released?.Invoke(reason);
        }
        public void ReleaseForVector(PhysicalTarget target)
        { if (HeldTarget == target && IsHolding) Release(GraspReleaseReason.Vector); }
        public void ResetGrasp() => Release(GraspReleaseReason.Reset);
        private void OnUnavailable() => Release(GraspReleaseReason.Unavailable);
    }
}
