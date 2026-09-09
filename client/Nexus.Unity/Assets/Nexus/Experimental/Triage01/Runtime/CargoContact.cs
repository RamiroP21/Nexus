using UnityEngine;

namespace Nexus.Triage01
{
    public sealed class CargoContact : MonoBehaviour
    {
        [SerializeField] private TriageDirector director;
        private void OnCollisionEnter(Collision collision) => director.CargoContact(collision.rigidbody);
    }
}
