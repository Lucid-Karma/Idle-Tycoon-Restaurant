using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Renders the Chibi Burger Cafe key art from the game's own models: the chef sprinting with a "Chaos
// Burger" held out in both hands — raw bun, burnt patty, a WHOLE tomato, lettuce, a WHOLE onion, a cheese
// brick and the top bun, teetering, a leaf flying off (see .claude/skills/chibi-burger-cafe-game-designer).
// Output: Graphics/Sprites/KeyArt/keyart_rush.png with a transparent background (matted from a black and
// a white render), used by the title screen and the store covers.
// Menu: Tools/Chibi UI/Render Key Art. The stage is built far below the level and removed afterwards.
public static class KeyArtRenderer
{
    const string Game = "Assets/Project/[GAME]/";
    const string ChefFbx = "Assets/Models/KayKit_Skeletons_1.0_FREE/characters/fbx/Skeleton_Warrior.fbx";
    public const string OutDir = Game + "Graphics/Sprites/KeyArt/";
    static readonly Vector3 StageOrigin = new Vector3(400f, -300f, 400f);

    // Pose: legs from a run cycle, arms from a two-handed "holding something out" clip.
    public static string LegsClip = "Running_B";
    public static float LegsTime = 0.5f;
    public static float StackLean = 12f;    // how far the tower leans back, degrees
    public static string ArmsClip = null;   // null = keep the run cycle's pumping arms
    public static float ArmsTime = 0.5f;
    public static float ChefYaw = 112f;     // 90 = pure profile running to screen-right
    public static float ChefLean = 10f;     // forward lean of the whole body, degrees
    public static float Framing = 0.9f;     // camera distance factor (1 = bounding sphere just fits)

    [MenuItem("Tools/Chibi UI/Render Key Art")]
    public static void RenderKeyArt() => Render(OutDir + "keyart_rush.png", 1400, 1400);

    // Contact sheet of arm poses (Temp/keyart_arms_*.png), to pick ArmsClip.
    [MenuItem("Tools/Chibi UI/Render Key Art Arm Sheet")]
    public static void RenderArmSheet()
    {
        var (savedClip, savedTime, savedLegs) = (ArmsClip, ArmsTime, LegsTime);
        ArmsClip = null;
        for (int i = 0; i < 8; i++)
        {
            LegsTime = i / 8f;
            Render($"Temp/keyart_arms_{i}.png", 400, 400, false);
        }
        (ArmsClip, ArmsTime, LegsTime) = (savedClip, savedTime, savedLegs);
    }

