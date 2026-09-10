using System;
using UnityEngine;

namespace Nexus.Gameplay.Interaction
{
    public readonly struct ForceImpact
    {
        public Vector3 Impulse { get; }
        public Vector3 Point { get; }
        public ForceImpact(Vector3 impulse, Vector3 point) { Impulse = impulse; Point = point; }
    }
    public interface IForceReceiver
    {
        bool CanReceiveForce { get; }
        bool ReceiveForce(ForceImpact impact);
    }
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class PhysicalTarget : MonoBehaviour, IForceReceiver
    {
        [SerializeField, Min(0)] private float collisionReactionSpeed = 2;
        private Rigidbody body;
        private Collider shape;
        public event Action<ForceImpact> Impacted;
        public Rigidbody Body => body ? body : body = GetComponent<Rigidbody>();
        public Collider Shape => shape ? shape : shape = GetComponent<Collider>();
        public Vector3 AimPoint => Shape.bounds.center;
        public bool CanReceiveForce => isActiveAndEnabled && Shape.enabled && !Shape.isTrigger && !Body.isKinematic;
        private void Awake() { body = GetComponent<Rigidbody>(); shape = GetComponent<Collider>(); }
        public bool ReceiveForce(ForceImpact impact)
        {
            if (!CanReceiveForce || !Finite(impact.Impulse) || !Finite(impact.Point)) return false;
            body.AddForceAtPosition(impact.Impulse, impact.Point, ForceMode.Impulse);
            Impacted?.Invoke(impact);
            return true;
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!CanReceiveForce || collision.contactCount == 0) return;
            var contact = collision.GetContact(0);
            if (Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal)) < collisionReactionSpeed) return;
            // Physics already applied the contact impulse. Notify observers without applying it twice.
            Impacted?.Invoke(new ForceImpact(collision.impulse, contact.point));
        }
        private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
