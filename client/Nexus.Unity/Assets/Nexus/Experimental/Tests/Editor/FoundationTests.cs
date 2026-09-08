using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nexus.Unity.Tests
{
    public sealed class FoundationTests
    {
        private const string ScenePath = "Assets/Nexus/Experimental/Bootstrap.unity";
        private const string InputPath = "Assets/Nexus/Input/NexusInput.inputactions";

        [Test]
        public void BootstrapIsTheOnlyBuildSceneAndHasNoMissingScripts()
        {
            Assert.That(EditorBuildSettings.scenes.Length, Is.EqualTo(1));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(ScenePath));
            Assert.That(EditorBuildSettings.scenes[0].enabled, Is.True);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AssertScene(scene);
        }

        [Test]
        public void WindowsUsesUrpLinearRenderingAndOnlyTheNewInputBackend()
        {
            Assert.That(EditorUserBuildSettings.activeBuildTarget, Is.EqualTo(BuildTarget.StandaloneWindows64));
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(GraphicsSettings.defaultRenderPipeline));
            Assert.That(PlayerSettings.colorSpace, Is.EqualTo(ColorSpace.Linear));
            Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64),
                Is.EqualTo(new[] { GraphicsDeviceType.Direct3D11 }));
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            Assert.That(settings.FindProperty("activeInputHandler").intValue, Is.EqualTo(1));
        }

        [Test]
        public void InputDefinesSixSemanticActionsAndBothControlSchemes()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.actionMaps.Count, Is.EqualTo(1));
            var map = asset.FindActionMap("Gameplay", true);
            Assert.That(map.actions.Select(action => action.name), Is.EquivalentTo(
                new[] { "Move", "Look", "Jump", "Sprint", "PrimaryPower", "Pause" }));
            Assert.That(map["Move"].expectedControlType, Is.EqualTo("Vector2"));
            Assert.That(map["Look"].expectedControlType, Is.EqualTo("Vector2"));
            Assert.That(asset.controlSchemes.Select(scheme => scheme.name),
                Is.EquivalentTo(new[] { "KeyboardMouse", "Gamepad" }));
            foreach (var action in map.actions)
            {
                Assert.That(action.bindings.Any(binding => binding.groups.Contains("KeyboardMouse")), Is.True, action.name);
                Assert.That(action.bindings.Any(binding => binding.groups.Contains("Gamepad")), Is.True, action.name);
            }
        }

        [UnityTest]
        public IEnumerator KeyboardMouseAndGamepadProduceSemanticIntentAndHandleRemoval()
        {
            yield return new EnterPlayMode();
            var previousUpdateMode = InputSystem.settings.updateMode;
            var previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            var previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var asset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath));
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                // Restrict the test copy to synthetic devices; do not consume or reset physical hardware.
                asset.devices = new InputDevice[] { keyboard, mouse, pad };
                var map = asset.FindActionMap("Gameplay", true);
                asset.bindingMask = InputBinding.MaskByGroup("KeyboardMouse");
                asset.Enable();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space, Key.LeftShift, Key.Escape));
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = new Vector2(12, -5), buttons = 1 });
                InputSystem.Update();
                Assert.That(keyboard.wKey.isPressed, Is.True, "Synthetic keyboard state reached manual update");
                Assert.That(map["Move"].controls.Count, Is.GreaterThan(0), "Move bindings resolve");
                Assert.That(map["Move"].ReadValue<Vector2>(), Is.EqualTo(Vector2.up));
                Assert.That(map["Look"].ReadValue<Vector2>(), Is.EqualTo(new Vector2(12, -5)));
                foreach (string name in new[] { "Jump", "Sprint", "PrimaryPower", "Pause" })
                    Assert.That(map[name].IsPressed(), Is.True, name);

                asset.Disable();
                asset.bindingMask = InputBinding.MaskByGroup("Gamepad");
                asset.Enable();
                var padState = new GamepadState { leftStick = Vector2.right, rightStick = Vector2.up, rightTrigger = 1 };
                padState = padState.WithButton(GamepadButton.South).WithButton(GamepadButton.LeftStick).WithButton(GamepadButton.Start);
                InputSystem.QueueStateEvent(pad, padState);
                InputSystem.Update();
                Assert.That(map["Move"].ReadValue<Vector2>(), Is.EqualTo(Vector2.right));
                Assert.That(map["Look"].ReadValue<Vector2>(), Is.EqualTo(Vector2.up));
                foreach (string name in new[] { "Jump", "Sprint", "PrimaryPower", "Pause" })
                    Assert.That(map[name].IsPressed(), Is.True, name);

                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0.001f, 0.001f) });
                InputSystem.Update();
                Assert.That(map["Move"].ReadValue<Vector2>(), Is.EqualTo(Vector2.zero), "Stick deadzone");
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.one });
                InputSystem.Update();
                InputSystem.RemoveDevice(pad);
                InputSystem.Update();
                Assert.That(map["Move"].ReadValue<Vector2>(), Is.EqualTo(Vector2.zero), "No stuck intent after unplug");
                asset.Disable();
                Assert.That(map.enabled, Is.False);
            }
            finally
            {
                asset.Disable();
                if (pad.added) InputSystem.RemoveDevice(pad);
                InputSystem.RemoveDevice(mouse);
                InputSystem.RemoveDevice(keyboard);
                Object.DestroyImmediate(asset);
                InputSystem.settings.updateMode = previousUpdateMode;
                InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
            }
            yield return new ExitPlayMode();
            Assert.That(Application.isPlaying, Is.False);
        }

        [UnityTest]
        public IEnumerator BootstrapEntersRendersAndLeavesPlayModeTwice()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            yield return new EnterPlayMode();
            Assert.That(Application.isPlaying, Is.True);
            yield return null;
            AssertScene(SceneManager.GetActiveScene());
            Assert.That(GraphicsSettings.currentRenderPipeline, Is.TypeOf<UniversalRenderPipelineAsset>());
            CaptureRender();
            Debug.Log("NEXUS FOUNDATION: entered Play Mode, Bootstrap loaded and URP rendered.");
            yield return new ExitPlayMode();
            Assert.That(Application.isPlaying, Is.False);
            yield return new EnterPlayMode();
            yield return null;
            Assert.That(Application.isPlaying, Is.True);
            AssertScene(SceneManager.GetActiveScene());
            yield return new ExitPlayMode();
            Assert.That(Application.isPlaying, Is.False);
            LogAssert.NoUnexpectedReceived();
            Debug.Log("NEXUS FOUNDATION: two Play Mode entry/exit cycles PASS.");
        }

        private static void AssertScene(Scene scene)
        {
            Assert.That(scene.path, Is.EqualTo(ScenePath));
            Assert.That(scene.isLoaded, Is.True);
            Assert.That(scene.GetRootGameObjects().Length, Is.EqualTo(2), "Only camera and light, no gameplay");
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    Assert.That(component, Is.Not.Null, "Missing script in " + root.name);
        }

        private static void CaptureRender()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null), "Run with graphics, not -nographics");
            var target = new RenderTexture(640, 360, 24, RenderTextureFormat.ARGB32);
            var pixels = new Texture2D(640, 360, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                Assert.That(RenderPipeline.SupportsRenderRequest(Camera.main, request), Is.True);
                RenderPipeline.SubmitRenderRequest(Camera.main, request);
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
                pixels.Apply();
                // Blank scene should render its blue camera clear color, not black or error-magenta.
                Color center = pixels.GetPixel(320, 180);
                Assert.That(center.b, Is.GreaterThan(center.r));
                Assert.That(center.g, Is.GreaterThan(0.01f));
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes("Logs/bootstrap-render.png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(pixels);
            }
        }
    }
}
