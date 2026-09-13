using System.Collections;
using System.IO;
using System.Linq;
using Nexus.Gameplay.Character;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Editor;
using Nexus.Gameplay.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Nexus.Gameplay.Tests
{
    // District session integration covers authored scene continuity and bounded evolution.
    public sealed class DistrictSessionPlaytests
    {
        private DistrictSessionDirector district;
        private DistrictCargoEmergency emergency;
        private UrbanBlockSituation crisis1;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene(SuperhumanPlaygroundAuthoring.DistrictScenePath);
            yield return new EnterPlayMode();
            district = Object.FindFirstObjectByType<DistrictSessionDirector>();
            emergency = Object.FindFirstObjectByType<DistrictCargoEmergency>();
            crisis1 = Object.FindFirstObjectByType<UrbanBlockSituation>();
            Assert.That(district && emergency && crisis1, Is.True, "District01 authoring must be applied.");
            Object.FindFirstObjectByType<LocalPlayerInput>().enabled = false;
            // The development bootstrap launches the external Host asynchronously;
            // allow bounded reconnect attempts to cover process startup latency.
            for (int i = 0; i < 100 && !district.AuthorityReady; i++)
                yield return new WaitForSecondsRealtime(0.05f);
            Assert.That(district.AuthorityReady, Is.True, "District01 must connect to the authoritative Host before playtest assertions.");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup() { yield return new ExitPlayMode(); }

        [UnityTest]
        public IEnumerator DistrictLoadsThreeZonesAndPhysicalConnectors()
        {
            Assert.That(GameObject.Find("Zone A - Market / Mixed Use"), Is.Not.Null);
            Assert.That(GameObject.Find("Zone B - Residential / Community"), Is.Not.Null);
            Assert.That(GameObject.Find("Zone C - Service / Infrastructure"), Is.Not.Null);
            Assert.That(GameObject.Find("Market to residential street"), Is.Not.Null);
            Assert.That(GameObject.Find("Market to service street"), Is.Not.Null);
            Assert.That(GameObject.Find("Residential vault shortcut"), Is.Not.Null);
            Assert.That(GameObject.Find("Service mantle shortcut"), Is.Not.Null);
            for (int i = 0; i < 120 && !district.State.Zone(DistrictZone.Market).Visited; i++) yield return null;
            Assert.That(district.State.Zone(DistrictZone.Market).Visited, Is.True);
            var motor = Object.FindFirstObjectByType<CharacterMotor>();
            motor.ResetMotion(new Vector3(28, .05f, 7)); Physics.SyncTransforms();
            for (int i = 0; i < 120 && !district.State.Zone(DistrictZone.Residential).Visited; i++) yield return null;
            Assert.That(district.State.Zone(DistrictZone.Residential).Visited, Is.True);
            motor.ResetMotion(new Vector3(-28, .05f, -8)); Physics.SyncTransforms();
            for (int i = 0; i < 120 && !district.State.Zone(DistrictZone.Service).Visited; i++) yield return null;
            Assert.That(district.State.Zone(DistrictZone.Service).Visited, Is.True);
            Capture("district-overview", new Vector3(42, 30, -40), new Vector3(0, 0, 0));
            Capture("district-zone-b", new Vector3(34, 12, 5), new Vector3(27, 1, 7));
            Capture("district-zone-c", new Vector3(-38, 12, -2), new Vector3(-28, 1, -8));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Crisis2OffersDiversionOrLiftAndOffscreenEvolution()
        {
            Assert.That(emergency.CargoCount, Is.EqualTo(2));
            Assert.That(emergency.Begin(false), Is.True);
            Capture("district-crisis2-active", new Vector3(-20, 9, -18), new Vector3(-23, 1, -12));
            var first = emergency.Cargo[0]; first.position += Vector3.forward * 4 + Vector3.up * 4; Physics.SyncTransforms(); emergency.Tick(.02f, true);
            Assert.That(emergency.Outcome(0), Is.EqualTo(CargoOutcome.Diverted));
            emergency.ResetEmergency();
            Assert.That(emergency.Begin(false), Is.True);
            var second = emergency.Cargo[1]; second.position += Vector3.up * 4; Physics.SyncTransforms(); emergency.Tick(.02f, true);
            Assert.That(emergency.Outcome(1), Is.EqualTo(CargoOutcome.Diverted));
            emergency.ResetEmergency(); Assert.That(emergency.Begin(true), Is.True);
            for (int i = 0; i < 35; i++) emergency.Tick(1, false);
            Assert.That(emergency.State, Is.EqualTo(DistrictCrisisState.Failed));
            Assert.That(emergency.RouteBlocked, Is.True);
            Assert.That(GameObject.Find("Debris blocks service shortcut").activeSelf, Is.True);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static void Capture(string name, Vector3 position, Vector3 lookAt)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var camera = Camera.main;
            if (!camera) return;
            var previousPosition = camera.transform.position; var previousRotation = camera.transform.rotation;
            var texture = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.transform.position = position; camera.transform.LookAt(lookAt);
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = texture });
                RenderTexture.active = texture; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/District01"); File.WriteAllBytes("Logs/District01/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.transform.SetPositionAndRotation(previousPosition, previousRotation); RenderTexture.active = previous;
                texture.Release(); Object.DestroyImmediate(texture); Object.DestroyImmediate(pixels);
            }
        }

        [UnityTest]
        public IEnumerator F8ResetRestoresCrisis2PropsAndSessionMemory()
        {
            district.State.Visit(DistrictZone.Service);
            Assert.That(emergency.Begin(true), Is.True);
            emergency.Cargo[0].position += Vector3.right * 3; Physics.SyncTransforms();
            crisis1.ResetBlock();
            Assert.That(district.State.Crisis1, Is.EqualTo(DistrictCrisisState.Dormant));
            Assert.That(district.State.Crisis2, Is.EqualTo(DistrictCrisisState.Dormant));
            Assert.That(district.State.Zone(DistrictZone.Service).Visited, Is.False);
            Assert.That(emergency.State, Is.EqualTo(DistrictCrisisState.Dormant));
            Assert.That(Vector3.Distance(emergency.Cargo[0].position, emergency.Cargo[1].position), Is.GreaterThan(.5f));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Crisis1InfrastructureLossChangesCrisis2InitialCondition()
        {
            var pressure = crisis1.CompoundPressure;
            var motor = Object.FindFirstObjectByType<CharacterMotor>();
            motor.ResetMotion(new Vector3(0, .05f, 3)); Physics.SyncTransforms(); crisis1.Evaluate(); crisis1.Tick(4.1f);
            pressure.ExitObstruction.position += Vector3.forward * 4;
            var residentBody = pressure.Resident.GetComponent<Rigidbody>(); residentBody.position = pressure.Resident.Refuge.position;
            pressure.Resident.transform.position = residentBody.position; Physics.SyncTransforms(); pressure.Resident.Tick(.02f);
            pressure.Tick(40);
            crisis1.Hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); crisis1.Evaluate();
            Assert.That(crisis1.Phase, Is.EqualTo(UrbanBlockPhase.Aftermath));
            for (int i = 0; i < 300 && district.State.Crisis1 != DistrictCrisisState.Failed; i++) yield return null;
            yield return null;
            Assert.That(district.State.Crisis1, Is.EqualTo(DistrictCrisisState.Failed));
            Assert.That(district.State.ServiceDegraded, Is.True);
            district.State.Tick(6);
            Assert.That(district.State.CanStartCrisis2(6), Is.True);
            Assert.That(emergency.Begin(district.State.ServiceDegraded), Is.True);
            Assert.That(emergency.InheritedDamage, Is.True);
            Assert.That(emergency.Deadline, Is.LessThan(42));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
