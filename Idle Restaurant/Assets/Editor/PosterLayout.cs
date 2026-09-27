using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// The "Rush Hour" poster: pink chibi-pattern field, a sun-burst, the chef sprinting with the Chaos Burger
// balanced on his head (KeyArtRenderer), speed streaks, sweat, a "Perfection optional!" callout, the logo
// and "Stack fast. Serve faster.". One layout, three uses:
//  - Tools/Chibi UI/Build Title Poster: the in-game title screen (Welcome panel, with the Start button);
//  - Tools/Chibi UI/Render Covers: Graphics/Sprites/KeyArt/cover_square.png (1024) and cover_wide.png
//    (1920x1080) for the game portal / store pages.
public static class PosterLayout
{
    const string Game = "Assets/Project/[GAME]/";
    const string Ui = Game + "Graphics/Sprites/UI/";
    public const string Tagline = "Stack fast. Serve faster.";
    public const string Callout = "Perfection optional!";

    public class Parts
    {
        public RectTransform Hero, Brand, Chef, Burst, Callout, Logo;
        public RectTransform[] Streaks, Drops;
    }

    static Sprite S(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static TMP_FontAsset Font(string name) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Game + "Graphics/Font/" + name);

    // Builds the poster under `root` (a full-size stretched RectTransform). `wide` = landscape layout
    // (title screen, banner); otherwise the square cover layout.
    public static Parts Build(RectTransform root, bool wide)
    {
        var parts = new Parts();
        var keyArt = S(KeyArtRenderer.OutDir + "keyart_rush.png");
        float chefH = wide ? 960f : 900f;
        float chefW = chefH * keyArt.rect.width / keyArt.rect.height;

        // Background pattern (covers the screen without distorting the faces).
        var pattern = Img("Pattern", root, S(Game + "Graphics/Sprites/Chibi Pattern Img.png"), new Color(1f, 1f, 1f, 0.22f));
        pattern.rectTransform.sizeDelta = new Vector2(1920f, 1080f);
        var fitter = pattern.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;

        // Hero: the chef and his effects, anchored left (wide) or centred-left (square).
        // Wide: the hero's origin is the screen's left edge. Square: the canvas centre.
        parts.Hero = Rect("Hero", root, wide ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f), wide ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(wide ? 1100f : 700f, 1080f));
        Vector2 chefPos = wide ? new Vector2(620f, -30f) : new Vector2(-190f, -40f);
        Vector2 Local(float fx, float fyFromTop) => chefPos + new Vector2((fx - 0.5f) * chefW, (0.5f - fyFromTop) * chefH);

        parts.Burst = Img("Burst", parts.Hero, S(Ui + "fx_sunburst.png"), new Color(1f, 0.98f, 0.94f, 0.4f)).rectTransform;
        parts.Burst.anchoredPosition = Local(0.55f, 0.62f);
        parts.Burst.sizeDelta = Vector2.one * (wide ? 2600f : 1900f);

        float[] streakY = { 0.18f, 0.34f, 0.5f, 0.66f, 0.82f };
        float[] streakLen = { 300f, 380f, 340f, 400f, 280f };
        parts.Streaks = new RectTransform[streakY.Length];
        for (int i = 0; i < streakY.Length; i++)
        {
            var streak = Img("Streak" + i, parts.Hero, S(Ui + "fx_speedline.png"), new Color(1f, 1f, 1f, 0.9f)).rectTransform;
            streak.pivot = new Vector2(1f, 0.5f);
            streak.sizeDelta = new Vector2(streakLen[i], streakLen[i] * 0.7f); // the sprite is square, the streak a thin band in it
            streak.anchoredPosition = Local(0.08f - 0.1f * (i % 2), streakY[i]);
            parts.Streaks[i] = streak;
        }

        var chef = Img("Chef", parts.Hero, keyArt, Color.white);
        chef.preserveAspect = true;
        parts.Chef = chef.rectTransform;
        parts.Chef.sizeDelta = new Vector2(chefW, chefH);
        parts.Chef.anchoredPosition = chefPos;

