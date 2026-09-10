using UnityEngine;

namespace Nexus.Gameplay.CameraSystem
{
    [CreateAssetMenu(menuName = "Nexus/Gameplay/Camera")]
    public sealed class CameraSettings : ScriptableObject
    {
        [SerializeField, Min(.1f)] private float distance = 5.8f, radius = .22f, pivotHeight = 1.55f;
        [SerializeField, Range(45, 100)] private float fieldOfView = 72;
        [SerializeField, Min(.01f)] private float mouseSensitivity = .12f, stickSensitivity = 150;
        [SerializeField, Min(0)] private float followSharpness = 12;
        [SerializeField] private float minimumPitch = -55, maximumPitch = 75, initialPitch = 12;
        [SerializeField, Min(.01f)] private float collisionPadding = .05f, hideBodyDistance = 1.3f;
        [SerializeField, Range(-1.5f, 1.5f)] private float shoulderOffset = 1;
        [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;
        public float Distance => distance;
        public float Radius => radius;
        public float PivotHeight => pivotHeight;
        public float FieldOfView => fieldOfView;
        public float MouseSensitivity => mouseSensitivity;
        public float StickSensitivity => stickSensitivity;
        public float FollowSharpness => followSharpness;
        public float MinimumPitch => minimumPitch;
        public float MaximumPitch => maximumPitch;
        public float InitialPitch => initialPitch;
        public float CollisionPadding => collisionPadding;
        public float HideBodyDistance => hideBodyDistance;
        public float ShoulderOffset => shoulderOffset;
        public int CollisionMask => collisionMask;
        public bool IsValid => distance > radius && radius > 0 && pivotHeight > 0 && minimumPitch < maximumPitch
            && initialPitch >= minimumPitch && initialPitch <= maximumPitch && fieldOfView >= 45 && fieldOfView <= 100
            && mouseSensitivity > 0 && stickSensitivity > 0 && followSharpness >= 0 && collisionPadding > 0 && hideBodyDistance > 0
            && float.IsFinite(shoulderOffset) && Mathf.Abs(shoulderOffset) <= 1.5f;
    }
}
