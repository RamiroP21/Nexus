using UnityEngine;

namespace Nexus.Gameplay.Interaction
{
    public readonly struct ValidatedTarget
    {
        public PhysicalTarget Target { get; }
        public Vector3 Point { get; }
        internal ValidatedTarget(PhysicalTarget target, Vector3 point) { Target = target; Point = point; }
    }
    public sealed class ContextualTargeting : MonoBehaviour
    {
        [SerializeField] private TargetingSettings settings;
        [SerializeField] private Camera view;
        [SerializeField] private Transform origin, owner;
        private readonly Collider[] candidates = new Collider[128];
        private readonly RaycastHit[] hits = new RaycastHit[64];
        public PhysicalTarget Current { get; private set; }
        public TargetingSettings Settings => settings;
        public Vector3 Origin => origin.position;
        public bool Suspended { get; set; }
        private void Awake()
        {
            if (!settings || !settings.IsValid || !view || !origin || !owner)
            { Debug.LogError("ContextualTargeting requires settings, camera, origin and owner.", this); enabled = false; }
        }
        public bool TrySelect(out ValidatedTarget selection)
        {
            selection = default; Current = null;
            if (!isActiveAndEnabled || Suspended) return false;
            float best = float.NegativeInfinity;
            int count = Physics.OverlapSphereNonAlloc(origin.position, settings.Range, candidates, settings.QueryMask, QueryTriggerInteraction.Ignore);
            // Fail closed rather than choose from an arbitrary truncated subset.
            if (count == candidates.Length) return false;
            for (int i = 0; i < count; i++)
            {
                if (!candidates[i].attachedRigidbody || !candidates[i].attachedRigidbody.TryGetComponent<PhysicalTarget>(out var target)) continue;
                if (!TryValidate(target, out var valid, out float score) || score <= best) continue;
                best = score; selection = valid; Current = target;
            }
            return Current;
        }
        public bool TryValidate(PhysicalTarget target, out ValidatedTarget selection) => TryValidate(target, out selection, out _);
        private bool TryValidate(PhysicalTarget target, out ValidatedTarget selection, out float score)
        {
            selection = default; score = float.NegativeInfinity;
            if (!isActiveAndEnabled || Suspended || !target || !target.CanReceiveForce || target.transform.IsChildOf(owner)) return false;
            Vector3 point = target.AimPoint;
            float distance = Vector3.Distance(origin.position, point);
            if (distance > settings.Range || distance < .05f) return false;
            Vector3 fromView = point - view.transform.position;
            float alignment = Vector3.Dot(view.transform.forward, fromView.normalized);
            if (alignment < Mathf.Cos(settings.HalfAngle * Mathf.Deg2Rad)) return false;
            if (Occluded(view.transform.position, point, target) || Occluded(origin.position, point, target)) return false;
            score = alignment - settings.DistanceWeight * distance / settings.Range;
            selection = new ValidatedTarget(target, point);
            return true;
        }
        private bool Occluded(Vector3 start, Vector3 end, PhysicalTarget target)
        {
            Vector3 offset = end - start;
            int count = Physics.RaycastNonAlloc(start, offset.normalized, hits, offset.magnitude, settings.QueryMask, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return true;
            for (int i = 0; i < count; i++)
                if (!hits[i].transform.IsChildOf(owner) && hits[i].rigidbody != target.Body) return true;
            return false;
        }
        private void OnDisable() => Current = null;
    }
}