        parts.Drops = new RectTransform[2];
        for (int i = 0; i < 2; i++)
        {
            var drop = Img("Sweat" + i, parts.Hero, S(Ui + "fx_drop.png"), new Color(0.72f, 0.93f, 0.96f, 1f)).rectTransform;
            drop.sizeDelta = Vector2.one * (i == 0 ? 60f : 46f);
            drop.localRotation = Quaternion.Euler(0f, 0f, 35f);
            drop.anchoredPosition = Local(0.55f - 0.08f * i, 0.6f - 0.03f * i);
            parts.Drops[i] = drop;
        }

        // "Perfection optional!" callout pointing at the tower.
        parts.Callout = Rect("Callout", parts.Hero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
        // Said by the chef: beside his head, the tail pointing back at him.
        // (Below the Start button's row on the title screen, which shares this layout.)
        parts.Callout.anchoredPosition = wide ? new Vector2(chefPos.x + chefW * 0.5f + 250f, -350f) : new Vector2(250f, -210f);
        parts.Callout.localRotation = Quaternion.Euler(0f, 0f, 6f);
        var calloutText = Text("Label", parts.Callout, Font("Fredoka/Fredoka-SemiBold SDF.asset"), wide ? 40f : 36f, UITokens.Colors.Ink, Callout);
        float w = calloutText.GetPreferredValues(Callout).x + 64f;
        var shadow = Img("Shadow", parts.Callout, S(Ui + "ui_shadow.png"), UITokens.Colors.Shadow);
        shadow.type = Image.Type.Sliced; shadow.pixelsPerUnitMultiplier = 2f;
        shadow.rectTransform.sizeDelta = new Vector2(w + 24f, 104f);
        shadow.rectTransform.anchoredPosition = new Vector2(0f, -8f);
        var tail = Img("Tail", parts.Callout, S(Ui + "ui_tail.png"), UITokens.Colors.Yellow);
        // Tail on the left end, pointing back at the chef.
        tail.rectTransform.sizeDelta = new Vector2(48f, 48f);
        tail.rectTransform.anchoredPosition = new Vector2(-w * 0.5f - 6f, -6f);
        tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -80f);
        var pill = Img("Pill", parts.Callout, S(Ui + "ui_pill.png"), UITokens.Colors.Yellow);
        pill.type = Image.Type.Sliced; pill.pixelsPerUnitMultiplier = 128f / 80f;
        pill.rectTransform.sizeDelta = new Vector2(w, 80f);
        calloutText.transform.SetAsLastSibling();
        calloutText.rectTransform.sizeDelta = new Vector2(w, 80f);

