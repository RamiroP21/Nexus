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
        public const string DistrictScenePath = Root + "/Scenes/District01.unity";

        [MenuItem("Nexus/Production/Create Persistent District 01")]
        public static void CreateDistrict01()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author outside Play Mode.");
            if (File.Exists(DistrictScenePath)) throw new InvalidOperationException("District01 already exists; edit it deliberately.");
            if (!AssetDatabase.CopyAsset(UrbanScenePath, DistrictScenePath)) throw new InvalidOperationException("UrbanBlock01 must exist before creating District01.");
            var scene = EditorSceneManager.OpenScene(DistrictScenePath, OpenSceneMode.Single);
            DisableLegacyBlockBoundaries();
            var steel = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Steel.mat");
            var concrete = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Concrete.mat");
            var plaster = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Warm panels.mat");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Pallet wood.mat");
            var accent = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Safety trim.mat");
            var shell = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Target shell.mat");
            var friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(Root + "/Data/PlaygroundContact.physicMaterial");
            var district = new GameObject("Persistent district session").transform;
            var market = new GameObject("Zone A - Market / Mixed Use").transform; market.SetParent(district); market.position = new Vector3(0, 0, 2);
            var residential = new GameObject("Zone B - Residential / Community").transform; residential.SetParent(district); residential.position = new Vector3(28, 0, 7);
            var service = new GameObject("Zone C - Service / Infrastructure").transform; service.SetParent(district); service.position = new Vector3(-28, 0, -8);
            Sign("ZONE A  /  MERCADO MIXED USE", new Vector3(-4, 3.6f, 16), Quaternion.Euler(0, 180, 0), market, .1f);
            BuildResidentialZone(residential, wood, plaster, steel, accent, shell, friction);
            BuildServiceZone(service, wood, plaster, steel, accent, shell, friction, out var emergency, out var serviceWorkers);
            BuildConnectors(district, concrete, accent, steel);
            var player = Object.FindFirstObjectByType<PlayerVitality>();
            var crisis1 = Object.FindFirstObjectByType<UrbanBlockSituation>();
            var director = district.gameObject.AddComponent<DistrictSessionDirector>();
            Set(director, "crisis1", crisis1); Set(director, "crisis2", emergency); Set(director, "player", player);
            Set(director, "marketZone", market); Set(director, "residentialZone", residential); Set(director, "serviceZone", service);
            var residents = Object.FindObjectsByType<CivilianPresence>(FindObjectsSortMode.None);
            SetArray(director, "resetCivilians", residents);
            Set(crisis1, "district", director);
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Any(s => s.path == DistrictScenePath)
                ? EditorBuildSettings.scenes
                : EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(DistrictScenePath, true) }).ToArray();
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Persistent District01 authored with three connected zones and Crisis 2.");
        }

        [MenuItem("Nexus/Production/Repair Persistent District Routes")]
        public static void RepairDistrict01Routes()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Author outside Play Mode.");
            var scene = EditorSceneManager.OpenScene(DistrictScenePath, OpenSceneMode.Single);
            DisableLegacyBlockBoundaries();
            EditorSceneManager.SaveScene(scene);
        }

        private static void DisableLegacyBlockBoundaries()
        {
            foreach (var boundary in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (boundary.name == "Block boundary") boundary.gameObject.SetActive(false);
        }

        private static void BuildConnectors(Transform district, Material concrete, Material accent, Material steel)
        {
            var connectors = new GameObject("District connectors - street alley intersection").transform; connectors.SetParent(district);
            Box("Market to residential street", new Vector3(22, -.5f, 7), new Vector3(14, 1, 8), concrete, connectors, true);
            Box("Market to service street", new Vector3(-22, -.5f, -8), new Vector3(14, 1, 8), concrete, connectors, true);
            Box("Residential vault shortcut", new Vector3(18, .45f, 2), new Vector3(2.8f, .9f, .35f), accent, connectors);
            Box("Service mantle shortcut", new Vector3(-18, .9f, -4), new Vector3(3, 1.8f, 3), steel, connectors);
            Sign("PATIO SUR  ->  MERCADO", new Vector3(18, 2.5f, 10), Quaternion.Euler(0, 180, 0), connectors, .08f);
            Sign("SERVICE ALLEY  ->  MERCADO", new Vector3(-18, 2.5f, -12), Quaternion.identity, connectors, .08f);
        }

        private static void BuildResidentialZone(Transform root, Material wood, Material plaster, Material steel, Material accent, Material shell, PhysicsMaterial friction)
        {
            Box("Community courtyard", new Vector3(0, -.5f, 0), new Vector3(16, 1, 14), plaster, root);
            Box("Community home north", new Vector3(5, 3, 5), new Vector3(6, 6, 5), plaster, root);
            Box("Community home south", new Vector3(5, 3, -5), new Vector3(6, 6, 5), plaster, root);
            Box("Courtyard low wall", new Vector3(-5, 1, 0), new Vector3(.5f, 2, 12), steel, root);
            Box("Community bench", new Vector3(-1, .5f, 0), new Vector3(4, .15f, .7f), wood, root, false);
            Sign("ZONE B  /  PATIO SUR COMMUNITY", new Vector3(0, 3.5f, 7), Quaternion.Euler(0, 180, 0), root, .1f);
            for (int i = 0; i < 2; i++)
            {
                var refuge = new GameObject("Community refuge " + (i + 1)).transform; refuge.SetParent(root); refuge.position = root.position + new Vector3(3.5f, .05f, i == 0 ? 3.5f : -3.5f);
                CreateCivilian("Courtyard resident " + (i + 1), root, root.position + new Vector3(-2 + i, .05f, i == 0 ? 3 : -3), refuge, wood, steel, accent);
            }
        }

        private static void BuildServiceZone(Transform root, Material wood, Material plaster, Material steel, Material accent, Material shell, PhysicsMaterial friction,
            out DistrictCargoEmergency emergency, out CivilianPresence[] workers)
        {
            Box("Service yard", new Vector3(0, -.5f, 0), new Vector3(16, 1, 14), steel, root);
            Box("Utility access building", new Vector3(-5, 3, 0), new Vector3(5, 6, 12), plaster, root);
            Sign("ZONE C  /  SERVICE INFRASTRUCTURE", new Vector3(2, 3.5f, 7), Quaternion.Euler(0, 180, 0), root, .1f);
            var crisisRoot = new GameObject("Crisis 2 - runaway service cargo").transform; crisisRoot.SetParent(root); crisisRoot.position = root.position + new Vector3(0, 0, -1);
            var laneStart = new GameObject("Cargo lane start").transform; laneStart.SetParent(crisisRoot); laneStart.position = root.position + new Vector3(-1, .85f, -4);
            var laneEnd = new GameObject("Cargo lane end").transform; laneEnd.SetParent(crisisRoot); laneEnd.position = root.position + new Vector3(11, .85f, -4);
            var cargoA = Crate("Runaway utility pallet A", laneStart.position, 3, wood, accent, crisisRoot, friction);
            var cargoB = Crate("Runaway utility pallet B", laneStart.position + Vector3.back * 2, 3, wood, accent, crisisRoot, friction);
            var warning = Sign("UTILITY OVERLOAD  /  CLEAR THE LANE", new Vector3(5, 2.8f, -5.3f), Quaternion.Euler(0, 180, 0), crisisRoot, .07f).gameObject;
            var damaged = Box("Damaged utility spill - persistent", new Vector3(5, .35f, -4), new Vector3(5, .15f, 1.5f), accent, crisisRoot, false); damaged.SetActive(false);
            var obstruction = Box("Debris blocks service shortcut", new Vector3(0, 1, -1), new Vector3(1, 2, 8), steel, crisisRoot, true); obstruction.SetActive(false);
            emergency = crisisRoot.gameObject.AddComponent<DistrictCargoEmergency>();
            SetArray(emergency, "cargo", new[] { cargoA, cargoB }); Set(emergency, "laneStart", laneStart); Set(emergency, "laneEnd", laneEnd);
            Set(emergency, "warningVisual", warning); Set(emergency, "damagedUtility", damaged); Set(emergency, "consequenceObstruction", obstruction);
            workers = new CivilianPresence[2];
            for (int i = 0; i < workers.Length; i++)
            {
                var refuge = new GameObject("Service worker refuge " + (i + 1)).transform; refuge.SetParent(root); refuge.position = root.position + new Vector3(5, .05f, i == 0 ? 3 : 1);
                workers[i] = CreateCivilian("Service worker " + (i + 1), root, root.position + new Vector3(5, .05f, i == 0 ? 2 : 0), refuge, shell, steel, accent);
            }
            SetArray(emergency, "workers", workers);
        }

        private static CivilianPresence CreateCivilian(string name, Transform parent, Vector3 position, Transform refuge, Material suit, Material joints, Material accent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent); go.transform.position = position;
            var shape = go.AddComponent<CapsuleCollider>(); shape.height = 2; shape.radius = .35f; shape.center = Vector3.up;
            var body = go.AddComponent<Rigidbody>(); ConfigureBody(body, 6); body.constraints = RigidbodyConstraints.FreezeRotation;
            go.AddComponent<PhysicalTarget>(); go.AddComponent<DamageReceiver>(); go.AddComponent<CollisionDamage>();
            var visual = Humanoid(go.transform, suit, joints, accent);
            var label = Sign("WORKER", new Vector3(0, 2.5f, 0), Quaternion.Euler(0, 180, 0), go.transform, .07f);
            var civilian = go.AddComponent<CivilianPresence>(); Set(civilian, "refuge", refuge); Set(civilian, "visual", visual); Set(civilian, "stateLabel", label);
            return civilian;
        }

        private static void SetArray<T>(Object target, string field, T[] values) where T : Object
        {
            var serialized = new SerializedObject(target); var property = serialized.FindProperty(field); property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
