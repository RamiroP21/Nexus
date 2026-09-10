using System;

namespace Nexus.Gameplay.Combat
{
    public readonly struct Damage
    {
        public float Amount { get; }
        public Damage(float amount)
        {
            if (!float.IsFinite(amount) || amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Amount = amount;
        }
    }
    public sealed class Health
    {
        public float Maximum { get; }
        public float Current { get; private set; }
        public bool IsDepleted => Current <= 0;
        public Health(float maximum)
        {
            if (!float.IsFinite(maximum) || maximum <= 0) throw new ArgumentOutOfRangeException(nameof(maximum));
            Maximum = Current = maximum;
        }
        public bool Apply(Damage damage)
        {
            if (IsDepleted || damage.Amount == 0) return false;
            Current = Math.Max(0, Current - damage.Amount);
            return true;
        }
    }
}
