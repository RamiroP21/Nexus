using UnityEngine;

namespace Nexus.Gameplay.Character
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [SerializeField] private MovementSettings settings;
        [SerializeField] private Transform visual;
        private CharacterController controller;
        private readonly JumpWindow jump = new();
        private Vector3 horizontal, impulse;
        private float vertical;
        public MovementSettings Settings => settings;
        public bool Grounded { get; private set; }
        public Vector3 Velocity => horizontal + impulse + Vector3.up * vertical;
        public int JumpCount { get; private set; }
        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (!settings || !settings.IsValid || !visual)
            { Debug.LogError("CharacterMotor requires valid movement settings and a visual root.", this); enabled = false; }
        }
        public void Tick(Vector2 input, bool sprint, bool jumpPressed, float yaw, float dt, double now)
        {
            if (!isActiveAndEnabled || dt <= 0) return;
            bool supported = vertical <= 0 && controller.isGrounded;
            if (supported) { jump.Support(now); vertical = -2; }
            if (jumpPressed) jump.Request(now);
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
            Vector3 desired = Vector3.ClampMagnitude(direction, 1) * (sprint ? settings.SprintSpeed : settings.MoveSpeed);
            float rate = input.sqrMagnitude > .001f ? settings.Acceleration : settings.Braking;
            if (!supported) rate *= input.sqrMagnitude > .001f ? settings.AirControl : 0;
            horizontal = Vector3.MoveTowards(horizontal, desired, rate * dt);
            if (jump.TryConsume(now, settings.CoyoteTime, settings.JumpBuffer))
            { vertical = settings.JumpSpeed; supported = false; JumpCount++; }
            vertical = Mathf.Max(vertical - settings.Gravity * dt, -settings.TerminalSpeed);
            impulse = Vector3.MoveTowards(impulse, Vector3.zero, (supported ? settings.GroundImpulseDrag : settings.AirImpulseDrag) * dt);
            var flags = controller.Move(Velocity * dt);
            Grounded = (flags & CollisionFlags.Below) != 0 && vertical <= 0;
            if (Grounded) vertical = -2;
            if ((flags & CollisionFlags.Above) != 0 && vertical > 0) vertical = 0;
            if (horizontal.sqrMagnitude > .05f)
                visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(horizontal), 1 - Mathf.Exp(-settings.TurnSharpness * dt));
        }
        public void AddImpulse(Vector3 velocityChange)
        {
            if (!isActiveAndEnabled || !IsFinite(velocityChange)) return;
            impulse = Vector3.ClampMagnitude(impulse + Vector3.ProjectOnPlane(velocityChange, Vector3.up), settings.MaximumImpulseSpeed);
            vertical = Mathf.Clamp((Grounded ? Mathf.Max(vertical, 0) : vertical) + velocityChange.y, -settings.TerminalSpeed, settings.MaximumImpulseSpeed);
            if (vertical > 0) jump.ClearSupport();
        }
        private static bool IsFinite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
