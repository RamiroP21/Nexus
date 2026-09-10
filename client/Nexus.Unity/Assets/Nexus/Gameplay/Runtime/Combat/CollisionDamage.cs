using UnityEngine;

namespace Nexus.Gameplay.Combat
{
    [RequireComponent(typeof(DamageReceiver), typeof(Rigidbody))]
    public sealed class CollisionDamage : MonoBehaviour
    {
        [SerializeField, Min(0)] private float minimumSpeed = 4;
        [SerializeField, Min(0)] private float damagePerSpeed = 3;
        [SerializeField, Min(.01f)] private float contactCooldown = .2f;
        private DamageReceiver receiver;
        private float readyAt;
        private void Awake()
        {
            receiver = GetComponent<DamageReceiver>();
            if (minimumSpeed < 0 || damagePerSpeed < 0 || contactCooldown <= 0)
            { Debug.LogError("CollisionDamage settings invalid.", this); enabled = false; }
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (!isActiveAndEnabled || Time.time < readyAt || collision.contactCount == 0) return;
            // Normal approach speed ignores harmless tangential sliding; damage is explicitly opt-in.
            float speed = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal));
            if (speed <= minimumSpeed) return;
            readyAt = Time.time + contactCooldown;
            receiver.Receive(new Damage((speed - minimumSpeed) * damagePerSpeed));
        }
    }
}
