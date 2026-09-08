using UnityEngine;

namespace Nexus.Feel01
{
    [CreateAssetMenu(menuName = "Nexus/Feel 01 Settings")]
    public sealed class FeelSettings : ScriptableObject
    {
        [Header("Movement — metres / seconds")]
        [Range(1, 15)] public float moveSpeed = 6;
        [Range(1, 22)] public float sprintSpeed = 10;
        [Range(1, 100)] public float acceleration = 28;
        [Range(1, 100)] public float deceleration = 30;
        [Range(1, 60)] public float gravity = 24;
        [Range(.2f, 6)] public float jumpHeight = 1.8f;
        [Range(0, 1)] public float airControl = .35f;
        [Range(5, 60)] public float terminalSpeed = 35;
        [Range(0, .25f)] public float coyoteTime = .1f;
        [Range(0, .25f)] public float jumpBuffer = .12f;
        [Range(1, 30)] public float turnSharpness = 16;

        [Header("Camera — degrees / metres")]
        [Range(1, 10)] public float cameraDistance = 5.8f;
        [Range(45, 100)] public float fieldOfView = 72;
        [Tooltip("Degrees per mouse delta unit, NOT multiplied by deltaTime.")]
        [Range(.01f, 1)] public float mouseSensitivity = .12f;
        [Tooltip("Degrees per second at full stick deflection.")]
        [Range(10, 360)] public float gamepadSensitivity = 150;
        [Range(0, 30)] public float followSharpness = 12;
        [Range(-80, 0)] public float minimumPitch = -55;
        [Range(10, 85)] public float maximumPitch = 75;
        [Range(.8f, 2.5f)] public float pivotHeight = 1.55f;
        [Range(.1f, .4f)] public float cameraRadius = .22f;

        [Header("Kinetic Vector — impulse, not damage")]
        [Range(2, 50)] public float range = 24;
        [Tooltip("Impulse in kg m/s. Equal impulse gives different delta-v by mass.")]
        [Range(1, 120)] public float impulse = 36;
        [Tooltip("Opposite camera direction, metres/second added to player velocity.")]
        [Range(0, 15)] public float selfImpulse = 5;
        [Range(.15f, 3)] public float cooldown = .65f;
        [Range(1, 25)] public float maximumSelfSpeed = 14;
        [Range(0, 25)] public float groundImpulseDrag = 8;
        [Range(0, 10)] public float airImpulseDrag = 1;

        public bool IsValid => moveSpeed > 0 && sprintSpeed >= moveSpeed && acceleration > 0 &&
            deceleration > 0 && gravity > 0 && jumpHeight > 0 && airControl >= 0 && airControl <= 1 &&
            cameraDistance > cameraRadius && fieldOfView >= 45 && fieldOfView <= 100 &&
            minimumPitch < maximumPitch && cooldown > 0 && range > 0 && impulse > 0 && selfImpulse >= 0;
    }
}
