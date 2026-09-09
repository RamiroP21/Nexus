using System;
using System.Collections;
using System.IO;
using System.Linq;
using Nexus.Feel01;
using Nexus.Triage01;
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

namespace Nexus.Persistence01.Tests
{
    public sealed class PersistencePlaytests
    {
        private const string ScenePath = "Assets/Nexus/Experimental/Persistence01.unity";
        private const string ScopeKey = "Nexus.Persistence01.Playtests";
        private static string Run => SessionState.GetInt(ScopeKey + ".run", 0) == 0 ? "A" : "B";
        private static string SavedJson => SessionState.GetString(ScopeKey + ".json", "");
        private string testPath, previousOverride;
        private FeelPlayer player;
        private FeelCamera orbit;
        private KineticPulse pulse;
        private TriageDirector triage;
        private Persistence01Experiment experiment;
        private FeelArena arena;
        private Gamepad pad;
        private float previousCapture;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditor;

        [SetUp]
        public void PreparePath()
        {
            // NUnit invokes SetUp again after domain reload. Keep this test's ownership stable.
            if (!SessionState.GetBool(ScopeKey, false))
            {
                previousOverride = Environment.GetEnvironmentVariable(Persistence01StateStore.TestPathVariable);
                testPath = Path.Combine(Path.GetTempPath(), "Nexus.Persistence01.Play." + Guid.NewGuid().ToString("N"), "outcome.json");
                SessionState.SetString(ScopeKey + ".path", testPath);
                SessionState.SetString(ScopeKey + ".previous", previousOverride ?? "");
                SessionState.SetInt(ScopeKey + ".run", 0);
                SessionState.EraseString(ScopeKey + ".json");
                SessionState.SetBool(ScopeKey, true);
            }
            else
            {
                testPath = SessionState.GetString(ScopeKey + ".path", "");
                previousOverride = SessionState.GetString(ScopeKey + ".previous", "");
                if (previousOverride.Length == 0) previousOverride = null;
            }
            Environment.SetEnvironmentVariable(Persistence01StateStore.TestPathVariable, testPath);
        }
        [UnityTearDown]
        public IEnumerator CleanUp()
        {
            if (Application.isPlaying) { RestoreInput(); yield return new ExitPlayMode(); }
            new Persistence01StateStore(testPath).Clear();
            string directory = Path.GetDirectoryName(testPath); if (Directory.Exists(directory)) Directory.Delete(directory);
            Environment.SetEnvironmentVariable(Persistence01StateStore.TestPathVariable, previousOverride);
            SessionState.EraseBool(ScopeKey); SessionState.EraseString(ScopeKey + ".path"); SessionState.EraseString(ScopeKey + ".previous");
            SessionState.EraseInt(ScopeKey + ".run"); SessionState.EraseString(ScopeKey + ".json");
        }

