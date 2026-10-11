using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// The close-up hero for the store covers (Tools/Covers/compose_covers.py): the chef facing the player with
// a grin, a tall burger teetering on his head. The first covers showed the whole cafe as a small diorama and
// lost to the busy, saturated thumbnails around them on CrazyGames (1.1% click-through); at thumbnail size only
// a big face and a big burger read. Same models as the game, same matting as the key art (KeyArtRenderer).
// Called from the editor bridge with scall, e.g.
//   HeroArtRenderer.Render("<out.png>", "Taunt", 0.5, 200, 1600)
// and HeroArtRenderer.Sheet("<dir>", 400) renders a contact sheet of candidate poses.
public static class HeroArtRenderer
{
    const string ChefFbx = "Assets/Models/KayKit_Skeletons_1.0_FREE/characters/fbx/Skeleton_Warrior.fbx";

    public static float StackLean = 6f;
    public static float BurgerScale = 1.55f;   // bigger than life: at thumbnail size it has to read as a burger
    public static float TomatoX = -0.55f, TomatoY = 1.08f;
    public static float Settle = 0.82f;       // how far each layer sinks into the one below (lower = tighter)
    public static float CamPitch = -6f;      // negative: the camera looks slightly up at him (the hero angle)
    public static float Framing = 0.86f;
    public static string Mouth = "Mouth_Grin";

    public static string Sheet(string dir, int size)
    {
        var poses = new (string clip, float t)[]
        {
            ("Idle", 0.3f), ("Taunt", 0.35f), ("Taunt", 0.6f), ("Spellcast_Raise", 0.5f),
            ("Jump_Idle", 0.5f), ("Unarmed_Idle", 0.5f), ("Interact", 0.4f), ("Spellcast_Shoot", 0.4f),
        };
        Directory.CreateDirectory(dir);
        foreach (var (clip, t) in poses)
            Render(Path.Combine(dir, $"pose_{clip}_{t:0.00}.png".Replace(',', '.')), clip, t, 200f, size);
        return "ok " + poses.Length;
    }

