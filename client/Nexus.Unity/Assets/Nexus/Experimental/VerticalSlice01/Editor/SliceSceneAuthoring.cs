using System.IO;
using System.Linq;
using Nexus.Feel01;
using Nexus.Persistence01;
using Nexus.Reactivity01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.VerticalSlice01.Editor
{
    public static class SliceSceneAuthoring
    {
        public const string ScenePath = "Assets/Nexus/Experimental/VerticalSlice01.unity";
        private const string DataPath = "Assets/Nexus/Experimental/VerticalSlice01/Data";
        [MenuItem("Nexus/Vertical Slice 01/Open gameplay scene")]
        public static void Open()
        { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }
        [MenuItem("Nexus/Vertical Slice 01/Clear experiment memory")]
        public static void Clear()
        {
            if (Application.isPlaying)
            {
                var incident = Object.FindFirstObjectByType<SliceIncident>();
                if (incident) { incident.ClearMemoryAndRestart(); return; }
            }
            if (new Persistence01StateStore(SliceIncident.RuntimePath, Debug.LogWarning).Clear()) Debug.Log("VerticalSlice01 experiment memory cleared.");
        }
        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("VerticalSlice01 already exists.");
            var scene = EditorSceneManager.OpenScene("Assets/Nexus/Experimental/GameplayFeel01.unity");
            var player = Object.FindFirstObjectByType<FeelPlayer>(); var orbit = Object.FindFirstObjectByType<FeelCamera>();
            foreach (var item in scene.GetRootGameObjects())
                if (item != player.gameObject && item != orbit.gameObject && item != RenderSettings.sun.gameObject) Object.DestroyImmediate(item);
            player.transform.position = new Vector3(0, .05f, -12);
            Directory.CreateDirectory(DataPath); AssetDatabase.Refresh();
            var asphalt = Material("Asphalt", new Color(.16f, .20f, .23f));
            var stone = Material("Paving", new Color(.49f, .48f, .41f));
            var brick = Material("Market brick", new Color(.46f, .24f, .16f));
            var plaster = Material("Warm plaster", new Color(.61f, .58f, .47f));
            var teal = Material("Depot teal", new Color(.12f, .35f, .36f));
            var glass = Material("Window glass", new Color(.10f, .19f, .24f));
            var yellow = Load<Material>("Light_mass.mat"); var heavy = Load<Material>("Heavy_mass.mat");
            var friction = Load<PhysicsMaterial>("ArenaContact.physicMaterial");
            var slide = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Nexus/Experimental/Triage01/Data/CargoContact.physicMaterial");
            var settings = ScriptableObject.CreateInstance<ReactionSettings>();
            AssetDatabase.CreateAsset(settings, DataPath + "/ReactionSettings.asset");
            var root = new GameObject("Pasaje del Mercado / local incident"); var parent = root.transform;
            var arena = root.AddComponent<FeelArena>(); var stage = root.AddComponent<ReactionStage>(); var incident = root.AddComponent<SliceIncident>();
            var pulse = player.GetComponent<KineticPulse>();
            Set(arena, "player", player); Bool(arena, "pauseOnlyHud", true); Bool(pulse, "contextualHud", true);
            Set(stage, "arena", arena); Set(stage, "settings", settings); Set(stage, "pulse", pulse);
            Set(incident, "arena", arena); Set(incident, "player", player); Set(incident, "pulse", pulse); Set(incident, "reactions", stage);
            Box("Market street", new Vector3(-4, -.5f, 0), new Vector3(36, 1, 36), asphalt, parent, friction);
            Box("North service apron", new Vector3(18, -.5f, 13.5f), new Vector3(8, 1, 9), asphalt, parent, friction);
            Box("South service apron", new Vector3(18, -.5f, -14), new Vector3(8, 1, 8), asphalt, parent, friction);
            Box("Service channel floor", new Vector3(18, -3.5f, -.5f), new Vector3(8, 1, 19), heavy, parent, friction);
            Box("Channel retaining wall", new Vector3(22, -1, 0), new Vector3(.4f, 6, 36), stone, parent);
            // Open central crossing. Buildings frame routes rather than hiding the two ends of the incident.
            Box("Market pavement", new Vector3(-16, .025f, 5), new Vector3(10, .05f, 23), stone, parent, null, false);
            for (int i = 0; i < 3; i++)
            {
                Building("Market " + i, new Vector3(-23, 3 + i * .3f, -9 + i * 10), new Vector3(5, 6 + i * .6f, 8), brick, glass, parent);
                Building("North frontage " + i, new Vector3(-14 + i * 12, 3.5f, 20), new Vector3(10, 7, 5), i == 2 ? teal : plaster, glass, parent);
            }
            Box("South boundary", new Vector3(-4, 1, -19), new Vector3(36, 2, .5f), brick, parent);
            Box("Depot loading lintel", new Vector3(12, 3.7f, 15.6f), new Vector3(8, .5f, .6f), teal, parent);
            Box("Depot roller door", new Vector3(12, 1.5f, 17.3f), new Vector3(6, 3, .12f), glass, parent);
            for (int i = 0; i < 6; i++) Box("Shutter slat", new Vector3(12, .4f + i * .45f, 17.2f), new Vector3(6, .07f, .06f), stone, parent, null, false);
            Sign("DEPOSITO 07", new Vector3(12, 4.3f, 15.5f), parent, .12f);
            Sign("MERCADO", new Vector3(-17, 3.6f, 15), parent, .13f);
            Box("Market canopy", new Vector3(-16, 3, 12.8f), new Vector3(8, .2f, 3), teal, parent);
            foreach (float x in new[] { -19.5f, -12.5f }) Box("Canopy post", new Vector3(x, 1.5f, 13), new Vector3(.16f, 3, .16f), stone, parent);
            for (int i = 0; i < 7; i++) Box("Crossing paint", new Vector3(-7 + i * 2, .035f, -2), new Vector3(1, .018f, 2.5f), stone, parent, null, false);
            for (int i = 0; i < 6; i++) Box("Service edge marking", new Vector3(13.75f, .035f, -8 + i * 3), new Vector3(.2f, .018f, 1.6f), yellow, parent, null, false);
            foreach (var position in new[] { new Vector3(-18, .4f, -5), new Vector3(-7, .4f, 13), new Vector3(4, .4f, -15) })
            {
                Box("Planter", position, new Vector3(2, .8f, 1), stone, parent);
                Box("Low planting", position + Vector3.up * .55f, new Vector3(1.8f, .5f, .8f), teal, parent, null, false);
            }
            foreach (var position in new[] { new Vector3(-18, .55f, 2), new Vector3(-7, .55f, 10) })
            {
                Box("Bench seat", position, new Vector3(2.5f, .18f, .6f), brick, parent);
                Box("Bench support", position - Vector3.up * .3f, new Vector3(1.8f, .6f, .4f), stone, parent);
            }
            Box("Low traversal plinth", new Vector3(-5, .45f, -8), new Vector3(3, .9f, 2), stone, parent);
            var goal = new GameObject("Depot destination").transform; goal.SetParent(parent); goal.position = new Vector3(12, 1, 14);
            var civil = Actor("Market visitor", new Vector3(-12, 1.01f, 6), ReactionRole.Civil, stage, player.transform, orbit.GetComponent<Camera>(), friction);
            var threat = Actor("Kinetic intruder", new Vector3(12, 1.01f, -7), ReactionRole.Enemy, stage, goal, orbit.GetComponent<Camera>(), friction);
            var actors = new[] { civil, threat,
                Actor("Depot worker", new Vector3(9, 1.01f, -9), ReactionRole.Civil, stage, player.transform, orbit.GetComponent<Camera>(), friction),
                Actor("Market neighbour", new Vector3(-7, 1.01f, 9), ReactionRole.Civil, stage, player.transform, orbit.GetComponent<Camera>(), friction),
                Actor("Passerby", new Vector3(-17, 1.01f, -1), ReactionRole.Civil, stage, player.transform, orbit.GetComponent<Camera>(), friction) };
            var cargo = Body("Dislodged freight", new Vector3(10, .8f, -6), new Vector3(1.5f, 1.5f, 1.5f), 12, heavy, parent, slide);
            cargo.linearDamping = 0; cargo.constraints = RigidbodyConstraints.FreezeRotation;
            Set(cargo.gameObject.AddComponent<SliceCargoContact>(), "incident", incident);
            // The hazard's civil contact is handled above; relaying every cargo impact would preemptively scare its target out of danger.
            var barrier = Body("Portable barrier", new Vector3(9, .65f, -1), new Vector3(1.2f, 1.2f, 2.5f), 2, yellow, parent, friction);
            Set(barrier.gameObject.AddComponent<ImpactRelay>(), "stage", stage);
            var crate = Body("Empty delivery crate", new Vector3(-5, .55f, 6), Vector3.one, 2, yellow, parent, friction);
            Set(crate.gameObject.AddComponent<ImpactRelay>(), "stage", stage);
            var territory = new Renderer[3]; var damage = new GameObject[3];
            var traces = new GameObject("Aftermath / same place changed").transform; traces.SetParent(parent);
            for (int i = 0; i < 3; i++)
            {
                territory[i] = Box("Loading bay " + (i + 1), new Vector3(12, .018f, i * 3), new Vector3(4, .025f, 2), teal, parent, null, false).GetComponent<Renderer>();
                // Broken side fixtures remain outside the approach corridor so evidence does not secretly contain the threat.
                Box("Intact loading fixture " + i, new Vector3(9.5f, .55f, i * 3), new Vector3(.4f, 1.1f, .4f), stone, parent);
                damage[i] = new GameObject("Damaged loading bay " + (i + 1)); damage[i].transform.SetParent(traces);
                Grounded("Broken fixture", new Vector3(10, 0, i * 3), new Vector3(1.4f, .3f, .5f), Quaternion.Euler(0, 35, 8), heavy, damage[i].transform);
                Grounded("Spilled freight", new Vector3(8.6f, 0, i * 3 + .6f), new Vector3(1.3f, 1, .9f), Quaternion.Euler(10, 25, 20), heavy, damage[i].transform);
                damage[i].SetActive(false);
            }
            var rescue = new GameObject("Accident cordon and fragments"); rescue.transform.SetParent(traces);
            Box("Accident cordon", new Vector3(-12, .85f, 4), new Vector3(5, .2f, .18f), yellow, rescue.transform);
            foreach (float x in new[] { -14.3f, -9.7f }) Box("Cordon stand", new Vector3(x, .45f, 4), new Vector3(.2f, .9f, .5f), heavy, rescue.transform);
            for (int i = 0; i < 4; i++) Grounded("Freight fragment", new Vector3(-13 + i, 0, 7 + i % 2), new Vector3(.7f, .3f, .5f), Quaternion.Euler(0, 23 * i, 10 * i), heavy, rescue.transform);
            rescue.SetActive(false);
            var contained = new GameObject("Service channel secured"); contained.transform.SetParent(traces);
            Box("Channel safety rail", new Vector3(14, .8f, -3), new Vector3(.2f, .18f, 7), yellow, contained.transform);
            foreach (float z in new[] { -6f, 0f }) Box("Safety post", new Vector3(14, .5f, z), new Vector3(.3f, 1, .3f), heavy, contained.transform);
            contained.SetActive(false);
            var beaconGo = new GameObject("Loading warning beacon"); beaconGo.transform.SetParent(parent); beaconGo.transform.position = new Vector3(11, 3, -6);
            var beacon = beaconGo.AddComponent<Light>(); beacon.type = LightType.Point; beacon.range = 9;
            Box("Beacon pole", new Vector3(11, 1.5f, -6), new Vector3(.12f, 3, .12f), stone, parent);
            var beam = root.AddComponent<LineRenderer>(); beam.positionCount = 2; beam.startWidth = .16f; beam.endWidth = .28f; beam.sharedMaterial = Load<Material>("Pulse.mat"); beam.enabled = false;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.name = "Kinetic impact flash"; marker.transform.SetParent(parent);
            Object.DestroyImmediate(marker.GetComponent<Collider>()); marker.GetComponent<Renderer>().sharedMaterial = Load<Material>("Pulse.mat"); marker.SetActive(false);
            Set(stage, "impactMarker", marker.transform); Array(stage, "actors", actors);
            Set(incident, "civil", civil); Set(incident, "threat", threat); Set(incident, "cargo", cargo); Array(incident, "actors", actors);
            Array(incident, "territory", territory); Array(incident, "depotDamage", damage); Set(incident, "rescueDamage", rescue); Set(incident, "containmentTrace", contained);
            Set(incident, "beacon", beacon); Set(incident, "discharge", beam);
            Array(arena, "props", Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets(); Debug.Log("VerticalSlice01 authored.");
        }
        public static void ApplyLayout()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            GameObject.Find("Empty delivery crate").transform.position = new Vector3(-5, .55f, 6);
            GameObject.Find("Portable barrier").transform.position = new Vector3(9, .65f, -1);
            var pavement = GameObject.Find("Market pavement").GetComponent<Collider>();
            if (pavement) Object.DestroyImmediate(pavement);
            EditorSceneManager.SaveScene(scene);
        }
        private static Material Material(string name, Color color)
        { var result = new Material(Load<Material>("Concrete.mat")); result.name = name; result.SetColor("_BaseColor", color); AssetDatabase.CreateAsset(result, DataPath + "/" + name + ".mat"); return result; }
        private static T Load<T>(string name) where T : Object => AssetDatabase.LoadAssetAtPath<T>("Assets/Nexus/Experimental/Feel01/Data/" + name);
        private static void Building(string name, Vector3 position, Vector3 scale, Material material, Material glass, Transform parent)
        {
            Box(name, position, scale, material, parent);
            for (int i = -1; i <= 1; i++)
                Box("Front window", position + new Vector3(i * scale.x * .25f, .4f, -scale.z * .5f - .03f), new Vector3(1.2f, 1.5f, .08f), glass, parent, null, false);
            Box("Roof cornice", position + Vector3.up * scale.y * .5f, new Vector3(scale.x + .2f, .2f, scale.z + .2f), material, parent);
        }
        private static ReactiveActor Actor(string name, Vector3 position, ReactionRole role, ReactionStage stage, Transform goal, Camera view, PhysicsMaterial friction)
        {
            var go = new GameObject(name); go.transform.SetParent(stage.transform); go.transform.position = position;
            var collider = go.AddComponent<CapsuleCollider>(); collider.height = 2; collider.radius = .4f; collider.sharedMaterial = friction;
            var body = go.AddComponent<Rigidbody>(); Configure(body, 8); body.constraints = RigidbodyConstraints.FreezeRotation;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule); visual.transform.SetParent(go.transform, false); visual.transform.localScale = new Vector3(.8f, 1, .8f);
            Object.DestroyImmediate(visual.GetComponent<Collider>()); visual.GetComponent<Renderer>().sharedMaterial = Load<Material>("Player.mat");
            var actor = go.AddComponent<ReactiveActor>(); Set(actor, "stage", stage); Set(actor, "player", goal); Set(actor, "view", view); Set(actor, "visual", visual.GetComponent<Renderer>());
            var label = Sign(name, position + Vector3.up * 1.5f, go.transform, .06f); label.GetComponent<Renderer>().enabled = false; Set(actor, "stateLabel", label);
            var data = new SerializedObject(actor); data.FindProperty("role").enumValueIndex = (int)role; data.ApplyModifiedPropertiesWithoutUndo(); return actor;
        }
        private static Rigidbody Body(string name, Vector3 position, Vector3 scale, float mass, Material material, Transform parent, PhysicsMaterial friction)
        { var body = Box(name, position, scale, material, parent, friction).AddComponent<Rigidbody>(); Configure(body, mass); return body; }
        private static void Configure(Rigidbody body, float mass)
        { body.mass = mass; body.linearDamping = .15f; body.angularDamping = .8f; body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, PhysicsMaterial friction = null, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent); go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>()); else if (friction) go.GetComponent<Collider>().sharedMaterial = friction; return go;
        }
        private static void Grounded(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material, Transform parent)
        { var go = Box(name, position, scale, material, parent); go.transform.rotation = rotation; go.transform.position += Vector3.up * (.02f - go.GetComponent<Renderer>().bounds.min.y); }
        private static TextMesh Sign(string text, Vector3 position, Transform parent, float size)
        { var label = new GameObject(text + " sign").AddComponent<TextMesh>(); label.transform.SetParent(parent); label.transform.position = position; label.text = text; label.characterSize = size; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter; return label; }
        private static void Set(Object target, string field, Object value)
        { var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Bool(Object target, string field, bool value)
        { var data = new SerializedObject(target); data.FindProperty(field).boolValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Array<T>(Object target, string field, T[] values) where T : Object
        { var data = new SerializedObject(target); var array = data.FindProperty(field); array.arraySize = values.Length; for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; data.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
