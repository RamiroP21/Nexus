using System;
using UnityEngine;

namespace Nexus.Gameplay.Combat
{
    public sealed class DamageReceiver : MonoBehaviour
    {
        [SerializeField, Min(.01f)] private float maximumHealth = 36;
        private Health health;
        public Health Health => health ??= new Health(maximumHealth);
        public event Action<Health> Changed;
        private void Awake()
        {
            if (!float.IsFinite(maximumHealth) || maximumHealth <= 0)
            { Debug.LogError("DamageReceiver requires positive finite maximum health.", this); enabled = false; return; }
            _ = Health;
        }
        public bool Receive(Damage damage)
        {
            if (!isActiveAndEnabled || !Health.Apply(damage)) return false;
            Changed?.Invoke(Health);
            return true;
        }
    }
}
