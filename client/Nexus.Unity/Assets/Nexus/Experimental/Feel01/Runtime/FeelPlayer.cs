using UnityEngine;
using UnityEngine.InputSystem;

namespace Nexus.Feel01
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FeelPlayer : MonoBehaviour
    {
        [SerializeField] private FeelSettings settings;
        [SerializeField] private InputActionAsset inputTemplate;
        [SerializeField] private FeelCamera orbit;
        [SerializeField] private KineticPulse power;
        [SerializeField] private Transform body;
        private CharacterController controller;
        private InputActionAsset actions;
        private InputAction move, look, jump, sprint, primary, pause;
        private Vector3 horizontal, impulseVelocity;
        private float vertical, lastGrounded = -10, lastJump = -10;
        private float previousTimeScale;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private Vector3 spawn;
        private Quaternion spawnRotation;
        public bool Grounded { get; private set; }
        public bool Paused { get; private set; }
        public Vector3 Velocity => horizontal + impulseVelocity + Vector3.up * vertical;
        public float LastLandingSpeed { get; private set; }
        public int LandingCount { get; private set; }
        public InputActionAsset Actions => actions;
        public FeelSettings Settings => settings;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!settings || !settings.IsValid || !inputTemplate || !orbit || !power || !body)
            { Debug.LogError("Feel player settings/references invalid.", this); enabled = false; return; }
            spawn = transform.position;
            spawnRotation = transform.rotation;
            actions = Instantiate(inputTemplate);
            move = actions.FindAction("Gameplay/Move", true);
            look = actions.FindAction("Gameplay/Look", true);
            jump = actions.FindAction("Gameplay/Jump", true);
            sprint = actions.FindAction("Gameplay/Sprint", true);
            primary = actions.FindAction("Gameplay/PrimaryPower", true);
            pause = actions.FindAction("Gameplay/Pause", true);
        }

        private void OnEnable()
        {
            if (!actions) return;
            previousTimeScale = Time.timeScale;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            actions.Enable();
            SetPaused(false);
        }

        private void OnDisable()
        {
            if (!actions) return;
            actions.Disable();
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }

        private void OnDestroy() { if (actions) Destroy(actions); }
        private void OnApplicationFocus(bool focused) { if (!focused && actions) SetPaused(true); }

        private void Update()
        {
            if (pause.WasPressedThisFrame()) SetPaused(!Paused);
            if (Paused) return;
            float dt = Time.deltaTime;
            orbit.Look(look.ReadValue<Vector2>(), look.activeControl?.device is Mouse, dt);
            if (jump.WasPressedThisFrame()) lastJump = Time.time;
            Move(move.ReadValue<Vector2>(), sprint.IsPressed(), dt);
            // Resolve jump before recoil, including when both actions arrive in the same update.
            if (primary.WasPressedThisFrame()) power.Fire(orbit.AimRay);
            if (transform.position.y < -8) Respawn();
        }

        private void Move(Vector2 input, bool sprinting, float dt)
        {
            bool supported = vertical <= 0 && (controller.isGrounded || ProbeGround());
            if (supported) lastGrounded = Time.time;
            if (supported && vertical < 0) vertical = -2;
            var desired = Quaternion.Euler(0, orbit.Yaw, 0) * new Vector3(input.x, 0, input.y);
            desired = Vector3.ClampMagnitude(desired, 1) * (sprinting ? settings.sprintSpeed : settings.moveSpeed);
            // Releasing input in the air preserves momentum; steering still has finite acceleration.
            float rate = input.sqrMagnitude > .001f ? settings.acceleration : settings.deceleration;
            if (!supported) rate *= input.sqrMagnitude > .001f ? settings.airControl : 0;
            horizontal = Vector3.MoveTowards(horizontal, desired, rate * dt);
            if (Time.time - lastJump <= settings.jumpBuffer && Time.time - lastGrounded <= settings.coyoteTime)
            {
                vertical = Mathf.Sqrt(2 * settings.gravity * settings.jumpHeight);
                lastJump = lastGrounded = -10;
                supported = false;
            }
            vertical = Mathf.Max(vertical - settings.gravity * dt, -settings.terminalSpeed);
            impulseVelocity = Vector3.MoveTowards(impulseVelocity, Vector3.zero,
                (supported ? settings.groundImpulseDrag : settings.airImpulseDrag) * dt);
            float fallingSpeed = vertical;
            var flags = controller.Move(Velocity * dt);
            bool landed = (flags & CollisionFlags.Below) != 0 && vertical <= 0;
            if (landed && !Grounded && fallingSpeed < -3)
            { LastLandingSpeed = -fallingSpeed; LandingCount++; power.LandingFeedback(LastLandingSpeed); }
            Grounded = landed;
            if (landed) vertical = -2;
            if ((flags & CollisionFlags.Above) != 0 && vertical > 0) vertical = 0;
            if (horizontal.sqrMagnitude > .05f)
                body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(horizontal),
                    1 - Mathf.Exp(-settings.turnSharpness * dt));
        }

        private bool ProbeGround()
        {
            float radius = controller.radius * .85f;
            return Physics.SphereCast(transform.position + Vector3.up * (radius + .12f), radius,
                Vector3.down, out var hit, .2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(hit.normal, Vector3.up) >= Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad);
        }

        public void AddImpulse(Vector3 velocityChange)
        {
            impulseVelocity = Vector3.ClampMagnitude(impulseVelocity + new Vector3(velocityChange.x, 0, velocityChange.z), settings.maximumSelfSpeed);
            // Discard only the grounded adhesion bias; preserve actual falling/jumping momentum.
            vertical = Mathf.Clamp((Grounded ? Mathf.Max(vertical, 0) : vertical) + velocityChange.y,
                -settings.terminalSpeed, settings.maximumSelfSpeed);
            if (vertical > 0) lastGrounded = -10;
        }

        public void SetPaused(bool paused)
        {
            Paused = paused;
            Time.timeScale = paused ? 0 : previousTimeScale;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        public void Respawn()
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawn, spawnRotation);
            body.rotation = spawnRotation;
            controller.enabled = true;
            horizontal = impulseVelocity = Vector3.zero;
            vertical = 0;
            lastJump = lastGrounded = -10;
            Grounded = false;
            orbit.Snap();
        }
    }
}
