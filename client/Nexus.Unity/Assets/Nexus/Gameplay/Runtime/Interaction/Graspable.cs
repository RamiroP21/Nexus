using Nexus.Gameplay.Combat;
using Nexus.Gameplay.World;
using UnityEngine;

namespace Nexus.Gameplay.Interaction
{
    // Explicit opt-in: receiving Vector impulses does not imply sustained control.
    [RequireComponent(typeof(PhysicalTarget))]
    public sealed class Graspable : MonoBehaviour
    {
        private PhysicalTarget target;
        public PhysicalTarget Target => target ? target : target = GetComponent<PhysicalTarget>();
        public bool CanGrasp => isActiveAndEnabled && Target.CanReceiveForce
            && !GetComponentInParent<CivilianPresence>() && !GetComponentInParent<HostileCombatant>();
    }
}
