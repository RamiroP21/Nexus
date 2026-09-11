using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Nexus.Gameplay.Tests
{
    public sealed class SuperhumanPlaytests
    {
        private LocalPlayerInput input;
        private CharacterMotor motor;
        private ThirdPersonCamera orbit;
        private ContextualTargeting targeting;
        private KineticVectorAbility ability;
        private Gamepad pad;
        private InputSettings.UpdateMode previousMode;
        private InputSettings.BackgroundBehavior previousBackground;
        private InputSettings.EditorInputBehaviorInPlayMode previousEditor;
        private float previousCapture;
        private readonly List<Object> temporary = new();

        [UnitySetUp]
        public IEnumerator Setup()
        {
            EditorSceneManager.OpenScene(SuperhumanPlaygroundAuthoring.ScenePath); yield return new EnterPlayMode();
            previousCapture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60;
            previousMode = InputSystem.settings.updateMode; previousBackground = InputSystem.settings.backgroundBehavior;
            previousEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            pad = InputSystem.AddDevice<Gamepad>(); input = Object.FindFirstObjectByType<LocalPlayerInput>();
            motor = Object.FindFirstObjectByType<CharacterMotor>(); orbit = Object.FindFirstObjectByType<ThirdPersonCamera>();
            targeting = Object.FindFirstObjectByType<ContextualTargeting>(); ability = Object.FindFirstObjectByType<KineticVectorAbility>();
            Assert.That(input && motor && orbit && targeting && ability, Is.True);
            input.Actions.devices = new InputDevice[] { pad }; input.Actions.bindingMask = InputBinding.MaskByGroup("Gamepad"); input.SetPaused(false);
            yield return Advance(.3f);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (input) input.SetPaused(false);
            if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
            InputSystem.settings.updateMode = previousMode; InputSystem.settings.backgroundBehavior = previousBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditor; Time.captureDeltaTime = previousCapture;
            foreach (var item in temporary) if (item) Object.DestroyImmediate(item); temporary.Clear();
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator PlayerMovementJumpOrbitAndCameraCover()
        {
            Assert.That(motor.Grounded, Is.True);
            Assert.That(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == SuperhumanPlaygroundAuthoring.ScenePath), Is.True);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true)) Assert.That(component, Is.Not.Null);
            Capture("spawn"); Overview();
            Vector3 start = motor.transform.position;
            State(new GamepadState { leftStick = Vector2.left }); yield return Advance(1);
            float walked = Vector3.Distance(start, motor.transform.position);
            Assert.That(walked, Is.InRange(4.5f, 6.5f));
            start = motor.transform.position;
            State(new GamepadState { leftStick = Vector2.up }.WithButton(GamepadButton.LeftStick)); yield return Advance(.8f);
            float ran = Vector3.Distance(start, motor.transform.position);
            Assert.That(ran, Is.GreaterThan(walked)); Neutral(); yield return Advance(.25f);
            float floor = motor.transform.position.y;
            State(new GamepadState().WithButton(GamepadButton.South)); yield return Advance(.12f); Neutral();
            Assert.That(motor.JumpCount, Is.EqualTo(1)); Assert.That(motor.transform.position.y, Is.GreaterThan(floor + .5f));
            yield return Advance(.03f); State(new GamepadState().WithButton(GamepadButton.South)); yield return Advance(.02f); Neutral();
            Assert.That(motor.JumpCount, Is.EqualTo(1), "No midair second jump");
            State(new GamepadState { rightStick = new Vector2(.5f, .1f) }); yield return Advance(.3f); Neutral();
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0, orbit.Yaw)), Is.GreaterThan(10)); Capture("movement-orbit");
            yield return Advance(1); Assert.That(motor.Grounded, Is.True);
            yield return Walk(new Vector3(-13, 0, -5));
            orbit.Look(new Vector2(Mathf.DeltaAngle(orbit.Yaw, 180), orbit.Pitch - 12) / orbit.Settings.MouseSensitivity, true, 0);
            yield return Advance(.3f);
            Assert.That(orbit.CurrentDistance, Is.LessThan(orbit.Settings.Distance - .5f), "Alcove wall compresses camera");
            Assert.That(Physics.OverlapSphere(orbit.transform.position, orbit.Settings.Radius * .8f).All(c => c.transform.IsChildOf(motor.transform)), Is.True, "Camera outside static cover");
            Capture("camera-cover");
            Record("movement", $"walk={walked:F2}; sprint={ran:F2}; jumps={motor.JumpCount}; cameraDistance={orbit.CurrentDistance:F2}");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator KineticFlowsLightHeavyReactiveDamageInvalidAndCooldown()
        {
            var light = Target("Light pallet"); var heavy = Target("Heavy equipment");
            yield return Walk(new Vector3(-7, 0, -2)); yield return AimAt(light);
            Assert.That(targeting.Current, Is.EqualTo(light)); Assert.That(Object.FindFirstObjectByType<TargetFeedback>().State, Is.EqualTo(ReticleState.Candidate));
            Capture("light-targeted"); Vector3 before = light.Body.position;
            Vector3 playerScreen = orbit.View.WorldToViewportPoint(motor.transform.position + Vector3.up);
            Vector3 targetScreen = orbit.View.WorldToViewportPoint(light.AimPoint);
            Assert.That(Mathf.Abs(playerScreen.x - targetScreen.x), Is.GreaterThan(.06f), "Shoulder framing separates player and target silhouettes");
            yield return Fire(); Assert.That(ability.LastTarget, Is.EqualTo(light)); Capture("light-impulse");
            Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.CoolingDown));
            yield return Advance(.02f); State(new GamepadState { rightTrigger = 1 }); yield return Advance(.02f); Neutral();
            Assert.That(ability.ActivationCount, Is.EqualTo(1)); yield return Advance(.4f);
            float lightTravel = Vector3.Distance(before, light.Body.position);
            Assert.That(lightTravel, Is.GreaterThan(2)); Assert.That(light.GetComponent<DamageReceiver>(), Is.Null);
            yield return Walk(new Vector3(0, 0, -2)); yield return AimAt(heavy); before = heavy.Body.position;
            yield return Fire(); Assert.That(ability.LastTarget, Is.EqualTo(heavy)); Capture("heavy-impulse"); yield return Advance(.44f);
            float heavyTravel = Vector3.Distance(before, heavy.Body.position);
            Assert.That(heavyTravel, Is.GreaterThan(.05f)); Assert.That(heavyTravel, Is.LessThan(lightTravel * .5f));
            var reactive = Target("Reactive training mannequin");
            yield return Walk(new Vector3(7, 0, -2)); yield return AimAt(reactive);
            yield return Fire(); Assert.That(ability.LastTarget, Is.EqualTo(reactive));
            var entity = reactive.GetComponent<ReactiveEntity>();
            Assert.That(entity.State, Is.EqualTo(ReactiveState.Impacted)); Assert.That(entity.ImpactCount, Is.EqualTo(1));
            Assert.That(reactive.GetComponent<DamageReceiver>().Health.Current, Is.LessThan(36)); Capture("reactive-impact");
            var cabinet = Target("Damageable power cabinet");
            yield return Walk(new Vector3(13, 0, 3)); yield return AimAt(cabinet); yield return Fire();
            Assert.That(ability.LastTarget, Is.EqualTo(cabinet)); Assert.That(cabinet.GetComponent<DamageStateVisual>().DamageVisible, Is.True);
            Capture("damaged-cabinet"); yield return Advance(.7f);
            orbit.Look(new Vector2(0, orbit.Pitch + 50) / orbit.Settings.MouseSensitivity, true, 0); yield return Advance(.2f);
            int shots = ability.ActivationCount; yield return Fire();
            Assert.That(ability.ActivationCount, Is.EqualTo(shots)); Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.InvalidTarget));
            Assert.That(ability.Ready, Is.True); Assert.That(Object.FindFirstObjectByType<TargetFeedback>().State, Is.EqualTo(ReticleState.Neutral)); Capture("no-target");
            Record("interactions", $"lightTravel={lightTravel:F3}; heavyTravel={heavyTravel:F3}; activations={shots}; reactiveHits={entity.ImpactCount}; cabinetHealth={cabinet.GetComponent<DamageReceiver>().Health.Current}");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator TargetingRejectsRangeDisabledAndOccludedCandidates()
        {
            var heavy = Target("Heavy equipment"); yield return AimAt(heavy);
            Assert.That(targeting.TrySelect(out var selected), Is.True); Assert.That(selected.Target, Is.EqualTo(heavy));
            heavy.enabled = false; Assert.That(targeting.TryValidate(heavy, out _), Is.False); heavy.enabled = true;
            heavy.Body.isKinematic = true; Assert.That(targeting.TryValidate(heavy, out _), Is.False); heavy.Body.isKinematic = false;
            Vector3 original = heavy.Body.position; heavy.Body.position = new Vector3(0, 1, 40); Physics.SyncTransforms();
            Assert.That(targeting.TryValidate(heavy, out _), Is.False); heavy.Body.position = original; Physics.SyncTransforms();
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube); temporary.Add(cover);
            cover.transform.position = Vector3.Lerp(targeting.Origin, heavy.AimPoint, .5f); cover.transform.localScale = new Vector3(4, 5, .5f); Physics.SyncTransforms();
            Assert.That(targeting.TryValidate(heavy, out _), Is.False); Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.InvalidTarget));
            Object.DestroyImmediate(cover); Physics.SyncTransforms();
            Assert.That(targeting.TryValidate(heavy, out _), Is.True);
            input.SetPaused(true); Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.Unavailable)); input.SetPaused(false);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator ForceAndDamageRemainIndependentAndLifecycleRetainsState()
        {
            var reactive = Target("Reactive training mannequin"); var receiver = reactive.GetComponent<DamageReceiver>();
            var original = ability.Settings; var forceOnly = Object.Instantiate(original); temporary.Add(forceOnly);
            var data = new SerializedObject(forceOnly); data.FindProperty("damage").floatValue = 0; data.ApplyModifiedPropertiesWithoutUndo();
            data = new SerializedObject(ability); data.FindProperty("settings").objectReferenceValue = forceOnly; data.ApplyModifiedPropertiesWithoutUndo();
            yield return Walk(new Vector3(7, 0, -2)); yield return AimAt(reactive);
            ForceImpact? observed = null; void OnImpact(ForceImpact impact) => observed = impact;
            reactive.Impacted += OnImpact; yield return Fire(); reactive.Impacted -= OnImpact;
            Assert.That(observed.HasValue, Is.True); Assert.That(observed.Value.Impulse.magnitude, Is.EqualTo(forceOnly.Impulse).Within(.001f));
            Assert.That(receiver.Health.Current, Is.EqualTo(receiver.Health.Maximum));
            Assert.That(reactive.Body.linearVelocity.magnitude, Is.GreaterThan(1));
            Vector3 velocity = reactive.Body.linearVelocity; Assert.That(receiver.Receive(new Damage(100)), Is.True);
            Assert.That(reactive.Body.linearVelocity, Is.EqualTo(velocity), "Damage itself applies no force");
            Assert.That(receiver.Health.IsDepleted, Is.True); Assert.That(reactive.GetComponent<ReactiveEntity>().State, Is.EqualTo(ReactiveState.Depleted));
            Capture("reactive-depleted");
            reactive.gameObject.SetActive(false); reactive.gameObject.SetActive(true);
            Assert.That(receiver.Health.IsDepleted, Is.True); Assert.That(reactive.GetComponent<ReactiveEntity>().State, Is.EqualTo(ReactiveState.Depleted));
            ability.enabled = false; Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.Unavailable)); ability.enabled = true;
            Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.CoolingDown));
            receiver.enabled = false; Assert.That(receiver.Receive(new Damage(1)), Is.False); receiver.enabled = true;
            LogAssert.NoUnexpectedReceived();
        }
        private PhysicalTarget Target(string name) => GameObject.Find(name).GetComponent<PhysicalTarget>();
        [UnityTest]
        public IEnumerator CentreRayBeatsNearLateralAndSizeCannotStealIntent()
        {
            IsolateAim();
            var lateral = AimTarget("Near lateral", .08f, 6, .3f);
            var centred = AimTarget("Far centred", 0, 18, .3f);
            AssertSelection(centred);
            lateral.transform.localScale = Vector3.one;
            Physics.SyncTransforms(); AssertSelection(centred);
            Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.Activated));
            Assert.That(ability.LastTarget, Is.EqualTo(centred));
            Assert.That(lateral.Body.linearVelocity, Is.EqualTo(Vector3.zero));
            yield return null;
        }
        [UnityTest]
        public IEnumerator AssistanceRetainsSmallJitterButReleasesAndRetargetsWithCamera()
        {
            IsolateAim();
            var left = AimTarget("Left", -.04f, 10, .15f);
            var right = AimTarget("Right", .045f, 10, .15f);
            AssertSelection(left);
            orbit.transform.rotation = Quaternion.Euler(0, .5f, 0);
            for (int i = 0; i < 8; i++) AssertSelection(left);
            orbit.transform.LookAt(right.AimPoint); AssertSelection(right);
            orbit.transform.rotation = Quaternion.Euler(0, 40, 0);
            Assert.That(targeting.TrySelect(out _), Is.False); Assert.That(targeting.Current, Is.Null);
            yield return null;
        }
        [UnityTest]
        public IEnumerator CloseCandidatesUseAuthoredTieBreakAndRetainSelection()
        {
            IsolateAim();
            var first = AimTarget("Equal first", -.04f, 10, .15f);
            AimTarget("Equal second", .04f, 10, .15f);
            for (int i = 0; i < 4; i++)
            {
                targeting.enabled = false; targeting.enabled = true;
                AssertSelection(first); AssertSelection(first);
            }
            yield return null;
        }
        [UnityTest]
        public IEnumerator SizeAllowanceIsBoundedAndDoesNotBecomeLockOn()
        {
            IsolateAim();
            var large = AimTarget("Large assisted", .09f, 10, 1);
            AssertSelection(large);
            targeting.enabled = false; targeting.enabled = true;
            large.transform.localScale = Vector3.one * .15f; Physics.SyncTransforms();
            Assert.That(targeting.TrySelect(out _), Is.False);
            large.transform.localScale = Vector3.one;
            PlaceAimTarget(large, .101f, 10); Assert.That(targeting.TrySelect(out _), Is.False);
            yield return null;
        }
        [UnityTest]
        public IEnumerator InvalidSnapshotLeavesForceDamageCooldownAndRecoilUntouched()
        {
            IsolateAim();
            var target = AimTarget("Blocked", 0, 10, .3f);
            var receiver = target.gameObject.AddComponent<DamageReceiver>();
            AssertSelection(target);
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cube); temporary.Add(cover);
            cover.transform.position = Vector3.Lerp(targeting.Origin, target.AimPoint, .5f);
            cover.transform.localScale = Vector3.one * 2; Physics.SyncTransforms();
            Vector3 velocity = motor.Velocity;
            int impacts = 0; target.Impacted += _ => impacts++;
            Assert.That(targeting.TryValidate(target, out _), Is.False);
            Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.InvalidTarget));
            Assert.That(ability.Ready, Is.True); Assert.That(ability.ActivationCount, Is.Zero);
            Assert.That(motor.Velocity, Is.EqualTo(velocity)); Assert.That(target.Body.linearVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(impacts, Is.Zero);
            Assert.That(receiver.Health.Current, Is.EqualTo(receiver.Health.Maximum));
            Object.DestroyImmediate(cover); PlaceAimTarget(target, 0, 30);
            Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.InvalidTarget));
            Assert.That(ability.Ready, Is.True); Assert.That(motor.Velocity, Is.EqualTo(velocity));
            yield return null;
        }
        [UnityTest]
        public IEnumerator ReticleNeutralCandidateActivationAndDisableClearTransientState()
        {
            IsolateAim();
            var feedback = Object.FindFirstObjectByType<TargetFeedback>();
            Assert.That(targeting.TrySelect(out _), Is.False);
            Assert.That(feedback.Visible, Is.True); Assert.That(feedback.State, Is.EqualTo(ReticleState.Neutral));
            Assert.That(feedback.GetComponent<LineRenderer>(), Is.Null, "No target-bound renderer remains on the prefab");
            Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.InvalidTarget));
            var target = AimTarget("Pulse", 0, 10, .3f); AssertSelection(target);
            Assert.That(feedback.State, Is.EqualTo(ReticleState.Candidate));
            Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.Activated));
            Assert.That(feedback.State, Is.EqualTo(ReticleState.Activation));
            targeting.Suspended = true;
            Assert.That(feedback.Visible, Is.False); Assert.That(feedback.State, Is.EqualTo(ReticleState.Neutral));
            targeting.Suspended = false;
            Assert.That(feedback.State, Is.EqualTo(ReticleState.Neutral));
            targeting.enabled = false; feedback.enabled = false;
            targeting.enabled = true; feedback.enabled = true;
            Assert.That(targeting.Current, Is.Null); Assert.That(feedback.State, Is.EqualTo(ReticleState.Neutral));
            yield return null;
        }
        [UnityTest]
        public IEnumerator DirectMouseAndGamepadUseIdenticalPrimaryPowerRoute()
        {
            IsolateAim(); input.enabled = true;
            var target = AimTarget("Direct input", 0, 10, .3f);
            AssertSelection(target);
            yield return Fire(); Assert.That(ability.LastTarget, Is.EqualTo(target)); Assert.That(ability.ActivationCount, Is.EqualTo(1));
            var feedback = Object.FindFirstObjectByType<TargetFeedback>();
            Assert.That(feedback.State, Is.EqualTo(ReticleState.Activation));
            yield return Advance(ability.Settings.Cooldown + .1f);
            target.Body.linearVelocity = Vector3.zero; target.Body.angularVelocity = Vector3.zero;
            PlaceAimTarget(target, 0, 10); AssertSelection(target);
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                input.Actions.devices = new InputDevice[] { mouse }; input.Actions.bindingMask = InputBinding.MaskByGroup("KeyboardMouse");
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                yield return Advance(1f / 60);
                InputSystem.QueueStateEvent(mouse, new MouseState()); yield return Advance(.02f);
                Assert.That(ability.ActivationCount, Is.EqualTo(2)); Assert.That(ability.LastTarget, Is.EqualTo(target));
                yield return Advance(.2f); Assert.That(feedback.State, Is.Not.EqualTo(ReticleState.Activation));
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }
        [UnityTest]
        public IEnumerator SceneReloadCreatesNeutralTransientState()
        {
            IsolateAim(); var target = AimTarget("Reload pulse", 0, 10, .3f); AssertSelection(target);
            Assert.That(ability.TryActivate(), Is.EqualTo(ActivationResult.Activated));
            Assert.That(Object.FindFirstObjectByType<TargetFeedback>().State, Is.EqualTo(ReticleState.Activation));
            yield return SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            input = Object.FindFirstObjectByType<LocalPlayerInput>(); targeting = Object.FindFirstObjectByType<ContextualTargeting>();
            input.SetPaused(true);
            Assert.That(targeting.Current, Is.Null);
            Assert.That(Object.FindFirstObjectByType<TargetFeedback>().State, Is.EqualTo(ReticleState.Neutral));
            Assert.That(Object.FindFirstObjectByType<KineticVectorAbility>().ActivationCount, Is.Zero);
        }
        private void IsolateAim()
        {
            input.enabled = false; orbit.enabled = false; targeting.Suspended = false;
            orbit.transform.SetPositionAndRotation(new Vector3(0, 100, 0), Quaternion.identity);
            orbit.View.fieldOfView = 60; orbit.View.aspect = 1.6f;
            var origin = new GameObject("Isolated force origin"); temporary.Add(origin); origin.transform.position = orbit.transform.position;
            var data = new SerializedObject(targeting); data.FindProperty("origin").objectReferenceValue = origin.transform; data.ApplyModifiedPropertiesWithoutUndo();
            Physics.SyncTransforms();
        }
        private PhysicalTarget AimTarget(string name, float radius, float depth, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); temporary.Add(go); go.name = name;
            go.transform.localScale = Vector3.one * size;
            var body = go.AddComponent<Rigidbody>(); body.useGravity = false;
            var target = go.AddComponent<PhysicalTarget>(); PlaceAimTarget(target, radius, depth); return target;
        }
        private void PlaceAimTarget(PhysicalTarget target, float radius, float depth)
        {
            target.transform.position = orbit.View.ViewportToWorldPoint(new Vector3(.5f + radius / orbit.View.aspect, .5f, depth));
            Physics.SyncTransforms();
        }
        private void AssertSelection(PhysicalTarget expected)
        { Assert.That(targeting.TrySelect(out var selected), Is.True); Assert.That(selected.Target, Is.EqualTo(expected)); }
        [UnityTest]
        public IEnumerator IndirectPhysicalCollisionProducesReactionAndDamage()
        {
            var reactive = Target("Reactive training mannequin");
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere); temporary.Add(projectile);
            projectile.transform.localScale = Vector3.one * .4f;
            projectile.transform.position = reactive.AimPoint + Vector3.back * 2;
            var body = projectile.AddComponent<Rigidbody>(); body.mass = 2; body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; body.linearVelocity = Vector3.forward * 10;
            yield return Advance(.3f);
            Assert.That(reactive.GetComponent<ReactiveEntity>().ImpactCount, Is.GreaterThan(0));
            Assert.That(reactive.GetComponent<DamageReceiver>().Health.Current, Is.LessThan(36));
            Assert.That(reactive.Body.position.z, Is.GreaterThan(5.05f));
            LogAssert.NoUnexpectedReceived();
        }
        private IEnumerator Walk(Vector3 destination)
        {
            float deadline = Time.time + 10;
            while (Vector3.ProjectOnPlane(destination - motor.transform.position, Vector3.up).magnitude > .35f && Time.time < deadline)
            {
                Vector3 local = Quaternion.Euler(0, -orbit.Yaw, 0) * Vector3.ProjectOnPlane(destination - motor.transform.position, Vector3.up).normalized;
                State(new GamepadState { leftStick = new Vector2(local.x, local.z) }); yield return Advance(1f / 60);
            }
            Neutral(); yield return Advance(.25f);
            Assert.That(Vector3.ProjectOnPlane(destination - motor.transform.position, Vector3.up).magnitude, Is.LessThan(1.4f), "Input route reached " + destination);
        }
        private IEnumerator AimAt(PhysicalTarget target)
        {
            Neutral();
            for (int i = 0; i < 16; i++)
            {
                Vector3 direction = (target.AimPoint - orbit.transform.position).normalized;
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, pitch = -Mathf.Asin(direction.y) * Mathf.Rad2Deg;
                orbit.Look(new Vector2(Mathf.DeltaAngle(orbit.Yaw, yaw), orbit.Pitch - pitch) / orbit.Settings.MouseSensitivity, true, 0);
                yield return Advance(1f / 60);
            }
            Assert.That(targeting.TrySelect(out var result), Is.True); Assert.That(result.Target, Is.EqualTo(target));
        }
        private IEnumerator Fire()
        { State(new GamepadState { rightTrigger = 1 }); yield return Advance(1f / 60); Neutral(); yield return Advance(.05f); }
        private void State(GamepadState value) => InputSystem.QueueStateEvent(pad, value);
        private void Neutral() => State(default);
        private IEnumerator Advance(float seconds)
        {
            float until = Time.time + seconds, timeout = Time.realtimeSinceStartup + 30;
            while (Time.time < until) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(timeout), "Unexpected pause"); InputSystem.Update(); yield return null; }
        }
        private static void Record(string name, string text)
        { Directory.CreateDirectory("Logs/Production01"); File.WriteAllText("Logs/Production01/" + name + ".txt", text); }
        private static void Overview()
        {
            var go = new GameObject("Validation overview camera"); var view = go.AddComponent<Camera>(); view.fieldOfView = 65;
            view.transform.position = new Vector3(28, 28, -32); view.transform.LookAt(Vector3.zero);
            Capture("playground-overview", view); Object.DestroyImmediate(go);
        }
        private static void Capture(string name, Camera camera = null)
        {
            var texture = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                RenderPipeline.SubmitRenderRequest(camera ? camera : Camera.main, new RenderPipeline.StandardRequest { destination = texture });
                RenderTexture.active = texture; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/Production01"); File.WriteAllBytes("Logs/Production01/" + name + ".png", pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; texture.Release(); Object.DestroyImmediate(texture); Object.DestroyImmediate(pixels); }
        }
    }
}
