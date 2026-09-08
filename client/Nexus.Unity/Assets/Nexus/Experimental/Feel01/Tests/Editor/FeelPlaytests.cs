using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nexus.Feel01.Tests
{
    public sealed class FeelPlaytests
    {
        private const string ScenePath = "Assets/Nexus/Experimental/GameplayFeel01.unity";
        private FeelPlayer player;
        private FeelCamera orbit;
        private KineticPulse power;
        private Keyboard keyboard;
        private Mouse mouse;
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
            previousCapture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60;
            previousMode = InputSystem.settings.updateMode;
            previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>(); pad = InputSystem.AddDevice<Gamepad>();
            player = Object.FindFirstObjectByType<FeelPlayer>(); orbit = Object.FindFirstObjectByType<FeelCamera>(); power = Object.FindFirstObjectByType<KineticPulse>();
            Assert.That(player && orbit && power, Is.True);
            player.Actions.devices = new InputDevice[] { keyboard, mouse, pad };
            player.Actions.bindingMask = InputBinding.MaskByGroup("KeyboardMouse");
            player.SetPaused(false);
            yield return Advance(.3f);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (player) player.SetPaused(false);
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            InputSystem.settings.updateMode = previousMode;
            InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor;
            Time.captureDeltaTime = previousCapture;
            yield return new ExitPlayMode();
            Assert.That(Application.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator MovementAndCameraWithBothSemanticControlSchemes()
        {
            Assert.That(player.Settings.IsValid, Is.True);
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(ScenePath));
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    Assert.That(component, Is.Not.Null, "Missing scene script");
            Capture("01-start");
            Keys(Key.W); yield return Advance(.12f);
            float accelerationSpeed = HorizontalSpeed();
            Assert.That(accelerationSpeed, Is.InRange(.5f, player.Settings.moveSpeed - .1f));
            yield return Advance(.8f);
            Assert.That(HorizontalSpeed(), Is.EqualTo(player.Settings.moveSpeed).Within(.2f));
            var beforeBrake = player.transform.position;
            Keys(); yield return Advance(.5f);
            float stoppingDistance = Vector3.Distance(beforeBrake, player.transform.position);
            Assert.That(HorizontalSpeed(), Is.LessThan(.05f));
            Record("movement", "speed at 0.12s=" + F(accelerationSpeed) + "; stopping distance=" + F(stoppingDistance));
            Keys(Key.W, Key.LeftShift); yield return Advance(.65f);
            Assert.That(HorizontalSpeed(), Is.EqualTo(player.Settings.sprintSpeed).Within(.2f));
            Keys(); yield return Advance(.5f);
            player.Respawn(); yield return Advance(.2f);
            float groundY = player.transform.position.y;
            Keys(Key.W, Key.Space); yield return Advance(.12f);
            Keys(Key.D); yield return Advance(.25f);
            Capture("02-air-control");
            float apex = player.transform.position.y;
            Assert.That(apex - groundY, Is.GreaterThan(.8f));
            Assert.That(player.Velocity.x, Is.GreaterThan(.3f), "Air steering changes trajectory");
            Keys(); yield return Advance(1.3f);
            Assert.That(player.Grounded, Is.True);
            Assert.That(player.LandingCount, Is.GreaterThan(0));
            float landedY = player.transform.position.y;
            yield return Advance(.5f);
            Assert.That(player.transform.position.y, Is.EqualTo(landedY).Within(.03f), "Stable landing");
            Record("jump", "sample height=" + F(apex - groundY) + "; landing speed=" + F(player.LastLandingSpeed));

            player.Respawn(); player.Actions.bindingMask = InputBinding.MaskByGroup("Gamepad");
            Pad(new GamepadState { leftStick = Vector2.up }); yield return Advance(.8f);
            Assert.That(HorizontalSpeed(), Is.EqualTo(player.Settings.moveSpeed).Within(.2f));
            Pad(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.LeftStick)); yield return Advance(.6f);
            Assert.That(HorizontalSpeed(), Is.EqualTo(player.Settings.sprintSpeed).Within(.2f));
            Pad(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.South)); yield return Advance(.15f);
            Assert.That(player.Grounded, Is.False);
            float yaw = orbit.Yaw;
            Pad(new GamepadState { rightStick = new Vector2(.6f, .3f) }); yield return Advance(.35f);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw, orbit.Yaw)), Is.GreaterThan(10));
            Pad(default); yield return Advance(1.2f);
            Pad(new GamepadState { leftStick = Vector2.up }); yield return Advance(.2f);
            InputSystem.RemoveDevice(pad); yield return Advance(.8f);
            Assert.That(player.Actions.FindAction("Gameplay/Move").ReadValue<Vector2>(), Is.EqualTo(Vector2.zero));
            Assert.That(HorizontalSpeed(), Is.LessThan(.1f), "Disconnect does not leave stuck input");
            player.Actions.bindingMask = InputBinding.MaskByGroup("KeyboardMouse");
            MouseDelta(new Vector2(250, 10000)); yield return Advance(.1f);
            Assert.That(orbit.Pitch, Is.EqualTo(player.Settings.minimumPitch).Within(.1f));
            MouseDelta(new Vector2(0, -10000)); yield return Advance(.1f);
            Assert.That(orbit.Pitch, Is.EqualTo(player.Settings.maximumPitch).Within(.1f));
            AssertFinite();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RampStepsDropAndCameraWallProtection()
        {
            PlacePlayer(new Vector3(-11, .05f, -7));
            Keys(Key.W); yield return Advance(2.5f);
            Keys(); yield return Advance(.35f);
            Assert.That(player.transform.position.y, Is.GreaterThan(2.8f), "Climb ramp to deck");
            Capture("03-ramp-deck");
            // Step sideways off the deck; the straight-ahead lane intentionally contains a box.
            Keys(Key.D); yield return Advance(1.5f);
            Keys(); yield return Advance(.8f);
            Assert.That(player.Grounded, Is.True);
            Assert.That(player.transform.position.y, Is.LessThan(.2f), "Drop from 3m deck to floor");
            Record("drop", "landing speed=" + F(player.LastLandingSpeed));
            PlacePlayer(new Vector3(12, .05f, -7));
            Keys(Key.W); yield return Advance(2.2f);
            Keys(); yield return Advance(.4f);
            Assert.That(player.transform.position.y, Is.GreaterThan(1.8f), "Climb eight 0.25m steps");
            Capture("04-stair-deck");
            PlacePlayer(new Vector3(7, .05f, -16));
            Keys(Key.W); yield return Advance(1);
            Assert.That(player.transform.position.z, Is.LessThan(-13.4f), "Controller cannot walk through wall");
            Keys(); PlacePlayer(new Vector3(7, .05f, -11.8f)); yield return Advance(.3f);
            float distance = Vector3.Distance(orbit.transform.position, player.transform.position + Vector3.up * player.Settings.pivotHeight);
            Assert.That(distance, Is.LessThan(2), "Camera pulls in before the rear obstruction");
            Assert.That(Physics.CheckSphere(orbit.transform.position, .08f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore), Is.False);
            Assert.That(player.GetComponentsInChildren<MeshRenderer>().All(r => r.forceRenderingOff), Is.True, "Close camera keeps aim unobstructed");
            Capture("05-camera-obstruction");
            Record("camera", "wall distance=" + F(distance));
            AssertFinite();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EqualImpulseDifferentMassesDummyCooldownAndCover()
        {
            var bodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            var light = bodies.Single(b => b.name == "LIGHT / 2 kg");
            var heavy = bodies.Single(b => b.name == "HEAVY / 12 kg");
            var dummy = bodies.Single(b => b.name == "DUMMY / 8 kg");
            // Use the same horizontal impulse through each centre to isolate mass response from torque.
            Assert.That(power.Fire(new Ray(light.worldCenterOfMass - Vector3.forward * 2, Vector3.forward)), Is.True);
            Assert.That(power.LastBody, Is.SameAs(light));
            Assert.That(power.Fire(orbit.AimRay), Is.False, "Cooldown prevents repeat activation");
            Assert.That(player.Velocity.z, Is.LessThan(-1), "Opposite self impulse");
            yield return Advance(.05f);
            float lightSpeed = light.linearVelocity.z;
            yield return Advance(player.Settings.cooldown);
            Assert.That(power.Fire(new Ray(heavy.worldCenterOfMass - Vector3.forward * 2, Vector3.forward)), Is.True);
            Assert.That(power.LastBody, Is.SameAs(heavy));
            yield return Advance(.05f);
            float heavySpeed = heavy.linearVelocity.z;
            Assert.That(heavySpeed, Is.GreaterThan(.5f));
            Assert.That(lightSpeed, Is.GreaterThan(heavySpeed * 3), "Lighter object has clearly larger delta-v");
            Record("mass", "light=" + F(lightSpeed) + "m/s; heavy=" + F(heavySpeed) + "m/s; ratio=" + F(lightSpeed / heavySpeed));
            Capture("06-mass-response");
            yield return Advance(player.Settings.cooldown);
            Assert.That(power.Fire(new Ray(dummy.worldCenterOfMass - Vector3.forward * 2, Vector3.forward)), Is.True);
            Assert.That(power.LastBody, Is.SameAs(dummy));
            yield return Advance(.15f);
            Assert.That(dummy.linearVelocity.z, Is.GreaterThan(.3f));
            Record("dummy", "velocity=" + F(dummy.linearVelocity.z));
            yield return Advance(player.Settings.cooldown);
            PlacePlayer(new Vector3(7, .05f, -16));
            Assert.That(power.Fire(new Ray(dummy.worldCenterOfMass - Vector3.forward * 2, Vector3.forward)), Is.True);
            Assert.That(power.LastBody, Is.Null, "Shoulder ray hits cover even when the supplied aim sees past it");
            AssertFinite();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AimFireSelfLaunchJumpCombinationPauseAndReset()
        {
            yield return AimAt(new Vector3(0, 0, -14));
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 });
            yield return Advance(.15f);
            Assert.That(power.ShotCount, Is.EqualTo(1), "Semantic PrimaryPower fires once per press");
            Record("self-launch", "height at 0.15s=" + F(player.transform.position.y) + "; pitch=" + F(orbit.Pitch));
            Assert.That(player.transform.position.y, Is.GreaterThan(.1f), "Downward shot lifts the player");
            Capture("07-self-impulse");
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return Advance(1.2f);
            player.Respawn(); yield return Advance(.2f);
            yield return AimAt(new Vector3(0, 0, -14));
            Keys(Key.Space); yield return Advance(.08f); Keys();
            float beforePower = player.Velocity.y;
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 }); yield return Advance(.05f);
            Assert.That(player.Velocity.y, Is.GreaterThan(beforePower), "Power adds lift to an active jump");
            float combinationHeight = player.transform.position.y;
            for (int i = 0; i < 15; i++)
            { yield return Advance(.05f); combinationHeight = Mathf.Max(combinationHeight, player.transform.position.y); }
            Capture("08-jump-plus-power");
            Record("combination", "sampled apex=" + F(combinationHeight));
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return Advance(1.5f);
            Keys(Key.Escape); yield return RealAdvance(.15f);
            Assert.That(player.Paused, Is.True);
            var pausedPosition = player.transform.position; yield return RealAdvance(.15f);
            Assert.That(player.transform.position, Is.EqualTo(pausedPosition));
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Keys(); yield return RealAdvance(.05f);
            Keys(Key.Space); yield return RealAdvance(.15f);
            Assert.That(player.Paused, Is.False, "Jump while paused resets and resumes");
            Assert.That(Vector3.Distance(player.transform.position, new Vector3(0, 0, -15)), Is.LessThan(.3f));
            Keys();
            yield return Advance(player.Settings.cooldown + .1f);
            yield return AimAt(new Vector3(0, 0, -14));
            player.Actions.bindingMask = InputBinding.MaskByGroup("Gamepad");
            int shots = power.ShotCount;
            // Both semantic actions in one update must compose, not consume the grounded jump.
            Pad(new GamepadState { rightTrigger = 1 }.WithButton(GamepadButton.South));
            yield return Advance(.1f);
            Assert.That(power.ShotCount, Is.EqualTo(shots + 1), "Gamepad PrimaryPower");
            Assert.That(player.Velocity.y, Is.GreaterThan(8), "Simultaneous gamepad jump plus power");
            Pad(default); yield return Advance(1.5f);
            Pad(new GamepadState().WithButton(GamepadButton.Start)); yield return RealAdvance(.1f);
            Assert.That(player.Paused, Is.True, "Gamepad Pause");
            Pad(default); yield return RealAdvance(.05f);
            Pad(new GamepadState().WithButton(GamepadButton.South)); yield return RealAdvance(.1f);
            Assert.That(player.Paused, Is.False, "Gamepad reset");
            Pad(default);
            AssertFinite();
            LogAssert.NoUnexpectedReceived();
        }

        private IEnumerator Advance(float seconds)
        {
            float end = Time.time + seconds;
            float timeout = Time.realtimeSinceStartup + 20;
            while (Time.time < end)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeout), "Scenario stalled / unexpected pause");
                InputSystem.Update(); yield return null;
            }
        }

        private IEnumerator RealAdvance(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) { InputSystem.Update(); yield return null; }
        }

        private IEnumerator AimAt(Vector3 point)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 dir = (point - orbit.transform.position).normalized;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;
                MouseDelta(new Vector2(Mathf.DeltaAngle(orbit.Yaw, yaw), orbit.Pitch - pitch) / player.Settings.mouseSensitivity);
                yield return Advance(.1f);
            }
        }

        private void PlacePlayer(Vector3 point)
        {
            player.Respawn();
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
            player.transform.position = point; cc.enabled = true; orbit.Snap(); Physics.SyncTransforms();
        }

        private void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        private void MouseDelta(Vector2 delta) => InputSystem.QueueStateEvent(mouse, new MouseState { delta = delta });
        private void Pad(GamepadState state) => InputSystem.QueueStateEvent(pad, state);
        private float HorizontalSpeed() => new Vector2(player.Velocity.x, player.Velocity.z).magnitude;
        private static string F(float n) => n.ToString("F3", CultureInfo.InvariantCulture);
        private static void Record(string name, string value)
        {
            Directory.CreateDirectory("Logs/Feel01");
            File.WriteAllText("Logs/Feel01/" + name + ".txt", value);
            string message = "FEEL01 OBSERVATION " + name + ": " + value;
            LogAssert.Expect(LogType.Log, message);
            Debug.Log(message);
        }

        private void AssertFinite()
        {
            Assert.That(float.IsNaN(player.Velocity.sqrMagnitude) || float.IsInfinity(player.Velocity.sqrMagnitude), Is.False);
            Assert.That(float.IsNaN(orbit.transform.position.sqrMagnitude), Is.False);
        }

        private static void Capture(string name)
        {
            var target = new RenderTexture(960, 540, 24);
            var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/Feel01");
                File.WriteAllBytes("Logs/Feel01/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
            }
        }
    }
}
