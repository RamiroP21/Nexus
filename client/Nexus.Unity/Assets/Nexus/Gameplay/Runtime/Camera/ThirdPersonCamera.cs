using UnityEngine;

namespace Nexus.Gameplay.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private CameraSettings settings;
        [SerializeField] private Transform target, visual;
        private readonly RaycastHit[] hits = new RaycastHit[32];
        private readonly Collider[] overlaps = new Collider[32];
        private Renderer[] renderers;
        private Vector3 pivot;
        public float Yaw { get; private set; }
        public float Pitch { get; private set; }
        public float CurrentDistance { get; private set; }
        public CameraSettings Settings => settings;
        public Camera View { get; private set; }
        private void Awake()
        {
            View = GetComponent<Camera>();
            if (!settings || !settings.IsValid || !target || !visual)
            { Debug.LogError("ThirdPersonCamera requires valid settings, target and visual root.", this); enabled = false; return; }
            renderers = visual.GetComponentsInChildren<Renderer>();
            pivot = target.position + Vector3.up * settings.PivotHeight;
            Yaw = target.eulerAngles.y; Pitch = settings.InitialPitch;
            Place(0);
        }
        public void Look(Vector2 delta, bool mouse, float dt)
        {
            if (!isActiveAndEnabled) return;
            float scale = mouse ? settings.MouseSensitivity : settings.StickSensitivity * dt;
            Yaw = Mathf.Repeat(Yaw + delta.x * scale, 360);
            Pitch = Mathf.Clamp(Pitch - delta.y * scale, settings.MinimumPitch, settings.MaximumPitch);
        }
        private void LateUpdate() { if (Time.timeScale > 0) Place(Time.deltaTime); }
        private void Place(float dt)
        {
            Vector3 anchor = target.position + Vector3.up * settings.PivotHeight;
            Vector3 desiredPivot = anchor + Quaternion.Euler(0, Yaw, 0) * Vector3.right * settings.ShoulderOffset;
            pivot = Vector3.Lerp(pivot, desiredPivot, dt <= 0 || settings.FollowSharpness == 0 ? 1 : 1 - Mathf.Exp(-settings.FollowSharpness * dt));
            // Sweep the shoulder/smoothing offset too: the pivot must remain on the player's side of cover.
            Vector3 offset = pivot - anchor;
            if (offset.sqrMagnitude > .0001f) pivot = anchor + offset.normalized * ClearDistance(anchor, offset.normalized, offset.magnitude);
            if (Blocked(pivot)) pivot = anchor;
            Quaternion rotation = Quaternion.Euler(Pitch, Yaw, 0);
            Vector3 backward = rotation * Vector3.back;
            float distance = ClearDistance(pivot, backward, settings.Distance);
            // SphereCast does not report colliders already overlapping its origin.
            while (distance > settings.CollisionPadding && Blocked(pivot + backward * distance)) distance *= .5f;
            CurrentDistance = distance;
            transform.SetPositionAndRotation(pivot + backward * distance, rotation);
            foreach (var renderer in renderers) if (renderer) renderer.forceRenderingOff = distance < settings.HideBodyDistance;
            View.fieldOfView = settings.FieldOfView;
        }
        private float ClearDistance(Vector3 start, Vector3 direction, float distance)
        {
            int count = Physics.SphereCastNonAlloc(start, settings.Radius, direction, hits, distance, settings.CollisionMask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return 0;
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(target)) distance = Mathf.Min(distance, Mathf.Max(0, hits[i].distance - settings.CollisionPadding));
            return distance;
        }
        private bool Blocked(Vector3 point)
        {
            int count = Physics.OverlapSphereNonAlloc(point, settings.Radius, overlaps, settings.CollisionMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return true;
            for (int i = 0; i < count; i++) if (!overlaps[i].transform.IsChildOf(target)) return true;
            return false;
        }
        private void OnDisable()
        { if (renderers != null) foreach (var renderer in renderers) if (renderer) renderer.forceRenderingOff = false; }
    }
}
