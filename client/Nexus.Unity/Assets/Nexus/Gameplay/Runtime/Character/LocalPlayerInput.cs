using Nexus.Gameplay.Abilities;
using Nexus.Gameplay.CameraSystem;
using Nexus.Gameplay.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus.Gameplay.Character
{
    public sealed class LocalPlayerInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputTemplate;
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private ThirdPersonCamera orbit;
        [SerializeField] private KineticVectorAbility primaryAbility;
        [SerializeField] private ContextualTargeting targeting;
        private InputAction move, look, jump, sprint, primary, pause;
        private float priorTimeScale;
        private CursorLockMode priorLock;
        private bool priorVisible;
        public InputActionAsset Actions { get; private set; }
        public bool Paused { get; private set; }
        private void Awake()
        {
            if (!inputTemplate || !motor || !orbit || !primaryAbility || !targeting)
            { Debug.LogError("LocalPlayerInput requires explicit input, motor, camera, ability and targeting references.", this); enabled = false; return; }
            Actions = Instantiate(inputTemplate);
            move = Actions.FindAction("Gameplay/Move", true); look = Actions.FindAction("Gameplay/Look", true);
            jump = Actions.FindAction("Gameplay/Jump", true); sprint = Actions.FindAction("Gameplay/Sprint", true);
            primary = Actions.FindAction("Gameplay/PrimaryPower", true); pause = Actions.FindAction("Gameplay/Pause", true);
        }
        private void OnEnable()
        {
            if (!Actions) return;
            priorTimeScale = Time.timeScale; priorLock = Cursor.lockState; priorVisible = Cursor.visible;
            Actions.Enable(); SetPaused(false);
        }
        private void Update()
        {
            if (pause.WasPressedThisFrame()) SetPaused(!Paused);
            if (Paused) return;
            orbit.Look(look.ReadValue<Vector2>(), look.activeControl?.device is Mouse, Time.deltaTime);
            motor.Tick(move.ReadValue<Vector2>(), sprint.IsPressed(), jump.WasPressedThisFrame(), orbit.Yaw, Time.deltaTime, Time.timeAsDouble);
            if (primary.WasPressedThisFrame()) primaryAbility.TryActivate();
        }
        public void SetPaused(bool value)
        {
            Paused = value; targeting.Suspended = value;
            Time.timeScale = value ? 0 : priorTimeScale;
            Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value;
        }
        private void OnApplicationFocus(bool focus) { if (!focus && Actions) SetPaused(true); }
        private void OnDisable()
        {
            if (!Actions) return;
            Actions.Disable(); targeting.Suspended = true;
            Time.timeScale = priorTimeScale; Cursor.lockState = priorLock; Cursor.visible = priorVisible;
        }
        private void OnDestroy() { if (Actions) Destroy(Actions); }
    }
}
