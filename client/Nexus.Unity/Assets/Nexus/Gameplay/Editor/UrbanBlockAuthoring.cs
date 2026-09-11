using System;
using System.IO;
using System.Linq;
using Nexus.Gameplay.Character;
using Nexus.Gameplay.Combat;
using Nexus.Gameplay.Interaction;
using Nexus.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Nexus.Gameplay.Editor
{
    public static partial class SuperhumanPlaygroundAuthoring
    {
        public const string UrbanScenePath = Root + "/Scenes/UrbanBlock01.unity";
        [MenuItem("Nexus/Production/Apply Urban Block 01 Legibility")]
        public static void ApplyUrbanLegibility()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author outside Play Mode.");
            var scene = EditorSceneManager.OpenScene(UrbanScenePath);
            if (GameObject.Find("Urban street detail")) return;
            var detail = new GameObject("Urban street detail").transform;
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Pallet wood.mat");
            var steel = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Steel.mat");
            var accent = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Safety trim.mat");
            var shell = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Target shell.mat");
            // Decorative geometry has no colliders: established traversal and sightlines are unchanged.
            for (int i = 0; i < 3; i++)
            {
                Box("Produce stall", new Vector3(-5.65f, .55f, 3 + i * 2), new Vector3(1, 1.1f, 1.3f), wood, detail, false);
                for (int j = 0; j < 3; j++) Box("Produce crate", new Vector3(-5.6f, 1.18f, 2.6f + i * 2 + j * .4f), new Vector3(.65f, .22f, .32f), accent, detail, false);
            }
            foreach (float z in new[] { -8f, -5f, 9f, 12f })
            {
                Box("Residential window", new Vector3(6.97f, 2.1f, z), new Vector3(.04f, 1.4f, 1.2f), steel, detail, false);
                Box("Window sill", new Vector3(6.8f, 1.38f, z), new Vector3(.4f, .12f, 1.4f), shell, detail, false);
            }
            Box("Courtyard bench seat", new Vector3(11.8f, .5f, 3), new Vector3(.6f, .15f, 3.8f), wood, detail, false);
            Box("Courtyard bench back", new Vector3(12.15f, .9f, 3), new Vector3(.12f, .8f, 3.8f), wood, detail, false);
            for (int i = 0; i < 3; i++) Box("Stacked service pallet", new Vector3(-8.7f, .15f + i * .3f, -7.5f), new Vector3(1.2f, .2f, 1.5f), wood, detail, false);
            int residentIndex = 0;
            foreach (var civil in Object.FindObjectsByType<CivilianPresence>(FindObjectsSortMode.None).OrderBy(c => c.name))
            {
                var visual = civil.transform.Find("Articulated silhouette");
                var visor = visual.Find("Visor"); if (visor) Object.DestroyImmediate(visor.gameObject);
                Box("Civilian hair", new Vector3(0, 1.99f, -.01f), new Vector3(.4f, .16f, .37f), wood, visual, false);
                if (residentIndex++ == 0)
                {
                    Box("Market apron", new Vector3(0, 1.02f, .2f), new Vector3(.56f, .85f, .06f), shell, visual, false);
                    Box("Shopping bag", new Vector3(-.53f, .64f, 0), new Vector3(.3f, .4f, .28f), wood, visual, false);
                }
                else Box("Shoulder satchel", new Vector3(.38f, 1.08f, -.2f), new Vector3(.3f, .5f, .2f), wood, visual, false);
            }
            var hostile = Object.FindFirstObjectByType<HostileCombatant>();
            var hostileSettings = new SerializedObject(hostile); hostileSettings.FindProperty("urbanPresentation").boolValue = true; hostileSettings.ApplyModifiedPropertiesWithoutUndo();
            var hostileVisual = hostile.transform.Find("Articulated silhouette");
            foreach (float side in new[] { -1f, 1f }) Box("Hostile shoulder armour", new Vector3(side * .47f, 1.53f, 0), new Vector3(.38f, .3f, .45f), steel, hostileVisual, false);
            var hazard = Object.FindObjectsByType<TrainingHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(h => !h.gameObject.activeSelf);
            var trail = hazard.gameObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = accent; trail.time = .23f; trail.startWidth = .24f; trail.endWidth = .01f;
            trail.minVertexDistance = .05f; trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var label in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
                if (label.GetComponentInParent<CivilianPresence>() || label.GetComponentInParent<HostileCombatant>() || label.text.Contains("RESIDENCIAS")) label.GetComponent<Renderer>().enabled = false;
            EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("Nexus/Production/Create Urban Block 01")]
        public static void CreateUrbanBlock()
        {
            if (EditorApplication.isPlaying || File.Exists(UrbanScenePath)) throw new InvalidOperationException("Author once outside Play Mode; preserve an existing urban scene.");
            var source = EditorSceneManager.OpenScene(ScenePath);
            var acceptedHostile = Object.FindFirstObjectByType<HostileCombatant>();
            var acceptedHazard = Object.FindObjectsByType<TrainingHazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
            if (!acceptedHostile) throw new InvalidOperationException("Accepted 01C authoring is required.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            RenderSettings.ambientLight = new Color(.55f, .6f, .67f);
            var sun = new GameObject("Afternoon daylight").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(48, -30, 0); RenderSettings.sun = sun;
            var concrete = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Concrete.mat");
            var plaster = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Warm panels.mat");
            var steel = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Steel.mat");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Pallet wood.mat");
            var accent = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Safety trim.mat");
            var shell = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Target shell.mat");
            var friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root + "/Data/PlaygroundContact.physicMaterial");
            var street = new GameObject("Main street - Calle Mercado").transform;
            Box("Street surface", new Vector3(0, -.5f, 0), new Vector3(30, 1, 40), concrete, street, true, friction);
            var market = new GameObject("Crossing and corner market").transform;
            Box("Market frontage", new Vector3(-10, 3, 6), new Vector3(7, 6, 12), plaster, market);
            Box("Market awning", new Vector3(-5.8f, 3, 6), new Vector3(2, .25f, 8), accent, market);
            Box("Shop window", new Vector3(-6.45f, 1.5f, 4), new Vector3(.1f, 2, 3), steel, market, false);
            Sign("MERCADO 24", new Vector3(-6.3f, 2.6f, 4), Quaternion.Euler(0, -90, 0), market, .14f);
            for (int i = 0; i < 6; i++) Box("Crosswalk stripe", new Vector3(-4 + i * 1.5f, .015f, -3), new Vector3(.7f, .025f, 3), shell, market, false);
            for (int i = 0; i < 5; i++) Box("Road marking", new Vector3(0, .015f, -16 + i * 7), new Vector3(.12f, .025f, 2), accent, street, false);
            var homes = new GameObject("Residential access - Patio Sur").transform;
            Box("Residential frontage south", new Vector3(10, 3, -6), new Vector3(6, 6, 9), plaster, homes);
            Box("Residential frontage north", new Vector3(10, 3, 11), new Vector3(6, 6, 9), plaster, homes);
            Box("Courtyard back wall", new Vector3(13, 2, 3), new Vector3(.5f, 4, 8), steel, homes);
            Box("Entrance upper beam", new Vector3(7.3f, 3.5f, 2.5f), new Vector3(.8f, .5f, 7), plaster, homes);
            Sign("PATIO SUR / HOMES", new Vector3(7, 3, 2.5f), Quaternion.Euler(0, 90, 0), homes, .11f);
            var alley = new GameObject("Service alley and roof shortcut").transform;
            Box("Service building", new Vector3(-11, 2, -13), new Vector3(5, 4, 9), steel, alley);
            Box("Service vault rail", new Vector3(-6.6f, .45f, -11), new Vector3(3, .9f, .35f), accent, alley);
            Box("Loading dock mantle", new Vector3(-6.5f, .85f, -5.5f), new Vector3(3, 1.7f, 3), concrete, alley);
            Box("Raised service walkway", new Vector3(-7.5f, .85f, -1.5f), new Vector3(2, 1.7f, 5), concrete, alley);
            Sign("SERVICE / MARKET", new Vector3(-6.5f, 2.8f, -5), Quaternion.identity, alley, .1f);
            foreach (float x in new[] { -15f, 15f }) Box("Block boundary", new Vector3(x, 2, 0), new Vector3(.4f, 4, 40), steel, street);
            foreach (float z in new[] { -20f, 20f }) Box("Block end", new Vector3(0, 2, z), new Vector3(30, 4, .4f), plaster, street);

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            var player = rig.GetComponentInChildren<PlayerVitality>(); player.transform.position = new Vector3(0, .05f, -14);
            var props = new GameObject("Street physical props").transform;
            var cart = Crate("Delivery cart - clear residential exit", new Vector3(6.2f, .85f, 2), 3, wood, accent, props, friction);
            var coverObject = Box("Steel waste container - movable cover", new Vector3(-3, 1, 7), new Vector3(2.2f, 2, 1.4f), steel, props);
            var cover = coverObject.AddComponent<Rigidbody>(); ConfigureBody(cover, 7); cover.constraints = RigidbodyConstraints.FreezeAll;
            coverObject.AddComponent<PhysicalTarget>(); coverObject.AddComponent<ImpactBarrier>();
            var hazard = Object.Instantiate(acceptedHazard); hazard.name = "Physical hazard template"; hazard.gameObject.SetActive(false);
            var hostile = Object.Instantiate(acceptedHostile); hostile.name = "Street hostile"; hostile.transform.SetParent(null);
            hostile.transform.SetPositionAndRotation(new Vector3(0, .05f, 11), Quaternion.Euler(0, 180, 0));
            Set(hostile, "target", player); Set(hostile, "hazardTemplate", hazard);
            var civilians = new CivilianPresence[2];
            for (int i = 0; i < civilians.Length; i++)
            {
                var refuge = new GameObject("Resident refuge " + (i + 1)).transform; refuge.SetParent(homes); refuge.position = new Vector3(10.8f, .05f, 2 + i * 2);
                var go = new GameObject("Market resident " + (i + 1)); go.transform.SetParent(homes); go.transform.position = new Vector3(4, .05f, 2 + i * 2);
                var shape = go.AddComponent<CapsuleCollider>(); shape.height = 2; shape.radius = .35f; shape.center = Vector3.up;
                var body = go.AddComponent<Rigidbody>(); ConfigureBody(body, 6); body.constraints = RigidbodyConstraints.FreezeRotation;
                go.AddComponent<PhysicalTarget>(); go.AddComponent<DamageReceiver>(); go.AddComponent<CollisionDamage>();
                var visual = Humanoid(go.transform, i == 0 ? wood : shell, steel, accent);
                var label = Sign("RESIDENT", new Vector3(0, 2.5f, 0), Quaternion.Euler(0, 180, 0), go.transform, .08f);
                civilians[i] = go.AddComponent<CivilianPresence>(); Set(civilians[i], "refuge", refuge); Set(civilians[i], "visual", visual); Set(civilians[i], "stateLabel", label);
            }
            var situation = new GameObject("Urban block situation").AddComponent<UrbanBlockSituation>();
            Set(situation, "player", player); Set(situation, "hostile", hostile);
            Set(situation, "streetNotice", Sign("MERCADO / RESIDENCIAS", new Vector3(0, 3.5f, 17), Quaternion.Euler(0, 180, 0), street, .11f));
            var serialized = new SerializedObject(situation);
            var residents = serialized.FindProperty("civilians"); residents.arraySize = civilians.Length;
            for (int i = 0; i < civilians.Length; i++) residents.GetArrayElementAtIndex(i).objectReferenceValue = civilians[i];
            var bodies = serialized.FindProperty("resetBodies"); bodies.arraySize = 2; bodies.GetArrayElementAtIndex(0).objectReferenceValue = cart; bodies.GetArrayElementAtIndex(1).objectReferenceValue = cover;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.CloseScene(source, true);
            EditorSceneManager.SaveScene(scene, UrbanScenePath); AssetDatabase.SaveAssets();
            Debug.Log("UrbanBlock01 authored using the accepted player prefab and 01C hostile.");
        }
        private static TextMesh Sign(string text, Vector3 local, Quaternion rotation, Transform parent, float size)
        {
            var label = new GameObject(text).AddComponent<TextMesh>(); label.transform.SetParent(parent, false); label.transform.localPosition = local;
            label.transform.localRotation = rotation; label.text = text; label.characterSize = size; label.fontSize = 48; label.anchor = TextAnchor.MiddleCenter; return label;
        }
    }
}
