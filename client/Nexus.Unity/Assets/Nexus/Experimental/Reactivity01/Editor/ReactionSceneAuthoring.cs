using System.IO;
using System.Linq;
using Nexus.Feel01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.Reactivity01.Editor
{
    public static class ReactionSceneAuthoring
    {
        public const string ScenePath = "Assets/Nexus/Experimental/Reactivity01.unity";
        private const string DataPath = "Assets/Nexus/Experimental/Reactivity01/Data";
        private const string FeelData = "Assets/Nexus/Experimental/Feel01/Data/";

        [MenuItem("Nexus/Reactivity 01/Open gameplay scene")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        // One-off reproducible authoring. Never overwrites either experiment.
        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("Reactivity01 already exists.");
            var scene = EditorSceneManager.OpenScene("Assets/Nexus/Experimental/GameplayFeel01.unity");
            var player = Object.FindFirstObjectByType<FeelPlayer>();
            var camera = Object.FindFirstObjectByType<FeelCamera>();
            var sun = RenderSettings.sun;
            foreach (var root in scene.GetRootGameObjects())
                if (root != player.gameObject && root != camera.gameObject && root != sun.gameObject) Object.DestroyImmediate(root);
            player.transform.position = new Vector3(0, .05f, -7);
            Directory.CreateDirectory(DataPath);
            AssetDatabase.Refresh();
            var settings = ScriptableObject.CreateInstance<ReactionSettings>();
            AssetDatabase.CreateAsset(settings, DataPath + "/ReactionSettings.asset");
            var floor = Load<Material>("Floor.mat");
            var concrete = Load<Material>("Concrete.mat");
            var edges = Load<Material>("Edges.mat");
            var light = Load<Material>("Light_mass.mat");
            var heavy = Load<Material>("Heavy_mass.mat");
            var actorMaterial = Load<Material>("Player.mat");
            var friction = Load<PhysicsMaterial>("ArenaContact.physicMaterial");
            var rootStage = new GameObject("Micro courtyard / Reactivity 01");
            var arena = rootStage.AddComponent<FeelArena>();
            var stage = rootStage.AddComponent<ReactionStage>();
            Set(stage, "settings", settings);
            Set(stage, "pulse", player.GetComponent<KineticPulse>());
            Set(stage, "arena", arena);
            Set(arena, "player", player);
            var arenaData = new SerializedObject(arena);
            arenaData.FindProperty("experimentTitle").stringValue = "KINETIC REACTION / V1";
            arenaData.ApplyModifiedPropertiesWithoutUndo();
            Box("Courtyard 24 x 28m", new Vector3(0, -.5f, 0), new Vector3(24, 1, 28), floor, rootStage.transform, friction);
            Box("Rear facade", new Vector3(0, 2, 14), new Vector3(24, 4, .5f), concrete, rootStage.transform);
            Box("Left boundary", new Vector3(-12, 1, 0), new Vector3(.5f, 2, 28), concrete, rootStage.transform);
            Box("Right boundary", new Vector3(12, 1, 0), new Vector3(.5f, 2, 28), concrete, rootStage.transform);
            Box("Entry boundary", new Vector3(0, 1, -14), new Vector3(24, 2, .5f), concrete, rootStage.transform);
            Box("Impact barrier", new Vector3(-7, .75f, 10), new Vector3(4, 1.5f, .5f), concrete, rootStage.transform);
            // Decorative pavement remains flush: no invisible curb that catches fleeing actors.
            Box("Pavement west", new Vector3(-10, .005f, 0), new Vector3(3, .01f, 26), edges, rootStage.transform, null, false);
            Box("Pavement east", new Vector3(10, .005f, 0), new Vector3(3, .01f, 26), edges, rootStage.transform, null, false);
            Label("LIGHT / HEAVY", new Vector3(0, .025f, -4), rootStage.transform, 90, .12f);
            Label("PHYSICAL CHAIN", new Vector3(-7, .025f, -3), rootStage.transform, 90, .09f);
            Label("CIVIL: STARTLE > FLEE       THREAT: DISPLACE > RECOVER", new Vector3(0, 3.2f, 13.65f), rootStage.transform, 0, .095f);
            var bodies = new[] {
                Prop("LIGHT / 2 kg", new Vector3(-3, .61f, -2), 2, light, rootStage.transform, friction, stage),
                Prop("HEAVY / 12 kg", new Vector3(3, .61f, -2), 12, heavy, rootStage.transform, friction, stage),
                Prop("CHAIN LIGHT / 2 kg", new Vector3(-7, .61f, 0), 2, light, rootStage.transform, friction, stage),
                Prop("CHAIN HEAVY / 12 kg", new Vector3(-7, .61f, 6), 12, heavy, rootStage.transform, friction, stage)
            };
            var actors = new[] {
                Actor("Civil west", new Vector3(-4.5f, 1.01f, 9), ReactionRole.Civil, stage, player.transform, camera.GetComponent<Camera>(), actorMaterial, friction),
                Actor("Civil east", new Vector3(5, 1.01f, 5), ReactionRole.Civil, stage, player.transform, camera.GetComponent<Camera>(), actorMaterial, friction),
                Actor("Threat", new Vector3(0, 1.01f, 10), ReactionRole.Enemy, stage, player.transform, camera.GetComponent<Camera>(), actorMaterial, friction)
            };
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Impact flash"; Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.GetComponent<Renderer>().sharedMaterial = Load<Material>("Pulse.mat");
            marker.transform.SetParent(rootStage.transform); marker.SetActive(false);
            Set(stage, "impactMarker", marker.transform);
            Array(stage, "actors", actors);
            Array(arena, "props", bodies.Concat(actors.Select(a => a.GetComponent<Rigidbody>())).ToArray());
            EditorSceneManager.SaveScene(scene, ScenePath);
            // Preserve Feel01 as the existing startup scene and add the new experiment explicitly.
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("REACTIVITY01: courtyard created; Feel01 scene, movement, camera and tuning preserved.");
        }

        private static T Load<T>(string file) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(FeelData + file);
            if (!asset) throw new System.InvalidOperationException("Missing reused Feel01 asset: " + file);
            return asset;
        }
        private static ReactiveActor Actor(string name, Vector3 position, ReactionRole role, ReactionStage stage,
            Transform player, Camera view, Material material, PhysicsMaterial friction)
        {
            var go = new GameObject(name); go.transform.position = position; go.transform.SetParent(stage.transform);
            var collider = go.AddComponent<CapsuleCollider>(); collider.height = 2; collider.radius = .4f; collider.sharedMaterial = friction;
            var body = go.AddComponent<Rigidbody>(); Configure(body, 8);
            body.constraints = RigidbodyConstraints.FreezeRotation;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule); visual.name = "Reaction pose";
            visual.transform.SetParent(go.transform, false); visual.transform.localScale = new Vector3(.8f, 1, .8f);
            Object.DestroyImmediate(visual.GetComponent<Collider>()); visual.GetComponent<Renderer>().sharedMaterial = material;
            var label = Label(role.ToString(), position + Vector3.up * 1.5f, go.transform, 0, .06f);
            var actor = go.AddComponent<ReactiveActor>();
            Set(actor, "stage", stage); Set(actor, "player", player); Set(actor, "view", view);
            Set(actor, "visual", visual.GetComponent<Renderer>()); Set(actor, "stateLabel", label);
            var data = new SerializedObject(actor); data.FindProperty("role").enumValueIndex = (int)role; data.ApplyModifiedPropertiesWithoutUndo();
            Set(go.AddComponent<ImpactRelay>(), "stage", stage);
            return actor;
        }
        private static Rigidbody Prop(string name, Vector3 position, float mass, Material material, Transform parent, PhysicsMaterial friction, ReactionStage stage)
        {
            var go = Box(name, position, Vector3.one * 1.2f, material, parent, friction);
            var body = go.AddComponent<Rigidbody>(); Configure(body, mass);
            Set(go.AddComponent<ImpactRelay>(), "stage", stage);
            Label(name, position + Vector3.up * 1, go.transform, 0, .055f);
            return body;
        }
        private static void Configure(Rigidbody body, float mass)
        {
            body.mass = mass; body.linearDamping = .15f; body.angularDamping = .8f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.maxAngularVelocity = 8;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, PhysicsMaterial friction = null, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name;
            go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>());
            else if (friction) go.GetComponent<Collider>().sharedMaterial = friction;
            return go;
        }
        private static TextMesh Label(string text, Vector3 position, Transform parent, float pitch, float size)
        {
            var label = new GameObject(text + " label").AddComponent<TextMesh>();
            label.transform.SetParent(parent); label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(pitch, 0, 0); label.text = text;
            label.characterSize = size; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter; label.color = Color.white;
            return label;
        }
        private static void Set(Object target, string field, Object value)
        {
            var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Array<T>(Object target, string field, T[] values) where T : Object
        {
            var data = new SerializedObject(target); var array = data.FindProperty(field); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
