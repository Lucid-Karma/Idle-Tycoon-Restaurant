using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Store covers rendered inside the game's own cafe (2026-10-14). The covers in use had been painted by an image
// generator from our renders, and showed things the game does not have (fries, drinks, bunny and cat customers, a
// menu board, pendant lamps, a second cook); the user asked for them again with only what is in the game. So the
// picture is now a real render of the cafe: the chef up front lifting a tall burger, the adventurer customers at
// the tables with their burgers, the kitchen behind, fresh ingredients in the foreground - seen from low down
// inside the cafe instead of from the game's high camera. Staged in the open GameScene with DontSave objects, all
// gone again after the render (the scene is never saved). The logo goes on in Tools/Covers/compose_store.py.
// Called from the editor bridge (scall): StoreCoverRenderer.Render("<out.png>", 1920, 1080); the static fields
// below are the knobs (bridge `set`), tuned by eye.
public static class StoreCoverRenderer
{
    const string Game = "Assets/Project/[GAME]/";
    const string RigFbx = "Assets/Models/KayKit_Skeletons_1.0_FREE/characters/fbx/Skeleton_Warrior.fbx";
    const string Npcs = Game + "Prefabs/CharacterPrefabs/NpcPrefabs/";

    // Camera: placed from the chef - `CamDistance` back along the view, `CamHeight` above the floor, looking
    // `CamYaw` (300 = the game camera's own direction, into the kitchen's corner).
    public static float CamYaw = 300f, CamPitch = 4f, CamDistance = 5.5f, CamHeight = 1.3f, CamSide = 0f, Fov = 40f;
    public static float AimUp = 1.1f;            // what it looks at: this far above the chef's feet
    public static Color Sky = new Color(0.98f, 0.80f, 0.86f);
    public static bool Blur = true;
    public static float BlurStart = 2.5f, BlurEnd = 12f, BlurRadius = 1.2f;   // behind the chef, softer with distance

    // The chef.
    public static Vector3 ChefAt = new Vector3(10.2f, 1.5f, 23.2f);
    public static float ChefTurn = 0f;           // extra turn from facing the camera
    public static string ChefClip = "Cheer";
    public static float ChefTime = 0.45f;
    public static string Mouth = "Mouth_Grin";
    public static float BurgerScale = 1.1f, BurgerLift = 0.05f, BurgerForward = 0.05f, BurgerSide = 0.0f;
    public static string HoldHand = "r";       // "r" / "l": the plate on one hand, beside his head; "both": over it
    public static float TableBurger = 0.5f;

    // The two tables either side of him, placed from him in the camera's terms: (sideways, back) in metres.
    public static Vector2 LeftTable = new Vector2(-2.7f, 1.6f), RightTable = new Vector2(2.9f, 1.4f);
    public static float LeftTableTurn = 20f, RightTableTurn = -15f;
    public static string LeftGuests = "Mage,Barbarian,Npc 1,", RightGuests = "Knight,Rogue,Ranger,";

    // The counter in the foreground, from the camera: (sideways, ahead) in metres, and its turn.
    public static Vector2 CounterAt = new Vector2(0f, 1.5f);
    public static float CounterTurn = 0f;
    public static bool Counter = true;
    public static float FoodScale = 0.6f;

    // The three store covers as they were made on 2026-10-14 (twice the cover size; compose_store.py scales them
    // down and puts the logo on): landscape 3840x2160, portrait 1600x2400, square 1600x1600 into `dir`.
    public static string RenderAll(string dir)
    {
        ChefAt = new Vector3(10.2f, 1.5f, 23.2f); ChefClip = "Cheer"; ChefTime = 0.3f; BurgerSide = 0.22f;
        LeftTableTurn = 35f; RightTableTurn = -30f;

        Cam(300f, 0f, 6.0f, 1.5f, 42f); AimUp = 2.3f; CamSide = 0f; CounterAt = new Vector2(0f, 2.0f);
        LeftTable = new Vector2(-3.0f, 2.0f); RightTable = new Vector2(3.4f, 1.8f);
        Render(Path.Combine(dir, "landscape.png"), 3840, 2160);

        Cam(300f, 0f, 5.6f, 1.5f, 44f); AimUp = 2.38f; CamSide = -0.45f; CounterAt = new Vector2(0.45f, 1.9f);
        LeftTable = new Vector2(-2.4f, 2.3f); RightTable = new Vector2(2.5f, 2.1f);
        Render(Path.Combine(dir, "portrait.png"), 1600, 2400);

        Cam(300f, 0f, 5.7f, 1.5f, 42f); AimUp = 2.5f; CamSide = -0.3f; CounterAt = new Vector2(0.3f, 1.9f);
        LeftTable = new Vector2(-2.7f, 2.2f); RightTable = new Vector2(2.8f, 2.0f);
        Render(Path.Combine(dir, "square.png"), 1600, 1600);
        return dir;
    }