        [UnityTest]
        public IEnumerator PlayedOutcomesSurviveReloadAndNewPlayModeAndReconstructDifferentWorlds()
        {
            // Domain reload restores the coroutine position, not its ordinary local references.
            // Both the loop cursor and the pre-exit JSON belong to this Editor-only test scope.
            while (SessionState.GetInt(ScopeKey + ".run", 0) < 2)
            {
                EditorSceneManager.OpenScene(ScenePath); yield return new EnterPlayMode();
                InitializeInput(); yield return Advance(.1f);
                Assert.That(experiment.Memory, Is.Null); Assert.That(experiment.Aftermath, Is.False);
                Assert.That(experiment.MemoryPath, Is.EqualTo(testPath)); Assert.That(File.Exists(testPath), Is.False);
                Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath), Is.True);
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    foreach (var component in root.GetComponentsInChildren<Component>(true)) Assert.That(component, Is.Not.Null);
                if (Run == "A")
                {
                    yield return Walk(new Vector3(-20, 0, -6)); yield return Shoot(triage.Cargo);
                    Assert.That(pulse.LastBody, Is.SameAs(triage.Cargo));
                }
                else
                {
                    yield return Walk(new Vector3(22, 0, -4)); yield return Shoot(triage.Threat.GetComponent<Rigidbody>());
                    yield return Advance(.7f); yield return Shoot(triage.Threat.GetComponent<Rigidbody>());
                    Assert.That(pulse.LastBody, Is.SameAs(triage.Threat.GetComponent<Rigidbody>()));
                }
                float deadline = Time.time + 14;
                while (experiment.Memory == null && Time.time < deadline) yield return Advance(.1f);
                Assert.That(experiment.Memory, Is.Not.Null, "Played, stable pair was saved"); Assert.That(experiment.SaveFailed, Is.False);
                AssertOutcome(Run);
                SessionState.SetString(ScopeKey + ".json", File.ReadAllText(testPath));
                var savedAt = File.GetLastWriteTimeUtc(testPath);
                Record(Run + "-saved", $"played t={triage.Elapsed:F2}; {SavedJson}");
                yield return Advance(1);
                Assert.That(File.GetLastWriteTimeUtc(testPath), Is.EqualTo(savedAt), "No per-frame writes");
                experiment.ReturnLater(); yield return Advance(.15f); BindScene();
                AssertAftermath(Run); Assert.That(File.ReadAllText(testPath), Is.EqualTo(SavedJson));
                arena.ResetArena(); yield return Advance(.1f);
                AssertAftermath(Run); Assert.That(File.ReadAllText(testPath), Is.EqualTo(SavedJson), "Normal reset retains memory");
                yield return Walk(new Vector3(-25, 0, 0)); yield return FrameArea(new Vector3(-32, 0, 6)); Capture(Run + "-rescue-no-labels");
                yield return Walk(new Vector3(24, 0, 0)); yield return FrameArea(new Vector3(31, 0, 3)); Capture(Run + "-depot-no-labels");
                RestoreInput(); yield return new ExitPlayMode();
                Assert.That(SavedJson, Is.Not.Empty);
                Assert.That(File.ReadAllText(testPath), Is.EqualTo(SavedJson), "JSON survives Stop Play Mode");
                EditorSceneManager.OpenScene(ScenePath); yield return new EnterPlayMode();
                InitializeInput(); yield return Advance(.1f); AssertAftermath(Run);
                Assert.That(File.ReadAllText(testPath), Is.EqualTo(SavedJson), "JSON survives Start Play Mode again");
                Record(Run + "-return", $"after new Play Mode: rescueDamage={experiment.RescueDamageVisible}; depotDamage={experiment.VisibleDepotDamage}; file unchanged={File.ReadAllText(testPath) == SavedJson}");
                experiment.ClearMemoryAndRestart(); yield return Advance(.15f); BindScene();
                Assert.That(File.Exists(testPath), Is.False); Assert.That(experiment.Memory, Is.Null); Assert.That(experiment.Aftermath, Is.False);
                Assert.That(triage.enabled, Is.True); Assert.That(experiment.RescueDamageVisible, Is.False);
                RestoreInput(); yield return new ExitPlayMode();
                SessionState.SetInt(ScopeKey + ".run", SessionState.GetInt(ScopeKey + ".run", 0) + 1);
            }
            Assert.That(File.Exists(testPath), Is.False); LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator InvalidMemoryStartsPlayableExperimentWithOneControlledWarning()
        {
            EditorSceneManager.OpenScene(ScenePath); yield return new EnterPlayMode();
            InitializeInput(); yield return Advance(.1f);
            Directory.CreateDirectory(Path.GetDirectoryName(testPath)); File.WriteAllText(testPath, "invalid");
            LogAssert.Expect(LogType.Warning, "Persistence01: unreadable or invalid memory; starting clean. Original file retained until save or clear.");
            experiment.ReturnLater(); yield return Advance(.15f); BindScene();
            Assert.That(experiment.Memory, Is.Null); Assert.That(triage.enabled, Is.True); Assert.That(player.Paused, Is.False);
            Assert.That(File.ReadAllText(testPath), Is.EqualTo("invalid")); LogAssert.NoUnexpectedReceived();
        }
        private void InitializeInput()
        {
            Assert.That(Application.isPlaying, Is.True, "Lifecycle transition must be yielded directly by the Unity test");
            previousCapture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60;
            previousMode = InputSystem.settings.updateMode; previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad = InputSystem.AddDevice<Gamepad>(); BindScene();
        }
        private void BindScene()
        {
            player = Object.FindFirstObjectByType<FeelPlayer>(); orbit = Object.FindFirstObjectByType<FeelCamera>();
            pulse = Object.FindFirstObjectByType<KineticPulse>(); arena = Object.FindFirstObjectByType<FeelArena>();
            triage = Object.FindFirstObjectByType<TriageDirector>(); experiment = Object.FindFirstObjectByType<Persistence01Experiment>();
            Assert.That(player && orbit && pulse && arena && triage && experiment, Is.True);
            player.Actions.devices = new InputDevice[] { pad }; player.Actions.bindingMask = InputBinding.MaskByGroup("Gamepad"); player.SetPaused(false);
        }
        private void RestoreInput()
        {
            if (player) player.SetPaused(false);
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad); pad = null;
            InputSystem.settings.updateMode = previousMode; InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor; Time.captureDeltaTime = previousCapture;
        }
        private void AssertOutcome(string run)
        {
            Assert.That(experiment.Memory.rescue, Is.EqualTo(run == "A" ? RescueOutcome.Protected : RescueOutcome.Hit));
            Assert.That(experiment.Memory.depot, Is.EqualTo(run == "A" ? DepotOutcome.Overrun : DepotOutcome.Contained));
            Assert.That(experiment.Memory.lostGround, Is.EqualTo(run == "A" ? 3 : 1));
        }
        private void AssertAftermath(string run)
        {
            AssertOutcome(run); Assert.That(experiment.Aftermath, Is.True); Assert.That(triage.enabled, Is.False);
            Assert.That(experiment.RescueDamageVisible, Is.EqualTo(run == "B"));
            Assert.That(experiment.VisibleDepotDamage, Is.EqualTo(run == "A" ? 3 : 1));
            Assert.That(triage.Civil.gameObject.activeSelf, Is.EqualTo(run == "A")); Assert.That(triage.Threat.gameObject.activeSelf, Is.False);
            foreach (var body in Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None)) Assert.That(float.IsFinite(body.position.sqrMagnitude), Is.True);
            var traces = GameObject.Find("Aftermath / factual reconstruction");
            foreach (var renderer in traces.GetComponentsInChildren<Renderer>()) Assert.That(renderer.bounds.min.y, Is.GreaterThanOrEqualTo(-.02f), renderer.name + " grounded");
        }
        private IEnumerator Walk(Vector3 destination)
        {
            float deadline = Time.time + 15;
            while (Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).magnitude > .5f && Time.time < deadline)
            {
                Vector3 delta = Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).normalized;
                Vector3 local = Quaternion.Euler(0, -orbit.Yaw, 0) * delta;
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(local.x, local.z) }.WithButton(GamepadButton.LeftStick));
                yield return Advance(1f / 60);
            }
            Neutral(); yield return Advance(.12f);
            Assert.That(Vector3.ProjectOnPlane(destination - player.transform.position, Vector3.up).magnitude, Is.LessThan(1.5f));
        }
        private IEnumerator Shoot(Rigidbody body)
        {
            Neutral(); for (int i = 0; i < 8; i++) { Aim(body.worldCenterOfMass); yield return Advance(1f / 60); }
            InputSystem.QueueStateEvent(pad, new GamepadState { rightTrigger = 1 }); yield return Advance(1f / 60);
            Neutral(); yield return Advance(.05f);
        }
        private void Aim(Vector3 point)
        {
            var direction = (point - orbit.transform.position).normalized;
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            orbit.Look(new Vector2(Mathf.DeltaAngle(orbit.Yaw, yaw), orbit.Pitch - pitch) / player.Settings.mouseSensitivity, true, 0);
        }
        private IEnumerator FrameArea(Vector3 point)
        {
            Vector3 direction = point - player.transform.position; float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            orbit.Look(new Vector2(Mathf.DeltaAngle(orbit.Yaw, yaw), orbit.Pitch - 22) / player.Settings.mouseSensitivity, true, 0);
            yield return Advance(.2f);
        }
        private void Neutral() => InputSystem.QueueStateEvent(pad, default(GamepadState));
        private IEnumerator Advance(float seconds)
        {
            float until = Time.time + seconds, timeout = Time.realtimeSinceStartup + 30;
            while (Time.time < until) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeout), "Unexpected pause/stall"); InputSystem.Update(); yield return null; }
        }
        private static void Capture(string name)
        {
            var labels = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Select(t => t.GetComponent<Renderer>()).ToArray();
            foreach (var label in labels) label.enabled = false;
            var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/Persistence01"); File.WriteAllBytes("Logs/Persistence01/" + name + ".png", pixels.EncodeToPNG());
            }
            finally { foreach (var label in labels) label.enabled = true; RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); }
        }
        private static void Record(string name, string value)
        {
            Directory.CreateDirectory("Logs/Persistence01"); File.WriteAllText("Logs/Persistence01/" + name + ".txt", value);
            string message = "PERSISTENCE01 " + name + ": " + value; LogAssert.Expect(LogType.Log, message); Debug.Log(message);
        }
    }
}