    public static string Render(string path, string clip, float time, float yaw, int size)
    {
        var stage = new GameObject("__HeroStage") { hideFlags = HideFlags.DontSave };
        stage.transform.position = KeyArtRenderer.StageOrigin;
        AnimationMode.StartAnimationMode();
        try
        {
            var model = BuildChef(stage.transform, clip, time, yaw);
            var top = BuildBurger(stage.transform, model, yaw);
            var cam = BuildCamera(stage.transform, model, top);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, KeyArtRenderer.RenderMatted(cam, size, Mathf.RoundToInt(size * 1.25f)));
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(stage);
        }
        return path;
    }

    static GameObject BuildChef(Transform stage, string clipName, float time, float yaw)
    {
        var player = Object.FindFirstObjectByType<PlayerFSM>();
        var source = player.transform.Find("Model");
        var model = Object.Instantiate(source.gameObject, stage);
        model.name = "Chef";
        model.hideFlags = HideFlags.DontSave;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        model.transform.localScale = source.lossyScale;
        foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        // No hat (the burger sits where it would go); the grin instead of the working smile.
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith("Mouth_")) t.gameObject.SetActive(t.name == Mouth);
            else if (t.name.StartsWith("Hat_") || t.name.Contains("Toque")) t.gameObject.SetActive(false);
        }
        var clip = AssetDatabase.LoadAllAssetsAtPath(ChefFbx).OfType<AnimationClip>().First(c => c.name == clipName);
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(model, clip, clip.length * time);
        AnimationMode.EndSampling();
        return model;
    }

    static Bounds PosedBounds(SkinnedMeshRenderer smr)
    {
        var mesh = new Mesh();
        smr.BakeMesh(mesh, true);
        var v = mesh.vertices;
        var b = new Bounds(smr.transform.position + smr.transform.rotation * v[0], Vector3.zero);
        foreach (var p in v) b.Encapsulate(smr.transform.position + smr.transform.rotation * p);
        Object.DestroyImmediate(mesh);
        return b;
    }

    static Bounds LocalBounds(Transform layer, Transform space)
    {
        bool any = false;
        var result = new Bounds();
        foreach (var filter in layer.GetComponentsInChildren<MeshFilter>())
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

    // A proper, tall burger (the one the game calls a Chef's Classic, plus a second patty), each layer a little
    // off-centre so it teeters, and a whole tomato bouncing off the top: appetising first, funny second.
    static float BuildBurger(Transform stage, GameObject model, float yaw)
    {
        var head = PosedBounds(model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name.Contains("Head")));
        var stack = new GameObject("Burger").transform;
        stack.SetParent(stage, false);
        stack.position = new Vector3(head.center.x, head.max.y - 0.05f, head.center.z);
        stack.rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, StackLean);
        stack.localScale = Vector3.one * BurgerScale;

        var layers = new (string prefab, string field, Vector3 tilt, Vector2 shift, float settle)[]
        {
            ("Bun", "bunSlice", new Vector3(0, 0, 0), Vector2.zero, 0.85f),
            ("Uncooked_Burger", "cookedBurger", new Vector3(3, 20, -2), new Vector2(0.03f, 0.01f), 0.8f),
            ("Cheese", "slicedCheese", new Vector3(-3, 45, 3), new Vector2(0.05f, 0.0f), 0.55f),
            ("Lettuce", "purePrefab", new Vector3(4, -20, -3), new Vector2(0.02f, 0.02f), 0.35f),
            ("Tomato", "slicedTomato", new Vector3(-4, 10, 5), new Vector2(0.07f, -0.01f), 0.7f),
            ("Uncooked_Burger", "cookedBurger", new Vector3(5, -30, -4), new Vector2(0.10f, 0.02f), 0.8f),
            ("Cheese", "slicedCheese", new Vector3(-2, 15, 6), new Vector2(0.12f, 0.0f), 0.55f),
            ("Hamburger", "finishBun", new Vector3(-8, 25, 10), new Vector2(0.16f, 0.03f), 0.85f),
        };
        float top = 0f;
        foreach (var (prefab, field, tilt, shift, settle) in layers)
        {
            var layer = KeyArtRenderer.Place(KeyArtRenderer.Visual(prefab, field), stack, prefab);
            layer.localRotation = Quaternion.Euler(tilt);
            layer.localPosition = Vector3.zero;
            var b = LocalBounds(layer, stack);
            layer.localPosition = new Vector3(shift.x - b.center.x, top - b.min.y, shift.y - b.center.z);
            top += b.size.y * settle * Settle;
        }

        var tomato = KeyArtRenderer.Place(KeyArtRenderer.Visual("Tomato", "purePrefab"), stage, "FlyingTomato");
        tomato.position = stack.TransformPoint(TomatoX, top * TomatoY, 0f);
        tomato.localScale *= BurgerScale * 0.8f;
        tomato.rotation = Quaternion.Euler(-20f, 30f, 25f);

        var rim = new GameObject("Rim") { hideFlags = HideFlags.DontSave };
        rim.transform.SetParent(stage, false);
        rim.transform.rotation = Quaternion.Euler(25f, 150f, 0f);
        var rimLight = rim.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.color = new Color(1f, 0.85f, 0.6f);
        rimLight.intensity = 0.8f;
        rimLight.shadows = LightShadows.None;
        return top;
    }

    static Camera BuildCamera(Transform stage, GameObject model, float top)
    {
        var go = new GameObject("HeroCamera") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(stage, false);
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 24f;
        cam.aspect = 0.8f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.allowHDR = false;
        cam.allowMSAA = true;
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        data.renderShadows = false;

        var bounds = new Bounds(model.transform.position, Vector3.zero);
        foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.gameObject.activeInHierarchy))
            bounds.Encapsulate(PosedBounds(smr));
        bounds.Encapsulate(KeyArtRenderer.WorldBounds(stage.Find("Burger")));
        bounds.Encapsulate(KeyArtRenderer.WorldBounds(stage.Find("FlyingTomato")));
        var dir = Quaternion.Euler(CamPitch, 0f, 0f) * Vector3.forward;
        float half = Mathf.Max(bounds.extents.y, bounds.extents.x / cam.aspect);
        float distance = half / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / Framing + bounds.extents.z;
        go.transform.rotation = Quaternion.LookRotation(dir);
        go.transform.position = bounds.center - dir * distance;
        cam.enabled = false;
        return cam;
    }
}
