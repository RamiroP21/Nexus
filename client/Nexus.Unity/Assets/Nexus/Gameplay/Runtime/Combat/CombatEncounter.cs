using Nexus.Gameplay.World;
using UnityEngine;

namespace Nexus.Gameplay.Combat
{
    public enum EncounterState { Ready, Active, Won, Lost }
    public sealed class CombatEncounter : MonoBehaviour
    {
        [SerializeField] private PlayerVitality player;
        [SerializeField] private HostileCombatant hostile;
        [SerializeField] private Rigidbody[] resetBodies;
        private Vector3[] positions;
        private Quaternion[] rotations;
        private RigidbodyConstraints[] constraints;
        public EncounterState State { get; private set; }
        public bool Finished => State == EncounterState.Won || State == EncounterState.Lost;
        private void Awake()
        {
            if (!player || !hostile || resetBodies == null)
            { Debug.LogError("CombatEncounter requires player, hostile and reset bodies.", this); enabled = false; return; }
            positions = new Vector3[resetBodies.Length]; rotations = new Quaternion[resetBodies.Length]; constraints = new RigidbodyConstraints[resetBodies.Length];
            for (int i = 0; i < resetBodies.Length; i++)
            { positions[i] = resetBodies[i].position; rotations[i] = resetBodies[i].rotation; constraints[i] = resetBodies[i].constraints; }
        }
        private void Update() => Evaluate();
        public void Evaluate()
        {
            if (!isActiveAndEnabled || Finished) return;
            if (player.Depleted) State = EncounterState.Lost;
            else if (hostile.State == HostileState.Depleted) State = EncounterState.Won;
            else if (hostile.Target) State = EncounterState.Active;
            if (Finished) { hostile.Halt(); ClearHazards(); }
        }
        public void ResetEncounter()
        {
            ClearHazards();
            for (int i = 0; i < resetBodies.Length; i++)
            {
                var body = resetBodies[i]; body.transform.SetPositionAndRotation(positions[i], rotations[i]);
                body.position = positions[i]; body.rotation = rotations[i];
                body.linearVelocity = body.angularVelocity = Vector3.zero; body.constraints = constraints[i];
                if (body.TryGetComponent<ImpactBarrier>(out var cover)) cover.ResetBarrier();
            }
            player.ResetTraining(); hostile.ResetCombatant(); State = EncounterState.Ready; Physics.SyncTransforms();
        }
        private void ClearHazards()
        {
            foreach (var hazard in FindObjectsByType<TrainingHazard>(FindObjectsSortMode.None))
                if (hazard.Source == hostile.gameObject) { hazard.gameObject.SetActive(false); Destroy(hazard.gameObject); }
        }
        private void OnGUI()
        {
            string text = Finished ? (State == EncounterState.Won ? "ENCOUNTER WON" : "ENCOUNTER LOST") + " — Space / South to reset"
                : State == EncounterState.Active ? "HOSTILE ENCOUNTER — move during the warning; return the hazard or use a prop"
                : "HOSTILE ENCOUNTER — approach the east combat lane";
            GUI.Label(new Rect(20, Screen.height - 90, 820, 30), text);
        }
    }
}
