using System;
using Nexus.Gameplay.Character;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.Abilities
{
    public enum ActivationResult { Activated, CoolingDown, InvalidTarget, Unavailable }
    public sealed class KineticVectorAbility : MonoBehaviour
    {
        [SerializeField] private KineticVectorSettings settings;
        [SerializeField] private ContextualTargeting targeting;
        [SerializeField] private CharacterMotor motor;
        private readonly AbilityCooldown cooldown = new();
        public event Action<ForceImpact> Executed;
        public KineticVectorSettings Settings => settings;
        public int ActivationCount { get; private set; }
        public PhysicalTarget LastTarget { get; private set; }
        public bool Ready => cooldown.IsReady(Time.timeAsDouble);
        private void Awake()
        {
            if (!settings || !settings.IsValid || !targeting || !motor)
            { Debug.LogError("KineticVectorAbility requires valid settings, targeting and motor.", this); enabled = false; }
        }
        public ActivationResult TryActivate()
        {
            if (!isActiveAndEnabled || Time.timeScale <= 0 || targeting.Suspended) return ActivationResult.Unavailable;
            if (!Ready) return ActivationResult.CoolingDown;
            if (!targeting.TrySelect(out var selected)) return ActivationResult.InvalidTarget;
            Vector3 direction = (selected.Point - targeting.Origin).normalized;
            var impact = new ForceImpact(direction * settings.Impulse, selected.Point);
            IForceReceiver receiver = selected.Target;
            if (!receiver.ReceiveForce(impact)) return ActivationResult.InvalidTarget;
            cooldown.TryConsume(Time.timeAsDouble, settings.Cooldown);
            if (settings.Damage > 0 && selected.Target.TryGetComponent<DamageReceiver>(out var damage)) damage.Receive(new Damage(settings.Damage));
            motor.AddImpulse(-direction * settings.Recoil);
            LastTarget = selected.Target; ActivationCount++;
            Executed?.Invoke(impact);
            return ActivationResult.Activated;
        }
    }
}
