using UnityEngine;

namespace Nexus.Reactivity01
{
    [CreateAssetMenu(menuName = "Nexus/Experimental/Reaction settings")]
    public sealed class ReactionSettings : ScriptableObject
    {
        [Header("Civil: metres and seconds")]
        [Min(.1f)] public float reactionRadius = 5;
        [Min(.05f)] public float startleDuration = .25f;
        [Min(.1f)] public float fleeSpeed = 3.5f;
        [Min(.1f)] public float fleeDuration = 2.4f;
        [Header("Enemy: metres and seconds")]
        [Min(.1f)] public float approachSpeed = 1.5f;
        [Min(.1f)] public float recoveryDuration = 1.1f;
        [Min(.1f)] public float stopDistance = 2;
        [Min(.1f)] public float threatRadius = 3;
        [Min(.1f)] public float threatInterval = 2;
        [Header("Physical impacts")]
        [Min(.1f)] public float impactSpeedThreshold = 2.5f;
        [Min(.05f)] public float impactCooldown = .3f;
        [Min(.1f)] public float acceleration = 8;
        public bool IsValid => reactionRadius > 0 && startleDuration > 0 && fleeSpeed > 0 && fleeDuration > 0
            && approachSpeed > 0 && recoveryDuration > 0 && stopDistance > 0 && threatRadius > 0
            && threatInterval > 0 && impactSpeedThreshold > 0 && impactCooldown > 0 && acceleration > 0;
    }
}