        // Brand: logo and tagline (the title screen adds the Start button under them).
        parts.Brand = Rect("Brand", root, wide ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0.5f), wide ? new Vector2(1f, 0.5f) : new Vector2(0.5f, 0.5f),
            wide ? new Vector2(-470f, 0f) : new Vector2(250f, 0f), new Vector2(760f, 1080f));
        var logo = Img("Logo", parts.Brand, S(Game + "Graphics/Sprites/GameTitle.png"), Color.white);
        logo.preserveAspect = true;
        parts.Logo = logo.rectTransform;
        parts.Logo.sizeDelta = Vector2.one * (wide ? 760f : 480f);
        parts.Logo.anchoredPosition = wide ? new Vector2(0f, 190f) : new Vector2(0f, 310f);
        parts.Logo.localRotation = Quaternion.Euler(0f, 0f, 3f);
        var tagline = Text("Tagline", parts.Brand, Font("Fredoka/Fredoka-SemiBold SDF.asset"), wide ? 46f : 36f, UITokens.Colors.Ink, Tagline);
        tagline.rectTransform.sizeDelta = new Vector2(760f, 70f);
        tagline.rectTransform.anchoredPosition = wide ? new Vector2(0f, -60f) : new Vector2(0f, 150f);
        return parts;
    }

    #region Title screen
    [MenuItem("Tools/Chibi UI/Build Title Poster")]
    public static void BuildTitleScreen()
    {
        var welcome = Object.FindObjectsByType<WelcomePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        var root = (RectTransform)welcome.transform;
        var old = root.Find("Poster");
        var card = root.Find("TitleCard");
        if (old != null)
        {
            // Rebuild: bring the Start button back out of the old poster first.
            var oldStart = old.GetComponentsInChildren<StartButton>(true).FirstOrDefault();
            if (oldStart != null) oldStart.transform.SetParent(card, false);
            Object.DestroyImmediate(old.gameObject);
        }

        var bg = welcome.GetComponent<Image>();
        bg.color = UITokens.Colors.Pink;
        bg.sprite = null;

        var poster = Rect("Poster", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        poster.SetSiblingIndex(0);
        var parts = Build(poster, true);

        // The old centred card: its Start button moves under the tagline; the card itself is retired.
        var start = card.GetComponentInChildren<StartButton>(true).transform as RectTransform;
        start.SetParent(parts.Brand, false);
        start.anchorMin = start.anchorMax = new Vector2(0.5f, 0.5f);
        start.anchoredPosition = new Vector2(0f, -210f);
        start.sizeDelta = new Vector2(400f, 112f);
        card.gameObject.SetActive(false);

        parts.Brand.gameObject.AddComponent<UIPanelIntro>(); // adds its CanvasGroup

        var motion = poster.gameObject.AddComponent<UITitleMotion>();
        var so = new SerializedObject(motion);
        so.FindProperty("chef").objectReferenceValue = parts.Chef;
        so.FindProperty("burst").objectReferenceValue = parts.Burst;
        so.FindProperty("callout").objectReferenceValue = parts.Callout;
        var streaks = so.FindProperty("streaks");
        streaks.arraySize = parts.Streaks.Length;
        for (int i = 0; i < parts.Streaks.Length; i++) streaks.GetArrayElementAtIndex(i).objectReferenceValue = parts.Streaks[i];
        var drops = so.FindProperty("drops");
        drops.arraySize = parts.Drops.Length;
        for (int i = 0; i < parts.Drops.Length; i++) drops.GetArrayElementAtIndex(i).objectReferenceValue = parts.Drops[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(welcome.gameObject.scene);
        EditorSceneManager.SaveScene(welcome.gameObject.scene);
        Debug.Log("[Poster] title screen built");
    }
    #endregion

    #region Covers
    [MenuItem("Tools/Chibi UI/Render Covers")]
    public static void RenderCovers()
    {
        RenderCover(KeyArtRenderer.OutDir + "cover_square.png", 1024, 1024, false);
        RenderCover(KeyArtRenderer.OutDir + "cover_wide.png", 1920, 1080, true);
        AssetDatabase.Refresh();
    }

    static void RenderCover(string path, int width, int height, bool wide)
    {
        var stage = new GameObject("__CoverStage") { hideFlags = HideFlags.DontSave };
        stage.transform.position = new Vector3(-400f, -300f, -400f);
        try
        {
            var camGo = new GameObject("CoverCamera") { hideFlags = HideFlags.DontSave };
            camGo.transform.SetParent(stage.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UITokens.Colors.Pink;
            cam.cullingMask = 1 << 5; // UI
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 50f;
            cam.enabled = false;

            var canvasGo = new GameObject("CoverCanvas", typeof(RectTransform)) { hideFlags = HideFlags.DontSave };
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(stage.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = wide ? new Vector2(1920f, 1080f) : new Vector2(1024f, 1024f);
            scaler.matchWidthOrHeight = 1f;

            var rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { msaaSamples = 4, sRGB = true });
            cam.targetTexture = rt;
            cam.aspect = width / (float)height;

            var root = Rect("Poster", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Build(root, wide);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) { t.gameObject.layer = 5; t.gameObject.hideFlags = HideFlags.DontSave; }

            // Screen-space-camera canvases size themselves from the camera's target on update.
            Canvas.ForceUpdateCanvases();
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            RenderTexture.ReleaseTemporary(rt);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log("[Poster] rendered " + path);
        }
        finally
        {
            Object.DestroyImmediate(stage);
        }
    }
    #endregion

    #region Helpers
    static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Image Img(string name, Transform parent, Sprite sprite, Color color)
    {
        var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Text(string name, Transform parent, TMP_FontAsset font, float size, Color color, string text)
    {
        var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600f, 80f));
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }
    #endregion
}
