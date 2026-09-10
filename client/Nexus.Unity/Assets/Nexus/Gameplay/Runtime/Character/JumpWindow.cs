namespace Nexus.Gameplay.Character
{
    // Consuming support prevents repeated jumps during the same coyote window.
    public sealed class JumpWindow
    {
        private double groundedAt = double.NegativeInfinity, requestedAt = double.NegativeInfinity;
        public void Support(double now) => groundedAt = now;
        public void Request(double now) => requestedAt = now;
        public void ClearSupport() => groundedAt = double.NegativeInfinity;
        public bool TryConsume(double now, float coyote, float buffer)
        {
            if (now - groundedAt > coyote || now - requestedAt > buffer) return false;
            groundedAt = requestedAt = double.NegativeInfinity;
            return true;
        }
    }
}
