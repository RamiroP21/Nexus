using Nexus.Gameplay.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus.Gameplay.World
{
    public enum UrbanSituationState { Quiet, Danger, Secured, SecuredWithCasualties }
    public sealed class UrbanBlockSituation : MonoBehaviour
    {
        [SerializeField] private PlayerVitality player;
        [SerializeField] private HostileCombatant hostile;
        [SerializeField] private CivilianPresence[] civilians;
        [SerializeField] private Rigidbody[] resetBodies;
        [SerializeField] private TextMesh streetNotice;
        private Vector3[] positions;
        private Quaternion[] rotations;
        private RigidbodyConstraints[] constraints;
        public UrbanSituationState State { get; private set; }
        public CivilianPresence[] Civilians => (CivilianPresence[])civilians.Clone();
        public HostileCombatant Hostile => hostile;
        public string Notice => streetNotice.text;
        private void Awake()
        {
            if (!player || !hostile || !streetNotice || civilians == null || civilians.Length == 0 || resetBodies == null)
            { Debug.LogError("UrbanBlockSituation requires actors, reset bodies and street notice.", this); enabled = false; return; }
            positions = new Vector3[resetBodies.Length]; rotations = new Quaternion[resetBodies.Length]; constraints = new RigidbodyConstraints[resetBodies.Length];
            for (int i = 0; i < resetBodies.Length; i++)
            { positions[i] = resetBodies[i].position; rotations[i] = resetBodies[i].rotation; constraints[i] = resetBodies[i].constraints; }
        }
        private void Start() => Evaluate();
        private void Update()
        {
            Evaluate();
            if ((Application.isEditor || Debug.isDebugBuild) && Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) ResetBlock();
        }
        public void Evaluate()
        {
            if (!isActiveAndEnabled) return;
            int harmed = 0, sheltered = 0;
            foreach (var civil in civilians) { if (civil.Harmed) harmed++; if (civil.State == CivilianState.Sheltered) sheltered++; }
            if (hostile.State == HostileState.Depleted) State = harmed > 0 ? UrbanSituationState.SecuredWithCasualties : UrbanSituationState.Secured;
            else if (hostile.Target || harmed > 0 || sheltered > 0) State = UrbanSituationState.Danger;
            streetNotice.text = State == UrbanSituationState.Quiet ? "MERCADO / RESIDENCIAS\nDelivery entrance obstructed"
                : State == UrbanSituationState.Danger ? $"DANGER IN THE STREET\nResidents sheltered: {sheltered}/{civilians.Length}   Injured: {harmed}\nClear the delivery cart / stop the hostile"
                : harmed > 0 ? $"STREET SECURED - RESIDENTS INJURED: {harmed}\nThe damage remains. You can return."
                : $"STREET SECURED - NO RESIDENTS INJURED\nResidents sheltered: {sheltered}/{civilians.Length}";
            streetNotice.color = harmed > 0 ? new Color(1, .35f, .15f) : State == UrbanSituationState.Secured ? Color.green : Color.white;
        }
        public void ResetBlock()
        {
            foreach (var hazard in FindObjectsByType<TrainingHazard>(FindObjectsSortMode.None))
                if (hazard.Source == hostile.gameObject) { hazard.gameObject.SetActive(false); Destroy(hazard.gameObject); }
            for (int i = 0; i < resetBodies.Length; i++)
            {
                var body = resetBodies[i]; body.transform.SetPositionAndRotation(positions[i], rotations[i]); body.position = positions[i]; body.rotation = rotations[i];
                body.linearVelocity = body.angularVelocity = Vector3.zero; body.constraints = constraints[i];
                if (body.TryGetComponent<ImpactBarrier>(out var barrier)) barrier.ResetBarrier();
                if (body.TryGetComponent<DamageReceiver>(out var receiver)) receiver.ResetHealth();
            }
            player.ResetTraining(); hostile.ResetCombatant();
            foreach (var civil in civilians) civil.ResetCivilian();
            State = UrbanSituationState.Quiet; Physics.SyncTransforms(); Evaluate();
        }
        private void OnGUI()
        {
            GUI.Label(new Rect(20, Screen.height - 110, 850, 90), "URBAN BLOCK 01 | Market / Homes / Service alley\n" + streetNotice.text
                + ((Application.isEditor || Debug.isDebugBuild) ? "\nF8: reset block (development)" : ""));
        }
    }
}
