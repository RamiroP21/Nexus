using UnityEngine;

namespace Nexus.Gameplay.Abilities
{
    [CreateAssetMenu(menuName = "Nexus/Gameplay/Kinetic Grasp")]
    public sealed class KineticGraspSettings : ScriptableObject
    {
        [SerializeField, Min(.1f)] private float acquireRange = 10, holdDistance = 3.5f, breakDistance = 13;
        [SerializeField, Min(.01f)] private float maximumMass = 25, spring = 28, damping = 10, maximumAcceleration = 45;
        [SerializeField] private bool requireLineOfSight = true;
        public float AcquireRange => acquireRange;
        public float HoldDistance => holdDistance;
        public float BreakDistance => breakDistance;
        public float MaximumMass => maximumMass;
        public float Spring => spring;
        public float Damping => damping;
        public float MaximumAcceleration => maximumAcceleration;
        public bool RequireLineOfSight => requireLineOfSight;
        public bool IsValid => Positive(acquireRange) && Positive(holdDistance) && Positive(breakDistance)
            && breakDistance >= acquireRange && breakDistance > holdDistance && Positive(maximumMass)
            && Positive(spring) && Positive(damping) && Positive(maximumAcceleration);
        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
    }
}
