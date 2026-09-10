using UnityEngine;

namespace Nexus.Gameplay.Interaction
{
    [CreateAssetMenu(menuName = "Nexus/Gameplay/Targeting")]
    public sealed class TargetingSettings : ScriptableObject
    {
        [SerializeField, Min(.1f)] private float range = 24;
        [SerializeField, Range(1, 45)] private float halfAngle = 14;
        [SerializeField, Range(0, 1)] private float distanceWeight = .12f;
        [SerializeField] private LayerMask queryMask = Physics.DefaultRaycastLayers;
        public float Range => range;
        public float HalfAngle => halfAngle;
        public float DistanceWeight => distanceWeight;
        public int QueryMask => queryMask;
        public bool IsValid => range > 0 && halfAngle > 0 && halfAngle < 90 && distanceWeight >= 0 && distanceWeight <= 1;
    }
}
