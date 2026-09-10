using System.IO;
using System.Linq;
using Nexus.Feel01;
using Nexus.Reactivity01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nexus.SessionLoop01.Editor
{
    public static class SessionSceneAuthoring
    {
        public const string ScenePath = "Assets/Nexus/Experimental/SessionLoop01.unity";
        [MenuItem("Nexus/Session Loop 01/Open gameplay scene")]
        public static void Open()
        { if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath); }
        public static void Create()
        {
            if (File.Exists(ScenePath)) throw new System.InvalidOperationException("SessionLoop01 already exists.");
            var scene = EditorSceneManager.OpenScene("Assets/Nexus/Experimental/GameplayFeel01.unity");
            var player = Object.FindFirstObjectByType<FeelPlayer>(); var orbit = Object.FindFirstObjectByType<FeelCamera>();
            foreach (var item in scene.GetRootGameObjects())
                if (item != player.gameObject && item != orbit.gameObject && item != RenderSettings.sun.gameObject) Object.DestroyImmediate(item);
            player.transform.position = new Vector3(0, .05f, -10);
            var root = new GameObject("SessionLoop01 / continuous world"); var parent = root.transform;
            var arena = root.AddComponent<FeelArena>(); var stage = root.AddComponent<ReactionStage>(); var director = root.AddComponent<SessionLoopDirector>();
            var pulse = player.GetComponent<KineticPulse>();
            Set(arena, "player", player); Bool(arena, "pauseOnlyHud", true); Bool(pulse, "contextualHud", true);
            var arenaData = new SerializedObject(arena); arenaData.FindProperty("experimentTitle").stringValue = "SESSION LOOP 01A"; arenaData.ApplyModifiedPropertiesWithoutUndo();
            Set(stage, "arena", arena); Set(stage, "pulse", pulse);
            Set(stage, "settings", AssetDatabase.LoadAssetAtPath<ReactionSettings>("Assets/Nexus/Experimental/Reactivity01/Data/ReactionSettings.asset"));
            Set(director, "arena", arena); Set(director, "player", player); Set(director, "pulse", pulse);
            var asphalt = Mat("Asphalt"); var plaster = Mat("Warm plaster"); var brick = Mat("Market brick"); var teal = Mat("Depot teal"); var glass = Mat("Window glass");
            var stone = Mat("Paving"); var friction = Load<PhysicsMaterial>("ArenaContact.physicMaterial");
            var market = new GameObject("Market square").transform; market.SetParent(parent);
            var residential = new GameObject("Residential courtyard").transform; residential.SetParent(parent);
            Box("Market ground", new Vector3(-2,-.5f,0), new Vector3(24,1,24), asphalt,market,friction);
            Box("Service channel", new Vector3(13,-3.5f,0),new Vector3(6,1,24),stone,market,friction);
            Box("Channel east wall",new Vector3(16,-1,0),new Vector3(.4f,6,24),stone,market);
            Box("North channel apron",new Vector3(13,-.5f,11),new Vector3(6,1,2),asphalt,market);
            Box("South channel apron",new Vector3(13,-.5f,-11),new Vector3(6,1,2),asphalt,market);
            Box("Connecting street",new Vector3(0,-.5f,21),new Vector3(10,1,18),asphalt,parent,friction);
            Box("Residential ground",new Vector3(0,-.5f,42),new Vector3(28,1,24),stone,residential,friction);
            foreach(float x in new[]{-5.1f,5.1f}) Box("Street parapet",new Vector3(x,.4f,21),new Vector3(.2f,.8f,18),brick,parent);
            foreach(float x in new[]{-14f,14f}) Box("Courtyard boundary",new Vector3(x,1,42),new Vector3(.3f,2,24),brick,residential);
            Box("Market west boundary",new Vector3(-14,1,0),new Vector3(.3f,2,24),brick,market);
            Box("Market south boundary",new Vector3(0,1,-12),new Vector3(28,2,.3f),brick,market);
            Box("Courtyard rear boundary",new Vector3(0,1,54),new Vector3(28,2,.3f),brick,residential);
            foreach(float z in new[]{-6f,5f}) Building("Market frontage",new Vector3(-11,3,z),new Vector3(5,6,8),brick,glass,market);
            foreach(float x in new[]{-10f,10f})
                Building("Apartment building",new Vector3(x,4,46),new Vector3(6,8,10),plaster,glass,residential);
            // Door jambs and lintel make the second obstacle's purpose explicit.
            foreach(float x in new[]{-3f,3f}) Box("Entrance wall",new Vector3(x,1.5f,40),new Vector3(3,3,.5f),brick,residential);
            Box("Entrance lintel",new Vector3(0,3,40),new Vector3(9,.4f,.6f),brick,residential);
            Sign("PATIO 12",new Vector3(0,3.6f,39.8f),residential,.1f);
            for(int i=0;i<5;i++) Box("Crossing stripe",new Vector3(-3+i*1.5f,.012f,5),new Vector3(.7f,.024f,2),stone,market,null,false);
            for(int i=0;i<4;i++) Box("Street centre marking",new Vector3(0,.012f,15+i*4),new Vector3(.14f,.024f,2),stone,parent,null,false);
            foreach(var p in new[]{new Vector3(-5,.5f,8),new Vector3(-7,.5f,35),new Vector3(7,.5f,35)})
            { Box("Bench slatted seat",p,new Vector3(2.5f,.2f,.7f),brick,parent); foreach(float dx in new[]{-.8f,.8f}) Box("Bench leg",p+new Vector3(dx,-.25f,0),new Vector3(.15f,.5f,.5f),stone,parent); }
            var marketPoint=new GameObject("Loading stall destination").transform; marketPoint.SetParent(market); marketPoint.position=new Vector3(7,1,8);
            var residentialPoint=new GameObject("Patio discovery point").transform; residentialPoint.SetParent(residential); residentialPoint.position=new Vector3(0,0,36);
            var intact=new GameObject("Intact market stall"); intact.transform.SetParent(market);
            Box("Stall counter",new Vector3(7,.7f,9.8f),new Vector3(3,1.4f,1),brick,intact.transform);
            Box("Stall awning",new Vector3(7,3,9),new Vector3(4,.15f,3),teal,intact.transform);
            foreach(float x in new[]{5.2f,8.8f}) Box("Awning post",new Vector3(x,1.5f,10),new Vector3(.16f,3,.16f),stone,intact.transform);
            Sign("MERCADO",new Vector3(7,3.6f,10),market,.1f);
            var damaged=new GameObject("Collapsed stall"); damaged.transform.SetParent(market);
            Grounded("Fallen awning",new Vector3(7,0,9.5f),new Vector3(4,.15f,2),Quaternion.Euler(0,15,12),teal,damaged.transform);
            for(int i=0;i<4;i++) Grounded("Broken timber",new Vector3(5.6f+i,0,7.7f+i%2),new Vector3(1.4f,.15f,.15f),Quaternion.Euler(0,25*i,8),brick,damaged.transform);
            damaged.SetActive(false);
            var rail=new GameObject("Secured service edge"); rail.transform.SetParent(market);
            BarrierVisual(rail.transform,new Vector3(10,.5f,-4),stone,teal); rail.SetActive(false);
            var worker=Actor("Market worker",new Vector3(5,1.01f,8),ReactionRole.Civil,stage,player.transform,orbit.GetComponent<Camera>(),friction);
            var threat=Actor("Stall intruder",new Vector3(7,1.01f,-5),ReactionRole.Enemy,stage,marketPoint,orbit.GetComponent<Camera>(),friction);
            var resident=Actor("Resident behind delivery",new Vector3(0,1.01f,43),ReactionRole.Civil,stage,player.transform,orbit.GetComponent<Camera>(),friction);
            var neighbour=Actor("Neighbour",new Vector3(-6,1.01f,35),ReactionRole.Civil,stage,player.transform,orbit.GetComponent<Camera>(),friction);
            var freight=Freight("Market pallet",new Vector3(3,.85f,0),market,plaster,glass,friction); Set(freight.gameObject.AddComponent<ImpactRelay>(),"stage",stage);
            var delivery=Freight("Delivery blocking doorway",new Vector3(0,.85f,40),residential,plaster,glass,friction); Set(delivery.gameObject.AddComponent<ImpactRelay>(),"stage",stage);
            var barricade=new GameObject("Portable trestle barrier"); barricade.transform.SetParent(market); barricade.transform.position=new Vector3(2,.5f,-4);
            barricade.AddComponent<BoxCollider>().size=new Vector3(2,.9f,.5f); var barrierBody=barricade.AddComponent<Rigidbody>(); Configure(barrierBody,2);
            BarrierVisual(barricade.transform,Vector3.zero,stone,teal,true); Set(barricade.AddComponent<ImpactRelay>(),"stage",stage);
            var marker=GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.name="Impact flash"; marker.transform.SetParent(parent);
            Object.DestroyImmediate(marker.GetComponent<Collider>()); marker.GetComponent<Renderer>().sharedMaterial=Load<Material>("Pulse.mat"); marker.SetActive(false);
            Set(stage,"impactMarker",marker.transform); Array(stage,"actors",new[]{worker,threat,resident,neighbour});
            Set(director,"threat",threat); Set(director,"worker",worker); Set(director,"resident",resident);
            Set(director,"marketPoint",marketPoint); Set(director,"residentialPoint",residentialPoint); Set(director,"delivery",delivery.GetComponent<Collider>());
            Set(director,"intactStall",intact); Set(director,"brokenStall",damaged); Set(director,"channelRail",rail);
            Set(director,"marketSignal",Signal("Market lamp",new Vector3(8,3,-4),market)); Set(director,"accessSignal",Signal("Entrance lamp",new Vector3(0,3.8f,40),residential));
            Array(arena,"props",Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None));
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,true)}).ToArray();
            AssetDatabase.SaveAssets(); Debug.Log("SessionLoop01 authored.");
        }
        private static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Nexus/Experimental/VerticalSlice01/Data/"+name+".mat");
        private static Light Signal(string name,Vector3 position,Transform parent)
        { var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;var light=go.AddComponent<Light>();light.range=7;light.intensity=.4f;return light; }
        private static Rigidbody Freight(string name,Vector3 position,Transform parent,Material wood,Material strap,PhysicsMaterial friction)
        {
            var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;
            var collider=go.AddComponent<BoxCollider>();collider.size=new Vector3(1.8f,1.6f,1.4f);collider.sharedMaterial=friction;
            var body=go.AddComponent<Rigidbody>();Configure(body,4);
            for(int i=0;i<4;i++) Box("Stacked crate slat",position+new Vector3(0,-.5f+i*.32f,0),new Vector3(1.8f,.25f,1.4f),wood,go.transform,null,false);
            foreach(float x in new[]{-.65f,.65f}) Box("Freight strap",position+new Vector3(x,.02f,-.72f),new Vector3(.1f,1.55f,.05f),strap,go.transform,null,false);
            foreach(float x in new[]{-.6f,.6f}) Box("Pallet runner",position+new Vector3(x,-.72f,0),new Vector3(.25f,.16f,1.4f),wood,go.transform,null,false);
            return body;
        }
        private static void BarrierVisual(Transform parent,Vector3 position,Material leg,Material rail,bool local=false)
        {
            if(local) position=parent.position;
            Box("Trestle rail",position+Vector3.up*.25f,new Vector3(2,.25f,.16f),rail,parent,null,false);
            foreach(float x in new[]{-.8f,.8f}) { Box("Trestle leg",position+new Vector3(x,0,0),new Vector3(.12f,.9f,.12f),leg,parent,null,false); Box("Trestle foot",position+new Vector3(x,-.4f,0),new Vector3(.35f,.12f,.65f),leg,parent,null,false); }
        }
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