    public static void Render(string path, int width, int height, bool import = true)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var stage = new GameObject("__KeyArtStage") { hideFlags = HideFlags.DontSave };
        stage.transform.position = StageOrigin;
        // Clips are sampled in animation mode (AnimationClip.SampleAnimation left the rig in its bind pose).
        AnimationMode.StartAnimationMode();
        try
        {
            BuildSubject(stage.transform);
            var cam = BuildCamera(stage.transform, width / (float)height);
            File.WriteAllBytes(path, RenderMatted(cam, width, height));
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(stage);
        }
        if (!import) return;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 1024;
        imp.textureCompression = TextureImporterCompression.CompressedHQ;
        imp.SaveAndReimport();
        Debug.Log("[KeyArt] rendered " + path);
    }

    #region Stage
    static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAllAssetsAtPath(ChefFbx).OfType<AnimationClip>().FirstOrDefault(c => c.name == name);

    static GameObject Visual(string foodPrefab, string field)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Game + "Prefabs/FoodPrefabs/" + foodPrefab + ".prefab");
        var food = prefab.GetComponent<EdibleBase>();
        return (GameObject)new SerializedObject(food).FindProperty(field).objectReferenceValue;
    }

    static Transform Place(GameObject visual, Transform parent, string name)
    {
        var go = Object.Instantiate(visual, parent);
        go.name = name;
        go.hideFlags = HideFlags.DontSave;
        foreach (var behaviour in go.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        foreach (var animator in go.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
        return go.transform;
    }

    static Bounds WorldBounds(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)).ToArray();
        var b = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    // Tight bounds of a layer's meshes in the stack's own space (world AABBs of tilted, flat pieces are
    // much taller than the pieces, which left gaps in the tower).
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

    // World bounds of a skinned mesh in its current pose (its renderer bounds don't follow edit-mode poses).
    static Bounds PosedBounds(SkinnedMeshRenderer smr)
    {
        var mesh = new Mesh();
        smr.BakeMesh(mesh, true);
        var vertices = mesh.vertices;
        var b = new Bounds(smr.transform.position + smr.transform.rotation * vertices[0], Vector3.zero);
        foreach (var v in vertices) b.Encapsulate(smr.transform.position + smr.transform.rotation * v);
        Object.DestroyImmediate(mesh);
        return b;
    }

    static Transform Bone(GameObject root, string name) =>
        root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

    static void BuildSubject(Transform stage)
    {
        // The chef, three-quarters towards the camera, sprinting to screen-right.
        var player = Object.FindFirstObjectByType<PlayerFSM>();
        var source = player.transform.Find("Model");
        var model = Object.Instantiate(source.gameObject, stage);
        model.name = "Chef";
        model.hideFlags = HideFlags.DontSave;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(0f, ChefYaw, 0f);
        model.transform.localScale = source.lossyScale;
        foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(behaviour);
        var apron = model.transform.Find("Kitchen Apron");
        if (apron != null) apron.gameObject.SetActive(false);

        // Legs from the run, arms (and their chain) from a two-handed hold.
        var legs = Clip(LegsClip);
        var arms = string.IsNullOrEmpty(ArmsClip) ? null : Clip(ArmsClip);
        AnimationMode.BeginSampling();
        AnimationMode.SampleAnimationClip(model, legs, legs.length * LegsTime);
        GameObject probe = null;
        if (arms != null)
        {
            probe = Object.Instantiate(model, stage);
            probe.hideFlags = HideFlags.DontSave;
            AnimationMode.SampleAnimationClip(probe, arms, arms.length * ArmsTime);
        }
        AnimationMode.EndSampling();
        if (probe != null)
        {
            foreach (var name in new[] { "upperarm.l", "lowerarm.l", "hand.l", "upperarm.r", "lowerarm.r", "hand.r" })
                Bone(model, name).localRotation = Bone(probe, name).localRotation;
            Object.DestroyImmediate(probe);
        }
        model.transform.localRotation = Quaternion.Euler(0f, ChefYaw, 0f) * Quaternion.Euler(ChefLean, 0f, 0f);

        // Balanced on his head (chibi arms can't reach over that big skull), leaning back from the sprint.
        var head = PosedBounds(model.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name.Contains("Head")));
        var stack = new GameObject("ChaosBurger").transform;
        stack.SetParent(stage, false);
        stack.position = new Vector3(head.center.x, head.max.y - 0.04f, head.center.z);
        // Leaning back from the sprint (stack space: +z is his running direction).
        stack.rotation = Quaternion.Euler(0f, ChefYaw, 0f) * Quaternion.Euler(-StackLean, 0f, 0f);

        // Tilts are in the stack's space (x pitches towards his back, z rolls sideways); shifts move each
        // layer along (sideways, back-and-forth) so the tower bends like it's about to go.
        var layers = new (string prefab, string field, Vector3 tilt, Vector2 shift, float settle)[]
        {
            ("Bun", "bunSlice", new Vector3(0, 0, 0), Vector2.zero, 0.8f),
            ("Uncooked_Burger", "overcookedBurger", new Vector3(-6, 20, 5), new Vector2(0.03f, -0.04f), 0.8f),
            ("Tomato", "purePrefab", new Vector3(-10, 40, -8), new Vector2(-0.04f, -0.10f), 0.78f),
            ("Lettuce", "purePrefab", new Vector3(-14, -25, 10), new Vector2(0.02f, -0.16f), 0.2f),
            ("Onion", "purePrefab", new Vector3(-12, 70, 14), new Vector2(0.06f, -0.20f), 0.78f),
            ("Cheese", "purePrefab", new Vector3(-20, -35, -10), new Vector2(-0.02f, -0.24f), 0.8f),
            ("Hamburger", "finishBun", new Vector3(-28, 15, 16), new Vector2(0.08f, -0.26f), 0.8f),
        };
        float top = 0f;
        foreach (var (prefab, field, tilt, shift, settle) in layers)
        {
            var layer = Place(Visual(prefab, field), stack, prefab);
            layer.localRotation = Quaternion.Euler(tilt);
            layer.localPosition = Vector3.zero;
            var b = LocalBounds(layer, stack);
            layer.localPosition = new Vector3(shift.x - b.center.x, top - b.min.y, shift.y - b.center.z);
            top += b.size.y * settle;
        }

        // A lettuce leaf flying off behind him, nearly edge-on so it reads as a leaf.
        var leaf = Place(Visual("Lettuce", "purePrefab"), stage, "FlyingLettuce");
        leaf.position = stack.TransformPoint(0f, top * 0.8f, -0.9f);
        leaf.rotation = Quaternion.Euler(55f, 30f, 70f);
        leaf.localScale *= 0.8f;

        // Teal rim from behind, like the kitchen's neon: separates him from any background.
        var rim = new GameObject("Rim") { hideFlags = HideFlags.DontSave };
        rim.transform.SetParent(stage, false);
        rim.transform.rotation = Quaternion.Euler(20f, -150f, 0f);
        var rimLight = rim.AddComponent<Light>();
        rimLight.type = LightType.Directional;
        rimLight.color = new Color(0.45f, 0.95f, 0.95f);
        rimLight.intensity = 0.9f;
        rimLight.shadows = LightShadows.None;
    }

    static Camera BuildCamera(Transform stage, float aspect)
    {
        var go = new GameObject("KeyArtCamera") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(stage, false);
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 26f;
        cam.aspect = aspect;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.allowHDR = false;
        cam.allowMSAA = true;
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        data.renderShadows = false;

        // Slightly low angle: the hero shot, framed on the chef and his tower (flying bits may crop).
        var bounds = WorldBounds(stage.Find("Chef"));
        bounds.Encapsulate(WorldBounds(stage.Find("ChaosBurger")));
        var dir = Quaternion.Euler(-4f, 0f, 0f) * Vector3.forward;
        float radius = bounds.extents.magnitude;
        float distance = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * Framing;
        go.transform.rotation = Quaternion.LookRotation(dir);
        go.transform.position = bounds.center - dir * distance;
        cam.enabled = false;
        return cam;
    }
    #endregion

    #region Render
    // Two renders over black and white give exact coverage (alpha) without relying on the pipeline's
    // alpha output: alpha = 1 - (white - black), colour = black / alpha, all in linear space.
    static byte[] RenderMatted(Camera cam, int width, int height)
    {
        var black = RenderOver(cam, width, height, Color.black);
        var white = RenderOver(cam, width, height, Color.white);
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var outPx = new Color32[black.Length];
        for (int i = 0; i < black.Length; i++)
        {
            Color b = black[i].linear, w = white[i].linear;
            float a = Mathf.Clamp01(1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f);
            Color c = a > 0.004f ? new Color(b.r / a, b.g / a, b.b / a) : Color.black;
            c = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b)).gamma;
            outPx[i] = new Color(c.r, c.g, c.b, a);
        }
        result.SetPixels32(outPx);

        // Trim to the subject (plus a small margin) so layouts can size it by its real extent.
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (outPx[y * width + x].a > 3)
                {
                    minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                }
        int margin = Mathf.RoundToInt(Mathf.Max(width, height) * 0.02f);
        minX = Mathf.Max(0, minX - margin); minY = Mathf.Max(0, minY - margin);
        maxX = Mathf.Min(width - 1, maxX + margin); maxY = Mathf.Min(height - 1, maxY + margin);
        var trimmed = new Texture2D(maxX - minX + 1, maxY - minY + 1, TextureFormat.RGBA32, false);
        trimmed.SetPixels(result.GetPixels(minX, minY, trimmed.width, trimmed.height));
        var png = trimmed.EncodeToPNG();
        Object.DestroyImmediate(result);
        Object.DestroyImmediate(trimmed);
        return png;
    }

    static Color[] RenderOver(Camera cam, int width, int height, Color background)
    {
        var rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { msaaSamples = 8, sRGB = true });
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        var px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        return px;
    }
    #endregion
}
