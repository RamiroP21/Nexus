using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nexus.Feel01;
using Nexus.Persistence01;
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
using Object = UnityEngine.Object;

namespace Nexus.VerticalSlice01.Tests
{
    public sealed class SlicePlaytests
    {
        private const string ScenePath = "Assets/Nexus/Experimental/VerticalSlice01.unity";
        private const string Scope = "Nexus.VerticalSlice01.Tests";
        private string testPath, previousOverride;
        private SliceIncident incident;
        private FeelPlayer player;
        private FeelCamera orbit;
        private KineticPulse pulse;
        private FeelArena arena;
        private Gamepad pad;
        private float previousCapture;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditor;
        private readonly List<string> shots = new();

        [SetUp]
        public void PreparePath()
        {
            if (!SessionState.GetBool(Scope, false))
            {
                SessionState.SetString(Scope + ".previous", Environment.GetEnvironmentVariable(SliceIncident.TestPathVariable) ?? "");
                SessionState.SetString(Scope + ".path", Path.Combine(Path.GetTempPath(), "Nexus.VerticalSlice01." + Guid.NewGuid().ToString("N"), "outcome.json"));
                SessionState.SetBool(Scope, true);
            }
            testPath = SessionState.GetString(Scope + ".path", ""); previousOverride = SessionState.GetString(Scope + ".previous", "");
            Environment.SetEnvironmentVariable(SliceIncident.TestPathVariable, testPath);
        }
        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (Application.isPlaying) { RestoreInput(); yield return new ExitPlayMode(); }
            new Persistence01StateStore(testPath).Clear();
            if (Directory.Exists(Path.GetDirectoryName(testPath))) Directory.Delete(Path.GetDirectoryName(testPath));
            Environment.SetEnvironmentVariable(SliceIncident.TestPathVariable, previousOverride.Length == 0 ? null : previousOverride);
            SessionState.EraseBool(Scope); SessionState.EraseString(Scope + ".path"); SessionState.EraseString(Scope + ".previous");
        }

