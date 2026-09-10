using Nexus.Feel01;
using Nexus.Reactivity01;
using UnityEngine;

namespace Nexus.SessionLoop01
{
    public enum MarketOutcome { Dormant, Active, Protected, Damaged }
    public enum AccessOutcome { Dormant, Active, Cleared }

    // EXPERIMENTAL: two scene-local situations and their in-session outcomes, not a quest scheduler.
    public sealed class SessionLoopDirector : MonoBehaviour
    {
        [SerializeField] private FeelArena arena;
        [SerializeField] private FeelPlayer player;
        [SerializeField] private KineticPulse pulse;
        [SerializeField] private ReactiveActor threat, worker, resident;
        [SerializeField] private Transform marketPoint, residentialPoint;
        [SerializeField] private GameObject intactStall, brokenStall, channelRail;
        [SerializeField] private Collider delivery;
        [SerializeField] private Light marketSignal, accessSignal;
        private float resetAt, closedAt;
        public MarketOutcome Market { get; private set; }
        public AccessOutcome Access { get; private set; }
        public bool FirstComplete => Market == MarketOutcome.Protected || Market == MarketOutcome.Damaged;
        public bool DamageVisible => brokenStall.activeSelf;
        public bool ProtectionVisible => channelRail.activeSelf;
        public ReactiveActor Threat => threat;
        public Rigidbody Delivery => delivery.attachedRigidbody;
        public Vector3 ResidentialCenter => residentialPoint.position;
        private void Awake()
        {
            if (!arena || !player || !pulse || !threat || !worker || !resident || !marketPoint || !residentialPoint
                || !intactStall || !brokenStall || !channelRail || !delivery || !marketSignal || !accessSignal)
            { Debug.LogError("SessionLoop01 scene references missing.", this); enabled = false; return; }
            threat.enabled = false;
        }
        private void OnEnable() { if (arena) arena.ResetPerformed += ResetSession; }
        private void OnDisable() { if (arena) arena.ResetPerformed -= ResetSession; }
        private void Start() => ResetSession();
        private void ResetSession()
        {
            resetAt = Time.time; closedAt = -1;
            Market = MarketOutcome.Dormant; Access = AccessOutcome.Dormant;
            intactStall.SetActive(true); brokenStall.SetActive(false); channelRail.SetActive(false);
            foreach (var actor in new[] { threat, worker, resident })
            {
                actor.gameObject.SetActive(true); actor.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                actor.enabled = true; actor.ResetReaction();
            }
            threat.enabled = false; pulse.ResetPulse();
            marketSignal.color = Color.white; marketSignal.intensity = .3f;
            accessSignal.color = Color.white; accessSignal.intensity = .3f;
        }
        private void FixedUpdate()
        {
            if (player.Paused) return;
            if (Market == MarketOutcome.Dormant && Time.time - resetAt > 3 && Near(marketPoint.position, 12))
            { Market = MarketOutcome.Active; threat.enabled = true; marketSignal.color = Color.red; }
            if (Market == MarketOutcome.Active)
            {
                marketSignal.intensity = 1 + Mathf.Sin(Time.time * 8) * .5f;
                if (threat.Position.y < -1.5f) CloseMarket(MarketOutcome.Protected);
                else if (Vector3.ProjectOnPlane(threat.Position - marketPoint.position, Vector3.up).magnitude < 2.2f)
                    CloseMarket(MarketOutcome.Damaged);
            }
            // Both elapsed breathing time AND travel are required. Finishing A alone cannot start B.
            if (Access == AccessOutcome.Dormant && FirstComplete && Time.time - closedAt >= 5 && Near(residentialPoint.position, 10))
            { Access = AccessOutcome.Active; accessSignal.color = new Color(1, .65f, .1f); resident.enabled = false; }
            if (Access == AccessOutcome.Active)
            {
                accessSignal.intensity = 1 + Mathf.Sin(Time.time * 4) * .3f;
                var doorway = new Bounds(new Vector3(0, 1, 40), new Vector3(2.4f, 2.5f, 1));
                if (!delivery.bounds.Intersects(doorway))
                {
                    var body = resident.GetComponent<Rigidbody>();
                    body.MovePosition(Vector3.MoveTowards(body.position, new Vector3(0, 1, 35), 2 * Time.fixedDeltaTime));
                    if (body.position.z < 35.3f) { Access = AccessOutcome.Cleared; accessSignal.color = Color.green; accessSignal.intensity = .6f; }
                }
            }
        }
        private bool Near(Vector3 point, float radius) => Vector3.ProjectOnPlane(player.transform.position - point, Vector3.up).sqrMagnitude < radius * radius;
        private void CloseMarket(MarketOutcome outcome)
        {
            Market = outcome; closedAt = Time.time; threat.enabled = false;
            var body = threat.GetComponent<Rigidbody>(); body.linearVelocity = body.angularVelocity = Vector3.zero; body.constraints = RigidbodyConstraints.FreezeAll;
            bool damaged = outcome == MarketOutcome.Damaged;
            intactStall.SetActive(!damaged); brokenStall.SetActive(damaged); channelRail.SetActive(!damaged);
            if (damaged) worker.React(StimulusKind.PhysicalImpact, marketPoint.position, body);
            marketSignal.color = damaged ? new Color(1, .55f, .1f) : Color.green; marketSignal.intensity = .6f;
        }
    }
}
