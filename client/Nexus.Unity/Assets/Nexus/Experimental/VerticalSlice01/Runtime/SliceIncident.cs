using System;
using System.IO;
using Nexus.Feel01;
using Nexus.Persistence01;
using Nexus.Reactivity01;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nexus.VerticalSlice01
{
    // One authored incident. Reuses the experimental record codec, with independent ownership/path.
    public sealed class SliceIncident : MonoBehaviour
    {
        public const string TestPathVariable = "NEXUS_VERTICALSLICE01_TEST_PATH";
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "VerticalSlice01", "outcome.json");
        public static string RuntimePath
        {
            get
            {
#if UNITY_EDITOR
                string path = Environment.GetEnvironmentVariable(TestPathVariable);
                if (!string.IsNullOrEmpty(path)) return path;
#endif
                return DefaultPath;
            }
        }
        [SerializeField] private FeelArena arena;
        [SerializeField] private FeelPlayer player;
        [SerializeField] private KineticPulse pulse;
        [SerializeField] private ReactionStage reactions;
        [SerializeField] private ReactiveActor civil, threat;
        [SerializeField] private ReactiveActor[] actors;
        [SerializeField] private Rigidbody cargo;
        [SerializeField] private Renderer[] territory;
        [SerializeField] private GameObject rescueDamage, containmentTrace;
        [SerializeField] private GameObject[] depotDamage;
        [SerializeField] private Light beacon;
        [SerializeField] private LineRenderer discharge;
        [SerializeField] private float orientationSeconds = 6;
        private Persistence01StateStore store;
        private float phaseStart, launchedAt, stableSince = -1;
        private RescueOutcome candidateRescue;
        private DepotOutcome candidateDepot;
        private int candidateGround;
        private bool struck;
        private bool[] occupied;
        private MaterialPropertyBlock tint;
        public bool IncidentStarted { get; private set; }
        public bool Aftermath { get; private set; }
        public bool SaveFailed { get; private set; }
        public int LostGround { get; private set; }
        public RescueOutcome Rescue { get; private set; }
        public DepotOutcome Depot { get; private set; }
        public Persistence01State Memory { get; private set; }
        public Rigidbody Cargo => cargo;
        public ReactiveActor Civil => civil;
        public ReactiveActor Threat => threat;
        public float Elapsed => IncidentStarted ? Time.time - launchedAt : 0;
        public string MemoryPath => store.FilePath;
        public bool RescueDamageVisible => rescueDamage.activeSelf;
        public int VisibleDepotDamage { get; private set; }

        private void Awake()
        {
            if (!arena || !player || !pulse || !reactions || !civil || !threat || !cargo || !rescueDamage || !containmentTrace
                || !beacon || !discharge || actors == null || actors.Length < 4 || territory.Length != 3 || depotDamage.Length != 3)
            { Debug.LogError("VerticalSlice01 references missing.", this); enabled = false; return; }
            tint = new MaterialPropertyBlock(); occupied = new bool[3];
            store = new Persistence01StateStore(RuntimePath, message => Debug.LogWarning("VerticalSlice01: " + message));
            Memory = store.Load();
            threat.enabled = false;
        }
        private void OnEnable() { if (arena) arena.ResetPerformed += ResetIncident; }
        private void OnDisable() { if (arena) arena.ResetPerformed -= ResetIncident; }
        private void Start() => ResetIncident();

        private void ResetIncident()
        {
            if (store == null) return;
            pulse.ResetPulse(); stableSince = -1; SaveFailed = false; discharge.enabled = false;
            if (Memory != null) { ShowAftermath(); return; }
            IncidentStarted = Aftermath = struck = false; phaseStart = Time.time;
            Rescue = RescueOutcome.Unknown; Depot = DepotOutcome.Unknown; LostGround = VisibleDepotDamage = 0;
            rescueDamage.SetActive(false); containmentTrace.SetActive(false);
            for (int i = 0; i < 3; i++) { occupied[i] = false; depotDamage[i].SetActive(false); Paint(territory[i], new Color(.27f, .42f, .43f)); }
            foreach (var actor in actors) { actor.enabled = true; actor.ResetReaction(); }
            threat.enabled = false; cargo.linearVelocity = Vector3.zero;
            beacon.color = new Color(1, .65f, .2f); beacon.intensity = .4f;
        }

        private void FixedUpdate()
        {
            if (Memory != null || player.Paused) return;
            if (!IncidentStarted)
            {
                if (Time.time - phaseStart < orientationSeconds) return;
                IncidentStarted = true; launchedAt = Time.time; threat.enabled = true;
                // The entering kinetic threat discharges into this load; subsequent movement/contact is physics.
                Vector3 direction = Vector3.ProjectOnPlane(civil.Position - cargo.position, Vector3.up).normalized;
                cargo.AddForce(direction * 6, ForceMode.VelocityChange);
                discharge.SetPosition(0, threat.Position); discharge.SetPosition(1, cargo.position); discharge.enabled = true;
                reactions.Report(StimulusKind.KineticImpact, cargo.position, 36, cargo);
            }
            discharge.enabled = Elapsed < .45f;
            beacon.color = Color.red; beacon.intensity = 1 + Mathf.Sin(Time.time * 9) * .6f;
            Vector3 relative = Vector3.ProjectOnPlane(cargo.position - civil.Position, Vector3.up);
            Vector3 velocity = Vector3.ProjectOnPlane(cargo.linearVelocity, Vector3.up);
            float approach = velocity.sqrMagnitude > .01f ? Mathf.Max(0, -Vector3.Dot(relative, velocity) / velocity.sqrMagnitude) : 0;
            float miss = (relative + velocity * approach).magnitude;
            Rescue = struck ? RescueOutcome.Hit : miss > 2.5f && relative.magnitude > 3 ? RescueOutcome.Protected : RescueOutcome.Unknown;
            for (int i = 0; i < 3; i++)
                if (!occupied[i] && threat.Position.y > 0 && Mathf.Abs(threat.Position.x - territory[i].transform.position.x) <= 2.5f
                    && threat.Position.z >= territory[i].transform.position.z)
                {
                    occupied[i] = true; LostGround++;
                    depotDamage[i].SetActive(true); VisibleDepotDamage++;
                    Paint(territory[i], new Color(.42f, .14f, .08f));
                }
            Depot = threat.Position.y < -1.5f ? DepotOutcome.Contained : LostGround == 3 ? DepotOutcome.Overrun : DepotOutcome.Unknown;
            if (Rescue == RescueOutcome.Unknown || Depot == DepotOutcome.Unknown) { stableSince = -1; return; }
            if (candidateRescue != Rescue || candidateDepot != Depot || candidateGround != LostGround || stableSince < 0)
            { candidateRescue = Rescue; candidateDepot = Depot; candidateGround = LostGround; stableSince = Time.time; }
            if (!SaveFailed && Time.time - stableSince > 1.5f) SaveOutcome();
        }
        public void CargoContact(Rigidbody other)
        {
            if (!IncidentStarted || Memory != null || other != civil.GetComponent<Rigidbody>()) return;
            struck = true; Rescue = RescueOutcome.Hit;
            civil.React(StimulusKind.PhysicalImpact, cargo.position, cargo);
            rescueDamage.SetActive(true);
        }
        private void SaveOutcome()
        {
            var state = new Persistence01State { version = 1, rescue = Rescue, depot = Depot, lostGround = LostGround };
            if (!store.Save(state)) { SaveFailed = true; return; }
            Memory = state;
            // Close only the hazards; the player and bystanders can still move through the result.
            threat.enabled = false; Freeze(threat.GetComponent<Rigidbody>()); Freeze(cargo);
            discharge.enabled = false; beacon.color = new Color(.3f, .7f, .5f); beacon.intensity = .7f;
        }
        private void ShowAftermath()
        {
            Aftermath = true; IncidentStarted = false; discharge.enabled = false;
            Rescue = Memory.rescue; Depot = Memory.depot; LostGround = Memory.lostGround;
            foreach (var actor in actors) { actor.enabled = false; Freeze(actor.GetComponent<Rigidbody>()); }
            Freeze(cargo);
            bool hit = Rescue == RescueOutcome.Hit;
            civil.gameObject.SetActive(!hit); threat.gameObject.SetActive(false);
            rescueDamage.SetActive(hit); containmentTrace.SetActive(Depot == DepotOutcome.Contained);
            Place(cargo, hit ? new Vector3(-12, 1.1f, 6) : new Vector3(-17, .8f, 8), hit ? Quaternion.Euler(0, 25, 35) : Quaternion.identity);
            if (!hit) Place(civil.GetComponent<Rigidbody>(), new Vector3(-12, 1.01f, 8), Quaternion.identity);
            VisibleDepotDamage = LostGround;
            for (int i = 0; i < 3; i++) { depotDamage[i].SetActive(i < LostGround); Paint(territory[i], i < LostGround ? new Color(.42f, .14f, .08f) : new Color(.27f, .42f, .43f)); }
            beacon.color = new Color(.3f, .7f, .5f); beacon.intensity = .4f;
            Physics.SyncTransforms();
        }
        private static void Freeze(Rigidbody body)
        { body.linearVelocity = body.angularVelocity = Vector3.zero; body.constraints = RigidbodyConstraints.FreezeAll; body.interpolation = RigidbodyInterpolation.None; }
        private static void Place(Rigidbody body, Vector3 position, Quaternion rotation)
        { body.position = position; body.rotation = rotation; body.transform.SetPositionAndRotation(position, rotation); }
        private void Paint(Renderer renderer, Color color) { tint.SetColor("_BaseColor", color); renderer.SetPropertyBlock(tint); }
        public void ReturnLater() => SceneManager.LoadScene(gameObject.scene.path);
        public void ClearMemoryAndRestart() { if (store.Clear()) ReturnLater(); }
        private void OnGUI()
        {
            if (!player.Paused)
            {
                if (SaveFailed) GUI.Box(new Rect(20, Screen.height - 50, 410, 30), "No se pudo guardar. Pausa para reintentar.");
                else if (Memory != null && !Aftermath) GUI.Box(new Rect(20, Screen.height - 50, 410, 30), "La calle se calma. Esc: volver más tarde.");
                else if (!IncidentStarted && !Aftermath) GUI.Label(new Rect(24, Screen.height - 45, 500, 30), "Pasaje del Mercado · WASD / mover · clic / impulso · Esc / controles");
                return;
            }
            if (GUI.Button(new Rect(20, Screen.height - 50, 180, 32), "Volver más tarde")) ReturnLater();
            if (GUI.Button(new Rect(210, Screen.height - 50, 240, 32), "Clear experiment memory")) ClearMemoryAndRestart();
            if (SaveFailed && GUI.Button(new Rect(460, Screen.height - 50, 140, 32), "Reintentar guardado")) { SaveFailed = false; stableSince = -1; }
        }
    }
}
