using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// How the cafe looks: who the chef and the regulars are, and the colour of the dining room floor.
// The "Preview" items only render pictures into Temp/ for a look; nothing in the project changes until
// the matching build step runs. See .claude/skills/chibi-burger-cafe-game-designer.
public static partial class CafeGrowthBuilder
{
    static readonly Vector3 LookStage = new Vector3(600f, -300f, 600f);
    const string RestMat = MatDir + "Rest.1.mat";
    const string Atlas = Game + "Graphics/Textures/texture 0.png";

    #region Chef candidates
    // Every adventurer on the chef's rig, in a toque, next to the skeleton he is today.
    public static readonly string[] ChefCandidates = { "", "Rogue", "Ranger", "Knight", "Mage", "Barbarian", "Rogue_Hooded" };

    [MenuItem("Tools/Chibi Cafe/L1 Preview Chef Candidates")]
    public static void PreviewChefs()
    {
        var stage = new GameObject("__LookStage") { hideFlags = HideFlags.DontSave };
        stage.transform.position = LookStage;
        AnimationMode.StartAnimationMode();
        try
        {
            var idle = AssetDatabase.LoadAllAssetsAtPath(SkullFbx).OfType<AnimationClip>().First(c => c.name == "Idle");
            AnimationMode.BeginSampling();
            for (int i = 0; i < ChefCandidates.Length; i++)
            {
                var chef = ChefLook(ChefCandidates[i], stage.transform);
                chef.transform.localPosition = new Vector3(i * 1.15f, 0f, 0f);
                chef.transform.localRotation = Quaternion.Euler(0f, 155f, 0f);
                AnimationMode.SampleAnimationClip(chef, idle, 0.2f);
            }
            AnimationMode.EndSampling();
            Shot(stage.transform, "Temp/look_chefs.png", 1960, 620, 10f);
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(stage);
        }
        Debug.Log("[CafeLook] Temp/look_chefs.png");
    }

    // Everyone in the cafe as they are now: the chef in his toque, then every customer.
    [MenuItem("Tools/Chibi Cafe/L1c Preview Everyone")]
    public static void PreviewEveryone()
    {
        var stage = new GameObject("__LookStage") { hideFlags = HideFlags.DontSave };
        stage.transform.position = LookStage;
        AnimationMode.StartAnimationMode();
        try
        {
            var idle = AssetDatabase.LoadAllAssetsAtPath(SkullFbx).OfType<AnimationClip>().First(c => c.name == "Idle");
            AnimationMode.BeginSampling();
            var chef = Object.Instantiate(GameObject.Find("Player/Model"), stage.transform);
            foreach (Transform hat in chef.transform.Find("Rig/root/hips/spine/chest/head"))
                if (hat.name.StartsWith("Hat_")) hat.gameObject.SetActive(hat.name == "Hat_Toque");
            // (body to pose, whole figure to place)
            var row = new List<(GameObject body, GameObject figure)> { (chef, chef) };
            var names = new List<string> { "Npc 1", "Npc 2", "Npc 3" };
            names.AddRange(Adventurers.Select(a => "Npc " + a.model));
            foreach (var name in names)
            {
                var npc = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(NpcDir + name + ".prefab"), stage.transform);
                npc.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                row.Add((npc.transform.Find("Model").gameObject, npc));
            }
            for (int i = 0; i < row.Count; i++)
            {
                var (body, figure) = row[i];
                figure.hideFlags = HideFlags.DontSave;
                foreach (var b in figure.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
                figure.transform.localPosition = new Vector3(i * 1.15f, 0f, i % 2 == 0 ? 0f : 0.3f);
                figure.transform.localRotation = Quaternion.Euler(0f, 155f, 0f);
                AnimationMode.SampleAnimationClip(body, idle, 0.2f);
            }
            AnimationMode.EndSampling();
            Shot(stage.transform, "Temp/look_everyone.png", 2400, 700, 8f);
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(stage);
        }
        Debug.Log("[CafeLook] Temp/look_everyone.png");
    }

    // The chef as he is now, bare-headed and then in each hat the shop sells.
    [MenuItem("Tools/Chibi Cafe/L1b Preview Chef Hats")]
    public static void PreviewChefHats()
    {
        var hats = new[] { "", "Hat_Toque", "Hat_Knight", "Hat_Wizard", "Hat_Bear" };
        var stage = new GameObject("__LookStage") { hideFlags = HideFlags.DontSave };
        stage.transform.position = LookStage;
        AnimationMode.StartAnimationMode();
        try
        {
            var idle = AssetDatabase.LoadAllAssetsAtPath(SkullFbx).OfType<AnimationClip>().First(c => c.name == "Idle");
            AnimationMode.BeginSampling();
            for (int i = 0; i < hats.Length; i++)
            {
                var copy = Object.Instantiate(GameObject.Find("Player/Model"), stage.transform);
                copy.hideFlags = HideFlags.DontSave;
                foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
                foreach (Transform hat in copy.transform.Find("Rig/root/hips/spine/chest/head"))
                    if (hat.name.StartsWith("Hat_")) hat.gameObject.SetActive(hat.name == hats[i]);
                copy.transform.localPosition = new Vector3(i * 1.15f, 0f, 0f);
                copy.transform.localRotation = Quaternion.Euler(0f, 155f, 0f);
                AnimationMode.SampleAnimationClip(copy, idle, 0.2f);
            }
            AnimationMode.EndSampling();
            Shot(stage.transform, "Temp/look_chef_hats.png", 1500, 640, 8f);
        }
        finally
        {
            AnimationMode.StopAnimationMode();
            Object.DestroyImmediate(stage);
        }
        Debug.Log("[CafeLook] Temp/look_chef_hats.png");
    }

