using UnityEngine;
using Nexus.Gameplay.Interaction;

namespace Nexus.Gameplay.Combat
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class TrainingHazard : MonoBehaviour
    {
        [SerializeField, Min(0)] private float damage = 12, knockback = 7;
        [SerializeField, Min(.1f)] private float lifetime = 5;
        private Rigidbody body;
        private GameObject owner;
        private Vector3 previous;
        private float remaining;
        private bool spent;
        private PhysicalTarget physical;
        private SphereCollider shape;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private readonly Collider[] contacts = new Collider[8];
        public bool Spent => spent;
        private void Awake() { physical = GetComponent<PhysicalTarget>(); shape = GetComponent<SphereCollider>(); }
        private void OnEnable() { if (physical) physical.Impacted += OnImpact; }
        private void OnDisable() { if (physical) physical.Impacted -= OnImpact; }
        private void OnImpact(ForceImpact impact) { if (impact.Impulse.sqrMagnitude > 1) owner = null; }
        public void Launch(GameObject source, Vector3 velocity)
        {
            owner = source; body = GetComponent<Rigidbody>(); previous = transform.position;
            remaining = lifetime; spent = false; body.linearVelocity = velocity;
        }
        private void FixedUpdate()
        {
            if (!body) return;
            remaining -= Time.fixedDeltaTime;
            if (remaining <= 0) { Destroy(gameObject); return; }
            // A swept player contact also covers a stationary CharacterController, which is not a Rigidbody.
            Vector3 travel = body.position - previous;
            float distance = travel.magnitude;
            if (!spent && distance > .0001f)
            {
                int count = Physics.SphereCastNonAlloc(previous, shape.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z), travel / distance, hits, distance, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var hit = hits[i].collider;
                    if (hit.GetComponent<PlayerVitality>() || hit.GetComponent<DamageReceiver>()) TryHit(hit.gameObject);
                }
            }
            if (!spent)
            {
                int contactCount = Physics.OverlapSphereNonAlloc(body.position, shape.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z), contacts, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < contactCount; i++)
                {
                    var contact = contacts[i];
                    if (contact.GetComponent<PlayerVitality>() || contact.GetComponent<DamageReceiver>()) TryHit(contact.gameObject);
                }
            }
            previous = body.position;
        }
        public bool TryHit(GameObject other)
        {
            if (spent || !other || other == owner || !body) return false;
            var player = other.GetComponent<PlayerVitality>();
            bool accepted = player ? player.ReceiveHit(new Damage(damage), body.linearVelocity.normalized * knockback + Vector3.up * 2)
                : other.TryGetComponent<DamageReceiver>(out var receiver) && receiver.Receive(new Damage(damage));
            if (accepted) spent = true;
            return accepted;
        }
        private void OnCollisionEnter(Collision collision) => TryHit(collision.gameObject);
    }
}
