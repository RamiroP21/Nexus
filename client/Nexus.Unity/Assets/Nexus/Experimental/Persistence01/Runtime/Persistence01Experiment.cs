using Nexus.Feel01;
using Nexus.Reactivity01;
using Nexus.Triage01;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus.Persistence01
{
    public sealed class Persistence01Experiment : MonoBehaviour
    {
        [SerializeField] private TriageDirector triage;
        [SerializeField] private ReactionStage reactions;
        [SerializeField] private FeelArena arena;
        [SerializeField] private FeelPlayer player;
        [SerializeField] private Rigidbody[] bodies;
        [SerializeField] private GameObject rescueDamage;
        [SerializeField] private GameObject[] depotDamage;
        [SerializeField] private Renderer rescueLane;
        [SerializeField] private Renderer[] territory;
        [SerializeField] private TextMesh rescueSign, depotSign, directionSign;
        private Persistence01StateStore store;
        private MaterialPropertyBlock tint;
        private float stableSince = -1;
        private RescueOutcome candidateRescue;
        private DepotOutcome candidateDepot;
        private int candidateGround;
        public Persistence01State Memory { get; private set; }
        public bool Aftermath { get; private set; }
        public bool SaveFailed { get; private set; }
        public bool RescueDamageVisible => rescueDamage.activeSelf;
        public int VisibleDepotDamage { get; private set; }
        public string MemoryPath => store.FilePath;

        private void Awake()
        {
            if (!triage || !reactions || !arena || !player || !rescueDamage || !rescueLane || !rescueSign || !depotSign || !directionSign
                || bodies == null || bodies.Length != 4 || depotDamage == null || depotDamage.Length != 3 || territory == null || territory.Length != 3)
            { Debug.LogError("Persistence01 references missing.", this); enabled = false; return; }
            tint = new MaterialPropertyBlock();
            store = new Persistence01StateStore(Persistence01StateStore.RuntimePath, Debug.LogWarning);
            Memory = store.Load();
            if (Memory != null) StopCrisis();
        }
        private void OnEnable() { if (arena) arena.ResetPerformed += OnReset; }
        private void OnDisable() { if (arena) arena.ResetPerformed -= OnReset; }
        private void Start() { if (Memory != null) ShowAftermath(); }

        private void Update()
        {
            if (Memory != null || SaveFailed || player.Paused) return;
            var rescue = triage.Rescue == CrisisState.Consequence ? RescueOutcome.Hit
                : triage.Rescue == CrisisState.Resolved ? RescueOutcome.Protected : RescueOutcome.Unknown;
            var depot = triage.Containment == CrisisState.Resolved ? DepotOutcome.Contained
                : triage.Containment == CrisisState.Consequence ? DepotOutcome.Overrun : DepotOutcome.Unknown;
            if (rescue == RescueOutcome.Unknown || depot == DepotOutcome.Unknown) { stableSince = -1; return; }
            if (stableSince < 0 || rescue != candidateRescue || depot != candidateDepot || triage.LostGround != candidateGround)
            {
                candidateRescue = rescue; candidateDepot = depot; candidateGround = triage.LostGround; stableSince = Time.time; return;
            }
            // A settled pair closes this short run; it is not a timer deciding who succeeds.
            if (Time.time - stableSince >= 1) CommitOutcome();
        }
        private void CommitOutcome()
        {
            var state = new Persistence01State { version = 1, rescue = candidateRescue, depot = candidateDepot, lostGround = candidateGround };
            if (!store.Save(state)) { SaveFailed = true; return; }
            Memory = state; StopCrisis(); FreezeBodies();
        }
        private void StopCrisis()
        {
            triage.enabled = false; reactions.enabled = false;
            triage.Civil.enabled = false; triage.Threat.enabled = false;
        }
        private void FreezeBodies()
        {
            foreach (var body in bodies)
            {
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                body.constraints = RigidbodyConstraints.FreezeAll;
                body.interpolation = RigidbodyInterpolation.None;
            }
        }
        private void OnReset()
        {
            if (Memory != null) ShowAftermath();
            else { stableSince = -1; SaveFailed = false; }
        }
        private void ShowAftermath()
        {
            Aftermath = true; StopCrisis(); FreezeBodies();
            bool hit = Memory.rescue == RescueOutcome.Hit;
            rescueDamage.SetActive(hit);
            triage.Civil.gameObject.SetActive(!hit);
            triage.Threat.gameObject.SetActive(false);
            triage.Cargo.position = hit ? new Vector3(-32, 1.05f, 6) : new Vector3(-37, .75f, 8);
            triage.Cargo.rotation = hit ? Quaternion.Euler(0, 25, 35) : Quaternion.identity;
            if (!hit)
            {
                var civil = triage.Civil.GetComponent<Rigidbody>(); civil.position = new Vector3(-32, 1.01f, 8); civil.rotation = Quaternion.identity;
            }
            VisibleDepotDamage = Memory.lostGround;
            for (int i = 0; i < depotDamage.Length; i++)
            {
                depotDamage[i].SetActive(i < Memory.lostGround);
                Paint(territory[i], i < Memory.lostGround ? new Color(.4f, .13f, .07f) : new Color(.15f, .65f, .75f));
            }
            Paint(rescueLane, hit ? new Color(.35f, .12f, .05f) : new Color(.2f, .6f, .3f));
            rescueSign.text = "RESCUE / AFTERMATH"; depotSign.text = "DEPOT / AFTERMATH";
            directionSign.text = "RETURN / EXPLORE WHAT REMAINS";
            Physics.SyncTransforms();
        }
        private void Paint(Renderer target, Color color) { tint.SetColor("_BaseColor", color); target.SetPropertyBlock(tint); }
        public void ReturnLater() => SceneManager.LoadScene(gameObject.scene.path);
        public void ClearMemoryAndRestart() { if (store.Clear()) ReturnLater(); }
        private void OnGUI()
        {
            string status = SaveFailed ? "MEMORY NOT SAVED / pause to retry"
                : Aftermath ? "RETURN / prior consequences restored"
                : Memory != null ? "MEMORY SAVED / stop and start Play, or pause to return later" : "ACTIVE / outcomes save when both crises settle";
            GUI.Box(new Rect(12, Screen.height - 80, 610, 68), status);
            if (!player.Paused) return;
            if (GUI.Button(new Rect(24, Screen.height - 48, 170, 28), "Return later / reload")) ReturnLater();
            if (GUI.Button(new Rect(204, Screen.height - 48, 245, 28), "CLEAR EXPERIMENT MEMORY")) ClearMemoryAndRestart();
            if (SaveFailed && GUI.Button(new Rect(459, Screen.height - 48, 140, 28), "Retry save")) { SaveFailed = false; stableSince = -1; }
        }
    }
}
