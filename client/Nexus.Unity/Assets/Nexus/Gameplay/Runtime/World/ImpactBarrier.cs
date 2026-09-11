using Nexus.Gameplay.Interaction;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    [RequireComponent(typeof(PhysicalTarget))]
    public sealed class ImpactBarrier : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float releaseImpulse = 8;
        private PhysicalTarget target;
        public bool Released { get; private set; }
        private void Awake() => target = GetComponent<PhysicalTarget>();
        private void OnEnable() { if (target) target.Impacted += OnImpact; }
        private void OnDisable() { if (target) target.Impacted -= OnImpact; }
        private void OnImpact(ForceImpact impact)
        {
            if (Released || impact.Impulse.magnitude < releaseImpulse) return;
            Released = true;
            target.Body.constraints = RigidbodyConstraints.None;
            // The incoming impulse already supplies the force; release only the physical support.
        }
        public void ResetBarrier() => Released = false;
    }
}
