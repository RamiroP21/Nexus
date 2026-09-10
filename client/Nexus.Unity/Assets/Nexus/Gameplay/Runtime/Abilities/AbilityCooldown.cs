using System;

namespace Nexus.Gameplay.Abilities
{
    public sealed class AbilityCooldown
    {
        private double readyAt = double.NegativeInfinity;
        public bool IsReady(double now) => double.IsFinite(now) && now >= readyAt;
        public bool TryConsume(double now, float duration)
        {
            if (!float.IsFinite(duration) || duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration));
            if (!IsReady(now)) return false;
            readyAt = now + duration;
            return true;
        }
    }
}
