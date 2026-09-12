using System.Collections;
using System.IO;
using Nexus.Gameplay.Character;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Editor;
using Nexus.Gameplay.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Nexus.Gameplay.Tests
{
    public sealed class CompoundCrisisPlaytests
    {
        private UrbanBlockSituation block;
        private CompoundCrisisPressure pressure;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene(SuperhumanPlaygroundAuthoring.UrbanScenePath);
            yield return new EnterPlayMode();
            block = Object.FindFirstObjectByType<UrbanBlockSituation>(); pressure = block.CompoundPressure;
            Assert.That(pressure, Is.Not.Null, "ApplyCompoundCrisis authoring must be saved.");
            Object.FindFirstObjectByType<LocalPlayerInput>().enabled = false;
        }
        [UnityTearDown] public IEnumerator Cleanup() { yield return new ExitPlayMode(); }
        private void Begin()
        {
            Object.FindFirstObjectByType<CharacterMotor>().ResetMotion(new Vector3(0, .05f, 3));
            Physics.SyncTransforms(); block.Evaluate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Warning));
            Assert.That(block.Hostile.Target, Is.Null);
            Assert.That(pressure.Civilian, Is.EqualTo(PressureOutcome.Pending));
            CaptureFrame("02c-warning", new Vector3(5, 4, 8), new Vector3(0, 1, 5));
            block.Tick(4.1f);
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Incident));
            CaptureFrame("02c-three-pressures", new Vector3(18, 14, -20), new Vector3(0, 0, 0));
            block.ToggleDebug(); CaptureFrame("02c-debug", new Vector3(18, 14, -20), new Vector3(0, 0, 0)); block.ToggleDebug();
        }
        [UnityTest]
        public IEnumerator HostileFirstDoesNotResolveOtherPressuresAndFailurePersistsOnReturn()
        {
            Begin(); block.Hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); block.Evaluate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Incident));
            Assert.That(pressure.Civilian, Is.EqualTo(PressureOutcome.Active));
            block.Tick(40);
            Assert.That(pressure.Civilian, Is.EqualTo(PressureOutcome.Failed));
            Assert.That(pressure.Infrastructure, Is.EqualTo(PressureOutcome.Failed));
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Aftermath));
            CaptureFrame("02c-imperfect-aftermath", new Vector3(-2, 5, -3), new Vector3(-5, 1, -1));
            var motor = Object.FindFirstObjectByType<CharacterMotor>();
            motor.ResetMotion(new Vector3(0, .05f, -16)); block.Tick(10); motor.ResetMotion(new Vector3(0, .05f, 3)); block.Tick(1);
            block.ToggleDebug(); block.ToggleDebug();
            Assert.That(pressure.InfrastructureDamaged, Is.True); Assert.That(pressure.Resident.Harmed, Is.True);
            block.ResetBlock();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Calm));
            Assert.That(pressure.InfrastructureDamaged, Is.False); Assert.That(pressure.Resident.Harmed, Is.False);
            Assert.That(pressure.Civilian, Is.EqualTo(PressureOutcome.Pending));
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator InfrastructureFirstAndSwitchingKeepsIndependentProgress()
        {
            Begin(); block.Tick(5);
            float civilianProgress = pressure.CivilianProgress;
            pressure.InfrastructureLoad.position += Vector3.forward * 4; Physics.SyncTransforms(); block.Tick(.02f);
            Assert.That(pressure.Infrastructure, Is.EqualTo(PressureOutcome.Resolved));
            Assert.That(pressure.Civilian, Is.EqualTo(PressureOutcome.Active));
            CaptureFrame("02c-infrastructure-resolved", new Vector3(-2, 5, -4), new Vector3(-5, 1, -1));
            Assert.That(pressure.CivilianProgress, Is.GreaterThanOrEqualTo(civilianProgress));
            block.Tick(25);
            Assert.That(pressure.Infrastructure, Is.EqualTo(PressureOutcome.Resolved));
            Assert.That(pressure.Civilian, Is.EqualTo(PressureOutcome.Failed));
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Incident));
            block.Hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); block.Evaluate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Aftermath));
            CaptureFrame("02c-civilian-fails-infrastructure-stable", new Vector3(-2, 5, -4), new Vector3(-5, 1, -1));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        private static void CaptureFrame(string name, Vector3 position, Vector3 lookAt)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var camera = Camera.main;
            Vector3 previousPosition = camera.transform.position; Quaternion previousRotation = camera.transform.rotation;
            var texture = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.transform.position = position; camera.transform.LookAt(lookAt);
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = texture });
                RenderTexture.active = texture; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/UrbanBlock02A");
                File.WriteAllBytes("Logs/UrbanBlock02A/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.transform.SetPositionAndRotation(previousPosition, previousRotation); RenderTexture.active = previous;
                texture.Release(); Object.DestroyImmediate(texture); Object.DestroyImmediate(pixels);
            }
        }
        [UnityTest]
        public IEnumerator TimelyRescueAndUnloadProduceIndependentSuccessBeforeHostile()
        {
            Begin(); pressure.ExitObstruction.position += Vector3.forward * 4;
            // Place at the refuge to isolate the consequence contract from already covered locomotion.
            var body = pressure.Resident.GetComponent<Rigidbody>(); body.position = pressure.Resident.Refuge.position;
            pressure.Resident.transform.position = body.position; Physics.SyncTransforms(); pressure.Resident.Tick(.02f);
            block.Tick(.02f);
            Assert.That(pressure.Civilian, Is.EqualTo(PressureOutcome.Resolved));
            Assert.That(pressure.Infrastructure, Is.EqualTo(PressureOutcome.Active));
            pressure.InfrastructureLoad.position += Vector3.forward * 4; block.Tick(.02f);
            Assert.That(pressure.Complete, Is.True); Assert.That(pressure.HasLoss, Is.False);
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Incident));
            block.Hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); block.Evaluate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Aftermath));
            CaptureFrame("02c-favorable-aftermath", new Vector3(7, 6, 7), new Vector3(4, 1, 2));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ContinuousSliceJourneyKeepsControlAcrossWarningCrisisAftermathAndReturn()
        {
            var motor = Object.FindFirstObjectByType<CharacterMotor>();
            for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Calm));
            motor.ResetMotion(new Vector3(0, .05f, 3)); Physics.SyncTransforms(); block.Evaluate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Warning));
            yield return null;
            block.Tick(4.1f); Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Incident));
            pressure.ExitObstruction.position += Vector3.forward * 4;
            var residentBody = pressure.Resident.GetComponent<Rigidbody>(); residentBody.position = pressure.Resident.Refuge.position;
            pressure.Resident.transform.position = residentBody.position; Physics.SyncTransforms(); pressure.Resident.Tick(.02f);
            pressure.InfrastructureLoad.position += Vector3.forward * 4; block.Tick(.02f);
            Assert.That(pressure.Civilian, Is.EqualTo(PressureOutcome.Resolved));
            Assert.That(pressure.Infrastructure, Is.EqualTo(PressureOutcome.Resolved));
            block.Hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); block.Evaluate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Aftermath));
            CaptureFrame("02c-continuous-aftermath", new Vector3(10, 8, 13), new Vector3(4, 1, 2));
            motor.ResetMotion(new Vector3(0, .05f, -16)); yield return new WaitForFixedUpdate();
            motor.ResetMotion(new Vector3(0, .05f, 3)); block.Evaluate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Aftermath));
            block.ResetBlock(); Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Calm));
            yield return null; LogAssert.NoUnexpectedReceived();
        }
    }
}
