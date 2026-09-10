using System.Collections;
using System.Collections.Generic;
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
using Object = UnityEngine.Object;

namespace Nexus.SessionLoop01.Tests
{
    public sealed class SessionPlaytests
    {
        private const string ScenePath="Assets/Nexus/Experimental/SessionLoop01.unity";
        private SessionLoopDirector director;
        private FeelPlayer player;
        private FeelCamera orbit;
        private KineticPulse pulse;
        private FeelArena arena;
        private Gamepad pad;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditor;
        private float previousCapture;
        private readonly List<string> shots=new();
        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene(ScenePath); yield return new EnterPlayMode();
            previousCapture=Time.captureDeltaTime;Time.captureDeltaTime=1f/60;
            previousMode=InputSystem.settings.updateMode;previousBackground=InputSystem.settings.backgroundBehavior;previousEditor=InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.updateMode=InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad=InputSystem.AddDevice<Gamepad>(); player=Object.FindFirstObjectByType<FeelPlayer>();orbit=Object.FindFirstObjectByType<FeelCamera>();pulse=Object.FindFirstObjectByType<KineticPulse>();
            director=Object.FindFirstObjectByType<SessionLoopDirector>();arena=Object.FindFirstObjectByType<FeelArena>();
            Assert.That(player && orbit && pulse && director && arena,Is.True);
            player.Actions.devices=new InputDevice[]{pad};player.Actions.bindingMask=InputBinding.MaskByGroup("Gamepad");player.SetPaused(false);
            yield return Advance(.1f);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(player) player.SetPaused(false);if(pad!=null && pad.added) InputSystem.RemoveDevice(pad);
            InputSystem.settings.updateMode=previousMode;InputSystem.settings.backgroundBehavior=previousBackground;InputSystem.settings.editorInputBehaviorInPlayMode=previousEditor;
            Time.captureDeltaTime=previousCapture;yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator InterventionContinuousTravelSecondSituationAndReturn()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s=>s.enabled && s.path==ScenePath),Is.True);
            Assert.That(GameObject.Find("Market square") && GameObject.Find("Residential courtyard"),Is.True);
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var component in root.GetComponentsInChildren<Component>(true)) Assert.That(component,Is.Not.Null);
            int instance=director.GetInstanceID();
            Assert.That(director.Market,Is.EqualTo(MarketOutcome.Dormant));Assert.That(director.Access,Is.EqualTo(AccessOutcome.Dormant));
            CaptureOverview("connected-world"); Capture("spawn");
            yield return Walk(new Vector3(4,0,-7));yield return Walk(new Vector3(4,0,-3));
            while(director.Market==MarketOutcome.Dormant) yield return Advance(.1f);
            var start=director.Threat.Position;yield return Advance(.4f);
            Assert.That(director.Threat.Position.z,Is.GreaterThan(start.z),"Threat physically approaches the stall");
            Capture("market-active");
            for(int i=0;i<3 && !director.FirstComplete;i++)
            {yield return Shoot(director.Threat.GetComponent<Rigidbody>());yield return Advance(.7f);}
            float deadline=Time.time+12;
            while(!director.FirstComplete && Time.time<deadline) yield return Advance(.1f);
            Record("protected",$"market={director.Market}; threat={director.Threat.Position}; shots={string.Join(" | ",shots)}");
            Assert.That(director.Market,Is.EqualTo(MarketOutcome.Protected)); Assert.That(director.ProtectionVisible,Is.True);
            Assert.That(director.Access,Is.EqualTo(AccessOutcome.Dormant),"No instant second incident");
            Capture("market-protected"); yield return Walk(new Vector3(0,0,10));
            yield return Walk(new Vector3(0,0,25)); Capture("connecting-street");
            yield return Walk(new Vector3(-3,0,35));yield return Advance(1);
            Assert.That(director.Access,Is.EqualTo(AccessOutcome.Active));
            Assert.That(director.GetInstanceID(),Is.EqualTo(instance),"Same scene director throughout traversal");
            Capture("residential-blocked");
            var before=director.Delivery.position;yield return Shoot(director.Delivery);yield return Advance(.8f);
            Assert.That(pulse.LastBody,Is.SameAs(director.Delivery));Assert.That(Vector3.Distance(before,director.Delivery.position),Is.GreaterThan(.3f));
            yield return Advance(6);
            Record("access",$"access={director.Access}; delivery={director.Delivery.position}");
            Assert.That(director.Access,Is.EqualTo(AccessOutcome.Cleared),"Displaced load allows the resident through");
            Capture("residential-cleared");
            yield return Walk(new Vector3(0,0,25));yield return Walk(new Vector3(0,0,10));yield return Walk(new Vector3(3,0,3));
            Assert.That(director.Market,Is.EqualTo(MarketOutcome.Protected));Assert.That(director.ProtectionVisible,Is.True);
            Assert.That(director.Access,Is.EqualTo(AccessOutcome.Cleared));Assert.That(director.GetInstanceID(),Is.EqualTo(instance));
            yield return FrameArea(new Vector3(7,0,8));Capture("market-return");
            arena.ResetArena();yield return Advance(.1f);
            Assert.That(director.Market,Is.EqualTo(MarketOutcome.Dormant));Assert.That(director.Access,Is.EqualTo(AccessOutcome.Dormant));
            Assert.That(director.ProtectionVisible,Is.False);Assert.That(director.DamageVisible,Is.False);
            Assert.That(Vector3.Distance(director.Delivery.position,new Vector3(0,.85f,40)),Is.LessThan(.2f));
            Assert.That(director.Threat.GetComponent<Rigidbody>().constraints,Is.EqualTo(RigidbodyConstraints.FreezeRotation));
            Assert.That(pulse.ShotCount,Is.Zero);LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator NeglectedStallDamageSurvivesTravelAndResetRestoresIt()
        {
            yield return Walk(new Vector3(4,0,-7));yield return Walk(new Vector3(4,0,-3));yield return Advance(18);
            Assert.That(director.Market,Is.EqualTo(MarketOutcome.Damaged));Assert.That(director.DamageVisible,Is.True);
            Capture("market-damaged");
            yield return Walk(new Vector3(0,0,10));yield return Walk(new Vector3(0,0,35));yield return Advance(1);
            Assert.That(director.Access,Is.EqualTo(AccessOutcome.Active));
            yield return Walk(new Vector3(0,0,10));yield return Walk(new Vector3(3,0,3));
            Assert.That(director.Market,Is.EqualTo(MarketOutcome.Damaged));Assert.That(director.DamageVisible,Is.True);
            yield return FrameArea(new Vector3(7,0,8));Capture("damaged-return");
            arena.ResetArena();yield return Advance(.1f);
            Assert.That(director.DamageVisible,Is.False);Assert.That(GameObject.Find("Intact market stall"),Is.Not.Null);
            Assert.That(director.Market,Is.EqualTo(MarketOutcome.Dormant));LogAssert.NoUnexpectedReceived();
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
            shots.Add($"t={Time.time:F2}, target={target.name}, hit={(pulse.LastBody ? pulse.LastBody.name : "none")}, state={director.Threat.State}");
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
            camera.transform.position = new Vector3(1, 43, -28); camera.transform.LookAt(new Vector3(0, 0, 20));
            Capture(name, camera); Object.DestroyImmediate(go);
        }
        private static void Capture(string name, Camera camera = null)
        {
            var target = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); var previous = RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera ? camera : Camera.main, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/SessionLoop01"); File.WriteAllBytes("Logs/SessionLoop01/" + name + ".png", pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels); }
        }
        private static void Record(string name, string value)
        { Directory.CreateDirectory("Logs/SessionLoop01"); File.WriteAllText("Logs/SessionLoop01/" + name + ".txt", value); }
    }
}
