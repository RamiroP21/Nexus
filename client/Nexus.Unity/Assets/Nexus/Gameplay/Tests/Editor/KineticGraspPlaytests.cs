using System.Collections;
using Nexus.Gameplay.Abilities;
using Nexus.Gameplay.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Nexus.Gameplay.Tests
{
    public sealed class KineticGraspPlaytests
    {
        private GameObject player, prop;
        private KineticGraspAbility grasp;
        private PhysicalTarget target;
        private KineticGraspSettings settings;
        private TargetingSettings targetingSettings;
        private SimulationMode previousSimulationMode;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            Time.timeScale = 1;
            previousSimulationMode = Physics.simulationMode; Physics.simulationMode = SimulationMode.FixedUpdate;
            player = new GameObject("Grasp fixture"); player.SetActive(false);
            var camera = player.AddComponent<Camera>();
            var targeting = player.AddComponent<ContextualTargeting>();
            targetingSettings = ScriptableObject.CreateInstance<TargetingSettings>();
            Set(targeting, "settings", targetingSettings); Set(targeting, "view", camera);
            Set(targeting, "origin", player.transform); Set(targeting, "owner", player.transform);
            grasp = player.AddComponent<KineticGraspAbility>(); settings = ScriptableObject.CreateInstance<KineticGraspSettings>();
            Set(grasp, "settings", settings); Set(grasp, "targeting", targeting);
            player.SetActive(true);
            prop = GameObject.CreatePrimitive(PrimitiveType.Cube); prop.transform.position = new Vector3(0, 0, 6);
            prop.AddComponent<Rigidbody>().useGravity = false; target = prop.AddComponent<PhysicalTarget>(); prop.AddComponent<Graspable>();
            Physics.SyncTransforms();
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.DestroyImmediate(player); Object.DestroyImmediate(prop);
            Object.DestroyImmediate(settings); Object.DestroyImmediate(targetingSettings);
            Physics.simulationMode = previousSimulationMode; Time.timeScale = 1;
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator AcquiresMovesPhysicallyAndReleasesWithoutOverridingBody()
        {
            Vector3 start = target.Body.position;
            Assert.That(grasp.TryAcquire(), Is.True);
            Assert.That(target.Body.position, Is.EqualTo(start), "Acquisition must not teleport.");
            Assert.That(grasp.TryAcquire(), Is.False, "A second acquisition cannot replace the held body.");
            float until = Time.time + 2f;
            while (Time.time < until) yield return null;
            Assert.That(Vector3.Distance(target.Body.position, grasp.HoldPoint), Is.LessThan(.15f));
            Assert.That(target.Body.isKinematic, Is.False);
            Assert.That(target.Body.useGravity, Is.False);
            target.Body.linearVelocity = Vector3.right;
            grasp.Release();
            Assert.That(grasp.IsHolding, Is.False);
            Assert.That(target.Body.linearVelocity, Is.EqualTo(Vector3.right));
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator RejectsMissingOptInMassRangeAndCoverAndBreaksOnCover()
        {
            prop.GetComponent<Graspable>().enabled = false; Assert.That(grasp.TryAcquire(), Is.False);
            prop.GetComponent<Graspable>().enabled = true;
            target.Body.mass = settings.MaximumMass + 1; Assert.That(grasp.TryAcquire(), Is.False); target.Body.mass = 1;
            target.Body.position = Vector3.forward * (settings.AcquireRange + 1); Physics.SyncTransforms();
            Assert.That(grasp.TryAcquire(), Is.False);
            target.Body.position = Vector3.forward * 6; Physics.SyncTransforms();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                wall.transform.position = Vector3.forward * 2; wall.transform.localScale = new Vector3(4, 4, .5f); Physics.SyncTransforms();
                Assert.That(grasp.TryAcquire(), Is.False);
                wall.SetActive(false); Physics.SyncTransforms(); Assert.That(grasp.TryAcquire(), Is.True);
                wall.SetActive(true); Physics.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null;
                Assert.That(grasp.IsHolding, Is.False); Assert.That(grasp.LastReleaseReason, Is.EqualTo(GraspReleaseReason.Occluded));
            }
            finally { Object.DestroyImmediate(wall); }
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator ReleasesOnDisableBreakResetAndVectorHandoff()
        {
            Assert.That(grasp.TryAcquire(), Is.True); grasp.ResetGrasp();
            Assert.That(grasp.LastReleaseReason, Is.EqualTo(GraspReleaseReason.Reset));
            Assert.That(grasp.TryAcquire(), Is.True); grasp.ReleaseForVector(target);
            Assert.That(grasp.LastReleaseReason, Is.EqualTo(GraspReleaseReason.Vector));
            Assert.That(grasp.TryAcquire(), Is.True); target.Body.position = Vector3.forward * (settings.BreakDistance + 1);
            Physics.SyncTransforms(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(grasp.IsHolding, Is.False); Assert.That(grasp.LastReleaseReason, Is.EqualTo(GraspReleaseReason.OutOfRange));
            target.Body.position = Vector3.forward * 6; Physics.SyncTransforms(); Assert.That(grasp.TryAcquire(), Is.True);
            grasp.enabled = false; Assert.That(grasp.IsHolding, Is.False);
            Assert.That(target.Body.isKinematic, Is.False); LogAssert.NoUnexpectedReceived();
        }
        private static void Set(Object component, string name, Object value)
        {
            var serialized = new SerializedObject(component); serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
