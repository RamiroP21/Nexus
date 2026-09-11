using System.Collections;
using System.IO;
using System.Linq;
using Nexus.Gameplay.Abilities;
using Nexus.Gameplay.CameraSystem;
using Nexus.Gameplay.Character;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Editor;
using Nexus.Gameplay.Interaction;
using Nexus.Gameplay.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Nexus.Gameplay.Tests
{
    public sealed class UrbanBlockPlaytests
    {
        private UrbanBlockSituation block;
        private PlayerVitality player;
        private CharacterMotor motor;
        private HostileCombatant hostile;
        private float capture;
        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene(SuperhumanPlaygroundAuthoring.UrbanScenePath);
            yield return new EnterPlayMode();
            capture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60;
            block = Object.FindFirstObjectByType<UrbanBlockSituation>(); player = Object.FindFirstObjectByType<PlayerVitality>();
            motor = player.GetComponent<CharacterMotor>(); hostile = block.Hostile;
            player.GetComponent<LocalPlayerInput>().enabled = false; Time.timeScale = 1;
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        { Time.captureDeltaTime = capture; Time.timeScale = 1; yield return new ExitPlayMode(); }
        private void Place(Vector3 position)
        { motor.ResetMotion(position); Physics.SyncTransforms(); StepMotor(.2f); }
        private void StepMotor(float seconds)
        { for (int i = 0; i < Mathf.CeilToInt(seconds * 60); i++) motor.Tick(Vector2.zero, false, false, 0, 1f / 60, Time.timeAsDouble + i / 60.0); }
        private void Engage()
        { Place(new Vector3(0, .05f, 3)); block.Evaluate(); hostile.Tick(.01f); block.Evaluate(); Assert.That(hostile.Target, Is.EqualTo(player)); }

        [UnityTest]
        public IEnumerator CalmResidentsMoveLocallyAndCrossingInterruptsCurrentPositions()
        {
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Calm));
            var civil = block.Civilians[0]; Vector3 initial = civil.transform.position;
            Assert.That(civil.ActivityAnchorCount, Is.EqualTo(2));
            for (int i = 0; i < 240; i++) yield return new WaitForFixedUpdate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Calm));
            Assert.That(hostile.Target, Is.Null); Assert.That(hostile.AttackCount, Is.Zero);
            Assert.That(Vector3.Distance(initial, civil.transform.position), Is.InRange(.3f, 1.6f));
            CaptureFrame("02b-calm-local-activity", new Vector3(0, 3, -5), new Vector3(4, 1, 3));
            Vector3 interrupted = civil.transform.position;
            Engage();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Incident));
            Assert.That(Vector3.Distance(interrupted, civil.transform.position), Is.LessThan(.02f));
            civil.Tick(.02f);
            Assert.That(civil.Activity, Is.EqualTo(CivilianActivity.Interrupted));
            Assert.That(civil.State, Is.EqualTo(CivilianState.Fleeing));
            CaptureFrame("02b-activity-interrupted", new Vector3(0, 3, -5), new Vector3(4, 1, 3));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AftermathSurvivorsRecoverLocallyWithoutErasingInjuryOrReturnState()
        {
            Engage();
            var injured = block.Civilians[0]; var survivor = block.Civilians[1];
            injured.GetComponent<DamageReceiver>().Receive(new Damage(12));
            for (int i = 0; i < 190; i++) yield return new WaitForFixedUpdate();
            Assert.That(survivor.State, Is.EqualTo(CivilianState.Sheltered));
            hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); block.Evaluate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Aftermath));
            Vector3 sheltered = survivor.transform.position;
            for (int i = 0; i < 480; i++) yield return new WaitForFixedUpdate();
            Assert.That(survivor.Activity, Is.EqualTo(CivilianActivity.Cautious));
            Assert.That(Vector3.Distance(sheltered, survivor.transform.position), Is.InRange(.3f, 1f));
            Assert.That(injured.Harmed, Is.True);
            CaptureFrame("02b-aftermath-survivor", new Vector3(5, 3, -1), new Vector3(10, 1, 4));
            Place(new Vector3(0, .05f, -16)); yield return null; Place(new Vector3(0, .05f, 3)); block.Evaluate();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Aftermath));
            Assert.That(injured.Harmed, Is.True); Assert.That(survivor.Activity, Is.EqualTo(CivilianActivity.Cautious));
            block.ToggleDebug(); block.ToggleDebug();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Aftermath));
            block.ResetBlock();
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Calm));
            Assert.That(injured.Harmed, Is.False); Assert.That(hostile.Target, Is.Null);
            Assert.That(block.Civilians.All(c => c.Activity == CivilianActivity.Waiting), Is.True);
            yield return null;
            Assert.That(block.Phase, Is.EqualTo(UrbanBlockPhase.Calm));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DebugDefaultsOffTogglesWithoutChangingSituationOrSignage()
        {
            Assert.That(block.DebugVisible, Is.False);
            Assert.That(player.ShowDiagnostics, Is.False);
            var label = block.Civilians[0].GetComponentInChildren<TextMesh>().GetComponent<Renderer>();
            Assert.That(label.enabled, Is.False);
            var sign = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Single(t => t.text == "MERCADO 24");
            Assert.That(sign.GetComponent<Renderer>().enabled, Is.True);
            CaptureFrame("debug-off-street", new Vector3(0, 2.3f, -8), new Vector3(0, 1, 5));
            var state = block.State;
            block.ToggleDebug(); Assert.That(label.enabled, Is.True); Assert.That(player.ShowDiagnostics, Is.True);
            CaptureFrame("debug-on-street", new Vector3(0, 2.3f, -8), new Vector3(0, 1, 5));
            block.ToggleDebug(); Assert.That(label.enabled, Is.False); Assert.That(block.State, Is.EqualTo(state));
            Assert.That(sign.GetComponent<Renderer>().enabled, Is.True);
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InjuryPoseRemainsPhysicalAfterReturnAndResetRestoresUpright()
        {
            var civil = block.Civilians[0];
            civil.GetComponent<DamageReceiver>().Receive(new Damage(12)); civil.Tick(.02f);
            Assert.That(civil.Visual.localPosition.y, Is.LessThan(-.3f));
            var injuryPose = civil.Visual.localRotation;
            CaptureFrame("resident-injury-close", new Vector3(2, 2.8f, -1), new Vector3(4, .8f, 2));
            hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); block.Evaluate();
            Place(new Vector3(0, .05f, -16)); yield return null; Place(new Vector3(0, .05f, 3));
            Assert.That(Quaternion.Angle(civil.Visual.localRotation, injuryPose), Is.LessThan(.01f));
            CaptureFrame("resident-injury-return", new Vector3(2, 2.8f, -1), new Vector3(4, .8f, 2));
            Assert.That(block.DebugVisible, Is.False);
            block.ResetBlock();
            Assert.That(civil.Visual.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(Quaternion.Angle(civil.Visual.localRotation, Quaternion.identity), Is.LessThan(.01f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ProductionSceneHasAcceptedRigSemanticZonesAndNoExperimentalDependencies()
        {
            Assert.That(Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(player.GetComponent<KineticVectorAbility>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<TrainingSentinel>(), Is.Null);
            Assert.That(player.GetComponent<CombatEncounter>(), Is.Null, "Urban outcomes do not hijack the jump/reset input.");
            foreach (string zone in new[] { "Main street - Calle Mercado", "Crossing and corner market", "Residential access - Patio Sur", "Service alley and roof shortcut" })
                Assert.That(GameObject.Find(zone), Is.Not.Null);
            Assert.That(AssetDatabase.GetDependencies(SuperhumanPlaygroundAuthoring.UrbanScenePath).Any(p => p.Contains("Experimental")), Is.False);
            Assert.That(block.Civilians.Length, Is.EqualTo(2));
            Assert.That(block.State, Is.EqualTo(UrbanSituationState.Quiet));
            Assert.That(block.Civilians.All(c => c.State == CivilianState.Calm), Is.True);
            CaptureFrame("block-overview", new Vector3(19, 25, -26), new Vector3(0, 0, 0));
            CaptureFrame("commercial-area", new Vector3(1, 8, -1), new Vector3(-6, 1, 5));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WorldDangerMakesResidentsFleeAndCartBlocksOneExit()
        {
            Engage();
            CaptureFrame("incident-state", new Vector3(5, 6, 8), new Vector3(0, 1, 10));
            foreach (var civil in block.Civilians) civil.Tick(.02f);
            Assert.That(block.Civilians.Any(c => c.State == CivilianState.Fleeing), Is.True);
            for (int i = 0; i < 190; i++) yield return new WaitForFixedUpdate();
            Assert.That(block.Civilians.Any(c => c.State == CivilianState.Blocked), Is.True);
            Assert.That(block.Civilians.Any(c => c.State == CivilianState.Sheltered), Is.True);
            CaptureFrame("civilian-reaction", new Vector3(3, 7, 10), new Vector3(8, 1, 4));
            Assert.That(block.State, Is.EqualTo(UrbanSituationState.Danger));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ClearingPhysicalCartAllowsRefugeAndSecuredOutcomePersistsAfterReturn()
        {
            Engage();
            var cart = GameObject.Find("Delivery cart - clear residential exit").GetComponent<PhysicalTarget>();
            cart.ReceiveForce(new ForceImpact(Vector3.forward * 36, cart.AimPoint));
            for (int i = 0; i < 190; i++) yield return new WaitForFixedUpdate();
            Assert.That(block.Civilians.All(c => c.State == CivilianState.Sheltered), Is.True, "Removing a real physical obstacle opens the refuge route.");
            hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); block.Evaluate();
            Assert.That(block.State, Is.EqualTo(UrbanSituationState.Secured));
            CaptureFrame("outcome-a", new Vector3(4, 9, 10), new Vector3(8, 1, 3));
            string notice = block.Notice;
            Place(new Vector3(0, .05f, -16)); yield return null; Place(new Vector3(0, .05f, 3)); block.Evaluate();
            Assert.That(block.Notice, Is.EqualTo(notice)); Assert.That(player.Depleted, Is.False);
            Assert.That(block.Civilians.All(c => c.State == CivilianState.Sheltered), Is.True);
            CaptureFrame("returned-consequence", new Vector3(4, 9, 10), new Vector3(8, 1, 3));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PhysicalHazardInjuresResidentAndConsequenceSurvivesCombatAndReturn()
        {
            Engage(); var civil = block.Civilians[0];
            var template = Object.FindObjectsByType<TrainingHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(h => !h.gameObject.activeSelf);
            var hazard = Object.Instantiate(template, civil.transform.position + new Vector3(-2, 1, 0), Quaternion.identity);
            hazard.gameObject.SetActive(true); hazard.Launch(hostile.gameObject, Vector3.right * 10);
            CaptureFrame("hazard-flight", new Vector3(4, 5, 6), new Vector3(0, 1, 3));
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.That(civil.Harmed, Is.True); Assert.That(civil.State, Is.EqualTo(CivilianState.Injured));
            hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); block.Evaluate();
            Assert.That(block.State, Is.EqualTo(UrbanSituationState.SecuredWithCasualties));
            Assert.That(block.Notice, Does.Contain("INJURED"));
            CaptureFrame("outcome-b", new Vector3(4, 9, 10), new Vector3(8, 1, 3));
            Place(new Vector3(0, .05f, -16)); yield return null; Place(new Vector3(0, .05f, 3)); block.Evaluate();
            Assert.That(civil.Harmed, Is.True); Assert.That(civil.GetComponentInChildren<TextMesh>().text, Does.Contain("INJURED"));
            Assert.That(player.Depleted, Is.False); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TargetingUnderUrbanPressureUsesExistingKineticContract()
        {
            Engage(); hostile.Tick(.5f);
            Assert.That(hostile.State, Is.EqualTo(HostileState.Telegraph));
            CaptureFrame("hostile-telegraph", new Vector3(4, 3, 6), new Vector3(0, 1.3f, 11));
            var camera = Object.FindFirstObjectByType<ThirdPersonCamera>(); camera.enabled = false;
            camera.transform.position = new Vector3(0, 1.2f, 2); camera.transform.LookAt(hostile.GetComponent<PhysicalTarget>().AimPoint);
            var selector = player.GetComponent<ContextualTargeting>(); selector.Suspended = false; Physics.SyncTransforms();
            Assert.That(selector.TrySelect(out var selected), Is.True); Assert.That(selected.Target, Is.EqualTo(hostile.GetComponent<PhysicalTarget>()));
            Assert.That(player.GetComponent<KineticVectorAbility>().TryActivate(), Is.EqualTo(ActivationResult.Activated));
            Assert.That(hostile.State, Is.EqualTo(HostileState.Staggered));
            CaptureFrame("hostile-stagger", new Vector3(4, 3, 6), new Vector3(0, 1.3f, 11));
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WasteContainerStopsHazardAndPhysicalPropDamagesHostile()
        {
            var cover = GameObject.Find("Steel waste container - movable cover").GetComponent<PhysicalTarget>();
            var template = Object.FindObjectsByType<TrainingHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(h => !h.gameObject.activeSelf);
            var hazard = Object.Instantiate(template, new Vector3(-3, 1, 10), Quaternion.identity);
            hazard.gameObject.SetActive(true); hazard.Launch(hostile.gameObject, Vector3.back * 10);
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.That(hazard.Spent, Is.True);
            var cart = GameObject.Find("Delivery cart - clear residential exit").GetComponent<Rigidbody>();
            cart.position = hostile.transform.position + new Vector3(0, .85f, -2.5f); cart.linearVelocity = Vector3.forward * 14; Physics.SyncTransforms();
            for (int i = 0; i < 15; i++) yield return new WaitForFixedUpdate();
            Assert.That(hostile.GetComponent<DamageReceiver>().Health.Current, Is.LessThan(36));
            cover.ReceiveForce(new ForceImpact(Vector3.right * 36, cover.AimPoint));
            Assert.That(cover.GetComponent<ImpactBarrier>().Released, Is.True); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UrbanServiceRouteSupportsVaultMantleAndReturn()
        {
            Place(new Vector3(-6.6f, .05f, -12));
            CaptureFrame("traversal-route", new Vector3(-1, 6, -8), new Vector3(-6, 1, -8));
            Assert.That(motor.TryBeginTraversal(Vector3.forward), Is.True); Assert.That(motor.State, Is.EqualTo(MotorState.Vault));
            StepMotor(1.5f); Assert.That(motor.transform.position.z, Is.GreaterThan(-9.5f));
            Place(new Vector3(-6.5f, .05f, -8));
            Assert.That(motor.TryBeginTraversal(Vector3.forward), Is.True); Assert.That(motor.State, Is.EqualTo(MotorState.Mantle));
            StepMotor(1.5f); Assert.That(motor.transform.position.y, Is.GreaterThan(1.6f));
            CaptureFrame("residential-service", new Vector3(4, 8, -3), new Vector3(-6, 1, -7));
            Place(new Vector3(0, .05f, -14)); Assert.That(motor.Grounded, Is.True);
            yield return null; LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DevelopmentResetRestoresCiviliansWorldThreatAndPlayer()
        {
            Engage(); var civil = block.Civilians[0]; civil.GetComponent<DamageReceiver>().Receive(new Damage(100));
            var cart = GameObject.Find("Delivery cart - clear residential exit").GetComponent<Rigidbody>(); Vector3 spawn = cart.position;
            cart.position += Vector3.forward * 4;
            hostile.GetComponent<DamageReceiver>().Receive(new Damage(100)); player.ReceiveHit(new Damage(100), Vector3.zero); block.Evaluate();
            block.ResetBlock();
            Assert.That(block.State, Is.EqualTo(UrbanSituationState.Quiet)); Assert.That(civil.State, Is.EqualTo(CivilianState.Calm));
            Assert.That(civil.Harmed, Is.False); Assert.That(player.Depleted, Is.False); Assert.That(hostile.State, Is.EqualTo(HostileState.Idle));
            Assert.That(Vector3.Distance(cart.position, spawn), Is.LessThan(.03f)); Assert.That(block.Notice, Does.Contain("Delivery entrance"));
            yield return null; LogAssert.NoUnexpectedReceived();
        }
        private static void CaptureFrame(string name, Vector3 position, Vector3 lookAt)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
            var camera = Camera.main;
            Vector3 previousPosition = camera.transform.position; Quaternion rotation = camera.transform.rotation;
            var texture = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.transform.position = position; camera.transform.LookAt(lookAt);
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = texture });
                RenderTexture.active = texture; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/UrbanBlock02A"); File.WriteAllBytes("Logs/UrbanBlock02A/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.transform.SetPositionAndRotation(previousPosition, rotation); RenderTexture.active = previous;
                texture.Release(); Object.DestroyImmediate(texture); Object.DestroyImmediate(pixels);
            }
        }
    }
}
