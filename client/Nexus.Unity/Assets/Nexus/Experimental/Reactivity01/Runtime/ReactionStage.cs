using Nexus.Feel01;
using UnityEngine;

namespace Nexus.Reactivity01
{
    public enum StimulusKind { KineticImpact, PhysicalImpact, Threat }

    // Scene-local fan-out to three actors, not a global bus or persistent event history.
    public sealed class ReactionStage : MonoBehaviour
    {
        [SerializeField] private ReactionSettings settings;
        [SerializeField] private KineticPulse pulse;
        [SerializeField] private FeelArena arena;
        [SerializeField] private ReactiveActor[] actors;
        [SerializeField] private Transform impactMarker;
        private float markerUntil;
        public ReactionSettings Settings => settings;
        public int PhysicalImpactCount { get; private set; }
        public Vector3 LastPhysicalPoint { get; private set; }

        private void Awake()
        {
            if (!settings || !settings.IsValid || !pulse || !arena || !impactMarker || actors == null)
            { Debug.LogError("Reaction stage references/settings invalid.", this); enabled = false; }
        }
        private void OnEnable()
        {
            if (!pulse || !arena) return;
            pulse.Impact += OnKineticImpact;
            arena.ResetPerformed += ResetReactions;
        }
        private void OnDisable()
        {
            if (pulse) pulse.Impact -= OnKineticImpact;
            if (arena) arena.ResetPerformed -= ResetReactions;
        }
        private void OnKineticImpact(Vector3 position, float impulse, Rigidbody body)
            => Report(StimulusKind.KineticImpact, position, impulse, body);

        public void Report(StimulusKind kind, Vector3 position, float intensity, Rigidbody source)
        {
            if (!isActiveAndEnabled || !settings || intensity <= 0 || !float.IsFinite(position.sqrMagnitude)) return;
            if (kind == StimulusKind.PhysicalImpact)
            {
                if (intensity < settings.impactSpeedThreshold) return;
                PhysicalImpactCount++;
                LastPhysicalPoint = position;
            }
            float radius = kind == StimulusKind.Threat ? settings.threatRadius : settings.reactionRadius;
            foreach (var actor in actors)
                if (actor && actor.isActiveAndEnabled && (actor.Position - position).sqrMagnitude <= radius * radius)
                    actor.React(kind, position, source);
            if (kind != StimulusKind.Threat)
            {
                impactMarker.position = position + Vector3.up * .15f;
                markerUntil = Time.time + .25f;
                impactMarker.gameObject.SetActive(true);
            }
        }

        private void Update()
        {
            if (!impactMarker) return;
            bool visible = Time.time < markerUntil;
            impactMarker.gameObject.SetActive(visible);
            if (visible) impactMarker.localScale = Vector3.one * Mathf.Lerp(.1f, .7f, 1 - (markerUntil - Time.time) / .25f);
        }

        private void ResetReactions()
        {
            foreach (var actor in actors) if (actor) actor.ResetReaction();
            markerUntil = 0;
            impactMarker.gameObject.SetActive(false);
            PhysicalImpactCount = 0;
        }
    }
}