    // A copy of the chef wearing `model`'s meshes (empty = his own skeleton), in a toque that fits that head.
    static GameObject ChefLook(string model, Transform parent)
    {
        var copy = Object.Instantiate(GameObject.Find("Player/Model"), parent);
        copy.name = string.IsNullOrEmpty(model) ? "Skeleton" : model;
        copy.hideFlags = HideFlags.DontSave;
        copy.transform.localScale = GameObject.Find("Player/Model").transform.lossyScale;
        foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);

        var head = copy.transform.Find("Rig/root/hips/spine/chest/head");
        foreach (Transform hat in head) if (hat.name.StartsWith("Hat_")) hat.gameObject.SetActive(false);
        var skull = HeadSpaceBounds(SkullFbx, "Skeleton_Warrior_Head");

        var (band, radius) = (0.63f, 1.02f);
        if (!string.IsNullOrEmpty(model))
        {
            Dress(copy, model, null, stripRig: false);
            skull = HeadSpaceBounds(AdvDir + model + ".fbx", HeadPiece(model));
            // An adventurer's head is wider and rounder than the skull: the hat sits higher and tighter,
            // so their hair still shows under it.
            (band, radius) = (0.74f, 0.78f);
        }

        // The toque, built for whichever head is under it.
        var toque = new GameObject("Hat_Toque_Preview");
        toque.transform.SetParent(head, false);
        toque.AddComponent<MeshFilter>().sharedMesh = BuildToque(skull, band, radius);
        toque.AddComponent<MeshRenderer>().sharedMaterials = new[]
        {
            AssetDatabase.LoadAssetAtPath<Material>(MatDir + "ChefToque.mat"),
            AssetDatabase.LoadAssetAtPath<Material>(MatDir + "ChefToqueBand.mat"),
        };
        return copy;
    }

    static string HeadPiece(string model) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(AdvDir + model + ".fbx")
            .GetComponentsInChildren<SkinnedMeshRenderer>(true).First(s => s.name.EndsWith("_Head")).name;

    // An adventurer's meshes on a skeleton rig (same bone names and rest pose, so the animations carry over),
    // in `material` if one is given. `stripRig` also clears what rides on the bones (a skeleton's own hood);
    // the chef's apron and hats ride there too, so his rig is left alone.
    static void Dress(GameObject model, string adventurer, Material material, bool stripRig)
    {
        var rig = model.transform.Find("Rig");
        // Only the old meshes go. Markers with nothing to draw stay: the chef's HamburgerHand (the spot his
        // food rides on) used to hang off the skeleton's arm mesh, and losing it broke carrying entirely.
        foreach (var child in model.transform.Cast<Transform>()
                     .Where(c => c != rig && c.GetComponentInChildren<Renderer>(true) != null).ToList())
            Object.DestroyImmediate(child.gameObject);
        if (stripRig)
            foreach (var prop in rig.GetComponentsInChildren<Renderer>(true).Select(r => r.gameObject).ToList())
                Object.DestroyImmediate(prop);

        var bones = rig.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
        var looks = new GameObject(adventurer).transform;
        looks.SetParent(model.transform, false);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(AdvDir + adventurer + ".fbx");
        foreach (var piece in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (NotWorn.Any(piece.name.Contains)) continue;
            var go = new GameObject(piece.name);
            go.transform.SetParent(looks, false);
            var skin = go.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = piece.sharedMesh;
            skin.sharedMaterial = material != null ? material : piece.sharedMaterial;
            skin.bones = piece.bones.Select(b => bones[b.name]).ToArray();
            skin.rootBone = bones[piece.rootBone.name];
            skin.localBounds = piece.localBounds;
            skin.shadowCastingMode = piece.shadowCastingMode;
        }
    }
    #endregion

    #region Hint colour
    // The colour things pulse in when they are what to tap next (HighlightController's _HintColor).
    public static readonly (string name, Color color)[] HintColors =
    {
        ("teal", UITokens.Colors.Teal),        // what it is today
        ("tomato", UITokens.Colors.Tomato),
        ("raspberry", new Color(0.878f, 0.271f, 0.482f)),
        ("pink", UITokens.Colors.Pink),
        ("berry", UITokens.Colors.Berry),
    };

    [MenuItem("Tools/Chibi Cafe/L2 Preview Hint Colours")]
    public static void PreviewHints()
    {
        var highlight = Object.FindFirstObjectByType<HighlightController>();
        var template = (Material)new SerializedObject(highlight).FindProperty("hintTemplate").objectReferenceValue;
        // What gets hinted with empty hands and nothing ready: the ingredient crates.
        var targets = Object.FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None)
            .SelectMany(s => s.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
        var originals = targets.Select(r => r.sharedMaterials).ToArray();
        var variants = targets.Select(r => r.sharedMaterials
            .Select(m => new Material(m) { shader = template.shader, hideFlags = HideFlags.DontSave }).ToArray()).ToArray();

        const int w = 700, h = 500;
        var sheet = new Texture2D(w * 3, h * 2, TextureFormat.RGB24, false);
        sheet.SetPixels(Enumerable.Repeat(UITokens.Colors.Ink, sheet.width * sheet.height).ToArray());
        // The game's own camera, pulled in on the crates: a copy renders without the scene's post-processing.
        var cam = Camera.main;
        var (where, size) = (cam.transform.position, cam.orthographicSize);
        try
        {
            for (int i = 0; i < targets.Length; i++) targets[i].sharedMaterials = variants[i];
            var bounds = targets[0].bounds;
            foreach (var r in targets) bounds.Encapsulate(r.bounds);
            cam.transform.position = bounds.center - cam.transform.forward * 60f;
            cam.orthographicSize = Mathf.Max(2.5f, bounds.extents.magnitude * 0.85f);
            Shader.SetGlobalFloat(Shader.PropertyToID("_HintAmount"), 0.9f);
            for (int i = 0; i < HintColors.Length; i++)
            {
                Shader.SetGlobalColor(Shader.PropertyToID("_HintColor"), HintColors[i].color);
                sheet.SetPixels((i % 3) * w, (1 - i / 3) * h, w, h, Shoot(cam, w, h));
            }
        }
        finally
        {
            Shader.SetGlobalFloat(Shader.PropertyToID("_HintAmount"), 0f);
            for (int i = 0; i < targets.Length; i++) targets[i].sharedMaterials = originals[i];
            foreach (var set in variants) foreach (var m in set) Object.DestroyImmediate(m);
            (cam.transform.position, cam.orthographicSize) = (where, size);
        }
        sheet.Apply();
        File.WriteAllBytes(Path.GetFullPath("Temp/look_hints.png"), sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
        Debug.Log("[CafeLook] Temp/look_hints.png");
    }
    #endregion

    // The Game view's own Mute Audio switch: play testing in the editor stays quiet, the build is untouched.
    [MenuItem("Tools/Chibi Cafe/Mute Editor Audio")]
    static void ToggleMute() => EditorUtility.audioMasterMute = !EditorUtility.audioMasterMute;

    [MenuItem("Tools/Chibi Cafe/Mute Editor Audio", true)]
    static bool ToggleMuteCheck()
    {
        Menu.SetChecked("Tools/Chibi Cafe/Mute Editor Audio", EditorUtility.audioMasterMute);
        return true;
    }

    // The kitchen as the player sees it, with the chef in each candidate look: contrast only means anything
    // in context. Skin and hair are part of it: the old skeleton read because he was pale, not because of
    // his clothes, and ink navy (tried next) was "too deep".
    public static readonly (string name, Color outfit)[] ChefLooks =
    {
        ("tomato", UITokens.Colors.Tomato),
        ("scarlet", new Color(0.82f, 0.25f, 0.25f)),
        ("mint", new Color(0.43f, 0.81f, 0.75f)),
    };

    [MenuItem("Tools/Chibi Cafe/L3 Preview Chef Contrast")]
    public static void PreviewChefContrast()
    {
        var chef = GameObject.Find("Player/Model");
        var skins = chef.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var was = skins[0].sharedMaterial;
        const int w = 820, h = 512;
        var sheet = new Texture2D(w * 3, h, TextureFormat.RGB24, false);
        // The game's own camera, pulled in on him: at the full view he is forty pixels tall and every
        // uniform looks the same.
        var cam = Camera.main;
        var (where, size) = (cam.transform.position, cam.orthographicSize);
        cam.transform.position = chef.transform.position - cam.transform.forward * 60f;
        cam.orthographicSize = 4.2f;
        try
        {
            for (int i = 0; i < ChefLooks.Length; i++)
            {
                var (name, color) = ChefLooks[i];
                var material = Tinted(ChefModel, "chef " + name, Green, color, ChefWhites, ChefSkin, ChefHair);
                foreach (var skin in skins) skin.sharedMaterial = material;
                sheet.SetPixels(i * w, 0, w, h, Shoot(Camera.main, w, h));
            }
        }
        finally
        {
            foreach (var skin in skins) skin.sharedMaterial = was;
            (cam.transform.position, cam.orthographicSize) = (where, size);
            // Each candidate made a texture and a material of its own; they were only ever for the picture.
            foreach (var (name, _) in ChefLooks)
            {
                AssetDatabase.DeleteAsset(Game + "Graphics/Textures/Characters/chef " + name + ".png");
                AssetDatabase.DeleteAsset(MatDir + "Character chef " + name + ".mat");
            }
        }
        sheet.Apply();
        File.WriteAllBytes(Path.GetFullPath("Temp/look_chef_contrast.png"), sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
        Debug.Log("[CafeLook] Temp/look_chef_contrast.png");
    }

    #region 8. The chef and the regulars
    // The chef is an adventurer body on his own rig, in the cafe's colours so he never reads as a customer:
    // a red cook's shirt under the white apron and the toque. Empty = the old skeleton (hats refit themselves).
    public const string ChefModel = "Rogue";

    // What he wears and what he looks like. It has to carry him: in a pink kitchen the old bone-white
    // skeleton read instantly and a salmon cook with brown hair did not. Navy fixed the contrast and was
    // rejected as "too deep", so he is pale instead: light skin, white hair, a bright uniform.
    public static Color ChefOutfit = new Color(0.82f, 0.25f, 0.25f);   // scarlet: tomato red was too close to the pink
    public static Color ChefSkin = new Color(0.95f, 0.86f, 0.80f);
    public static Color ChefHair = new Color(0.94f, 0.93f, 0.97f);
    const int SkinSwatch = 0, HairSwatch = 1;   // both on row 3 of the Rogue's palette sheet ("L0 Dump Character Swatches")

    // The hue the chef's clothes are dyed in, so a repaint knows what to catch: the Rogue wears green.
    const float Green = 0.42f;

    // Worn, not carried: a chef has no use for a quiver, and the apron is his cape.
    static readonly string[] NotWorn = { "Hat", "Helmet", "Quiver", "Cape" };

    // The adventurer's leather bracers, chest strap and metal studs can't be taken off: they are the arm
    // surface itself, in the same mesh as the sleeve and the hand, so deleting them would leave a hole
    // between sleeve and wrist. They are repainted instead — cream cuffs over a red sleeve read as a cook.
    // (Swatches of the palette sheet, from "L0 Dump Character Swatches".)
    static readonly (int x, int y)[] ChefWhites = { (5, 3), (7, 1), (3, 3) };

    [MenuItem("Tools/Chibi Cafe/8 Build Chef and Regulars")]
    public static void BuildPeople()
    {
        // Only the chef is re-dressed. The plain customers are the skeletons they always were: the user
        // asked for them back, and a cafe full of skeletons is the joke the game is built on.
        var chef = GameObject.Find("Player/Model");
        // The chef keeps what rides on his bones: the apron on his hips, the hats on his head.
        Dress(chef, ChefModel, Tinted(ChefModel, "chef", Green, ChefOutfit, ChefWhites, ChefSkin, ChefHair), stripRig: false);
        HoldPoint(chef);
        Apron(chef);
        Marker(chef);
        Face(chef);
        BuildHats();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(chef.scene);
        Debug.Log("[CafeLook] chef and regulars built");
    }

    // A ring on the floor under him. Whatever he is wearing, this is the thing that says "this one is
    // you" at the distance the game is actually played from.
    const float RingInner = 0.46f, RingOuter = 0.60f;   // outside the apron, which hangs low and wide

    static void Marker(GameObject chef)
    {
        var player = chef.transform.parent;
        var old = player.Find("Marker");
        var go = old != null ? old.gameObject : new GameObject("Marker");
        go.transform.SetParent(player, false);
        go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        go.transform.localRotation = Quaternion.identity;

        const int sides = 24;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides, b = (i + 1) * Mathf.PI * 2f / sides;
            Vector3 At(float angle, float r) => new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            int at = verts.Count;
            verts.Add(At(a, RingInner)); verts.Add(At(a, RingOuter));
            verts.Add(At(b, RingOuter)); verts.Add(At(b, RingInner));
            // Wound to face up, or the ring is culled away and nothing shows under him.
            tris.AddRange(new[] { at, at + 2, at + 1, at, at + 3, at + 2 });
        }
        var mesh = new Mesh { name = "PlayerMarker" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(verts.Select(_ => Vector3.up).ToList());
        mesh.RecalculateBounds();
        Get<MeshFilter>(go).sharedMesh = SaveMesh(mesh);
        var renderer = Get<MeshRenderer>(go);
        renderer.sharedMaterial = CafeGrowthBuilder.FlatMaterial("PlayerMarker", UITokens.Colors.Yellow);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // The apron hangs on the hips bone, right against the body, and the adventurer's belt buckle came
    // straight through the front of it. Pushing the apron a little away from his belly hides the buckle
    // without touching the body mesh (the apron only covers his front, so there is nothing to open up
    // behind). Set absolutely, from where ApronOnHips leaves it, so re-running does not add up.
    static readonly Vector3 ApronAt = new Vector3(0f, -0.4057f, 0f);
    const float ApronPush = 0.035f;
    static readonly Vector3 ApronSize = new Vector3(1.13f, 1f, 1.13f);

    static void Apron(GameObject chef)
    {
        var apron = chef.GetComponentsInChildren<Transform>(true).First(t => t.name == "Kitchen Apron");
        var forward = apron.parent.InverseTransformDirection(chef.transform.forward).normalized;
        apron.localPosition = ApronAt + forward * ApronPush;
        apron.localScale = ApronSize;
    }

    #region His face
    // KayKit heads have eyes and brows and nothing under them. These are his mouths, built flat against the
    // front of his face in the very colour his eyes are painted (sampled from his own palette sheet), one
    // shown at a time by ChefFace.
    const float MouthDrop = 0.19f;    // how far under the eyes it sits, in head-bone units
    const float MouthLift = 0.012f;
    const float NoseBottom = 0.21f;   // in the head bone's space: above this is nose, not face   // off the skin, so it never z-fights
    const float MouthWide = 0.19f;
    const float MouthThick = 0.05f;

    static void Face(GameObject chef)
    {
        var head = chef.transform.Find("Rig/root/hips/spine/chest/head");
        foreach (var old in head.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Mouth_")).ToList())
            Object.DestroyImmediate(old.gameObject);

        var piece = Skinned(AdvDir + ChefModel + ".fbx", HeadPiece(ChefModel));
        var material = CafeGrowthBuilder.FlatMaterial("ChefFace", EyeColor(piece));
        GameObject[] mouths;
        // Smile, grin, shock, frown - the order ChefFace.Look stores them in.
        using (var face = MouthSpot(piece))
            mouths = new[]
            {
                Mouth("Mouth_Smile", Curve(0.06f, MouthWide, MouthThick), head, face, material),
                Mouth("Mouth_Grin", Open(), head, face, material),
                Mouth("Mouth_Shock", Ellipse(0.05f, 0.062f), head, face, material),
                Mouth("Mouth_Frown", Curve(-0.06f, 0.14f, 0.033f), head, face, material),
            };

        var chefFace = Get<ChefFace>(chef.transform.parent.gameObject);
        var so = new SerializedObject(chefFace);
        var list = so.FindProperty("mouths");
        list.arraySize = mouths.Length;
        for (int i = 0; i < mouths.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = mouths[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static GameObject Mouth(string name, Mesh mesh, Transform head, FaceSpot face, Material material)
    {
        mesh.name = name;
        Bend(mesh, face);
        var go = new GameObject(name);
        go.transform.SetParent(head, false);
        go.transform.localPosition = new Vector3(face.At.x, face.At.y, 0f);
        Get<MeshFilter>(go).sharedMesh = SaveMesh(mesh);
        var renderer = Get<MeshRenderer>(go);
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        go.SetActive(name.EndsWith("Smile"));
        return go;
    }

    // A mouth drawn flat sinks into his cheeks at the corners and floats off his chin at the bottom, so
    // every vertex is laid on his actual skin: his head is dropped into a collider of its own, far from
    // anything else in the scene, and each point of the mouth is a ray fired at his face.
    public class FaceSpot : System.IDisposable
    {
        static readonly Vector3 Away = new Vector3(900f, -300f, 900f);   // nothing else is out here

        public readonly Vector2 At;        // where the mouth sits, in the head bone's space
        private readonly GameObject probe;
        private readonly MeshCollider collider;

        public FaceSpot(Vector2 at, Mesh posed)
        {
            At = at;
            probe = new GameObject("__FaceProbe") { hideFlags = HideFlags.HideAndDontSave };
            probe.transform.position = Away;
            collider = probe.AddComponent<MeshCollider>();
            collider.sharedMesh = posed;
            Physics.SyncTransforms();
        }

        // How far forward his face is at (x, y), or NaN where there is no face.
        public float SurfaceZ(float x, float y)
        {
            var ray = new Ray(Away + new Vector3(x, y, 3f), Vector3.back);
            return collider.Raycast(ray, out var hit, 6f) ? hit.point.z - Away.z + MouthLift : float.NaN;
        }

        public void Dispose() => Object.DestroyImmediate(probe);
    }

    static void Bend(Mesh mesh, FaceSpot face)
    {
        var verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            float z = face.SurfaceZ(face.At.x + verts[i].x, face.At.y + verts[i].y);
            // A corner that misses his face (past his cheek) keeps the depth of the middle of the mouth.
            verts[i].z = float.IsNaN(z) ? face.SurfaceZ(face.At.x, face.At.y) : z;
        }
        mesh.SetVertices(verts);
        // One flat shade for the whole mouth: it is a drawing on his face, not a bulge.
        mesh.SetNormals(verts.Select(_ => Vector3.forward).ToList());
        mesh.RecalculateBounds();
    }

    // The colour his eyes are actually painted, read off his own sheet at the eyes' own texture coordinate.
    static Color EyeColor(SkinnedMeshRenderer head)
    {
        var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        sheet.LoadImage(File.ReadAllBytes(Path.GetFullPath(Game + "Graphics/Textures/Characters/chef.png")));
        var uv = head.sharedMesh.uv.Where(u => (int)(u.x * 8) == 2 && (int)(u.y * 4) == 3).ToArray();
        var color = sheet.GetPixelBilinear(uv.Average(u => u.x), uv.Average(u => u.y));
        Object.DestroyImmediate(sheet);
        return color;
    }

    // His head in its bind pose, in the head bone's space, and the mouth's place on it: under his eyes.
    static FaceSpot MouthSpot(SkinnedMeshRenderer head)
    {
        var mesh = head.sharedMesh;
        var bind = mesh.bindposes[System.Array.FindIndex(head.bones, b => b.name == "head")];
        var verts = mesh.vertices.Select(v => bind.MultiplyPoint3x4(v)).ToArray();
        var uv = mesh.uv;

        var eyes = new Bounds();
        bool any = false;
        for (int i = 0; i < uv.Length; i++)
        {
            if ((int)(uv[i].x * 8) != 2 || (int)(uv[i].y * 4) != 3) continue;
            if (!any) { eyes = new Bounds(verts[i], Vector3.zero); any = true; } else eyes.Encapsulate(verts[i]);
        }

        var posed = new Mesh { hideFlags = HideFlags.HideAndDontSave };
        posed.SetVertices(verts);
        posed.SetTriangles(mesh.triangles, 0);
        return new FaceSpot(new Vector2(eyes.center.x, eyes.center.y - MouthDrop), posed);
    }
    #endregion

    #region Mouth shapes
    // A line through `points` with some thickness. The corners share one averaged normal, so the quads
    // meet cleanly: offsetting each segment on its own left little gaps along the outside of the curve,
    // and the smile came out looking like a comb.
    static Mesh Stroke(Vector2[] points, float thickness)
    {
        var verts = new List<Vector3>();
        var tris = new List<int>();
        for (int i = 0; i < points.Length; i++)
        {
            var dir = i == 0 ? points[1] - points[0]
                    : i == points.Length - 1 ? points[i] - points[i - 1]
                    : points[i + 1] - points[i - 1];
            var n = Vector2.Perpendicular(dir).normalized * (thickness * 0.5f);
            verts.Add(new Vector3(points[i].x - n.x, points[i].y - n.y, 0f));
            verts.Add(new Vector3(points[i].x + n.x, points[i].y + n.y, 0f));
        }
        for (int i = 0; i + 1 < points.Length; i++)
        {
            int at = i * 2;
            tris.AddRange(new[] { at, at + 3, at + 1, at, at + 2, at + 3 });
        }
        return Flat(verts, tris);
    }

    // `sag` is how far the middle drops: positive smiles, negative frowns. A frown wants to be shorter and
    // finer than a smile, or the arc reads as a moustache.
    static Mesh Curve(float sag, float wide, float thick)
    {
        const int steps = 8;
        var points = new Vector2[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps * 2f - 1f;          // -1 to 1 across the mouth
            // Centred on the mouth line, so a frown curls up without climbing into his nose.
            points[i] = new Vector2(t * wide * 0.5f, sag * (0.5f - (1f - t * t)));
        }
        return Stroke(points, thick);
    }

    // An open mouth: a straight top lip and a round bottom, like a laugh.
    static Mesh Open()
    {
        const int steps = 12;
        float rx = MouthWide * 0.45f, ry = MouthWide * 0.5f;
        var verts = new List<Vector3> { Vector3.zero };
        var tris = new List<int>();
        for (int i = 0; i <= steps; i++)
        {
            float a = Mathf.PI + i * Mathf.PI / steps;      // the lower half, left to right
            verts.Add(new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f));
            if (i > 0) tris.AddRange(new[] { 0, i, i + 1 });
        }
        return Flat(verts, tris);
    }

    static Mesh Ellipse(float rx, float ry)
    {
        const int steps = 14;
        var verts = new List<Vector3> { Vector3.zero };
        var tris = new List<int>();
        for (int i = 0; i <= steps; i++)
        {
            float a = i * Mathf.PI * 2f / steps;
            verts.Add(new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0f));
            if (i > 0) tris.AddRange(new[] { 0, i, i + 1 });
        }
        return Flat(verts, tris);
    }

    // Face-on: the head bone's +z is out of his face, so the shapes lie in its xy plane.
    static Mesh Flat(List<Vector3> verts, List<int> tris)
    {
        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(verts.Select(_ => Vector3.forward).ToList());
        mesh.RecalculateBounds();
        return mesh;
    }
    #endregion

    // Where the chef's food rides: an empty marker in front of him, a fixed spot in the model's own space
    // (CarryWobble leans it; PlayerFSM.holdParent parents the food to it). It is tagged Player, which is how
    // IngredientsSource finds where a new ingredient should appear.
    static readonly Vector3 HandAt = new Vector3(0.865f, 1.087f, 0.134f);

    static void HoldPoint(GameObject chef)
    {
        var hand = chef.transform.Find("HamburgerHand");
        if (hand == null)
        {
            hand = new GameObject("HamburgerHand").transform;
            hand.SetParent(chef.transform, false);
        }
        hand.localPosition = HandAt;
        hand.localRotation = Quaternion.identity;
        hand.localScale = Vector3.one;
        hand.gameObject.tag = "Player";
        var fsm = new SerializedObject(chef.GetComponentInParent<PlayerFSM>());
        fsm.FindProperty("holdParent").objectReferenceValue = hand.gameObject;
        fsm.ApplyModifiedPropertiesWithoutUndo();
    }

    // That character's own palette sheet with everything dyed `hue` repainted in `outfit`, plus a material
    // using it. Only the clothes change: skin, hair and leather are nowhere near that hue.
    static Material Tinted(string model, string name, float hue, Color outfit, (int x, int y)[] whites = null,
                           Color? skin = null, Color? hair = null)
    {
        var body = Skinned(AdvDir + model + ".fbx", BodyPiece(model));
        string texPath = Game + "Graphics/Textures/Characters/" + name + ".png";
        string matPath = MatDir + "Character " + name + ".mat";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(texPath)));

        var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        sheet.LoadImage(File.ReadAllBytes(Path.GetFullPath(AssetDatabase.GetAssetPath(body.sharedMaterial.mainTexture))));
        Dye(sheet, hue, outfit);
        if (whites != null) foreach (var cell in whites) Repaint(sheet, cell, UITokens.Colors.Cream);
        if (skin.HasValue) Repaint(sheet, (SkinSwatch, 3), skin.Value);
        if (hair.HasValue) Repaint(sheet, (HairSwatch, 3), hair.Value);
        File.WriteAllBytes(Path.GetFullPath(texPath), sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceSynchronousImport);

        var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (material == null)
        {
            material = new Material(body.sharedMaterial);
            AssetDatabase.CreateAsset(material, matPath);
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        EditorUtility.SetDirty(material);
        return material;
    }

    static string BodyPiece(string model) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(AdvDir + model + ".fbx")
            .GetComponentsInChildren<SkinnedMeshRenderer>(true).First(s => s.name.EndsWith("_Body")).name;
    #endregion

    // Which swatch of the palette sheet each piece of a character is painted from, and how much of it.
    [MenuItem("Tools/Chibi Cafe/L0 Dump Character Swatches")]
    static void DumpSwatches()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(AdvDir + ChefModel + ".fbx");
        var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var first = source.GetComponentInChildren<SkinnedMeshRenderer>(true);
        sheet.LoadImage(File.ReadAllBytes(Path.GetFullPath(AssetDatabase.GetAssetPath(first.sharedMaterial.mainTexture))));
        int w = sheet.width / 8, h = sheet.height / 4;

        foreach (var piece in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = piece.sharedMesh;
            var uv = mesh.uv;
            var count = new Dictionary<(int, int), int>();
            foreach (var t in mesh.triangles)
            {
                var cell = (Mathf.Clamp((int)(uv[t].x * 8), 0, 7), Mathf.Clamp((int)(uv[t].y * 4), 0, 3));
                count[cell] = count.GetValueOrDefault(cell) + 1;
            }
            var lines = count.OrderByDescending(e => e.Value).Select(e =>
            {
                var px = sheet.GetPixels(e.Key.Item1 * w, e.Key.Item2 * h, w, h);
                var mean = new Color(px.Average(c => c.r), px.Average(c => c.g), px.Average(c => c.b));
                return $"({e.Key.Item1},{e.Key.Item2}) #{ColorUtility.ToHtmlStringRGB(mean)} x{e.Value / 3}";
            });
            Debug.Log(piece.name + ": " + string.Join("  ", lines));
        }
        Object.DestroyImmediate(sheet);
    }

    // Where the chef's eyes and skin sit in his head bone's space, so a mouth can be placed against them.
    [MenuItem("Tools/Chibi Cafe/L0 Dump Face")]
    static void DumpFace()
    {
        var head = Skinned(AdvDir + ChefModel + ".fbx", HeadPiece(ChefModel));
        var mesh = head.sharedMesh;
        var bind = mesh.bindposes[System.Array.FindIndex(head.bones, b => b.name == "head")];
        var uv = mesh.uv;
        foreach (var (name, cell) in new[] { ("eyes", (2, 3)), ("skin", (0, 3)), ("hair", (1, 3)) })
        {
            bool any = false;
            var b = new Bounds();
            for (int i = 0; i < uv.Length; i++)
            {
                if ((int)(uv[i].x * 8) != cell.Item1 || (int)(uv[i].y * 4) != cell.Item2) continue;
                var p = bind.MultiplyPoint3x4(mesh.vertices[i]);
                if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
            }
            Debug.Log($"[face] {name} center={b.center} size={b.size}");
        }
        Debug.Log($"[face] whole head {ChefHeadBounds()}");

        // The front of his face across the mouth, to see how far it curves away from the middle.
        var verts = mesh.vertices.Select(v => bind.MultiplyPoint3x4(v)).ToArray();
        for (float y = 0.30f; y >= 0.10f; y -= 0.05f)
        {
            var row = new System.Text.StringBuilder($"[face] y={y:0.00} ");
            for (float x = -0.18f; x <= 0.181f; x += 0.045f)
            {
                var near = verts.Where((v, i) => (int)(uv[i].x * 8) == 0 && (int)(uv[i].y * 4) == 3
                                                 && Mathf.Abs(v.x - x) < 0.05f && Mathf.Abs(v.y - y) < 0.06f).ToArray();
                row.Append($"x={x:0.00}:{(near.Length == 0 ? "-" : near.Max(v => v.z).ToString("0.000"))} ");
            }
            Debug.Log(row.ToString());
        }
    }

    #region Palette
    // KayKit models are painted from a sheet of flat gradients, so one colour can be redyed everywhere it
    // appears without touching the rest of the character.
    const float HueWindow = 0.09f;   // how far from the dyed hue still counts as that colour

    // Repaints one swatch of the 8x4 sheet in `to`, keeping its own light and shade.
    static void Repaint(Texture2D sheet, (int x, int y) cell, Color to)
    {
        int w = sheet.width / 8, h = sheet.height / 4;
        var px = sheet.GetPixels(cell.x * w, cell.y * h, w, h);
        float mean = px.Average(c => c.maxColorComponent);
        Color.RGBToHSV(to, out float hue, out float saturation, out float value);
        for (int i = 0; i < px.Length; i++)
            px[i] = Color.HSVToRGB(hue, saturation, Mathf.Clamp01(px[i].maxColorComponent / mean * value));
        sheet.SetPixels(cell.x * w, cell.y * h, w, h, px);
        sheet.Apply();
    }

    // Repaints every pixel within HueWindow of `hue` in `to`, keeping each one's own light and shade.
    static void Dye(Texture2D sheet, float hue, Color to)
    {
        var px = sheet.GetPixels();
        var hit = new List<int>();
        for (int i = 0; i < px.Length; i++)
        {
            Color.RGBToHSV(px[i], out float h, out float s, out _);
            if (s > 0.2f && Mathf.Abs(Mathf.DeltaAngle(h * 360f, hue * 360f)) < HueWindow * 360f) hit.Add(i);
        }
        if (hit.Count == 0) throw new System.Exception($"nothing at hue {hue} to dye");
        // Scaled around the average of what was dyed, so the cloth reads as the colour that was asked for
        // and keeps its own shading above and below it.
        float mean = hit.Average(i => px[i].maxColorComponent);
        Color.RGBToHSV(to, out float toHue, out float toSaturation, out float toValue);
        foreach (int i in hit)
            px[i] = Color.HSVToRGB(toHue, toSaturation, Mathf.Clamp01(px[i].maxColorComponent / mean * toValue));
        sheet.SetPixels(px);
        sheet.Apply();
    }
    #endregion

    #region Pictures
    // The row on the stage, straight on and a little above, over the cafe's pink.
    static void Shot(Transform stage, string path, int width, int height, float pitch)
    {
        var go = new GameObject("LookCamera") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(stage, false);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = UITokens.Colors.Pink;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.enabled = false;
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;

        var bounds = RendererBounds(stage);
        go.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
        go.transform.position = bounds.center - go.transform.forward * 60f;
        float half = 0f;
        for (int i = 0; i < 8; i++)
        {
            var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var local = go.transform.InverseTransformPoint(corner);
            half = Mathf.Max(half, Mathf.Abs(local.y), Mathf.Abs(local.x) * height / (float)width);
        }
        cam.orthographicSize = half * 1.1f;
        cam.aspect = width / (float)height;
        Save(cam, path, width, height);
    }

    static void Save(Camera cam, string path, int width, int height)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.SetPixels(Shoot(cam, width, height));
        tex.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static Color[] Shoot(Camera cam, int width, int height)
    {
        var rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { msaaSamples = 8, sRGB = true });
        var (target, aspect) = (cam.targetTexture, cam.aspect);
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = target;
        cam.aspect = aspect;
        RenderTexture.ReleaseTemporary(rt);
        var px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        return px;
    }
    #endregion
}
