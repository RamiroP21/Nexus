using Nexus.Gameplay.Combat;
using UnityEngine;

namespace Nexus.Gameplay.World
{
    [RequireComponent(typeof(DamageReceiver))]
    public sealed class DamageStateVisual : MonoBehaviour
    {
        [SerializeField] private GameObject intact, damaged;
        private DamageReceiver receiver;
        public bool DamageVisible => damaged && damaged.activeSelf;
        private void Awake()
        {
            receiver = GetComponent<DamageReceiver>();
            if (!intact || !damaged || intact == damaged)
            { Debug.LogError("DamageStateVisual requires distinct intact and damaged visual roots.", this); enabled = false; }
        }
        private void OnEnable()
        { if (receiver) { receiver.Changed += Refresh; Refresh(receiver.Health); } }
        private void OnDisable() { if (receiver) receiver.Changed -= Refresh; }
        private void Refresh(Health health)
        {
            bool changed = health.Current < health.Maximum;
            intact.SetActive(!changed); damaged.SetActive(changed);
        }
    }
}
