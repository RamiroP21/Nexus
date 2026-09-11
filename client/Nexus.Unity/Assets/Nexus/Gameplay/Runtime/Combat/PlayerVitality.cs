using Nexus.Gameplay.Character;
using UnityEngine;

namespace Nexus.Gameplay.Combat
{
    [RequireComponent(typeof(CharacterMotor), typeof(DamageReceiver))]
    public sealed class PlayerVitality : MonoBehaviour
    {
        [SerializeField, Min(.01f)] private float recoverySeconds = .45f;
        [SerializeField] private float safetyFloor = -8;
        private CharacterMotor motor;
        private DamageReceiver receiver;
        private Vector3 spawn;
        public bool Depleted => receiver && receiver.Health.IsDepleted;
        public DamageReceiver Receiver => receiver;
        private void Awake()
        { motor = GetComponent<CharacterMotor>(); receiver = GetComponent<DamageReceiver>(); spawn = transform.position; }
        private void OnEnable() { receiver.Changed += OnHealth; OnHealth(receiver.Health); }
        private void OnDisable() { if (receiver) receiver.Changed -= OnHealth; }
        private void OnHealth(Health health) => motor.SetDepleted(health.IsDepleted);
        public bool ReceiveHit(Damage damage, Vector3 velocityChange)
        {
            if (!isActiveAndEnabled || Depleted) return false;
            bool accepted = receiver.Receive(damage);
            if (accepted) motor.KnockBack(velocityChange, recoverySeconds);
            return accepted;
        }
        public void ResetTraining()
        {
            motor.ResetMotion(spawn);
            receiver.ResetHealth();
        }
        private void Update() { if (transform.position.y < safetyFloor) ResetTraining(); }
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var hazard = hit.collider.GetComponent<TrainingHazard>();
            if (hazard) hazard.TryHit(gameObject);
        }
        private void OnGUI()
        {
            string status = Depleted ? "DEPLETED — Space / South to recover" : motor.State == MotorState.Knockback ? "IMPACT — recovering" : motor.State.ToString();
            GUI.Label(new Rect(20, Screen.height - 58, 410, 45), $"Integrity {receiver.Health.Current:0}/{receiver.Health.Maximum:0}   |   {status}");
        }
    }
}
