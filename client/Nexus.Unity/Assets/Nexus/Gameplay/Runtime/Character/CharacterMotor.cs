using UnityEngine;

namespace Nexus.Gameplay.Character
{
    public enum MotorState { Grounded, Rising, Falling, Landing, Vault, Mantle, Knockback, Depleted }
    [RequireComponent(typeof(CharacterController))]
    public sealed class CharacterMotor : MonoBehaviour
    {
        [SerializeField] private MovementSettings settings;
        [SerializeField] private Transform visual;
        private CharacterController controller;
        private readonly JumpWindow jump = new();
        private Vector3 horizontal, impulse;
        private float vertical;
        private Vector3 traversalTop, traversalEnd;
        private int traversalStage;
        private float recovery;
        private bool depleted;
        public MotorState State { get; private set; } = MotorState.Falling;
        public bool Traversing => State == MotorState.Vault || State == MotorState.Mantle;
        public float LastLandingSpeed { get; private set; }
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
            if (Traversing) { TickTraversal(dt); return; }
            recovery = Mathf.Max(0, recovery - dt);
            if (depleted || recovery > 0) { input = Vector2.zero; jumpPressed = false; }
            bool supported = vertical <= 0 && controller.isGrounded;
            Vector3 direction = Quaternion.Euler(0, yaw, 0) * new Vector3(input.x, 0, input.y);
            if (jumpPressed && supported && direction.sqrMagnitude > .1f && TryBeginTraversal(direction)) return;
            if (supported) { jump.Support(now); vertical = -2; }
            if (jumpPressed) jump.Request(now);
            Vector3 desired = Vector3.ClampMagnitude(direction, 1) * (sprint ? settings.SprintSpeed : settings.MoveSpeed);
            float rate = input.sqrMagnitude > .001f ? settings.Acceleration : settings.Braking;
            if (!supported) rate *= input.sqrMagnitude > .001f ? settings.AirControl : 0;
            horizontal = Vector3.MoveTowards(horizontal, desired, rate * dt);
            if (jump.TryConsume(now, settings.CoyoteTime, settings.JumpBuffer))
            { vertical = settings.JumpSpeed; supported = false; JumpCount++; }
            vertical = Mathf.Max(vertical - settings.Gravity * dt, -settings.TerminalSpeed);
            impulse = Vector3.MoveTowards(impulse, Vector3.zero, (supported ? settings.GroundImpulseDrag : settings.AirImpulseDrag) * dt);
            float fallingSpeed = -vertical;
            bool previouslyGrounded = Grounded;
            var flags = controller.Move(Velocity * dt);
            Grounded = (flags & CollisionFlags.Below) != 0 && vertical <= 0;
            if (Grounded && !previouslyGrounded)
            {
                LastLandingSpeed = fallingSpeed;
                if (fallingSpeed >= settings.HardLandingSpeed) recovery = Mathf.Max(recovery, settings.LandingRecovery);
            }
            State = depleted ? MotorState.Depleted : recovery > 0 ? (State == MotorState.Knockback ? MotorState.Knockback : MotorState.Landing)
                : Grounded ? MotorState.Grounded : vertical > 0 ? MotorState.Rising : MotorState.Falling;
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
        public void KnockBack(Vector3 velocityChange, float seconds)
        {
            if (!IsFinite(velocityChange) || !float.IsFinite(seconds) || seconds < 0) return;
            traversalStage = 0; State = depleted ? MotorState.Depleted : MotorState.Knockback;
            recovery = Mathf.Max(recovery, seconds); AddImpulse(velocityChange);
        }
        public void SetDepleted(bool value)
        {
            depleted = value;
            if (value) { traversalStage = 0; State = MotorState.Depleted; jump.ClearSupport(); horizontal = Vector3.zero; }
        }
        public void ResetMotion(Vector3 position)
        {
            if (!IsFinite(position)) return;
            controller.enabled = false; transform.position = position; controller.enabled = true;
            horizontal = impulse = Vector3.zero; vertical = 0; recovery = 0; depleted = false;
            traversalStage = 0; Grounded = false; jump.ClearSupport(); State = MotorState.Falling;
        }
        public bool TryBeginTraversal(Vector3 direction)
        {
            if (!isActiveAndEnabled || depleted || recovery > 0 || Traversing || !controller.isGrounded || !IsFinite(direction)) return false;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            if (direction.sqrMagnitude < .5f) return false;
            Vector3 feet = transform.position;
            if (!Physics.Raycast(feet + Vector3.up * .45f, direction, out var wall, settings.TraversalReach, ~0, QueryTriggerInteraction.Ignore)
                || wall.collider.attachedRigidbody || wall.normal.y > .2f) return false;
            Vector3 over = wall.point + direction * .08f;
            if (!Physics.Raycast(new Vector3(over.x, feet.y + settings.MantleHeight + .1f, over.z), Vector3.down, out var top,
                settings.MantleHeight, ~0, QueryTriggerInteraction.Ignore) || top.normal.y < .9f || top.collider.attachedRigidbody) return false;
            float height = top.point.y - feet.y;
            if (height < .4f || height > settings.MantleHeight) return false;
            bool vault = height <= settings.VaultHeight;
            Vector3 end = top.point + direction * (controller.radius + .15f) + Vector3.up * .08f;
            if (vault)
            {
                Vector3 beyond = end + direction * (controller.radius * 2 + .7f);
                if (!Physics.Raycast(beyond + Vector3.up * .1f, Vector3.down, out var landing, height + .4f, ~0, QueryTriggerInteraction.Ignore)
                    || landing.normal.y < .9f || landing.collider.attachedRigidbody || landing.point.y > feet.y + .35f) return false;
                end = landing.point + Vector3.up * .08f;
            }
            Vector3 raised = new Vector3(feet.x, top.point.y + .12f, feet.z);
            Vector3 across = new Vector3(end.x, raised.y, end.z);
            if (!ClearAt(raised) || !ClearAt(across) || !ClearAt(end)) return false;
            traversalTop = raised; traversalEnd = end; traversalStage = 1;
            horizontal = impulse = Vector3.zero; vertical = 0; jump.ClearSupport(); Grounded = false;
            State = vault ? MotorState.Vault : MotorState.Mantle;
            return true;
        }
        private bool ClearAt(Vector3 position)
        {
            Vector3 centre = position + controller.center;
            float radius = controller.radius * .95f;
            float half = controller.height * .5f - controller.radius;
            var hits = Physics.OverlapCapsule(centre + Vector3.up * half, centre - Vector3.up * half, radius, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits) if (hit != controller && !hit.transform.IsChildOf(transform)) return false;
            return true;
        }
        private void TickTraversal(float dt)
        {
            Vector3 goal = traversalStage == 1 ? traversalTop : traversalStage == 2
                ? new Vector3(traversalEnd.x, traversalTop.y, traversalEnd.z) : traversalEnd;
            Vector3 before = transform.position;
            controller.Move(Vector3.ClampMagnitude(goal - before, settings.TraversalSpeed * dt));
            if (Vector3.Distance(transform.position, goal) < .045f)
            {
                if (++traversalStage > 3) { traversalStage = 0; State = MotorState.Falling; }
            }
            else if (Vector3.Distance(before, transform.position) < settings.TraversalSpeed * dt * .1f)
            { traversalStage = 0; State = MotorState.Falling; }
        }
        private static bool IsFinite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
