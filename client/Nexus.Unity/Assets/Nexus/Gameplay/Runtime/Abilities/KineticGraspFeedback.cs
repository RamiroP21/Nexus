using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.Abilities
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class KineticGraspFeedback : MonoBehaviour
    {
        [SerializeField] private KineticGraspAbility ability;
        [SerializeField] private Transform muzzle;
        private LineRenderer beam;

        private void Awake()
        {
            beam = GetComponent<LineRenderer>();
            beam.positionCount = 2;
            beam.enabled = false;
            if (!ability || !muzzle)
            { Debug.LogError("KineticGraspFeedback requires ability and muzzle.", this); enabled = false; }
        }
        private void OnEnable()
        {
            if (ability) { ability.Acquired += OnAcquired; ability.Released += OnReleased; }
        }
        private void OnDisable()
        {
            if (ability) { ability.Acquired -= OnAcquired; ability.Released -= OnReleased; }
            if (beam) beam.enabled = false;
        }
        private void Update()
        {
            if (!beam || !ability || !ability.IsHolding || !ability.HeldTarget)
            { if (beam) beam.enabled = false; return; }
            beam.SetPosition(0, muzzle.position); beam.SetPosition(1, ability.HeldTarget.AimPoint); beam.enabled = true;
        }
        private void OnAcquired(PhysicalTarget target) { if (beam) beam.enabled = true; }
        private void OnReleased(GraspReleaseReason reason) { if (beam) beam.enabled = false; }
    }
}
