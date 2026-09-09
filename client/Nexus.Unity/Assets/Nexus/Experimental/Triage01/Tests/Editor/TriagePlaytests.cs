using System.Collections;
using System.IO;
using System.Linq;
using Nexus.Feel01;
using Nexus.Reactivity01;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nexus.Triage01.Tests
{
    public sealed class TriagePlaytests
    {
        private const string ScenePath = "Assets/Nexus/Experimental/Triage01.unity";
        private TriageDirector triage;
        private FeelPlayer player;
        private FeelCamera orbit;
        private KineticPulse pulse;
        private FeelArena arena;
        private Gamepad pad;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditor;
        private float previousCapture;
        private readonly System.Collections.Generic.List<string> interventions = new();
        private string activeStrategy;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EditorSceneManager.OpenScene(ScenePath); yield return new EnterPlayMode();
            previousCapture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60;
            previousMode = InputSystem.settings.updateMode; previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad = InputSystem.AddDevice<Gamepad>(); player = Object.FindFirstObjectByType<FeelPlayer>();
            orbit = Object.FindFirstObjectByType<FeelCamera>(); pulse = Object.FindFirstObjectByType<KineticPulse>();
            arena = Object.FindFirstObjectByType<FeelArena>(); triage = Object.FindFirstObjectByType<TriageDirector>();
            Assert.That(player && orbit && pulse && arena && triage, Is.True);
            player.Actions.devices = new InputDevice[] { pad }; player.Actions.bindingMask = InputBinding.MaskByGroup("Gamepad");
            player.SetPaused(false); yield return Advance(.1f);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (player) player.SetPaused(false);
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            InputSystem.settings.updateMode = previousMode; InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor; Time.captureDeltaTime = previousCapture;
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator AutonomousCrisesConsequencesAndReset()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath), Is.True);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true)) Assert.That(component, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<ReactiveActor>(FindObjectsSortMode.None).Length, Is.EqualTo(2));
            Assert.That(triage.Rescue, Is.EqualTo(CrisisState.Active)); Assert.That(triage.Containment, Is.EqualTo(CrisisState.Active));
            var cargo = triage.Cargo.position; var enemy = triage.Threat.Position; var spawn = player.transform.position;
            Capture("initial"); yield return Advance(2);
            Assert.That(triage.Cargo.position.z, Is.GreaterThan(cargo.z + 5));
            Assert.That(triage.Threat.Position.z, Is.GreaterThan(enemy.z + 2));
            float contactAt = -1; var groundAt = new float[3];
            while (triage.Elapsed < 9)
            {
                if (contactAt < 0 && triage.Rescue == CrisisState.Consequence) contactAt = triage.Elapsed;
                if (triage.LostGround > 0 && groundAt[triage.LostGround - 1] == 0) groundAt[triage.LostGround - 1] = triage.Elapsed;
                yield return Advance(.05f);
            }
            Record("physical-windows", $"cargo contact={contactAt:F2}s; territory={groundAt[0]:F2}/{groundAt[1]:F2}/{groundAt[2]:F2}s");
            Record("ignored-technical", Outcome());
            Assert.That(triage.Rescue, Is.EqualTo(CrisisState.Consequence)); Assert.That(triage.LostGround, Is.EqualTo(3));
            Assert.That(triage.Civil.ReactionCount, Is.GreaterThan(0));
            Assert.That(pulse.Fire(orbit.AimRay), Is.True);
            arena.ResetArena();
            Assert.That(pulse.ShotCount, Is.Zero);
            Assert.That(pulse.Fire(orbit.AimRay), Is.True, "Reset clears pending cooldown");
            arena.ResetArena(); yield return Advance(.1f);
            Assert.That(triage.Rescue, Is.EqualTo(CrisisState.Active)); Assert.That(triage.Containment, Is.EqualTo(CrisisState.Active));
            Assert.That(triage.LostGround, Is.Zero); Assert.That(triage.Elapsed, Is.LessThan(.2f));
            Assert.That(Vector3.Distance(triage.Cargo.position, cargo), Is.LessThan(.6f));
            Assert.That(Vector3.Distance(player.transform.position, spawn), Is.LessThan(.2f));
            Assert.That(triage.Civil.ReactionCount, Is.Zero); Assert.That(triage.Civil.State, Is.EqualTo(ReactionState.Idle));
            Assert.That(triage.Threat.State, Is.EqualTo(ReactionState.Approach));
            Finite(); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator PhysicalRescueDoesNotFreezeOtherCrisis()
        {
            // Technical fixture only. Strategy matrix below never teleports the player.
            Place(new Vector3(-26, .05f, -10));
            yield return Shoot(triage.Cargo);
            Assert.That(pulse.LastBody, Is.SameAs(triage.Cargo));
            yield return Advance(7);
            Record("rescue-technical", Outcome());
            Assert.That(triage.Rescue, Is.EqualTo(CrisisState.Resolved));
            Assert.That(triage.Civil.ReactionCount, Is.Zero, "Cargo was diverted before entering the civil perception radius");
            Assert.That(triage.LostGround, Is.EqualTo(3));
            Capture("rescue-clear"); Finite(); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator PhysicalContainmentPreservesRescueConsequenceAndResets()
        {
            Place(new Vector3(26, .05f, -4));
            yield return Shoot(triage.Threat.GetComponent<Rigidbody>()); yield return Advance(.7f);
            yield return Shoot(triage.Threat.GetComponent<Rigidbody>()); yield return Advance(7);
            Record("containment-technical", Outcome());
            Assert.That(triage.Containment, Is.EqualTo(CrisisState.Resolved)); Assert.That(triage.Threat.Position.y, Is.LessThan(-1.5f));
            Assert.That(triage.Rescue, Is.EqualTo(CrisisState.Consequence)); Capture("contained");
            arena.ResetArena(); yield return Advance(.1f);
            Assert.That(triage.Threat.Position.y, Is.GreaterThan(.8f)); Assert.That(triage.LostGround, Is.Zero);
            Assert.That(triage.Containment, Is.EqualTo(CrisisState.Active)); Finite(); LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator SixStrategiesThroughRealInputMovementAndPhysics()
        {
            var results = new System.Collections.Generic.HashSet<string>();
            foreach (string strategy in new[] { "A-first", "B-first", "alternate", "ignore", "try-both", "creative-barrier" })
            {
                activeStrategy = strategy; interventions.Clear();
                Neutral(); arena.ResetArena(); yield return Advance(.1f);
                int shots = pulse.ShotCount;
                if (strategy == "A-first" || strategy == "try-both" || strategy == "alternate")
                {
                    yield return Walk(new Vector3(-20, 0, -6), true);
                    yield return Shoot(triage.Cargo);
                    if (strategy == "A-first") yield return Advance(1);
                    yield return Walk(new Vector3(22, 0, 1), strategy == "try-both");
                    yield return Shoot(triage.Threat.GetComponent<Rigidbody>());
                    yield return Advance(.7f); yield return Shoot(triage.Threat.GetComponent<Rigidbody>());
                    if (strategy == "alternate") { yield return Walk(new Vector3(-20, 0, 0), true); yield return Shoot(triage.Cargo); }
                }
                else if (strategy == "B-first")
                {
                    yield return Walk(new Vector3(22, 0, -4), true);
                    yield return Shoot(triage.Threat.GetComponent<Rigidbody>()); yield return Advance(.7f);
                    yield return Shoot(triage.Threat.GetComponent<Rigidbody>());
                    yield return Walk(new Vector3(-20, 0, 0), true); yield return Shoot(triage.Cargo);
                }
                else if (strategy == "creative-barrier")
                {
                    yield return Walk(new Vector3(21, 0, -1), true);
                    var barrier = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Single(b => b.name.StartsWith("Movable barrier"));
                    yield return Shoot(barrier); yield return Walk(new Vector3(-20, 0, -1), true); yield return Shoot(triage.Cargo);
                }
                yield return Advance(Mathf.Max(2, 13 - triage.Elapsed));
                string outcome = Outcome(); results.Add(triage.Rescue + "/" + triage.Containment + "/" + triage.LostGround);
                Record(strategy, outcome + $"; shots={pulse.ShotCount - shots}; player={player.transform.position:F2}; interventions=" + string.Join(" | ", interventions));
                Aim(triage.Civil.Position); yield return Advance(.15f); Capture(strategy + "-rescue");
                Aim(triage.Threat.Position); yield return Advance(.15f); Capture(strategy + "-depot");
                Finite();
            }
            Assert.That(results.Count, Is.GreaterThan(1), "Priorities must not all produce the same physical result");
            LogAssert.NoUnexpectedReceived();
        }
        private string Outcome() => $"t={triage.Elapsed:F2}; rescue={triage.Rescue}; containment={triage.Containment}; ground={triage.LostGround}/3; cargo={triage.Cargo.position:F2}; civil={triage.Civil.Position:F2}; threat={triage.Threat.Position:F2}";
        private IEnumerator Walk(Vector3 destination, bool sprint)
        {
            float deadline = Time.time + 15;
            while (Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).magnitude > .5f && Time.time < deadline)
            {
                var delta = Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).normalized;
                var local = Quaternion.Euler(0, -orbit.Yaw, 0) * delta;
                var state = new GamepadState { leftStick = new Vector2(local.x, local.z) };
                if (sprint) state = state.WithButton(GamepadButton.LeftStick);
                InputSystem.QueueStateEvent(pad, state); yield return Advance(1f / 60);
            }
            Neutral(); yield return Advance(.12f);
            Assert.That(Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).magnitude, Is.LessThan(1.5f), "Route reached using movement");
        }
        private IEnumerator Shoot(Rigidbody target)
        {
            Neutral();
            for (int i = 0; i < 8; i++) { Aim(target.worldCenterOfMass); yield return Advance(1f / 60); }
            InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1 }); yield return Advance(1f / 60);
            Neutral(); yield return Advance(.05f);
            interventions.Add($"t={triage.Elapsed:F2}, aimed={target.name}, hit={(pulse.LastBody ? pulse.LastBody.name : "none")}, threat={triage.Threat.State}, ground={triage.LostGround}");
            if (activeStrategy != null) Capture(activeStrategy + "-shot-" + interventions.Count);
        }
        private void Aim(Vector3 position)
        {
            var direction = (position - orbit.transform.position).normalized;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            orbit.Look(new Vector2(Mathf.DeltaAngle(orbit.Yaw, yaw), orbit.Pitch - pitch) / player.Settings.mouseSensitivity, true, 0);
        }
        private void Place(Vector3 position)
        {
            player.Respawn(); var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.position = position; controller.enabled = true; orbit.Snap(); Physics.SyncTransforms();
        }
        private void Neutral() => InputSystem.QueueStateEvent(pad, default(GamepadState));
        private IEnumerator Advance(float seconds)
        {
            float until = Time.time + seconds, timeout = Time.realtimeSinceStartup + 30;
            while (Time.time < until) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeout), "Unexpected pause/stall"); InputSystem.Update(); yield return null; }
        }
        private static void Finite()
        {
            foreach (var body in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            { Assert.That(float.IsFinite(body.position.sqrMagnitude), Is.True); Assert.That(body.linearVelocity.magnitude, Is.LessThan(50)); }
        }
        private static void Capture(string name)
        {
            var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/Triage01"); File.WriteAllBytes("Logs/Triage01/" + name + ".png", pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); }
        }
        private static void Record(string name, string value)
        {
            Directory.CreateDirectory("Logs/Triage01"); File.WriteAllText("Logs/Triage01/" + name + ".txt", value);
            string message = "TRIAGE01 " + name + ": " + value; LogAssert.Expect(LogType.Log, message); Debug.Log(message);
        }
    }
}
