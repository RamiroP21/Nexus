using UnityEngine;

namespace Nexus.Feel01
{
    public sealed class FeelArena : MonoBehaviour
    {
        [SerializeField] private FeelPlayer player;
        [SerializeField] private Rigidbody[] props;
        private Vector3[] positions;
        private Quaternion[] rotations;
        private GUIStyle title, text;

        private void Awake()
        {
            positions = new Vector3[props.Length];
            rotations = new Quaternion[props.Length];
            for (int i = 0; i < props.Length; i++)
            { positions[i] = props[i].position; rotations[i] = props[i].rotation; }
        }

        private void Update()
        {
            for (int i = 0; i < props.Length; i++)
                if (props[i].position.y < -8) ResetProp(i);
            if (player.Paused && player.Actions.FindAction("Gameplay/Jump").WasPressedThisFrame()) ResetArena();
        }

        public void ResetArena()
        {
            for (int i = 0; i < props.Length; i++) ResetProp(i);
            player.Respawn();
            player.SetPaused(false);
        }

        private void ResetProp(int i)
        {
            props[i].position = positions[i];
            props[i].rotation = rotations[i];
            props[i].linearVelocity = props[i].angularVelocity = Vector3.zero;
            props[i].Sleep();
        }

        private void OnGUI()
        {
            title ??= new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            text ??= new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            GUI.Box(new Rect(12, 12, 410, 88), GUIContent.none);
            GUI.Label(new Rect(24, 18, 390, 28), "KINETIC VECTOR / FEEL 01", title);
            GUI.Label(new Rect(24, 49, 390, 48), "Move · Look · Jump · Sprint · PrimaryPower\nEsc / Start: pause, controls and reset", text);
            if (!player.Paused) return;
            float x = Screen.width / 2f - 230, y = Screen.height / 2f - 140;
            GUI.Box(new Rect(x, y, 460, 285), GUIContent.none);
            GUI.Label(new Rect(x + 20, y + 16, 420, 30), "PAUSED — HUMAN PLAYTEST", title);
            GUI.Label(new Rect(x + 20, y + 55, 420, 125),
                "WASD / left stick: move\nMouse / right stick: look\nSpace / South: jump · Shift / L3: sprint\nLeft mouse / RT: kinetic impulse\nEsc / Start: resume · Jump while paused: reset", text);
            if (GUI.Button(new Rect(x + 20, y + 194, 200, 42), "Resume")) player.SetPaused(false);
            if (GUI.Button(new Rect(x + 240, y + 194, 200, 42), "Reset arena")) ResetArena();
        }
    }
}
