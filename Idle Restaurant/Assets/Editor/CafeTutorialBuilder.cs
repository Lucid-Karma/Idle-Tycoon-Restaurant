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

    [MenuItem("Tools/Chibi Cafe/10 Build the Tutorial")]
    public static void BuildTutorial()
    {
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
    // Nothing on screen any more (UICoachCard): only the object the lesson shows and hides.
    static UICoachCard BuildCard(Transform hud)
    {
        var card = Rect("Coach", hud, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(860f, 190f));
        return Get<UICoachCard>(card.gameObject);
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
}
