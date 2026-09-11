using UnityEngine;

namespace Nexus.Gameplay.Interaction
{
    [CreateAssetMenu(menuName = "Nexus/Gameplay/Targeting")]
    public sealed class TargetingSettings : ScriptableObject
    {
        [SerializeField, Min(.1f)] private float range = 24;
        [SerializeField, Range(1, 45)] private float halfAngle = 14;
        [SerializeField, Range(0, 1)] private float distanceWeight = .12f;
        [SerializeField, Range(.001f, .1f)] private float centreTolerance = .025f;
        [SerializeField, Range(.001f, .2f)] private float assistTolerance = .08f;
        [SerializeField, Range(.001f, .25f)] private float releaseTolerance = .1f;
        [SerializeField, Range(0, .03f)] private float maximumSizeAllowance = .015f;
        [SerializeField, Range(0, 1)] private float sizeAssistance = .25f;
        [SerializeField, Range(0, .05f)] private float switchMargin = .015f;
        [SerializeField] private LayerMask queryMask = Physics.DefaultRaycastLayers;
        public float Range => range;
        public float HalfAngle => halfAngle;
        public float DistanceWeight => distanceWeight;
        // Radii are fractions of viewport height, so assistance is circular at every aspect ratio.
        public float CentreTolerance => centreTolerance;
        public float AssistTolerance => assistTolerance;
        public float ReleaseTolerance => releaseTolerance;
        public float MaximumSizeAllowance => maximumSizeAllowance;
        public float SizeAssistance => sizeAssistance;
        public float SwitchMargin => switchMargin;
        public int QueryMask => queryMask;
        public bool IsValid => float.IsFinite(range) && range > 0 && halfAngle > 0 && halfAngle < 90
            && distanceWeight >= 0 && distanceWeight < .15f
            && centreTolerance > 0 && centreTolerance <= assistTolerance
            && assistTolerance <= releaseTolerance && releaseTolerance <= .25f
            && maximumSizeAllowance >= 0 && maximumSizeAllowance <= .03f
            && sizeAssistance >= 0 && sizeAssistance <= 1 && switchMargin >= 0 && switchMargin < assistTolerance;
    }
}
