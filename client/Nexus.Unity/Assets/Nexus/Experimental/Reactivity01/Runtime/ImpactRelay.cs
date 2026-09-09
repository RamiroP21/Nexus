using UnityEngine;

namespace Nexus.Reactivity01
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ImpactRelay : MonoBehaviour
    {
        [SerializeField] private ReactionStage stage;
        private Rigidbody body;
        private float readyAt;
        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (!stage) { Debug.LogError("Impact relay needs its local stage.", this); enabled = false; }
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!enabled || Time.time < readyAt || collision.contactCount == 0) return;
            float speed = collision.relativeVelocity.magnitude;
            if (speed < stage.Settings.impactSpeedThreshold) return;
            readyAt = Time.time + stage.Settings.impactCooldown;
            stage.Report(StimulusKind.PhysicalImpact, collision.GetContact(0).point, speed, body);
        }
    }
}
