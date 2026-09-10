using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.Abilities
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class KineticFeedback : MonoBehaviour
    {
        [SerializeField] private KineticVectorAbility ability;
        [SerializeField] private Transform muzzle;
        private LineRenderer beam;
        private float until;
        private void Awake()
        {
            beam = GetComponent<LineRenderer>(); beam.enabled = false; beam.positionCount = 2;
            if (!ability || !muzzle) { Debug.LogError("KineticFeedback requires ability and muzzle.", this); enabled = false; }
        }
        private void OnEnable() { if (ability) ability.Executed += OnExecuted; }
        private void OnDisable() { if (ability) ability.Executed -= OnExecuted; if (beam) beam.enabled = false; }
        private void OnExecuted(ForceImpact impact)
        {
            beam.SetPosition(0, muzzle.position); beam.SetPosition(1, impact.Point);
            until = Time.time + ability.Settings.FeedbackDuration; beam.enabled = true;
        }
        private void Update() => beam.enabled = Time.time < until;
    }
}
