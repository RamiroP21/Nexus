using UnityEngine;

namespace Nexus.Gameplay.Abilities
{
    [CreateAssetMenu(menuName = "Nexus/Gameplay/Kinetic Vector")]
    public sealed class KineticVectorSettings : ScriptableObject
    {
        [SerializeField, Min(.01f)] private float impulse = 36, cooldown = .65f;
        [SerializeField, Min(0)] private float recoil = 5, damage = 12;
        [SerializeField, Min(.01f)] private float feedbackDuration = .12f;
        public float Impulse => impulse;
        public float Cooldown => cooldown;
        public float Recoil => recoil;
        public float Damage => damage;
        public float FeedbackDuration => feedbackDuration;
        public bool IsValid => float.IsFinite(impulse) && impulse > 0 && float.IsFinite(cooldown) && cooldown > 0
            && float.IsFinite(recoil) && recoil >= 0 && float.IsFinite(damage) && damage >= 0 && feedbackDuration > 0;
    }
}