    public static string Render(string path, int width, int height)
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(c => c.enabled).ToList();
        var hidden = new List<Renderer>();
        var stage = new GameObject("__StoreCoverStage") { hideFlags = HideFlags.DontSave };
        AnimationMode.StartAnimationMode();
        try
        {
            foreach (var c in canvases) c.enabled = false;
            var player = Object.FindFirstObjectByType<PlayerFSM>();
            foreach (var r in player.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; hidden.Add(r); }
            // The "CHIBI BURGER" letters on the kitchen wall would sit right behind the logo, blurred: a second title.
            var letters = GameObject.Find("Letters");
            if (letters != null)
                foreach (var r in letters.GetComponentsInChildren<Renderer>()) if (r.enabled) { r.enabled = false; hidden.Add(r); }

            var forward = Quaternion.Euler(0f, CamYaw, 0f) * Vector3.forward;
            var right = Quaternion.Euler(0f, CamYaw, 0f) * Vector3.right;
            float faceCamera = CamYaw + 180f;

            var chef = BuildChef(stage.transform, ChefAt, faceCamera + ChefTurn);
            BuildHeldBurger(stage.transform, chef);
            BuildTable(stage.transform, ChefAt + right * LeftTable.x + forward * LeftTable.y, faceCamera + LeftTableTurn, LeftGuests, forward);
            BuildTable(stage.transform, ChefAt + right * RightTable.x + forward * RightTable.y, faceCamera + RightTableTurn, RightGuests, forward);

            var cam = BuildCamera(stage.transform, forward, right, width / (float)height);
            if (Counter) BuildCounter(stage.transform, cam.transform.position, forward, right);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, Shoot(cam, width, height));
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(stage);
            foreach (var r in hidden) if (r != null) r.enabled = true;
            foreach (var c in canvases) if (c != null) c.enabled = true;
        }
        return path;
    }

    #region People
    static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAllAssetsAtPath(RigFbx).OfType<AnimationClip>().First(c => c.name == name);

    static void Pose(GameObject model, string clipName, float time)
    {
        var clip = Clip(clipName);
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(model, clip, clip.length * time);
        AnimationMode.EndSampling();
    }

    static GameObject BuildChef(Transform stage, Vector3 at, float yaw)
    {
        var source = Object.FindFirstObjectByType<PlayerFSM>().transform.Find("Model");
        var model = Object.Instantiate(source.gameObject, stage);
        model.name = "Chef";
        model.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
        model.transform.localScale = source.lossyScale;
        foreach (var b in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("Mouth_")) t.gameObject.SetActive(t.name == Mouth);
            else if (t.name.StartsWith("Hat_") || t.name.Contains("Toque")) t.gameObject.SetActive(false);
        }
        foreach (var r in model.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        Pose(model, ChefClip, ChefTime);
        return model;
    }

    // A customer (one of the game's own Npc prefabs) sitting on `chair`, the way NpcFsm seats them.
    static void Seat(Transform stage, string kind, Transform chair)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Npcs + (kind.StartsWith("Npc") ? kind : "Npc " + kind) + ".prefab");
        if (prefab == null) throw new System.Exception("no customer " + kind);
        var npc = (GameObject)Object.Instantiate(prefab, stage);
        npc.name = kind;
        foreach (var b in npc.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
        foreach (var c in npc.GetComponentsInChildren<Canvas>(true)) Object.DestroyImmediate(c.gameObject);
        var agent = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) Object.DestroyImmediate(agent);
        npc.transform.SetPositionAndRotation(chair.position + chair.forward * 0.65f, chair.rotation);
        Pose(npc.GetComponentInChildren<Animator>(true).gameObject, "Sit_Chair_Idle", 0.3f);
    }
    #endregion

    #region Props
    static Transform Strip(GameObject go)
    {
        go.hideFlags = HideFlags.DontSave;
        foreach (var b in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
        foreach (var o in go.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true)) Object.DestroyImmediate(o);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        return go.transform;
    }

    static GameObject Visual(string foodPrefab, string field)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Game + "Prefabs/FoodPrefabs/" + foodPrefab + ".prefab");
        var food = prefab.GetComponent<EdibleBase>();
        return (GameObject)new SerializedObject(food).FindProperty(field).objectReferenceValue;
    }

    static Bounds LocalBounds(Transform item, Transform space)
    {
        bool any = false;
        var result = new Bounds();
        foreach (var filter in item.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            var mb = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var p = space.InverseTransformPoint(filter.transform.TransformPoint(corner));
                if (!any) { result = new Bounds(p, Vector3.zero); any = true; }
                else result.Encapsulate(p);
            }
        }
        return result;
    }

    static readonly (string prefab, string field, float settle)[] Classic =
    {
        ("Bun", "bunSlice", 0.85f), ("Uncooked_Burger", "cookedBurger", 0.8f), ("Cheese", "slicedCheese", 0.5f),
        ("Lettuce", "purePrefab", 0.35f), ("Tomato", "slicedTomato", 0.7f), ("Hamburger", "finishBun", 0.85f),
    };
    static readonly (string prefab, string field, float settle)[] Tower =
    {
        ("Bun", "bunSlice", 0.85f), ("Uncooked_Burger", "cookedBurger", 0.8f), ("Cheese", "slicedCheese", 0.5f),
        ("Lettuce", "purePrefab", 0.35f), ("Tomato", "slicedTomato", 0.7f), ("Uncooked_Burger", "cookedBurger", 0.8f),
        ("Cheese", "slicedCheese", 0.5f), ("Onion", "slicedOnion", 0.6f), ("Hamburger", "finishBun", 0.85f),
    };

    // A plate with a burger stacked on it, layer by layer, the way the game stacks them; `wobble` sets each layer a
    // little off-centre. Returns the plate.
    static Transform Burger(Transform parent, (string prefab, string field, float settle)[] layers, float scale, float wobble, int seed)
    {
        var root = new GameObject("Burger") { hideFlags = HideFlags.DontSave }.transform;
        root.SetParent(parent, false);
        root.localScale = Vector3.one * scale;
        var plateSource = GameObject.Find("Level/Selectables/Plates/plate");
        float top = 0f;
        if (plateSource != null)
        {
            var plate = Object.Instantiate(plateSource.GetComponentInChildren<MeshFilter>().gameObject, root);
            foreach (Transform child in plate.transform) Object.DestroyImmediate(child.gameObject);
            Strip(plate);
            plate.name = "Plate";
            plate.transform.localPosition = Vector3.zero;
            plate.transform.localRotation = Quaternion.identity;
            plate.transform.localScale = Vector3.one;
            var pb = LocalBounds(plate.transform, root);
            plate.transform.localPosition = new Vector3(-pb.center.x, -pb.min.y, -pb.center.z);
            top = pb.size.y * 0.6f;
        }
        var random = new System.Random(seed);
        foreach (var (prefab, field, settle) in layers)
        {
            var layer = Strip(Object.Instantiate(Visual(prefab, field), root));
            layer.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
            layer.localPosition = Vector3.zero;
            var b = LocalBounds(layer, root);
            float dx = ((float)random.NextDouble() - 0.5f) * wobble, dz = ((float)random.NextDouble() - 0.5f) * wobble;
            layer.localPosition = new Vector3(dx - b.center.x, top - b.min.y, dz - b.center.z);
            top += b.size.y * settle;
        }
        return root;
    }

    static Transform Bone(GameObject model, string name) =>
        model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

    // The tall burger on a plate, up in both hands over his head.
    static void BuildHeldBurger(Transform stage, GameObject chef)
    {
        var l = Bone(chef, "handslot.l") ?? Bone(chef, "hand.l");
        var r = Bone(chef, "handslot.r") ?? Bone(chef, "hand.r");
        var burger = Burger(stage, Tower, BurgerScale, 0.08f, 7);
        var hand = HoldHand == "l" ? l.position : HoldHand == "r" ? r.position : (l.position + r.position) * 0.5f;
        // His right is the camera's left (he faces it): `BurgerSide` pushes the plate further out that way.
        float side = HoldHand == "l" ? -BurgerSide : BurgerSide;
        burger.position = hand + Vector3.up * BurgerLift + chef.transform.forward * BurgerForward + chef.transform.right * side;
        burger.rotation = chef.transform.rotation;
    }

    // A round table for four (the Big Table from the shop, with its chairs, ketchup and mustard), customers on the
    // chairs named in `guests` (comma separated, by chair; empty = an empty chair) and a burger in front of each.
    static void BuildTable(Transform stage, Vector3 at, float yaw, string guests, Vector3 forward)
    {
        var source = GameObject.Find("Level/Upgrades").transform.Find("Upgrade BigTable");
        var group = Object.Instantiate(source.gameObject, stage);
        group.SetActive(true);
        foreach (var t in group.GetComponentsInChildren<Transform>(true)) t.gameObject.SetActive(true);
        var table = group.transform.Find("table_round_A_small");
        var chairs = group.GetComponentsInChildren<Chair>(true).Select(c => c.transform).ToList();
        var spots = group.GetComponentsInChildren<ServiceBase>(true).Select(s => s.transform).ToList();
        // Move the group so its table stands at `at`, turned by `yaw` about it.
        var pivot = new GameObject("pivot") { hideFlags = HideFlags.DontSave }.transform;
        pivot.SetParent(stage, false);
        pivot.position = new Vector3(table.position.x, table.position.y, table.position.z);
        group.transform.SetParent(pivot, true);
        pivot.position = at;   // the table's own origin is on the floor
        pivot.rotation = Quaternion.Euler(0f, yaw, 0f);

        // The chairs between the table and the camera would show the backs of chairs and customers in front of
        // everything: gone.
        foreach (var near in chairs.Where(c => Vector3.Dot(c.forward, forward) > 0.2f).ToList())
        {
            chairs.Remove(near);
            Object.DestroyImmediate(near.gameObject);
        }
        var names = guests.Split(',');
        // Customers on the far side first (facing the camera): the chairs sorted by how much they face it.
        var ordered = chairs.OrderBy(c => Vector3.Dot(c.forward, forward)).ToList();
        for (int i = 0; i < ordered.Count && i < names.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(names[i])) continue;
            var chair = ordered[i];
            Seat(stage, names[i].Trim(), chair);
            var spot = spots.OrderBy(s => Vector3.Distance(s.position, chair.position + chair.forward * 1.2f)).First();
            var burger = Burger(stage, Classic, TableBurger, 0.03f, i * 13 + 1);
            burger.position = new Vector3(spot.position.x, spot.position.y, spot.position.z);
            burger.rotation = chair.rotation;
            spots.Remove(spot);
        }
        foreach (var s in group.GetComponentsInChildren<ServiceBase>(true)) Object.DestroyImmediate(s.gameObject);
        var menu = group.transform.Find("menu");
        if (menu != null) Object.DestroyImmediate(menu.gameObject);
        Strip(pivot.gameObject);
    }
    #endregion

    // A kitchen work table from the game near the camera, with what goes into a burger on it: two patties in the
    // pan, tomato slices, lettuce, cheese, the ketchup and the mustard.
    static void BuildCounter(Transform stage, Vector3 camPos, Vector3 forward, Vector3 right)
    {
        var source = GameObject.Find("Level/Kitchen Utensils/kitchentable_A_large");
        var counter = Strip(Object.Instantiate(source, stage));
        foreach (Transform child in counter) Object.DestroyImmediate(child.gameObject);
        var ground = new Vector3(camPos.x, ChefAt.y, camPos.z);
        counter.position = ground + right * CounterAt.x + forward * CounterAt.y;
        counter.rotation = Quaternion.Euler(0f, CamYaw + CounterTurn, 0f);
        var rs = counter.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        float top = b.max.y;
        var centre = new Vector3(b.center.x, top, b.center.z);

        Transform Put(GameObject visual, float along, float ahead, float yaw, float scale = 1f)
        {
            var item = Strip(Object.Instantiate(visual, stage));
            item.localScale *= scale * FoodScale;
            item.rotation = Quaternion.Euler(0f, yaw, 0f);
            item.position = centre + right * along + forward * ahead;
            var ib = item.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
            item.position += Vector3.up * (top - ib.min.y);
            return item;
        }

        var pan = Put(GameObject.Find("Level/Small Kitchen Utensil/pan_B"), -0.75f, 0.05f, CamYaw + 40f);
        var panBounds = pan.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
        var patty = Visual("Uncooked_Burger", "cookedBurger");
        for (int i = 0; i < 2; i++)
        {
            var p = Strip(Object.Instantiate(patty, stage));
            p.localScale *= FoodScale;
            p.rotation = Quaternion.Euler(0f, 30f * i, 0f);
            p.position = new Vector3(panBounds.center.x, panBounds.min.y + panBounds.size.y * 0.35f, panBounds.center.z) + (right * (i == 0 ? -0.13f : 0.14f) + forward * (i == 0 ? 0.05f : -0.06f)) * FoodScale * 1.6f;
        }
        var slice = Visual("Tomato", "slicedTomato");
        Put(slice, -0.15f, -0.05f, 10f);
        Put(slice, -0.02f, 0.12f, 70f);
        Put(slice, 0.1f, -0.08f, 140f);
        Put(Visual("Lettuce", "purePrefab"), 0.35f, 0.1f, 20f);
        Put(Visual("Cheese", "slicedCheese"), 0.62f, -0.05f, 35f);
        Put(Visual("Cheese", "slicedCheese"), 0.66f, 0.1f, 80f);
        Put(GameObject.Find("Level/Small Kitchen Utensil/ketchup"), 0.95f, 0.08f, CamYaw + 180f);
        Put(GameObject.Find("Level/Small Kitchen Utensil/mustard"), 1.1f, -0.05f, CamYaw + 160f);
    }

    #region Camera
    static Camera BuildCamera(Transform stage, Vector3 forward, Vector3 right, float aspect)
    {
        var go = new GameObject("__StoreCoverCamera") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(stage, false);
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = Fov;
        cam.aspect = aspect;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 300f;
        cam.allowHDR = true;
        cam.allowMSAA = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Sky;
        var aim = ChefAt + Vector3.up * AimUp;
        var dir = Quaternion.AngleAxis(-CamPitch, right) * forward;
        go.transform.position = aim - dir * CamDistance + right * CamSide;
        go.transform.position = new Vector3(go.transform.position.x, ChefAt.y + CamHeight, go.transform.position.z);
        go.transform.rotation = Quaternion.LookRotation(aim - go.transform.position);
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        data.renderShadows = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        if (Blur)
        {
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.DontSave;
            var dof = profile.Add<DepthOfField>(true);
            float focus = Vector3.Distance(go.transform.position, aim);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(focus + BlurStart);
            dof.gaussianEnd.Override(focus + BlurEnd);
            dof.gaussianMaxRadius.Override(BlurRadius);
            dof.highQualitySampling.Override(true);
            volume.sharedProfile = profile;
        }
        cam.enabled = false;
        return cam;
    }

    static byte[] Shoot(Camera cam, int width, int height)
    {
        var rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { msaaSamples = 8, sRGB = true });
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        var png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        return png;
    }

    // Sets the camera in one call (the bridge's arguments can't hold several values in one field).
    public static string Cam(float yaw, float pitch, float distance, float height, float fov)
    {
        CamYaw = yaw; CamPitch = pitch; CamDistance = distance; CamHeight = height; Fov = fov;
        return "ok";
    }

    // Any knob by name: numbers, words, true/false, and "x;y" or "x;y;z" for vectors.
    public static string Set(string name, string value)
    {
        var f = typeof(StoreCoverRenderer).GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (f == null) return "no " + name;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        float[] Parts() => value.Split(';').Select(v => float.Parse(v, inv)).ToArray();
        object v = f.FieldType == typeof(float) ? float.Parse(value, inv)
            : f.FieldType == typeof(bool) ? bool.Parse(value)
            : f.FieldType == typeof(Vector2) ? new Vector2(Parts()[0], Parts()[1])
            : f.FieldType == typeof(Vector3) ? new Vector3(Parts()[0], Parts()[1], Parts()[2])
            : (object)value.Replace("_", " ");
        f.SetValue(null, v);
        return name + "=" + v;
    }

    public static string Chef(float x, float z, string clip, float time)
    {
        ChefAt = new Vector3(x, ChefAt.y, z); ChefClip = clip; ChefTime = time;
        return "ok";
    }
    #endregion
}
