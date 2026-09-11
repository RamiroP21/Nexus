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
        public event System.Action Cleared;
        public TargetingSettings Settings => settings;
        public Vector3 Origin => origin ? origin.position : default;
        private bool suspended;
        public bool Suspended
        {
            get => suspended;
            set { suspended = value; if (value) Clear(); }
        }
        private void Awake()
        {
            if (!settings || !settings.IsValid || !view || !origin || !owner)
            { Debug.LogError("ContextualTargeting requires settings, camera, origin and owner.", this); enabled = false; }
        }
        public bool TrySelect(out ValidatedTarget selection)
        {
            selection = default;
            if (!isActiveAndEnabled || Suspended || !view.isActiveAndEnabled || !origin || !owner)
            { Current = null; return false; }

            // The actual centre ray has priority over every soft-assist score and retained candidate.
            var ray = view.ViewportPointToRay(new Vector3(.5f, .5f));
            int hitCount = Physics.RaycastNonAlloc(ray, hits, settings.Range + Vector3.Distance(view.transform.position, Origin), settings.QueryMask, QueryTriggerInteraction.Ignore);
            if (hitCount == hits.Length) { Current = null; return false; }
            Collider nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].transform.IsChildOf(owner) || hits[i].distance >= nearestDistance) continue;
                nearest = hits[i].collider; nearestDistance = hits[i].distance;
            }
            if (nearest && nearest.attachedRigidbody && nearest.attachedRigidbody.TryGetComponent<PhysicalTarget>(out var direct)
                && ValidateGeometry(direct, out selection))
            { Current = direct; return true; }

            bool retained = Evaluate(Current, settings.ReleaseTolerance, out var previous);
            Candidate best = default;
            bool found = false;
            int count = Physics.OverlapSphereNonAlloc(origin.position, settings.Range, candidates, settings.QueryMask, QueryTriggerInteraction.Ignore);
            // Fail closed rather than choose from an arbitrary truncated subset.
            if (count == candidates.Length) { Current = null; return false; }
            for (int i = 0; i < count; i++)
            {
                if (!candidates[i].attachedRigidbody || !candidates[i].attachedRigidbody.TryGetComponent<PhysicalTarget>(out var target)) continue;
                if (!Evaluate(target, settings.AssistTolerance, out var candidate)) continue;
                if (!found || Better(candidate, best)) { found = true; best = candidate; }
            }
            if (retained && (!found || best.Selection.Target == Current || previous.Score - best.Score < settings.SwitchMargin))
            { selection = previous.Selection; return true; }
            Current = found ? best.Selection.Target : null;
            if (found) selection = best.Selection;
            return found;
        }
        public bool TryValidate(PhysicalTarget target, out ValidatedTarget selection)
        {
            selection = default;
            if (!ValidateGeometry(target, out var valid)) return false;
            var ray = view.ViewportPointToRay(new Vector3(.5f, .5f));
            if (target.Shape.Raycast(ray, out _, settings.Range + Vector3.Distance(view.transform.position, Origin)))
            { selection = valid; return true; }
            if (!Evaluate(target, target == Current ? settings.ReleaseTolerance : settings.AssistTolerance, out var candidate)) return false;
            selection = candidate.Selection; return true;
        }
        private bool ValidateGeometry(PhysicalTarget target, out ValidatedTarget selection)
        {
            selection = default;
            if (!isActiveAndEnabled || Suspended || !view.isActiveAndEnabled || !origin || !owner
                || !target || !target.CanReceiveForce || target.transform.IsChildOf(owner)) return false;
            if ((settings.QueryMask & (1 << target.Shape.gameObject.layer)) == 0) return false;
            Vector3 point = target.AimPoint;
            float distance = Vector3.Distance(origin.position, point);
            if (distance > settings.Range || distance < .05f) return false;
            Vector3 fromView = point - view.transform.position;
            float alignment = Vector3.Dot(view.transform.forward, fromView.normalized);
            if (alignment < Mathf.Cos(settings.HalfAngle * Mathf.Deg2Rad)) return false;
            if (Occluded(view.transform.position, point, target) || Occluded(origin.position, point, target)) return false;
            selection = new ValidatedTarget(target, point);
            return true;
        }
        private struct Candidate
        {
            public ValidatedTarget Selection;
            public float Score, Radius, Size, Distance;
        }
        private bool Evaluate(PhysicalTarget target, float tolerance, out Candidate candidate)
        {
            candidate = default;
            if (!ValidateGeometry(target, out var selection)) return false;
            Vector3 projected = view.WorldToViewportPoint(selection.Point);
            if (projected.z <= view.nearClipPlane || projected.x < 0 || projected.x > 1 || projected.y < 0 || projected.y > 1) return false;
            float radius = new Vector2((projected.x - .5f) * view.aspect, projected.y - .5f).magnitude;
            var edge = view.WorldToViewportPoint(selection.Point + view.transform.up * target.Shape.bounds.extents.magnitude);
            float size = Mathf.Abs(edge.y - projected.y);
            float allowance = Mathf.Min(settings.MaximumSizeAllowance, size * settings.SizeAssistance);
            if (radius > tolerance + allowance) return false;
            float distance = Vector3.Distance(Origin, selection.Point);
            // Size only extends eligibility. It never improves a target's centre score.
            candidate = new Candidate { Selection = selection, Radius = radius, Size = Mathf.Min(size, settings.MaximumSizeAllowance), Distance = distance,
                Score = radius + settings.CentreTolerance * settings.DistanceWeight * distance / settings.Range };
            return true;
        }
        private static bool Better(Candidate candidate, Candidate best)
        {
            const float epsilon = .00001f;
            if (Mathf.Abs(candidate.Score - best.Score) > epsilon) return candidate.Score < best.Score;
            if (Mathf.Abs(candidate.Radius - best.Radius) > epsilon) return candidate.Radius < best.Radius;
            if (Mathf.Abs(candidate.Size - best.Size) > epsilon) return candidate.Size > best.Size;
            if (Mathf.Abs(candidate.Distance - best.Distance) > epsilon) return candidate.Distance < best.Distance;
            return CompareHierarchy(candidate.Selection.Target.transform, best.Selection.Target.transform) < 0;
        }
        // Stable authored sibling order, never physics overlap order or CLR/instance identity.
        private static int CompareHierarchy(Transform a, Transform b)
        {
            if (a == b) return 0;
            if (!a) return -1;
            if (!b) return 1;
            int parent = CompareHierarchy(a.parent, b.parent);
            return parent != 0 ? parent : a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());
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
        private void Clear() { Current = null; Cleared?.Invoke(); }
        private void OnDisable() => Clear();
    }
}
