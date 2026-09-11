using System;
using System.IO;
using System.Linq;
using Nexus.Gameplay.Abilities;
using Nexus.Gameplay.CameraSystem;
using Nexus.Gameplay.Character;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Interaction;
using Nexus.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Nexus.Gameplay.Editor
{
    // One-time authoring entry point; never silently overwrites the permanent scene or its assets.
    public static partial class SuperhumanPlaygroundAuthoring
    {
        public const string Root = "Assets/Nexus/Gameplay";
        public const string ScenePath = Root + "/Scenes/SuperhumanPlayground.unity";
        public const string PrefabPath = Root + "/Prefabs/SuperhumanPlayerRig.prefab";
        [MenuItem("Nexus/Production/Add First Combat Encounter")]
        public static void AddFirstCombatEncounter()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author outside Play Mode.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (Object.FindFirstObjectByType<HostileCombatant>()) throw new InvalidOperationException("Encounter already exists.");
            var player = Object.FindFirstObjectByType<PlayerVitality>();
            if (!player) throw new InvalidOperationException("01B player required.");
            var root = new GameObject("First hostile encounter").transform;
            var shell = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Target shell.mat");
            var steel = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Steel.mat");
            var accent = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Safety trim.mat");
            var friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root + "/Data/PlaygroundContact.physicMaterial");
            var cover = Box("Encounter reusable cover", new Vector3(8, 1.1f, 10), new Vector3(2.4f, 2.2f, .4f), steel, root);
            var coverBody = cover.AddComponent<Rigidbody>(); ConfigureBody(coverBody, 7); coverBody.constraints = RigidbodyConstraints.FreezeAll;
            cover.AddComponent<PhysicalTarget>(); cover.AddComponent<ImpactBarrier>();
            var prop = Crate("Encounter kinetic prop", new Vector3(12, .85f, 11), 3, shell, accent, root, friction);
            var hostile = new GameObject("Greybox hostile"); hostile.transform.SetParent(root); hostile.transform.position = new Vector3(10, .05f, 16);
            hostile.transform.rotation = Quaternion.Euler(0, 180, 0);
            var shape = hostile.AddComponent<CapsuleCollider>(); shape.center = Vector3.up; shape.height = 2; shape.radius = .45f;
            var body = hostile.AddComponent<Rigidbody>(); ConfigureBody(body, 8); body.constraints = RigidbodyConstraints.FreezeRotation;
            hostile.AddComponent<PhysicalTarget>(); hostile.AddComponent<DamageReceiver>(); hostile.AddComponent<CollisionDamage>();
            var visual = Humanoid(hostile.transform, steel, shell, accent);
            var muzzle = new GameObject("Hostile muzzle").transform; muzzle.SetParent(hostile.transform, false); muzzle.localPosition = new Vector3(0, 1.2f, .85f);
            var warning = new GameObject("Committed attack warning"); warning.transform.SetParent(hostile.transform, false);
            Line(warning, AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Kinetic feedback.mat"), .09f);
            var line = warning.GetComponent<LineRenderer>(); line.positionCount = 2; line.startColor = line.endColor = new Color(1, .25f, .05f);
            var label = new GameObject("Hostile state").AddComponent<TextMesh>(); label.transform.SetParent(hostile.transform, false);
            label.transform.localPosition = new Vector3(0, 2.7f, 0); label.transform.localRotation = Quaternion.Euler(0, 180, 0);
            label.characterSize = .08f; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter;
            var threat = hostile.AddComponent<HostileCombatant>();
            var hazard = Object.FindObjectsByType<TrainingHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            Set(threat, "target", player); Set(threat, "hazardTemplate", hazard); Set(threat, "muzzle", muzzle); Set(threat, "visual", visual); Set(threat, "telegraph", line); Set(threat, "stateLabel", label);
            var encounter = player.gameObject.AddComponent<CombatEncounter>(); Set(encounter, "player", player); Set(encounter, "hostile", threat);
            var serialized = new SerializedObject(encounter); var bodies = serialized.FindProperty("resetBodies"); bodies.arraySize = 2;
            bodies.GetArrayElementAtIndex(0).objectReferenceValue = coverBody; bodies.GetArrayElementAtIndex(1).objectReferenceValue = prop; serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("First hostile encounter authored in existing SuperhumanPlayground.");
        }
        [MenuItem("Nexus/Production/Open Superhuman Playground")]
        public static void Open()
        { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }

        [MenuItem("Nexus/Production/Add Traversal and Combat Foundation")]
        public static void AddTraversalCombat()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author outside Play Mode.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            if (scene.GetRootGameObjects().Any(g => g.name == "Traversal and combat foundation"))
                throw new InvalidOperationException("Foundation exists; edit deliberately rather than duplicate fixtures.");
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var prefabMotor = prefabRoot.GetComponentInChildren<CharacterMotor>();
                if (!prefabMotor.GetComponent<DamageReceiver>()) prefabMotor.gameObject.AddComponent<DamageReceiver>();
                if (!prefabMotor.GetComponent<PlayerVitality>()) prefabMotor.gameObject.AddComponent<PlayerVitality>();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
            var player = Object.FindFirstObjectByType<PlayerVitality>();
            if (!player) throw new InvalidOperationException("Player prefab did not update.");
            var root = new GameObject("Traversal and combat foundation").transform;
            var concrete = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Concrete.mat");
            var accent = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Safety trim.mat");
            var steel = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Steel.mat");
            var shell = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Target shell.mat");
            Box("Vault rail", new Vector3(7, .45f, -14), new Vector3(4, .9f, .35f), accent, root);
            Box("Mantle ledge", new Vector3(15, .95f, -15), new Vector3(4, 1.9f, 3), concrete, root);
            Box("Landing drop platform", new Vector3(17, 1.8f, -10), new Vector3(3, 3.6f, 3), steel, root);
            var barrier = Box("Releasable combat cover", new Vector3(-4, 1.1f, 10), new Vector3(3, 2.2f, .3f), accent, root);
            var barrierBody = barrier.AddComponent<Rigidbody>(); ConfigureBody(barrierBody, 4); barrierBody.constraints = RigidbodyConstraints.FreezeAll;
            barrier.AddComponent<PhysicalTarget>(); barrier.AddComponent<ImpactBarrier>();
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere); projectile.name = "Training hazard template";
            projectile.transform.SetParent(root); projectile.transform.localScale = Vector3.one * .55f;
            projectile.GetComponent<Renderer>().sharedMaterial = accent;
            var projectileBody = projectile.AddComponent<Rigidbody>(); ConfigureBody(projectileBody, 1); projectileBody.useGravity = false;
            projectile.AddComponent<PhysicalTarget>(); var hazard = projectile.AddComponent<TrainingHazard>(); projectile.SetActive(false);
            var sentinel = new GameObject("Training sentinel"); sentinel.transform.SetParent(root); sentinel.transform.position = new Vector3(-4, .05f, 15);
            var shape = sentinel.AddComponent<CapsuleCollider>(); shape.center = Vector3.up; shape.height = 2; shape.radius = .45f;
            var body = sentinel.AddComponent<Rigidbody>(); ConfigureBody(body, 8); body.constraints = RigidbodyConstraints.FreezeRotation;
            sentinel.AddComponent<PhysicalTarget>(); sentinel.AddComponent<DamageReceiver>(); sentinel.AddComponent<CollisionDamage>();
            var visual = Humanoid(sentinel.transform, shell, steel, accent); visual.localRotation = Quaternion.Euler(0, 180, 0);
            var muzzle = new GameObject("Hazard muzzle").transform; muzzle.SetParent(sentinel.transform, false); muzzle.localPosition = new Vector3(0, 1.2f, -.8f);
            var warning = new GameObject("Attack telegraph"); warning.transform.SetParent(sentinel.transform, false);
            Line(warning, AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Kinetic feedback.mat"), .07f);
            var line = warning.GetComponent<LineRenderer>(); line.positionCount = 2; line.startColor = line.endColor = new Color(1, .25f, .05f);
            var label = new GameObject("Sentinel state").AddComponent<TextMesh>(); label.transform.SetParent(sentinel.transform, false);
            label.transform.localPosition = new Vector3(0, 2.5f, 0); label.characterSize = .09f; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter;
            var threat = sentinel.AddComponent<TrainingSentinel>(); Set(threat, "player", player); Set(threat, "hazardTemplate", hazard);
            Set(threat, "muzzle", muzzle); Set(threat, "visual", visual); Set(threat, "telegraph", line); Set(threat, "stateLabel", label);
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Production traversal/combat fixtures added to the existing playground and player prefab.");
        }

        public static void Create()
        {
            if (File.Exists(ScenePath) || File.Exists(PrefabPath)) throw new InvalidOperationException("Production playground already exists; edit it deliberately.");
            // Establish the scene before creating unreferenced ScriptableObjects: scene changes can unload them.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (string folder in new[] { "Data", "Materials", "Scenes", "Prefabs" })
                if (!AssetDatabase.IsValidFolder(Root + "/" + folder)) AssetDatabase.CreateFolder(Root, folder);
            var movement = Config<MovementSettings>("Movement");
            var cameraSettings = Config<CameraSettings>("Camera");
            var targetingSettings = Config<TargetingSettings>("Targeting");
            var kineticSettings = Config<KineticVectorSettings>("KineticVector");
            var concrete = Material("Concrete", new Color(.32f, .36f, .4f));
            var plaster = Material("Warm panels", new Color(.62f, .59f, .5f));
            var steel = Material("Steel", new Color(.1f, .17f, .22f));
            var wood = Material("Pallet wood", new Color(.55f, .34f, .16f));
            var suit = Material("Player suit", new Color(.12f, .38f, .55f));
            var porcelain = Material("Target shell", new Color(.83f, .78f, .64f));
            var accent = Material("Safety trim", new Color(1, .63f, .14f));
            var pulse = Material("Kinetic feedback", new Color(.2f, 1, .88f), true);
            var friction = new PhysicsMaterial("Playground contact") { dynamicFriction = .35f, staticFriction = .4f, bounciness = 0 };
            AssetDatabase.CreateAsset(friction, Root + "/Data/PlaygroundContact.physicMaterial");
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.55f, .6f, .67f);
            var sun = new GameObject("Daylight").AddComponent<Light>(); sun.type = LightType.Directional;
            sun.intensity = 1.5f; sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(48, -30, 0); RenderSettings.sun = sun;
            var environment = new GameObject("Playground architecture").transform;
            Box("Courtyard", new Vector3(0, -.5f, 0), new Vector3(42, 1, 42), concrete, environment, true, friction);
            foreach (float x in new[] { -21f, 21f }) Box("Boundary", new Vector3(x, 2, 0), new Vector3(.5f, 4, 42), plaster, environment);
            foreach (float z in new[] { -21f, 21f }) Box("Boundary", new Vector3(0, 2, z), new Vector3(42, 4, .5f), plaster, environment);
            for (int i = 0; i < 3; i++)
                Box("Elevation step", new Vector3(12, .2f + i * .2f, -10 + i * 2), new Vector3(5, .4f + i * .4f, 2), plaster, environment);
            Box("Raised landing", new Vector3(12, .6f, -3), new Vector3(5, 1.2f, 4), concrete, environment);
            Box("Camera alcove back", new Vector3(-14, 2, -2), new Vector3(7, 4, .5f), plaster, environment);
            Box("Camera alcove side", new Vector3(-17, 2, 1), new Vector3(.5f, 4, 6), plaster, environment);
            Box("Overhead beam", new Vector3(-14, 3.3f, 1), new Vector3(6, .3f, 6), steel, environment);
            Box("Targeting cover", new Vector3(-10, 1.4f, 11), new Vector3(4, 2.8f, .5f), concrete, environment);
            for (int i = 0; i < 6; i++) Box("Lane inlay", new Vector3(-17 + i * 6, .008f, -4), new Vector3(3, .016f, .12f), accent, environment, false);
            var props = new GameObject("Physical targets").transform;
            Crate("Light pallet", new Vector3(-7, .85f, 5), 2, wood, steel, props, friction);
            Crate("Heavy equipment", new Vector3(0, .85f, 5), 18, steel, accent, props, friction);
            Crate("Movable pallet", new Vector3(-13, .85f, 15), 4, wood, steel, props, friction);
            var dummy = new GameObject("Reactive training mannequin"); dummy.transform.SetParent(props); dummy.transform.position = new Vector3(7, .03f, 5);
            var shape = dummy.AddComponent<CapsuleCollider>(); shape.height = 2; shape.radius = .38f; shape.center = Vector3.up; shape.sharedMaterial = friction;
            var dummyBody = dummy.AddComponent<Rigidbody>(); ConfigureBody(dummyBody, 8); dummyBody.constraints = RigidbodyConstraints.FreezeRotation;
            dummy.AddComponent<PhysicalTarget>(); dummy.AddComponent<DamageReceiver>(); dummy.AddComponent<CollisionDamage>();
            var dummyVisual = Humanoid(dummy.transform, porcelain, steel, accent);
            Set(dummy.AddComponent<ReactiveEntity>(), "visual", dummyVisual);
            var cabinet = Crate("Damageable power cabinet", new Vector3(13, .85f, 9), 8, porcelain, steel, props, friction);
            cabinet.gameObject.AddComponent<DamageReceiver>(); cabinet.gameObject.AddComponent<CollisionDamage>();
            var intact = cabinet.transform.GetChild(0).gameObject;
            var broken = new GameObject("Broken panels and exposed frame"); broken.transform.SetParent(cabinet.transform, false);
            for (int i = 0; i < 3; i++)
            {
                var panel = Box("Bent panel", new Vector3(0, -.45f + i * .4f, 0), new Vector3(1.6f, .18f, 1.2f), porcelain, broken.transform, false);
                panel.transform.localRotation = Quaternion.Euler(0, i * 18, 12 + i * 8);
            }
            Box("Exposed core", Vector3.zero, new Vector3(.5f, 1.5f, .5f), steel, broken.transform, false);
            broken.SetActive(false);
            var stateVisual = cabinet.gameObject.AddComponent<DamageStateVisual>(); Set(stateVisual, "intact", intact); Set(stateVisual, "damaged", broken);
            CreateRig(movement, cameraSettings, targetingSettings, kineticSettings, suit, steel, accent, pulse);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Superhuman production playground authored with independent production assets.");
        }

        private static void CreateRig(MovementSettings movement, CameraSettings cameraSettings, TargetingSettings targetingSettings,
            KineticVectorSettings kineticSettings, Material suit, Material steel, Material accent, Material pulse)
        {
            var rig = new GameObject("Superhuman player rig");
            var player = new GameObject("Player"); player.transform.SetParent(rig.transform); player.transform.position = new Vector3(0, .05f, -10);
            var controller = player.AddComponent<CharacterController>(); controller.height = 2; controller.radius = .38f; controller.center = Vector3.up;
            controller.stepOffset = .3f; controller.slopeLimit = 50;
            var visual = Humanoid(player.transform, suit, steel, accent);
            var motor = player.AddComponent<CharacterMotor>(); Set(motor, "settings", movement); Set(motor, "visual", visual);
            var viewObject = new GameObject("Third person camera"); viewObject.transform.SetParent(rig.transform); viewObject.tag = "MainCamera";
            var view = viewObject.AddComponent<Camera>(); view.nearClipPlane = .05f; view.farClipPlane = 200;
            view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.11f, .16f, .22f); view.fieldOfView = cameraSettings.FieldOfView;
            viewObject.AddComponent<AudioListener>();
            var orbit = viewObject.AddComponent<ThirdPersonCamera>(); Set(orbit, "settings", cameraSettings); Set(orbit, "target", player.transform); Set(orbit, "visual", visual);
            var muzzle = new GameObject("Force origin").transform; muzzle.SetParent(player.transform, false); muzzle.localPosition = new Vector3(.45f, 1.35f, .25f);
            var targeting = player.AddComponent<ContextualTargeting>(); Set(targeting, "settings", targetingSettings); Set(targeting, "view", view); Set(targeting, "origin", muzzle); Set(targeting, "owner", player.transform);
            var ability = player.AddComponent<KineticVectorAbility>(); Set(ability, "settings", kineticSettings); Set(ability, "targeting", targeting); Set(ability, "motor", motor);
            var input = player.AddComponent<LocalPlayerInput>();
            Set(input, "inputTemplate", AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Nexus/Input/NexusInput.inputactions"));
            Set(input, "motor", motor); Set(input, "orbit", orbit); Set(input, "primaryAbility", ability); Set(input, "targeting", targeting);
            var reticle = new GameObject("Centre reticle"); reticle.transform.SetParent(rig.transform);
            var targetFeedback = reticle.AddComponent<TargetFeedback>(); Set(targetFeedback, "targeting", targeting); Set(targetFeedback, "view", view);
            var beam = new GameObject("Kinetic impulse feedback"); beam.transform.SetParent(rig.transform); Line(beam, pulse, .055f);
            var feedback = beam.AddComponent<KineticFeedback>(); Set(feedback, "ability", ability); Set(feedback, "muzzle", muzzle);
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(rig, PrefabPath, InteractionMode.AutomatedAction);
            if (!prefab) throw new InvalidOperationException("Player rig prefab could not be saved.");
        }
        private static T Config<T>(string name) where T : ScriptableObject
        { var config = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(config, Root + "/Data/" + name + ".asset"); return config; }
        private static Material Material(string name, Color color, bool unlit = false)
        {
            var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.name = name; material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .2f);
            AssetDatabase.CreateAsset(material, Root + "/Materials/" + name + ".mat"); return material;
        }
        private static void Line(GameObject go, Material material, float width)
        { var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = material; line.startWidth = line.endWidth = width; line.useWorldSpace = true; line.enabled = false; }
        private static Rigidbody Crate(string name, Vector3 position, float mass, Material panel, Material trim, Transform parent, PhysicsMaterial friction)
        {
            var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position;
            var collider = go.AddComponent<BoxCollider>(); collider.size = new Vector3(1.8f, 1.6f, 1.4f); collider.sharedMaterial = friction;
            var body = go.AddComponent<Rigidbody>(); ConfigureBody(body, mass); go.AddComponent<PhysicalTarget>();
            var visual = new GameObject("Intact casing").transform; visual.SetParent(go.transform, false);
            for (int i = 0; i < 4; i++) Box("Casing slat", new Vector3(0, -.5f + i * .32f, 0), new Vector3(1.8f, .25f, 1.4f), panel, visual, false);
            foreach (float x in new[] { -.65f, .65f })
            { Box("Cargo strap", new Vector3(x, 0, -.72f), new Vector3(.09f, 1.5f, .05f), trim, visual, false); Box("Pallet runner", new Vector3(x, -.73f, 0), new Vector3(.22f, .14f, 1.4f), trim, visual, false); }
            return body;
        }
        private static Transform Humanoid(Transform parent, Material suit, Material joints, Material accent)
        {
            var visual = new GameObject("Articulated silhouette").transform; visual.SetParent(parent, false);
            Box("Torso", new Vector3(0, 1.25f, 0), new Vector3(.65f, .7f, .34f), suit, visual, false);
            Box("Belt", new Vector3(0, .88f, 0), new Vector3(.52f, .15f, .38f), joints, visual, false);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere); head.name = "Head"; head.transform.SetParent(visual, false);
            head.transform.localPosition = new Vector3(0, 1.82f, 0); head.transform.localScale = Vector3.one * .4f;
            Object.DestroyImmediate(head.GetComponent<Collider>()); head.GetComponent<Renderer>().sharedMaterial = suit;
            Box("Visor", new Vector3(0, 1.85f, .18f), new Vector3(.27f, .08f, .05f), accent, visual, false);
            foreach (float sign in new[] { -1f, 1f })
            {
                Box("Arm", new Vector3(sign * .45f, 1.18f, 0), new Vector3(.2f, .67f, .23f), suit, visual, false);
                Box("Leg", new Vector3(sign * .18f, .47f, 0), new Vector3(.23f, .7f, .27f), joints, visual, false);
                Box("Boot", new Vector3(sign * .18f, .1f, .07f), new Vector3(.27f, .2f, .4f), joints, visual, false);
            }
            return visual;
        }
        private static GameObject Box(string name, Vector3 local, Vector3 scale, Material material, Transform parent, bool collision = true, PhysicsMaterial friction = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = local; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>()); else go.GetComponent<Collider>().sharedMaterial = friction;
            return go;
        }
        private static void ConfigureBody(Rigidbody body, float mass)
        { body.mass = mass; body.linearDamping = .15f; body.angularDamping = .8f; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; }
        private static void Set(Object target, string field, Object value)
        {
            var data = new SerializedObject(target); var property = data.FindProperty(field);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + "." + field + " not found.");
            if (!value) throw new InvalidOperationException(target.GetType().Name + "." + field + " cannot reference a missing asset/object.");
            property.objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
