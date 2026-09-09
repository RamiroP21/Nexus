using System.IO;
using System.Linq;
using Nexus.Feel01;
using Nexus.Reactivity01;
using Nexus.Triage01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.Persistence01.Editor
{
    public static class PersistenceSceneAuthoring
    {
        public const string ScenePath = "Assets/Nexus/Experimental/Persistence01.unity";
        [MenuItem("Nexus/Persistence 01/Open gameplay scene")]
        public static void Open()
        { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }

        [MenuItem("Nexus/Persistence 01/Clear experiment memory")]
        public static void Clear()
        {
            if (Application.isPlaying)
            {
                var experiment = Object.FindFirstObjectByType<Persistence01Experiment>();
                if (experiment) { experiment.ClearMemoryAndRestart(); return; }
            }
            var store = new Persistence01StateStore(Persistence01StateStore.RuntimePath, Debug.LogWarning);
            if (store.Clear()) Debug.Log("PERSISTENCE01 memory cleared: " + store.FilePath);
        }

        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("Persistence01 already exists.");
            var scene = EditorSceneManager.OpenScene("Assets/Nexus/Experimental/Triage01.unity");
            var triage = Object.FindFirstObjectByType<TriageDirector>();
            var experiment = triage.gameObject.AddComponent<Persistence01Experiment>();
            Set(experiment, "triage", triage); Set(experiment, "arena", Object.FindFirstObjectByType<FeelArena>());
            Set(experiment, "reactions", Object.FindFirstObjectByType<ReactionStage>()); Set(experiment, "player", Object.FindFirstObjectByType<FeelPlayer>());
            Array(experiment, "bodies", Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None));
            Set(experiment, "rescueLane", GameObject.Find("Rescue lane").GetComponent<Renderer>());
            Set(experiment, "rescueSign", GameObject.Find("RESCUE sign").GetComponent<TextMesh>());
            Set(experiment, "depotSign", GameObject.Find("CONTAINMENT sign").GetComponent<TextMesh>());
            Set(experiment, "directionSign", GameObject.Find("RESCUE  <                         >  DEPOT THREAT sign").GetComponent<TextMesh>());
            Array(experiment, "territory", Enumerable.Range(1, 3).Select(i => GameObject.Find("Depot territory " + i).GetComponent<Renderer>()).ToArray());
            var arenaData = new SerializedObject(Object.FindFirstObjectByType<FeelArena>());
            arenaData.FindProperty("experimentTitle").stringValue = "PERSISTENCE / CONSEQUENCE REMAINS"; arenaData.ApplyModifiedPropertiesWithoutUndo();
            var root = new GameObject("Aftermath / factual reconstruction").transform;
            var concrete = Load("Concrete.mat"); var heavy = Load("Heavy_mass.mat"); var light = Load("Light_mass.mat");
            var rescue = new GameObject("Rescue / accident cordon and debris"); rescue.transform.SetParent(root);
            // Grounded, walkable evidence. Text is not required to distinguish these layouts.
            Box("Accident scar", new Vector3(-32, .02f, 6), new Vector3(5, .04f, 5), Quaternion.identity, concrete, rescue.transform, false);
            Box("Cordon west support", new Vector3(-35, .5f, 3), new Vector3(.25f, 1, .4f), Quaternion.identity, heavy, rescue.transform);
            Box("Cordon east support", new Vector3(-29, .5f, 3), new Vector3(.25f, 1, .4f), Quaternion.identity, heavy, rescue.transform);
            Box("Accident cordon", new Vector3(-32, .8f, 3), new Vector3(6.5f, .22f, .18f), Quaternion.identity, light, rescue.transform);
            for (int i = 0; i < 4; i++) GroundedBox("Cargo debris " + i, new Vector3(-34 + i * 1.25f, 0, 7 + (i % 2)), new Vector3(.7f, .3f, .55f), Quaternion.Euler(0, 23 * i, 10 * i), heavy, rescue.transform);
            rescue.SetActive(false); Set(experiment, "rescueDamage", rescue);
            var depot = new GameObject[3];
            for (int i = 0; i < depot.Length; i++)
            {
                depot[i] = new GameObject("Depot / damaged ground " + (i + 1)); depot[i].transform.SetParent(root);
                GroundedBox("Dislodged cargo " + i, new Vector3(30, 0, i * 3), new Vector3(1.6f, 1.4f, 1.2f), Quaternion.Euler(10, 25 + i * 20, 20), heavy, depot[i].transform);
                GroundedBox("Broken barrier " + i, new Vector3(32.4f, 0, i * 3 + .5f), new Vector3(2, .3f, .5f), Quaternion.Euler(0, -25, 8), light, depot[i].transform);
                depot[i].SetActive(false);
            }
            Array(experiment, "depotDamage", depot);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("PERSISTENCE01 created. Runtime record: " + Persistence01StateStore.DefaultPath);
        }
        private static Material Load(string file) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Experimental/Feel01/Data/" + file);
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material, Transform parent, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(position, rotation); go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) Object.DestroyImmediate(go.GetComponent<Collider>()); return go;
        }
        private static void GroundedBox(string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material, Transform parent)
        {
            var go = Box(name, position, scale, rotation, material, parent);
            go.transform.position += Vector3.up * (.02f - go.GetComponent<Renderer>().bounds.min.y);
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
