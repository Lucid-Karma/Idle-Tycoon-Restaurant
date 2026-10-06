using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Step 10: the first-shift lesson (Tutorial, UICoachCard).
// - Mochi's card along the bottom of the HUD, with his face rendered from the waiter himself.
// - A yellow arrow that bobs over whatever the lesson is about, built here like the toque.
// - "How to play" on the Help screen starts the lesson again.
public static partial class CafeGrowthBuilder
{
    const string CoachFace = Game + "Graphics/Sprites/UI/mochi_face.png";

    [MenuItem("Tools/Chibi Cafe/10 Build the Tutorial")]
    public static void BuildTutorial()
    {
        RenderCoachFace();
        var canvas = GameObject.Find(Canvas).transform;
        var hud = canvas.Find("InGamePanel/HUD");

        var old = hud.Find("Coach");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var card = BuildCard(hud);

        var controllers = GameObject.Find("<<<Controllers>>>");
        var arrow = BuildArrow(controllers.transform);
        var tutorial = Get<Tutorial>(controllers);
        var so = new SerializedObject(tutorial);
        so.FindProperty("card").objectReferenceValue = card;
        so.FindProperty("arrow").objectReferenceValue = arrow;
        // Written here rather than left to the field defaults: the values are already serialised on the
        // scene object, so changing a default in the script alone does nothing.
        so.FindProperty("arrowGap").floatValue = 0.12f;          // air between the arrow's TIP and the top of the thing
        so.FindProperty("arrowBob").floatValue = 0.22f;
        so.FindProperty("aboveCustomer").floatValue = 1.05f;     // a customer's order bubble floats over their head
        so.ApplyModifiedPropertiesWithoutUndo();

        // The Help screen can run the lesson again.
        var help = canvas.Find("HelpPanel/HelpCard");
        var again = help.Find("TeachMeBTN");
        if (again == null)
        {
            var source = canvas.Find("HelpPanel/HelpCard/CloseBTN") ?? canvas.Find("HelpPanel/HelpCard").GetComponentsInChildren<Button>(true).First().transform;
            again = Object.Instantiate(source.gameObject, help).transform;
            again.name = "TeachMeBTN";
        }
        // Beside "Back to kitchen", as the second button of a normal footer row. (Up on the title's line
        // it read as something bolted on; centred along the bottom it sat on top of the tip.) The tip is
        // shortened to make room, and its second half moves into the serving step where it belongs.
        var tip = (RectTransform)help.Find("Tip");
        tip.sizeDelta = new Vector2(400f, tip.sizeDelta.y);
        var tipLabel = (RectTransform)tip.Find("Label");
        tipLabel.sizeDelta = new Vector2(320f, tipLabel.sizeDelta.y);
        tipLabel.GetComponent<TMP_Text>().text = "Pink pulse = use it next.";

        var rt = (RectTransform)again;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-420f, 58f);
        rt.sizeDelta = new Vector2(320f, 80f);
        foreach (var text in again.GetComponentsInChildren<TMP_Text>(true))
        {
            text.text = "Show me again";
            text.color = UITokens.Colors.Ink;
        }
        // Secondary: "Back to kitchen" stays the one teal button on the card.
        Tint(again.Find("Face"), UITokens.Colors.SubtleFace);
        Tint(again.Find("Edge"), UITokens.Colors.SubtleEdge);
        Listen(again.GetComponent<Button>(), tutorial.Begin);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[CafeGrowth] built the tutorial");
    }

    static void Tint(Transform t, Color color)
    {
        if (t != null && t.TryGetComponent<Image>(out var image)) image.color = color;
    }

    #region The card
    static UICoachCard BuildCard(Transform hud)
    {
        var card = Rect("Coach", hud, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(860f, 190f));
        var group = Get<CanvasGroup>(card.gameObject);
        Get<Canvas>(card.gameObject);                      // its own canvas: it animates every frame
        Get<GraphicRaycaster>(card.gameObject);

        var shadow = Rect("Shadow", card, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(16f, 16f));
        Img(shadow.gameObject, "ui_rounded", UITokens.Colors.Shadow, sliced: true, ppu: 2.6f);
        var surface = Rect("Surface", card, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var face = Img(surface.gameObject, "ui_rounded", UITokens.Colors.WarmWhite, sliced: true, ppu: 2.6f);
        face.raycastTarget = true;                          // the whole card is the "tap to go on" target

        // Mochi, in a pink disc on the left.
        var disc = Rect("Disc", card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(105f, 0f), new Vector2(150f, 150f));
        Img(disc.gameObject, "ui_circle", UITokens.Colors.Pink);
        var mochi = Rect("Mochi", disc, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(136f, 136f));
        var portrait = Get<Image>(mochi.gameObject);
        // Freshly written a moment ago: ask for it again, or the sprite comes back null and the card
        // shows a plain white square where his face should be.
        AssetDatabase.ImportAsset(CoachFace, ImportAssetOptions.ForceSynchronousImport);
        portrait.sprite = AssetDatabase.LoadAllAssetsAtPath(CoachFace).OfType<Sprite>().FirstOrDefault();
        if (portrait.sprite == null) Debug.LogWarning("[CafeGrowth] no sprite at " + CoachFace);
        portrait.enabled = portrait.sprite != null;
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;

        var line = Rect("Line", card, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(90f, 8f), new Vector2(-230f, -36f));
        var text = Text(line, "", SemiBold, 40f, UITokens.Colors.Ink, TextAlignmentOptions.Left);

        var hint = Rect("TapHint", card, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 16f), new Vector2(260f, 40f));
        Text(hint, "tap to go on", Medium, 26f, UITokens.Colors.InkMuted, TextAlignmentOptions.Right);

        var coach = Get<UICoachCard>(card.gameObject);
        var so = new SerializedObject(coach);
        so.FindProperty("group").objectReferenceValue = group;
        so.FindProperty("line").objectReferenceValue = text;
        so.FindProperty("tapHint").objectReferenceValue = hint.gameObject;
        so.FindProperty("face").objectReferenceValue = disc;
        so.ApplyModifiedPropertiesWithoutUndo();
        return coach;
    }
    #endregion

    #region The arrow
    // A big yellow arrowhead pointing down with a dark outline, drawn on top of everything (Chibi/Marker).
    // It was a plain lit mesh first, and the kitchen is full of things that hide one: the range hood above
    // the stove swallowed it completely whenever it pointed at the pan. It is also bigger than a "tasteful"
    // arrow on purpose - the whole cafe is a few hundred pixels across on a phone, and an arrow nobody can
    // see teaches nothing.
    const float ArrowRadius = 0.85f, ArrowDepth = 1.2f, OutlineScale = 1.24f;

    static Transform BuildArrow(Transform controllers)
    {
        var old = controllers.Find("TutorialArrow");
        var go = old != null ? old.gameObject : new GameObject("TutorialArrow");
        go.transform.SetParent(controllers, false);
        var mesh = new Mesh { name = "TutorialArrow" };
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();
        var tip = new Vector3(0f, -ArrowDepth, 0f);
        const int sides = 6;
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2f / sides, b = (i + 1) * Mathf.PI * 2f / sides;
            var p = new Vector3(Mathf.Cos(a) * ArrowRadius, 0f, Mathf.Sin(a) * ArrowRadius);
            var q = new Vector3(Mathf.Cos(b) * ArrowRadius, 0f, Mathf.Sin(b) * ArrowRadius);
            int at = verts.Count;
            verts.Add(p); verts.Add(tip); verts.Add(q);           // the cone
            verts.Add(Vector3.zero); verts.Add(q); verts.Add(p);  // the cap on top
            tris.AddRange(new[] { at, at + 1, at + 2, at + 3, at + 4, at + 5 });
        }
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        Get<MeshFilter>(go).sharedMesh = SaveMesh(mesh);
        Style(Get<MeshRenderer>(go), MarkerMaterial("TutorialArrow", UITokens.Colors.Yellow, 4000));

        // The outline: the same shape a little larger, drawn first, centred on the arrowhead's middle so the
        // rim is even all round (scaling about the origin would stretch it away from the cap).
        var rim = go.transform.Find("Outline");
        var rimGo = rim != null ? rim.gameObject : new GameObject("Outline");
        rimGo.transform.SetParent(go.transform, false);
        rimGo.transform.localScale = Vector3.one * OutlineScale;
        rimGo.transform.localPosition = new Vector3(0f, -ArrowDepth * 0.5f * (1f - OutlineScale), 0f);
        Get<MeshFilter>(rimGo).sharedMesh = mesh;
        Style(Get<MeshRenderer>(rimGo), MarkerMaterial("TutorialArrowOutline", UITokens.Colors.Ink, 3999));

        go.SetActive(false);
        return go.transform;
    }

    static void Style(MeshRenderer renderer, Material material)
    {
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    static Material MarkerMaterial(string name, Color color, int queue)
    {
        var shader = Shader.Find("Chibi/Marker") ?? throw new System.Exception("no shader Chibi/Marker");
        string path = MatDir + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = shader;          // TutorialArrow.mat started life as a lit material
        material.SetColor("_BaseColor", color);
        material.renderQueue = queue;
        EditorUtility.SetDirty(material);
        return material;
    }
    #endregion

    // Mochi's face for the card: the waiter himself, head on, matted like the shop icons.
    static void RenderCoachFace()
    {
        // He is switched off until he is bought, so he has to be looked up among the inactive too.
        var found = Object.FindObjectsByType<Waiter>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (found == null) { Debug.LogWarning("[CafeGrowth] no waiter to photograph - run step 9 first"); return; }
        var waiter = found.gameObject;
        bool was = waiter.transform.parent.gameObject.activeSelf;
        waiter.transform.parent.gameObject.SetActive(true);

        var stage = new GameObject("__FaceStage") { hideFlags = HideFlags.DontSave };
        try
        {
            var copy = Object.Instantiate(waiter.transform.Find("Cat").gameObject, stage.transform);
            copy.transform.position = IconStage;
            copy.transform.rotation = Quaternion.identity;
            var cam = new GameObject("FaceCamera") { hideFlags = HideFlags.DontSave }.AddComponent<Camera>();
            cam.transform.SetParent(stage.transform, false);
            cam.orthographic = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 50f;
            cam.enabled = false;
            cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderShadows = false;
            var head = copy.GetComponent<Renderer>().bounds;
            cam.transform.position = new Vector3(head.center.x, head.center.y + 0.18f, head.center.z) + Vector3.forward * 6f;
            cam.transform.rotation = Quaternion.LookRotation(Vector3.back);
            cam.orthographicSize = 0.34f;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(CoachFace)));
            File.WriteAllBytes(Path.GetFullPath(CoachFace), RenderMatted(cam, 256, 256));
        }
        finally
        {
            Object.DestroyImmediate(stage);
            waiter.transform.parent.gameObject.SetActive(was);
        }
        AssetDatabase.ImportAsset(CoachFace, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(CoachFace);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;   // the folder's default is Multiple, which has no sprite in it
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 256;
        imp.SaveAndReimport();
    }
}
