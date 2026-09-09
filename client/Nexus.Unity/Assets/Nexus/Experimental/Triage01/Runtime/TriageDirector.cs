using Nexus.Feel01;
using Nexus.Reactivity01;
using UnityEngine;

namespace Nexus.Triage01
{
    public enum CrisisState { Active, Escalating, Resolved, Consequence }

    // Local experiment, not a quest system. Outcomes follow physical contact and occupied space.
    public sealed class TriageDirector : MonoBehaviour
    {
        [SerializeField] private FeelArena arena;
        [SerializeField] private KineticPulse pulse;
        [SerializeField] private Rigidbody cargo;
        [SerializeField] private ReactiveActor civil, threat;
        [SerializeField] private Renderer rescueZone;
        [SerializeField] private Renderer[] territory;
        [SerializeField] private TextMesh rescueSign, containmentSign;
        [SerializeField] private float cargoSpeed = 4;
        private MaterialPropertyBlock tint;
        private bool[] occupied;
        public CrisisState Rescue { get; private set; }
        public CrisisState Containment { get; private set; }
        public int LostGround { get; private set; }
        public float Elapsed { get; private set; }
        public Rigidbody Cargo => cargo;
        public ReactiveActor Civil => civil;
        public ReactiveActor Threat => threat;

        private void Awake()
        {
            if (!arena || !pulse || !cargo || !civil || !threat || !rescueZone || !rescueSign || !containmentSign || territory == null || territory.Length != 3)
            { Debug.LogError("Triage references missing.", this); enabled = false; return; }
            tint = new MaterialPropertyBlock(); occupied = new bool[territory.Length];
        }
        private void OnEnable() { if (arena) arena.ResetPerformed += ResetCrises; }
        private void OnDisable() { if (arena) arena.ResetPerformed -= ResetCrises; }
        private void Start() => ResetCrises();

        private void ResetCrises()
        {
            if (tint == null) return;
            Rescue = Containment = CrisisState.Active; LostGround = 0; Elapsed = 0;
            pulse.ResetPulse();
            cargo.linearVelocity = Vector3.forward * cargoSpeed; cargo.WakeUp();
            for (int i = 0; i < occupied.Length; i++) { occupied[i] = false; Paint(territory[i], new Color(.15f, .65f, .75f)); }
            RefreshSigns();
        }
        private void FixedUpdate()
        {
            Elapsed += Time.fixedDeltaTime;
            if (Rescue != CrisisState.Consequence)
            {
                // A clear lateral miss or passing the civil closes the immediate collision threat.
                // Re-evaluated: pushing the cargo back into the lane can reopen it.
                Vector3 relative = cargo.position - civil.Position;
                bool passed = relative.z > 2;
                bool clear = Mathf.Abs(relative.x) > 3 && Mathf.Sign(relative.x) * cargo.linearVelocity.x >= -.1f;
                Rescue = passed || clear ? CrisisState.Resolved : relative.z > -6 ? CrisisState.Escalating : CrisisState.Active;
            }
            for (int i = 0; i < territory.Length; i++)
                if (!occupied[i] && threat.Position.y > 0 && Mathf.Abs(threat.Position.x - territory[i].transform.position.x) <= territory[i].bounds.extents.x
                    && threat.Position.z >= territory[i].transform.position.z)
                {
                    occupied[i] = true; LostGround++; Paint(territory[i], new Color(.85f, .12f, .07f));
                }
            // The open service trench contains the body through geometry, not by disabling its AI.
            Containment = threat.Position.y < -1.5f ? CrisisState.Resolved
                : LostGround == territory.Length ? CrisisState.Consequence : LostGround > 0 ? CrisisState.Escalating : CrisisState.Active;
            RefreshSigns();
        }
        public void CargoContact(Rigidbody other)
        {
            if (other != civil.GetComponent<Rigidbody>()) return;
            Rescue = CrisisState.Consequence;
            civil.React(StimulusKind.PhysicalImpact, cargo.position, cargo);
            RefreshSigns();
        }
        private void RefreshSigns()
        {
            Paint(rescueZone, Rescue == CrisisState.Consequence ? Color.red : Rescue == CrisisState.Resolved ? Color.green
                : Rescue == CrisisState.Escalating ? new Color(1, .45f, .05f) : Color.yellow);
            rescueSign.text = Rescue == CrisisState.Consequence ? "RESCUE / CIVIL STRUCK"
                : Rescue == CrisisState.Resolved ? "RESCUE / PATH CLEAR" : "RESCUE / RUNAWAY CARGO";
            containmentSign.text = Containment == CrisisState.Resolved ? (LostGround > 0 ? "CONTAINED / GROUND LOST" : "THREAT CONTAINED")
                : LostGround == territory.Length ? "DEPOT OVERRUN / CONTAIN THREAT" : "THREAT ADVANCING / PROTECT DEPOT";
        }
        private void Paint(Renderer target, Color color) { tint.SetColor("_BaseColor", color); target.SetPropertyBlock(tint); }
    }
}
