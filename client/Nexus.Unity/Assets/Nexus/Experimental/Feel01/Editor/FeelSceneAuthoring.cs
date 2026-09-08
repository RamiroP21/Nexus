using System.IO;
using Nexus.Feel01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nexus.Feel01.Editor
{
    // One-off authoring retained for reproducibility. Refuses to overwrite an existing experiment.
    public static class FeelSceneAuthoring
    {
        public const string ScenePath = "Assets/Nexus/Experimental/GameplayFeel01.unity";
        private const string DataPath = "Assets/Nexus/Experimental/Feel01/Data";

        [MenuItem("Nexus/Feel 01/Open gameplay scene")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("GameplayFeel01 already exists; edit the scene directly.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Directory.CreateDirectory(DataPath);
            AssetDatabase.Refresh();
            var settings = ScriptableObject.CreateInstance<FeelSettings>();
            AssetDatabase.CreateAsset(settings, DataPath + "/Feel01Settings.asset");
            var floor = Material("Floor", new Color(.19f, .23f, .27f));
            var concrete = Material("Concrete", new Color(.46f, .49f, .51f));
            var edge = Material("Edges", new Color(.78f, .8f, .78f));
            var light = Material("Light mass", new Color(.08f, .8f, .65f));
            var heavy = Material("Heavy mass", new Color(.95f, .4f, .1f));
            var dummy = Material("Dummy", new Color(.72f, .23f, .33f));
            var playerMaterial = Material("Player", new Color(.72f, .83f, .95f));
            var forward = Material("Forward marker", new Color(1, .85f, .2f));
            var beamMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            beamMaterial.SetColor("_BaseColor", new Color(.25f, 1, .8f));
            AssetDatabase.CreateAsset(beamMaterial, DataPath + "/Pulse.mat");
            var friction = new PhysicsMaterial("Arena contact") { dynamicFriction = .45f, staticFriction = .55f, bounciness = .05f };
            AssetDatabase.CreateAsset(friction, DataPath + "/ArenaContact.physicMaterial");

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.55f, .6f, .67f);
            RenderSettings.fog = false;
            var arena = new GameObject("Greybox — 36 x 44 metres");
            Box("Floor", new Vector3(0, -.5f, 0), new Vector3(36, 1, 44), floor, arena.transform, friction);
            Box("Rear wall", new Vector3(0, 1.5f, -22), new Vector3(36, 3, .5f), concrete, arena.transform);
            Box("Front wall", new Vector3(0, 1.5f, 22), new Vector3(36, 3, .5f), concrete, arena.transform);
            Box("Left wall", new Vector3(-18, 1.5f, 0), new Vector3(.5f, 3, 44), concrete, arena.transform);
            Box("Right wall", new Vector3(18, 1.5f, 0), new Vector3(.5f, 3, 44), concrete, arena.transform);
            Box("Camera obstruction wall", new Vector3(7, 1.7f, -13), new Vector3(5, 3.4f, .5f), concrete, arena.transform);
            // Ramp top rises from approximately y=0 at z=-5 to y=3 at z=5.
            var ramp = Box("Ramp 17 degrees", new Vector3(-11, 1.35f, 0), new Vector3(6, .3f, 10.44f), concrete, arena.transform);
            ramp.transform.rotation = Quaternion.Euler(-16.7f, 0, 0);
            Box("Upper deck — 3m drop", new Vector3(-11, 1.5f, 8), new Vector3(7, 3, 6), concrete, arena.transform);
            for (int i = 0; i < 8; i++)
            {
                float height = (i + 1) * .25f;
                Box("Step " + (i + 1), new Vector3(12, height / 2, -5 + i), new Vector3(4, height, 1), concrete, arena.transform);
            }
            Box("Step deck — 2m", new Vector3(12, 1, 5), new Vector3(5, 2, 5), concrete, arena.transform);
            Box("Jump block 0.75m", new Vector3(1, .375f, 10), new Vector3(3, .75f, 3), concrete, arena.transform);
            Box("Jump block 1.5m", new Vector3(5, .75f, 13), new Vector3(3, 1.5f, 3), concrete, arena.transform);
            for (int z = -15; z <= 15; z += 5)
                Box("5m track marker " + z, new Vector3(-5.5f, .005f, z), new Vector3(.08f, .01f, .8f), edge, arena.transform, null, false);
            Label("MOVE / BRAKE / JUMP", new Vector3(-3, .025f, -18), 90, .15f, arena.transform);
            Label("RAMP / DROP 3m", new Vector3(-11, .025f, -7), 90, .13f, arena.transform);
            Label("STEPS 0.25m", new Vector3(12, .025f, -7), 90, .13f, arena.transform);

            var propsRoot = new GameObject("Mass response — equal impulse");
            var lightBody = Prop("LIGHT / 2 kg", new Vector3(-3, .65f, -5), 2, light, propsRoot.transform, friction);
            var heavyBody = Prop("HEAVY / 12 kg", new Vector3(2, .65f, -5), 12, heavy, propsRoot.transform, friction);
            var extra = Prop("LIGHT / 2 kg (air experiment)", new Vector3(-11, 3.65f, 8), 2, light, propsRoot.transform, friction);
            var dummyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummyObject.name = "DUMMY / 8 kg";
            dummyObject.transform.SetParent(propsRoot.transform);
            dummyObject.transform.position = new Vector3(7, 1, 1);
            dummyObject.GetComponent<Renderer>().sharedMaterial = dummy;
            dummyObject.GetComponent<Collider>().sharedMaterial = friction;
            var dummyBody = dummyObject.AddComponent<Rigidbody>();
            ConfigureBody(dummyBody, 8);
            dummyBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Label("DUMMY / 8 kg", new Vector3(7, 2.5f, 1), 0, .09f, propsRoot.transform);

            var player = new GameObject("Player");
            player.layer = 2; // Built-in Ignore Raycast: camera, power and ground probes exclude the player.
            player.transform.position = new Vector3(0, .05f, -15);
            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = .35f; controller.center = new Vector3(0, .9f, 0);
            controller.skinWidth = .035f; controller.stepOffset = .3f; controller.slopeLimit = 48; controller.minMoveDistance = 0;
            var body = new GameObject("Facing"); body.transform.SetParent(player.transform, false);
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Capsule"; capsule.transform.SetParent(body.transform, false);
            capsule.transform.localPosition = Vector3.up * .9f; capsule.transform.localScale = new Vector3(.65f, .9f, .65f);
            Object.DestroyImmediate(capsule.GetComponent<Collider>());
            capsule.GetComponent<Renderer>().sharedMaterial = playerMaterial;
            var marker = Box("Front visor", Vector3.zero, new Vector3(.32f, .15f, .12f), forward, body.transform, null, false);
            marker.transform.localPosition = new Vector3(0, 1.5f, .32f);
            var muzzle = new GameObject("Pulse origin"); muzzle.transform.SetParent(player.transform, false);
            muzzle.transform.localPosition = new Vector3(0, 1.4f, .1f);

            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.nearClipPlane = .05f; camera.farClipPlane = 150;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.09f, .14f, .21f);
            cameraObject.AddComponent<AudioListener>(); cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var orbit = cameraObject.AddComponent<FeelCamera>();
            var actor = player.AddComponent<FeelPlayer>();
            var pulse = player.AddComponent<KineticPulse>();
            var line = player.GetComponent<LineRenderer>(); line.positionCount = 2; line.useWorldSpace = true;
            line.startWidth = .055f; line.endWidth = .018f; line.sharedMaterial = beamMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false; line.enabled = false;
            var audio = player.GetComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 0; audio.volume = .5f;
            Set(orbit, "settings", settings); Set(orbit, "target", player.transform); Set(orbit, "body", body.transform);
            Set(actor, "settings", settings); Set(actor, "orbit", orbit); Set(actor, "power", pulse); Set(actor, "body", body.transform);
            Set(actor, "inputTemplate", AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Nexus/Input/NexusInput.inputactions"));
            Set(pulse, "settings", settings); Set(pulse, "player", actor); Set(pulse, "orbit", orbit); Set(pulse, "muzzle", muzzle.transform);
            var reset = arena.AddComponent<FeelArena>(); Set(reset, "player", actor);
            var resetData = new SerializedObject(reset); var bodies = resetData.FindProperty("props"); bodies.arraySize = 4;
            var allBodies = new[] { lightBody, heavyBody, extra, dummyBody };
            for (int i = 0; i < allBodies.Length; i++) bodies.GetArrayElementAtIndex(i).objectReferenceValue = allBodies[i];
            resetData.ApplyModifiedPropertiesWithoutUndo();

            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.4f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0); sun.gameObject.AddComponent<UniversalAdditionalLightData>();
            RenderSettings.sun = sun;
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene("Assets/Nexus/Experimental/Bootstrap.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("FEEL01: scene and settings authored; Bootstrap preserved.");
        }

        private static Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .18f);
            AssetDatabase.CreateAsset(material, DataPath + "/" + name.Replace(' ', '_') + ".mat");
            return material;
        }

        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material,
            Transform parent, PhysicsMaterial friction = null, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            else if (friction) go.GetComponent<Collider>().sharedMaterial = friction;
            return go;
        }

        private static Rigidbody Prop(string name, Vector3 position, float mass, Material material, Transform parent, PhysicsMaterial friction)
        {
            var go = Box(name, position, Vector3.one * 1.2f, material, parent, friction);
            var rb = go.AddComponent<Rigidbody>(); ConfigureBody(rb, mass);
            Label(name, position + Vector3.up * 1.1f, 0, .08f, parent);
            return rb;
        }

        private static void ConfigureBody(Rigidbody body, float mass)
        {
            body.mass = mass; body.linearDamping = .15f; body.angularDamping = .8f;
            body.maxAngularVelocity = 8; body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private static void Label(string text, Vector3 position, float pitch, float scale, Transform parent)
        {
            var label = new GameObject(text + " label").AddComponent<TextMesh>();
            label.transform.SetParent(parent); label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(pitch, 0, 0);
            label.text = text; label.characterSize = scale; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter;
            label.color = new Color(.9f, .95f, 1);
        }

        private static void Set(Object target, string name, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
