using System.Collections;
using System.IO;
using System.Linq;
using Nexus.Feel01;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nexus.Reactivity01.Tests
{
    public sealed class ReactionPlaytests
    {
        private const string ScenePath = "Assets/Nexus/Experimental/Reactivity01.unity";
        private FeelPlayer player;
        private FeelCamera orbit;
        private KineticPulse pulse;
        private ReactionStage stage;
        private FeelArena arena;
        private ReactiveActor[] actors;
        private Gamepad pad;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditor;
        private float previousCapture;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath);
            yield return new EnterPlayMode();
            previousCapture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60;
            previousMode = InputSystem.settings.updateMode;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad = InputSystem.AddDevice<Gamepad>();
            player = Object.FindFirstObjectByType<FeelPlayer>(); orbit = Object.FindFirstObjectByType<FeelCamera>();
            pulse = Object.FindFirstObjectByType<KineticPulse>(); stage = Object.FindFirstObjectByType<ReactionStage>();
            arena = Object.FindFirstObjectByType<FeelArena>(); actors = Object.FindObjectsByType<ReactiveActor>(FindObjectsSortMode.None);
            Assert.That(player && orbit && pulse && stage && arena, Is.True);
            player.Actions.devices = new InputDevice[] { pad }; player.Actions.bindingMask = InputBinding.MaskByGroup("Gamepad");
            player.SetPaused(false);
            yield return Advance(.3f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (player) player.SetPaused(false);
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            InputSystem.settings.updateMode = previousMode;
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
            Time.captureDeltaTime = previousCapture;
            yield return new ExitPlayMode();
            Assert.That(Application.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator SceneMovementCameraPowerMassesAndResetRemainUsable()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == ScenePath && s.enabled), Is.True);
            Assert.That(stage.Settings.IsValid, Is.True);
            Assert.That(actors.Count(a => a.Role == ReactionRole.Civil), Is.EqualTo(2));
            Assert.That(actors.Count(a => a.Role == ReactionRole.Enemy), Is.EqualTo(1));
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true)) Assert.That(component, Is.Not.Null);
            Capture("01-courtyard");
            InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.LeftStick));
            yield return Advance(.6f);
            Assert.That(new Vector2(player.Velocity.x, player.Velocity.z).magnitude, Is.GreaterThan(9));
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
            yield return Advance(.2f);
            Assert.That(player.Grounded, Is.False);
            float yaw = orbit.Yaw;
            InputSystem.QueueStateEvent(pad, new GamepadState { rightStick = new Vector2(.4f, 0) });
            yield return Advance(.3f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw, orbit.Yaw)), Is.GreaterThan(5));
            InputSystem.QueueStateEvent(pad, default(GamepadState));
            arena.ResetArena(); yield return Advance(.25f);
            var light = Body("LIGHT / 2 kg"); var heavy = Body("HEAVY / 12 kg");
            Assert.That(pulse.Fire(new Ray(light.worldCenterOfMass - Vector3.forward * 2, Vector3.forward)), Is.True);
            Assert.That(pulse.LastBody, Is.SameAs(light)); yield return Advance(.05f);
            float lightSpeed = light.linearVelocity.z;
            yield return Advance(player.Settings.cooldown);
            Assert.That(pulse.Fire(new Ray(heavy.worldCenterOfMass - Vector3.forward * 2, Vector3.forward)), Is.True);
            Assert.That(pulse.LastBody, Is.SameAs(heavy)); yield return Advance(.05f);
            Assert.That(heavy.linearVelocity.z, Is.GreaterThan(.5f));
            Assert.That(lightSpeed, Is.GreaterThan(heavy.linearVelocity.z * 3));
            Record("mass", $"light={lightSpeed:F2} m/s; heavy={heavy.linearVelocity.z:F2} m/s");
            player.SetPaused(true);
            var position = actors[0].transform.position;
            for (int i = 0; i < 8; i++) yield return null;
            Assert.That(actors[0].transform.position, Is.EqualTo(position));
            arena.ResetArena(); yield return Advance(.1f);
            Assert.That(player.Paused, Is.False);
            Assert.That(actors.Where(a => a.Role == ReactionRole.Civil).All(a => a.State == ReactionState.Idle), Is.True);
            Assert.That(actors.All(a => a.ReactionCount == 0), Is.True);
            AssertFinite(); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator NearbyDistantDirectAndThreatStimuliHaveVisibleBoundedResponses()
        {
            var civil = actors.Single(a => a.name == "Civil east");
            Vector3 initial = civil.transform.position;
            stage.Report(StimulusKind.KineticImpact, initial + Vector3.right * (stage.Settings.reactionRadius + 1), 36, null);
            Assert.That(civil.State, Is.EqualTo(ReactionState.Idle));
            stage.Report(StimulusKind.PhysicalImpact, initial, stage.Settings.impactSpeedThreshold - .1f, null);
            Assert.That(civil.State, Is.EqualTo(ReactionState.Idle));
            stage.Report(StimulusKind.KineticImpact, initial - Vector3.forward * 2, 36, null);
            Assert.That(civil.State, Is.EqualTo(ReactionState.React));
            PlacePlayer(new Vector3(7, .05f, 1)); AimAt(initial);
            yield return Advance(.1f); Capture("02-civil-startle");
            yield return Advance(stage.Settings.startleDuration + .5f);
            Assert.That(civil.State, Is.EqualTo(ReactionState.Flee));
            Assert.That(civil.transform.position.z, Is.GreaterThan(initial.z + .5f));
            Capture("03-civil-flee");
            Record("civil", $"flee displacement={Vector3.Distance(initial, civil.transform.position):F2} m; state={civil.State}");
            yield return Advance(stage.Settings.fleeDuration + .2f);
            Assert.That(civil.State, Is.EqualTo(ReactionState.Idle));
            arena.ResetArena(); yield return Advance(.1f);
            stage.Report(StimulusKind.Threat, civil.transform.position - Vector3.right, 1, null);
            Assert.That(civil.State, Is.EqualTo(ReactionState.React));
            Assert.That(civil.LastStimulus, Is.EqualTo(StimulusKind.Threat));
            arena.ResetArena(); yield return Advance(player.Settings.cooldown + .1f);
            Assert.That(Vector3.Distance(civil.transform.position, civil.Position), Is.LessThan(.1f), "Reset refreshes interpolated presentation");
            // Real direct hit, including body impulse, unlike the nearby-only event above.
            PlacePlayer(new Vector3(5, .05f, 1));
            var body = civil.GetComponent<Rigidbody>();
            Assert.That(pulse.Fire(new Ray(body.worldCenterOfMass - Vector3.forward * 2, Vector3.forward)), Is.True);
            Assert.That(pulse.LastBody, Is.SameAs(body));
            Record("direct-civil", $"physical distance={Vector3.Distance(civil.Position, pulse.LastPoint):F2} m; visual drift={Vector3.Distance(civil.Position, civil.transform.position):F3} m; state={civil.State}");
            Assert.That(civil.State, Is.EqualTo(ReactionState.React));
            yield return Advance(.1f);
            Assert.That(body.linearVelocity.z, Is.GreaterThan(.5f));
            AssertFinite(); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EnemyLosesPositionThenRecoversItsApproach()
        {
            var enemy = actors.Single(a => a.Role == ReactionRole.Enemy);
            var body = enemy.GetComponent<Rigidbody>();
            Assert.That(enemy.State, Is.EqualTo(ReactionState.Approach));
            float beforeApproach = body.position.z; yield return Advance(.5f);
            Assert.That(body.position.z, Is.LessThan(beforeApproach - .2f));
            Vector3 before = body.position;
            Assert.That(pulse.Fire(new Ray(body.worldCenterOfMass - Vector3.forward * 2, Vector3.forward)), Is.True);
            Assert.That(pulse.LastBody, Is.SameAs(body));
            Assert.That(enemy.State, Is.EqualTo(ReactionState.Recover));
            yield return Advance(.2f);
            Assert.That(body.position.z, Is.GreaterThan(before.z + .1f));
            AimAt(body.position); Capture("04-enemy-displaced");
            yield return Advance(stage.Settings.recoveryDuration + .5f);
            Assert.That(enemy.State, Is.EqualTo(ReactionState.Approach));
            float recovered = body.position.z; yield return Advance(.8f);
            Assert.That(body.position.z, Is.LessThan(recovered - .3f));
            Capture("05-enemy-recovers");
            Record("enemy", $"initial z={before.z:F2}; recovery z={recovered:F2}; approach z={body.position.z:F2}");
            var civil = actors.Single(a => a.name == "Civil east");
            PlacePlayer(new Vector3(5, .05f, 1));
            body.position = civil.Position + Vector3.forward * 2.5f;
            body.linearVelocity = Vector3.zero;
            yield return Advance(stage.Settings.threatInterval + .2f);
            Assert.That(civil.ReactionCount, Is.GreaterThan(0), "Approaching enemy emits its own local threat");
            Assert.That(civil.LastStimulus, Is.EqualTo(StimulusKind.Threat));
            AimAt(civil.Position); yield return Advance(.1f); Capture("09-local-threat");
            AssertFinite(); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator KineticBoxCollisionTriggersAnIndirectCivilReaction()
        {
            var civil = actors.Single(a => a.name == "Civil west");
            var light = Body("CHAIN LIGHT / 2 kg");
            Assert.That(Vector3.Distance(civil.transform.position, light.position), Is.GreaterThan(stage.Settings.reactionRadius + 1));
            PlacePlayer(new Vector3(-7, .05f, -5)); AimAt(light.position);
            Capture("06-chain-before");
            Assert.That(pulse.Fire(new Ray(light.worldCenterOfMass - Vector3.forward * 2, Vector3.forward)), Is.True);
            Assert.That(pulse.LastBody, Is.SameAs(light));
            Assert.That(civil.State, Is.EqualTo(ReactionState.Idle), "The original pulse is outside civil perception");
            float deadline = Time.time + 2;
            while (civil.State == ReactionState.Idle && Time.time < deadline) yield return Advance(.05f);
            Assert.That(stage.PhysicalImpactCount, Is.GreaterThan(0), "Actual collision callback, no scripted chain");
            Assert.That(civil.ReactionCount, Is.GreaterThan(0));
            Assert.That(civil.LastStimulus, Is.EqualTo(StimulusKind.PhysicalImpact));
            var atReaction = civil.transform.position;
            AimAt(atReaction); Capture("07-chain-impact");
            yield return Advance(stage.Settings.startleDuration + .6f);
            Assert.That(civil.State, Is.EqualTo(ReactionState.Flee));
            Assert.That(Vector3.Distance(atReaction, civil.transform.position), Is.GreaterThan(.5f));
            Capture("08-chain-flee");
            Record("chain", $"physical callbacks={stage.PhysicalImpactCount}; civil={civil.State}; displacement={Vector3.Distance(atReaction, civil.transform.position):F2} m");
            AssertFinite(); LogAssert.NoUnexpectedReceived();
        }

        private static Rigidbody Body(string name) => Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Single(b => b.name == name);
        private IEnumerator Advance(float seconds)
        {
            float until = Time.time + seconds, timeout = Time.realtimeSinceStartup + 20;
            while (Time.time < until)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeout), "Unexpected pause/stall");
                InputSystem.Update(); yield return null;
            }
        }
        private void PlacePlayer(Vector3 position)
        {
            player.Respawn(); var controller = player.GetComponent<CharacterController>();
            controller.enabled = false; player.transform.position = position; controller.enabled = true;
            orbit.Snap(); Physics.SyncTransforms();
        }
        private void AimAt(Vector3 position)
        {
            var direction = (position - orbit.transform.position).normalized;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            orbit.Look(new Vector2(Mathf.DeltaAngle(orbit.Yaw, yaw), orbit.Pitch - pitch) / player.Settings.mouseSensitivity, true, 0);
        }
        private void AssertFinite()
        {
            foreach (var body in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                Assert.That(float.IsFinite(body.position.sqrMagnitude) && float.IsFinite(body.linearVelocity.sqrMagnitude), Is.True);
                Assert.That(body.linearVelocity.magnitude, Is.LessThan(50), "No explosive physics");
            }
        }
        private static void Capture(string name)
        {
            var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/Reactivity01"); File.WriteAllBytes("Logs/Reactivity01/" + name + ".png", pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); }
        }
        private static void Record(string name, string text)
        {
            Directory.CreateDirectory("Logs/Reactivity01"); File.WriteAllText("Logs/Reactivity01/" + name + ".txt", text);
            string message = "REACTIVITY01 " + name + ": " + text;
            LogAssert.Expect(LogType.Log, message);
            Debug.Log(message);
        }
    }
}
