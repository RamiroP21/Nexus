using Nexus.Gameplay.Abilities;
using UnityEngine;

namespace Nexus.Gameplay.Interaction
{
    public enum ReticleState { Neutral, Candidate, Activation }

    // Runs after the production camera's LateUpdate; selection describes the rendered camera.
    [DefaultExecutionOrder(100)]
    public sealed class TargetFeedback : MonoBehaviour
    {
        [SerializeField] private ContextualTargeting targeting;
        [SerializeField] private Camera view;
        [SerializeField, Range(2, 8)] private float radiusPixels = 4;
        [SerializeField, Range(.05f, .3f)] private float activationDuration = .12f;
        private KineticVectorAbility ability;
        private double activationUntil;
        public bool Visible => isActiveAndEnabled && targeting && targeting.isActiveAndEnabled
            && !targeting.Suspended && view && view.isActiveAndEnabled && Time.timeScale > 0;
        public ReticleState State => !Visible ? ReticleState.Neutral
            : Time.timeAsDouble < activationUntil ? ReticleState.Activation
            : targeting.Current ? ReticleState.Candidate : ReticleState.Neutral;
        private void Awake()
        {
            if (!targeting || !view) { Debug.LogError("TargetFeedback requires targeting and camera.", this); enabled = false; return; }
            ability = targeting.GetComponent<KineticVectorAbility>();
        }
        private void OnEnable()
        {
            Clear();
            if (targeting) targeting.Cleared += Clear;
            if (ability) ability.Executed += OnExecuted;
        }
        private void LateUpdate()
        {
            if (!Visible) { Clear(); return; }
            targeting.TrySelect(out _);
        }
        private void OnExecuted(ForceImpact impact)
        { if (Visible) activationUntil = Time.timeAsDouble + activationDuration; }
        private void Clear() => activationUntil = 0;
        private void OnDisable()
        {
            if (targeting) targeting.Cleared -= Clear;
            if (ability) ability.Executed -= OnExecuted;
            Clear();
        }
        private void OnGUI()
        {
            if (!Visible || Event.current.type != EventType.Repaint) return;
            var rect = view.pixelRect;
            float x = rect.center.x, y = Screen.height - rect.center.y;
            var state = State;
            float radius = radiusPixels + (state == ReticleState.Activation ? 2 : 0);
            Color previous = GUI.color;
            GUI.color = new Color(0, 0, 0, .7f);
            GUI.DrawTexture(new Rect(x - radius - 1, y - 2, radius * 2 + 2, 4), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 2, y - radius - 1, 4, radius * 2 + 2), Texture2D.whiteTexture);
            GUI.color = state == ReticleState.Neutral ? new Color(.85f, .88f, .9f, .7f)
                : state == ReticleState.Candidate ? new Color(.45f, .95f, 1, .95f) : Color.white;
            GUI.DrawTexture(new Rect(x - radius, y - 1, radius * 2, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 1, y - radius, 2, radius * 2), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
