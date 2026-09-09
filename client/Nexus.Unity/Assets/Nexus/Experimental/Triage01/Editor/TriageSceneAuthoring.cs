using System.IO;
using System.Linq;
using Nexus.Feel01;
using Nexus.Reactivity01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.Triage01.Editor
{
    public static class TriageSceneAuthoring
    {
        public const string ScenePath = "Assets/Nexus/Experimental/Triage01.unity";
        private const string DataPath = "Assets/Nexus/Experimental/Triage01/Data";
        [MenuItem("Nexus/Triage 01/Open gameplay scene")]
        public static void Open()
        { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }

        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("Triage01 already exists.");
            var scene = EditorSceneManager.OpenScene("Assets/Nexus/Experimental/GameplayFeel01.unity");
            var player = Object.FindFirstObjectByType<FeelPlayer>(); var camera = Object.FindFirstObjectByType<FeelCamera>();
            foreach (var root in scene.GetRootGameObjects())
                if (root != player.gameObject && root != camera.gameObject && root != RenderSettings.sun.gameObject) Object.DestroyImmediate(root);
            player.transform.position = new Vector3(0, .05f, -6);
            Directory.CreateDirectory(DataPath); AssetDatabase.Refresh();
            var reaction = ScriptableObject.CreateInstance<ReactionSettings>();
            AssetDatabase.CreateAsset(reaction, DataPath + "/ReactionSettings.asset");
            var slide = new PhysicsMaterial("Runaway cargo contact") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
            AssetDatabase.CreateAsset(slide, DataPath + "/CargoContact.physicMaterial");
            var floor = Load<Material>("Floor.mat"); var concrete = Load<Material>("Concrete.mat");
            var light = Load<Material>("Light_mass.mat"); var heavy = Load<Material>("Heavy_mass.mat");
            var actorMaterial = Load<Material>("Player.mat"); var friction = Load<PhysicsMaterial>("ArenaContact.physicMaterial");
            var rootStage = new GameObject("Triage / two simultaneous crises");
            var arena = rootStage.AddComponent<FeelArena>(); var stage = rootStage.AddComponent<ReactionStage>();
            var director = rootStage.AddComponent<TriageDirector>();
            Set(arena, "player", player); Set(stage, "arena", arena); Set(stage, "settings", reaction);
            Set(stage, "pulse", player.GetComponent<KineticPulse>()); Set(director, "arena", arena);
            Set(director, "pulse", player.GetComponent<KineticPulse>());
            var data = new SerializedObject(arena); data.FindProperty("experimentTitle").stringValue = "TRIAGE / CHOICE UNDER PRESSURE"; data.ApplyModifiedPropertiesWithoutUndo();
            var parent = rootStage.transform;
            // Open courtyard. The service trench is accessible from every side, not a route gate.
            Box("Courtyard", new Vector3(-4, -.5f, 0), new Vector3(76, 1, 30), floor, parent, friction);
            Box("East apron north", new Vector3(38, -.5f, 12), new Vector3(8, 1, 6), floor, parent, friction);
            Box("East apron south", new Vector3(38, -.5f, -12), new Vector3(8, 1, 6), floor, parent, friction);
            Box("Service trench floor", new Vector3(38, -3.5f, 0), new Vector3(8, 1, 18), heavy, parent, friction);
            Box("North parapet", new Vector3(0, 1, 15), new Vector3(84, 2, .5f), concrete, parent);
            Box("South parapet", new Vector3(0, 1, -15), new Vector3(84, 2, .5f), concrete, parent);
            Box("West parapet", new Vector3(-42, 1, 0), new Vector3(.5f, 2, 30), concrete, parent);
            Box("Trench outer wall", new Vector3(42, -1, 0), new Vector3(.5f, 6, 30), concrete, parent);
            var rescueZone = Box("Rescue lane", new Vector3(-32, .01f, -1), new Vector3(3, .02f, 24), light, parent, null, false).GetComponent<Renderer>();
            var goal = new GameObject("Depot / threat destination"); goal.transform.SetParent(parent); goal.transform.position = new Vector3(32, 1, 12);
            var civil = Actor("Civil / exposed", new Vector3(-32, 1.01f, 6), ReactionRole.Civil, stage, player.transform, camera.GetComponent<Camera>(), actorMaterial, friction);
            var threat = Actor("Threat / depot advance", new Vector3(32, 1.01f, -4), ReactionRole.Enemy, stage, goal.transform, camera.GetComponent<Camera>(), actorMaterial, friction);
            var cargo = Box("Runaway cargo / 12 kg", new Vector3(-32, .8f, -10), new Vector3(1.5f, 1.5f, 1.5f), heavy, parent, slide).AddComponent<Rigidbody>();
            Configure(cargo, 12); cargo.linearDamping = 0; cargo.constraints = RigidbodyConstraints.FreezeRotation;
            Set(cargo.gameObject.AddComponent<CargoContact>(), "director", director);
            // Collision reaction comes from actual contact; no threat aura makes the rescue auto-solve.
            var barrier = Box("Movable barrier / 2 kg", new Vector3(29, .65f, -1), new Vector3(1.2f, 1.2f, 2.5f), light, parent, friction).AddComponent<Rigidbody>();
            Configure(barrier, 2); Set(barrier.gameObject.AddComponent<ImpactRelay>(), "stage", stage);
            var strips = new Renderer[3];
            for (int i = 0; i < strips.Length; i++) strips[i] = Box("Depot territory " + (i + 1), new Vector3(32, .012f, i * 3), new Vector3(4, .024f, 2), light, parent, null, false).GetComponent<Renderer>();
            Set(director, "cargo", cargo); Set(director, "civil", civil); Set(director, "threat", threat); Set(director, "rescueZone", rescueZone);
            Set(director, "rescueSign", Label("RESCUE", new Vector3(-32, 4, 10), parent, .2f));
            Set(director, "containmentSign", Label("CONTAINMENT", new Vector3(32, 4, 10), parent, .18f));
            Label("RESCUE  <                         >  DEPOT THREAT", new Vector3(0, 3, 4), parent, .18f);
            Label("SERVICE TRENCH", new Vector3(39, .2f, 9), parent, .12f);
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.name = "Kinetic impact flash";
            marker.transform.SetParent(parent); Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = Load<Material>("Pulse.mat"); marker.SetActive(false); Set(stage, "impactMarker", marker.transform);
            Array(director, "territory", strips); Array(stage, "actors", new[] { civil, threat });
            Array(arena, "props", new[] { cargo, barrier, civil.GetComponent<Rigidbody>(), threat.GetComponent<Rigidbody>() });
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets(); Debug.Log("TRIAGE01 scene created; accepted experiments unchanged.");
        }
        // Bounded first-round geometry correction, scoped strictly to the new scene.
        public static void ApplyTrenchTuning()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            Set(Object.FindFirstObjectByType<TriageDirector>(), "pulse", Object.FindFirstObjectByType<KineticPulse>());
            Layout("Courtyard", new Vector3(-4, -.5f, 0), new Vector3(76, 1, 30));
            Layout("East apron north", new Vector3(38, -.5f, 12), new Vector3(8, 1, 6));
            Layout("East apron south", new Vector3(38, -.5f, -12), new Vector3(8, 1, 6));
            Layout("Service trench floor", new Vector3(38, -3.5f, 0), new Vector3(8, 1, 18));
            for (int i = 0; i < 3; i++) Layout("Depot territory " + (i + 1), new Vector3(32, .012f, i * 3), new Vector3(4, .024f, 2));
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        private static void Layout(string name, Vector3 position, Vector3 scale)
        { var item = GameObject.Find(name); item.transform.position = position; item.transform.localScale = scale; }
        private static T Load<T>(string name) where T : Object => AssetDatabase.LoadAssetAtPath<T>("Assets/Nexus/Experimental/Feel01/Data/" + name);
        private static ReactiveActor Actor(string name, Vector3 position, ReactionRole role, ReactionStage stage, Transform goal, Camera view, Material material, PhysicsMaterial friction)
        {
            var go = new GameObject(name); go.transform.SetParent(stage.transform); go.transform.position = position;
            var collider = go.AddComponent<CapsuleCollider>(); collider.height = 2; collider.radius = .4f; collider.sharedMaterial = friction;
            var body = go.AddComponent<Rigidbody>(); Configure(body, 8); body.constraints = RigidbodyConstraints.FreezeRotation;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule); visual.transform.SetParent(go.transform, false); visual.transform.localScale = new Vector3(.8f, 1, .8f);
            Object.DestroyImmediate(visual.GetComponent<Collider>()); visual.GetComponent<Renderer>().sharedMaterial = material;
            var actor = go.AddComponent<ReactiveActor>(); Set(actor, "stage", stage); Set(actor, "player", goal); Set(actor, "view", view);
            Set(actor, "visual", visual.GetComponent<Renderer>()); Set(actor, "stateLabel", Label(role.ToString(), position + Vector3.up * 1.5f, go.transform, .06f));
            var data = new SerializedObject(actor); data.FindProperty("role").enumValueIndex = (int)role; data.ApplyModifiedPropertiesWithoutUndo();
            return actor;
        }
        private static void Configure(Rigidbody body, float mass)
        { body.mass = mass; body.linearDamping = .15f; body.angularDamping = .8f; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, PhysicsMaterial friction = null, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>()); else if (friction) go.GetComponent<Collider>().sharedMaterial = friction;
            return go;
        }
        private static TextMesh Label(string text, Vector3 position, Transform parent, float size)
        {
            var label = new GameObject(text + " sign").AddComponent<TextMesh>(); label.transform.SetParent(parent); label.transform.position = position;
            label.text = text; label.characterSize = size; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter; label.color = Color.white; return label;
        }
        private static void Set(Object target, string field, Object value)
        { var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Array<T>(Object target, string field, T[] values) where T : Object
        {
            var data = new SerializedObject(target); var array = data.FindProperty(field); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
