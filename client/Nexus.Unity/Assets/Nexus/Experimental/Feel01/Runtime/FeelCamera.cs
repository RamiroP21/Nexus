using UnityEngine;

namespace Nexus.Feel01
{
    [RequireComponent(typeof(Camera))]
    public sealed class FeelCamera : MonoBehaviour
    {
        [SerializeField] private FeelSettings settings;
        [SerializeField] private Transform target;
        [SerializeField] private Transform body;
        private Renderer[] bodyRenderers;
        private Camera view;
        private Vector3 pivot;
        private float yaw;
        private float pitch = 12;
        public float Yaw => yaw;
        public float Pitch => pitch;
        public Ray AimRay => new Ray(transform.position, Quaternion.Euler(pitch, yaw, 0) * Vector3.forward);

        private void Awake()
        {
            view = GetComponent<Camera>();
            if (!settings || !target || !body) { Debug.LogError("Feel camera references missing.", this); enabled = false; return; }
            bodyRenderers = body.GetComponentsInChildren<Renderer>();
            Snap();
        }

        private void OnDisable()
        {
            if (bodyRenderers != null)
                foreach (var renderer in bodyRenderers) if (renderer) renderer.forceRenderingOff = false;
        }

        public void Look(Vector2 look, bool mouse, float deltaTime)
        {
            float scale = mouse ? settings.mouseSensitivity : settings.gamepadSensitivity * deltaTime;
            yaw = Mathf.Repeat(yaw + look.x * scale, 360);
            pitch = Mathf.Clamp(pitch - look.y * scale, settings.minimumPitch, settings.maximumPitch);
        }

        public void Snap()
        {
            pivot = target.position + Vector3.up * settings.pivotHeight;
            yaw = target.eulerAngles.y;
            pitch = 12;
            PlaceCamera(0);
        }

        private void LateUpdate()
        {
            if (Time.timeScale > 0) PlaceCamera(Time.deltaTime);
        }

        private void PlaceCamera(float dt)
        {
            var desiredPivot = target.position + Vector3.up * settings.pivotHeight;
            float blend = dt <= 0 || settings.followSharpness <= 0 ? 1 : 1 - Mathf.Exp(-settings.followSharpness * dt);
            pivot = Vector3.Lerp(pivot, desiredPivot, blend);
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            var backward = rotation * Vector3.back;
            float distance = settings.cameraDistance;
            if (Physics.SphereCast(pivot, settings.cameraRadius, backward, out var hit, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(.05f, hit.distance - .05f);
            transform.SetPositionAndRotation(pivot + backward * distance, rotation);
            // A close obstruction must not turn the player's capsule into a fullscreen occluder.
            foreach (var renderer in bodyRenderers) renderer.forceRenderingOff = distance < 1.3f;
            view.fieldOfView = settings.fieldOfView;
        }
    }
}
