using UnityEngine;

namespace Nexus.VerticalSlice01
{
    public sealed class SliceCargoContact : MonoBehaviour
    {
        [SerializeField] private SliceIncident incident;
        private void OnCollisionEnter(Collision collision) => incident.CargoContact(collision.rigidbody);
    }
}
