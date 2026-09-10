using UnityEngine;

namespace Nexus.Gameplay.Interaction
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TargetFeedback : MonoBehaviour
    {
        [SerializeField] private ContextualTargeting targeting;
        [SerializeField] private Camera view;
        [SerializeField, Min(.01f)] private float sizePadding = .15f;
        private LineRenderer ring;
        public bool Visible => ring && ring.enabled;
        private void Awake()
        {
            ring = GetComponent<LineRenderer>(); ring.enabled = false;
            if (!targeting || !view) { Debug.LogError("TargetFeedback requires targeting and camera.", this); enabled = false; return; }
            ring.positionCount = 24; ring.loop = true; ring.useWorldSpace = true;
        }
        private void LateUpdate()
        {
            ring.enabled = targeting.TrySelect(out var selection);
            if (!ring.enabled) return;
            var bounds = selection.Target.Shape.bounds;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.y) + sizePadding;
            Vector3 centre = selection.Point - view.transform.forward * (bounds.extents.magnitude + .05f);
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2 / ring.positionCount;
                ring.SetPosition(i, centre + radius * (view.transform.right * Mathf.Cos(angle) + view.transform.up * Mathf.Sin(angle)));
            }
        }
        private void OnDisable() { if (ring) ring.enabled = false; }
    }
}