        [UnityTest]
        public IEnumerator IntegratedSixStrategiesAndPersistentAftermathMatrix()
        {
            EditorSceneManager.OpenScene(ScenePath); yield return new EnterPlayMode();
            InitializeInput(); yield return Advance(.1f);
            Assert.That(incident.Memory, Is.Null); Assert.That(incident.IncidentStarted, Is.False);
            Assert.That(incident.MemoryPath, Is.EqualTo(testPath));
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath), Is.True);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true)) Assert.That(component, Is.Not.Null);
            var cargoStart = incident.Cargo.position; var threatStart = incident.Threat.Position;
            Capture("BEFORE-player"); CaptureOverview("BEFORE");
            yield return Advance(2);
            Assert.That(incident.IncidentStarted, Is.False); Assert.That(Vector3.Distance(incident.Cargo.position, cargoStart), Is.LessThan(.3f));
            Assert.That(Vector3.Distance(incident.Threat.Position, threatStart), Is.LessThan(.3f));
            arena.ResetArena(); yield return Advance(.1f);
            Assert.That(incident.IncidentStarted, Is.False, "Reset during orientation starts a clean orientation");
            var outcomes = new HashSet<string>(); string aJson = null;
            foreach (string strategy in new[] { "A-first", "B-first", "alternate", "ignore", "try-both", "creative-barrier" })
            {
                shots.Clear(); Neutral();
                if (strategy != "A-first") { incident.ClearMemoryAndRestart(); yield return Advance(.2f); Bind(); }
                Assert.That(File.Exists(testPath), Is.False);
                while (!incident.IncidentStarted) yield return Advance(.1f);
                if (strategy == "A-first")
                {
                    CaptureOverview("ACTIVE"); Capture("ACTIVE-player");
                    Assert.That(Object.FindObjectsByType<ReactiveActor>(FindObjectsSortMode.None).Count(a => a.ReactionCount > 0), Is.GreaterThan(0), "Entry discharge is perceived");
                }
                if (strategy == "A-first" || strategy == "alternate" || strategy == "try-both")
                {
                    yield return Walk(new Vector3(-8, 0, -10)); yield return Walk(new Vector3(-8, 0, -6));
                    yield return Shoot(incident.Cargo);
                    Assert.That(pulse.LastBody, Is.SameAs(incident.Cargo), "Rescue intervention hits actual moving cargo");
                    if (strategy != "A-first")
                    {
                        yield return Walk(new Vector3(6, 0, -5), strategy == "try-both");
                        yield return Shoot(incident.Threat.GetComponent<Rigidbody>()); yield return Advance(.7f);
                        yield return Shoot(incident.Threat.GetComponent<Rigidbody>());
                        if (strategy == "alternate") { yield return Walk(new Vector3(-8, 0, 1)); yield return Shoot(incident.Cargo); }
                    }
                }
                else if (strategy == "B-first")
                {
                    yield return Walk(new Vector3(6, 0, -7)); yield return Shoot(incident.Threat.GetComponent<Rigidbody>());
                    yield return Advance(.7f); yield return Shoot(incident.Threat.GetComponent<Rigidbody>());
                }
                else if (strategy == "creative-barrier")
                {
                    yield return Walk(new Vector3(6, 0, -3));
                    var barrier = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Single(b => b.name == "Portable barrier");
                    Vector3 before = barrier.position; yield return Shoot(barrier); yield return Advance(.8f);
                    Assert.That(Vector3.Distance(before, barrier.position), Is.GreaterThan(.3f), "Improvised barrier actually moves under power");
                    yield return Walk(new Vector3(-8, 0, -3)); yield return Shoot(incident.Cargo);
                }
                float deadline = Time.time + 24;
                while (incident.Memory == null && Time.time < deadline) yield return Advance(.1f);
                Record(strategy, Describe() + "; shots=" + string.Join(" | ", shots));
                Assert.That(incident.Memory, Is.Not.Null, "Incident must stabilize: " + Describe()); Assert.That(incident.SaveFailed, Is.False);
                Assert.That(incident.Memory.IsValid, Is.True);
                string json = File.ReadAllText(testPath); outcomes.Add(json);
                if (strategy == "A-first")
                {
                    Assert.That(incident.Rescue, Is.EqualTo(RescueOutcome.Protected)); Assert.That(incident.Depot, Is.EqualTo(DepotOutcome.Overrun));
                    aJson = json;
                }
                if (strategy == "B-first")
                {
                    Assert.That(incident.Rescue, Is.EqualTo(RescueOutcome.Hit)); Assert.That(incident.Depot, Is.EqualTo(DepotOutcome.Contained));
                    Assert.That(json, Is.Not.EqualTo(aJson));
                }
                if (strategy == "ignore")
                { Assert.That(incident.Rescue, Is.EqualTo(RescueOutcome.Hit)); Assert.That(incident.LostGround, Is.EqualTo(3)); }
                CaptureOverview("RESULT-" + strategy);
                var written = File.GetLastWriteTimeUtc(testPath); yield return Advance(1);
                Assert.That(File.GetLastWriteTimeUtc(testPath), Is.EqualTo(written), "Record is not rewritten each frame");
                incident.ReturnLater(); yield return Advance(.2f); Bind();
                AssertAftermath(json); CaptureOverview("AFTERMATH-" + strategy);
                if (strategy == "A-first" || strategy == "B-first")
                {
                    yield return Walk(new Vector3(-8, 0, -10)); yield return Walk(new Vector3(-8, 0, 0));
                    yield return FrameArea(new Vector3(-12, 0, 6)); Capture("AFTERMATH-" + strategy + "-market");
                    yield return Walk(new Vector3(5, 0, 0)); yield return FrameArea(new Vector3(12, 0, 3)); Capture("AFTERMATH-" + strategy + "-depot");
                }
                arena.ResetArena(); yield return Advance(.1f); AssertAftermath(json);
                foreach (var body in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
                { Assert.That(float.IsFinite(body.position.sqrMagnitude), Is.True); Assert.That(body.linearVelocity.magnitude, Is.LessThan(50)); }
            }
            Assert.That(outcomes.Count, Is.GreaterThan(1));
            incident.ClearMemoryAndRestart(); yield return Advance(.2f); Bind();
            Assert.That(incident.Memory, Is.Null); Assert.That(incident.Aftermath, Is.False); Assert.That(incident.RescueDamageVisible, Is.False);
            Assert.That(File.Exists(testPath), Is.False); Assert.That(File.Exists(testPath + ".tmp"), Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        private string Describe() => $"t={incident.Elapsed:F2}; rescue={incident.Rescue}; depot={incident.Depot}; ground={incident.LostGround}; cargo={incident.Cargo.position:F2}; civil={incident.Civil.Position:F2}; threat={incident.Threat.Position:F2}";
        private void AssertAftermath(string json)
        {
            var record = JsonUtility.FromJson<Persistence01State>(json);
            Assert.That(incident.Aftermath, Is.True); Assert.That(File.ReadAllText(testPath), Is.EqualTo(json));
            Assert.That(incident.RescueDamageVisible, Is.EqualTo(record.rescue == RescueOutcome.Hit));
            Assert.That(incident.VisibleDepotDamage, Is.EqualTo(record.lostGround));
            Assert.That(incident.Civil.gameObject.activeSelf, Is.EqualTo(record.rescue == RescueOutcome.Protected));
            Assert.That(incident.Threat.gameObject.activeSelf, Is.False);
            Vector3 cargoPosition = record.rescue == RescueOutcome.Hit ? new Vector3(-12, 1.1f, 6) : new Vector3(-17, .8f, 8);
            Assert.That(Vector3.Distance(incident.Cargo.position, cargoPosition), Is.LessThan(.05f), "Reconstructed cargo physics pose");
            Assert.That(Vector3.Distance(incident.Cargo.transform.position, cargoPosition), Is.LessThan(.05f), "Reconstructed cargo rendered pose");
            foreach (var renderer in GameObject.Find("Aftermath / same place changed").GetComponentsInChildren<Renderer>())
                Assert.That(renderer.bounds.min.y, Is.GreaterThanOrEqualTo(-.02f), renderer.name);
        }
        private void InitializeInput()
        {
            previousCapture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60;
            previousMode = InputSystem.settings.updateMode; previousBackground = InputSystem.settings.backgroundBehavior; previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad = InputSystem.AddDevice<Gamepad>(); Bind();
        }
        private void Bind()
        {
            incident = Object.FindFirstObjectByType<SliceIncident>(); player = Object.FindFirstObjectByType<FeelPlayer>(); orbit = Object.FindFirstObjectByType<FeelCamera>();
            pulse = Object.FindFirstObjectByType<KineticPulse>(); arena = Object.FindFirstObjectByType<FeelArena>();
            Assert.That(incident && player && orbit && pulse && arena, Is.True);
            player.Actions.devices = new InputDevice[] { pad }; player.Actions.bindingMask = InputBinding.MaskByGroup("Gamepad"); player.SetPaused(false);
        }
        private void RestoreInput()
        {
            if (player) player.SetPaused(false); if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            InputSystem.settings.updateMode = previousMode; InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor; Time.captureDeltaTime = previousCapture;
        }
        private IEnumerator Walk(Vector3 destination, bool sprint = true)
        {
            float deadline = Time.time + 12;
            while (Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).magnitude > .5f && Time.time < deadline)
            {
                Vector3 local = Quaternion.Euler(0, -orbit.Yaw, 0) * Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).normalized;
                var state = new GamepadState { leftStick = new Vector2(local.x, local.z) };
                if (sprint) state = state.WithButton(GamepadButton.LeftStick);
                InputSystem.QueueStateEvent(pad, state); yield return Advance(1f / 60);
            }
            Neutral(); yield return Advance(.12f);
            Assert.That(Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).magnitude, Is.LessThan(1.5f), "Route reached through input: " + destination);
        }
        private IEnumerator Shoot(Rigidbody target)
        {
            Neutral(); for (int i = 0; i < 8; i++) { Aim(target.worldCenterOfMass); yield return Advance(1f / 60); }
            InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1 }); yield return Advance(1f / 60); Neutral(); yield return Advance(.05f);
            shots.Add($"t={incident.Elapsed:F2}, target={target.name}, hit={(pulse.LastBody ? pulse.LastBody.name : "none")}, state={incident.Threat.State}");
        }
        private void Aim(Vector3 point)
        {
            Vector3 direction = (point - orbit.transform.position).normalized;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            orbit.Look(new Vector2(Mathf.DeltaAngle(orbit.Yaw, yaw), orbit.Pitch - pitch) / player.Settings.mouseSensitivity, true, 0);
        }
        private IEnumerator FrameArea(Vector3 point)
        { Vector3 direction = point - player.transform.position; orbit.Look(new Vector2(Mathf.DeltaAngle(orbit.Yaw, Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg), orbit.Pitch - 22) / player.Settings.mouseSensitivity, true, 0); yield return Advance(.2f); }
        private void Neutral() => InputSystem.QueueStateEvent(pad, default(GamepadState));
        private IEnumerator Advance(float seconds)
        {
            float until = Time.time + seconds, timeout = Time.realtimeSinceStartup + 30;
            while (Time.time < until) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeout), "Unexpected pause/stall"); InputSystem.Update(); yield return null; }
        }
        private static void CaptureOverview(string name)
        {
            var go = new GameObject("Test overview camera"); var camera = go.AddComponent<Camera>(); camera.fieldOfView = 64;
            camera.transform.position = new Vector3(1, 29, -30); camera.transform.LookAt(new Vector3(0, 0, 1));
            Capture(name, camera); Object.DestroyImmediate(go);
        }
        private static void Capture(string name, Camera camera = null)
        {
            var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera ? camera : Camera.main, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/VerticalSlice01"); File.WriteAllBytes("Logs/VerticalSlice01/" + name + ".png", pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); }
        }
        private static void Record(string name, string value)
        { Directory.CreateDirectory("Logs/VerticalSlice01"); File.WriteAllText("Logs/VerticalSlice01/" + name + ".txt", value); }
    }
}
