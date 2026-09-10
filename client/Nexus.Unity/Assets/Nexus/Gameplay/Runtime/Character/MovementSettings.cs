using UnityEngine;

namespace Nexus.Gameplay.Character
{
    [CreateAssetMenu(menuName = "Nexus/Gameplay/Movement")]
    public sealed class MovementSettings : ScriptableObject
    {
        [SerializeField, Min(.1f)] private float moveSpeed = 6, sprintSpeed = 10;
        [SerializeField, Min(.1f)] private float acceleration = 28, braking = 30, gravity = 24, jumpHeight = 1.8f;
        [SerializeField, Range(0, 1)] private float airControl = .35f;
        [SerializeField, Min(.1f)] private float terminalSpeed = 35, turnSharpness = 16;
        [SerializeField, Range(0, .25f)] private float coyoteTime = .1f, jumpBuffer = .12f;
        [SerializeField, Min(0)] private float groundImpulseDrag = 8, airImpulseDrag = 1;
        [SerializeField, Min(.1f)] private float maximumImpulseSpeed = 14;
        public float MoveSpeed => moveSpeed;
        public float SprintSpeed => sprintSpeed;
        public float Acceleration => acceleration;
        public float Braking => braking;
        public float Gravity => gravity;
        public float JumpSpeed => Mathf.Sqrt(2 * gravity * jumpHeight);
        public float AirControl => airControl;
        public float TerminalSpeed => terminalSpeed;
        public float TurnSharpness => turnSharpness;
        public float CoyoteTime => coyoteTime;
        public float JumpBuffer => jumpBuffer;
        public float GroundImpulseDrag => groundImpulseDrag;
        public float AirImpulseDrag => airImpulseDrag;
        public float MaximumImpulseSpeed => maximumImpulseSpeed;
        public bool IsValid => moveSpeed > 0 && sprintSpeed >= moveSpeed && acceleration > 0 && braking > 0
            && gravity > 0 && jumpHeight > 0 && terminalSpeed > 0 && turnSharpness > 0
            && airControl >= 0 && airControl <= 1 && coyoteTime >= 0 && jumpBuffer >= 0
            && groundImpulseDrag >= 0 && airImpulseDrag >= 0 && maximumImpulseSpeed > 0;
    }
}
