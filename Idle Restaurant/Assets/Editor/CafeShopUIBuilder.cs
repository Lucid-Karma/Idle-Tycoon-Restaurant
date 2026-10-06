using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Step 5 of the cafe growth content: the shop UI (re-runnable).
// - The shop panel moves out of the HUD to the top of StaticCanvas (it also opens from the result card) and
//   becomes a catalogue: title, cafe level + stars bar, wallet, close, and a 4 x 3 grid of UIShopTile.
// - HUD shop button: opens it (UIShop.Open), yellow dot while something is affordable (UIShopBadge).
// - Result card: "EARNED" shows this shift's money, the old best-rating tile becomes the cafe level tile
//   (+stars this shift, bar), and a Shop button sits next to Exit.
// Tokens and component anatomy from .claude/skills/chibi-burger-cafe-ui (§29).
public static partial class CafeGrowthBuilder
{
    const string UIDir = Game + "Graphics/Sprites/UI/";
    const string Canvas = "<<<UI>>>/StaticCanvas";

    static TMP_FontAsset SemiBold => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Game + "Graphics/Font/Fredoka/Fredoka-SemiBold SDF.asset");
    static TMP_FontAsset Medium => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Game + "Graphics/Font/Fredoka/Fredoka-Medium SDF.asset");
    static Sprite UISprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(UIDir + name + ".png");

    [MenuItem("Tools/Chibi Cafe/5 Build Shop UI")]
    public static void BuildShopUI()
    {
        var canvas = GameObject.Find(Canvas).transform;
        var purchase = canvas.Find("InGamePanel/Purchase");
        var panel = First(purchase.Find("PurchasePanel"), canvas.Find("ShopPanel"));
        panel.name = "ShopPanel";
        panel.SetParent(canvas, false);
        panel.SetAsLastSibling();
        foreach (var t in panel.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);

        var card = (RectTransform)panel.Find("ShopCard");
        card.sizeDelta = new Vector2(1580f, 870f);
        foreach (var old in new[] { "ItemTile", "Kitchen ApronTXT", "Description", "BuyBTN", "Hint", "ClaimBTN", "Level", "Grid", "Shelves" })
        {
            var t = card.Find(old);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        BuildLevelBlock(card);
        // Three rows fit on the card; the catalogue outgrew that, so the shelves scroll (drag anywhere).
        var shelves = Rect("Shelves", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1500f, 700f));
        shelves.gameObject.AddComponent<RectMask2D>();
        var grid = Rect("Grid", shelves, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 700f));
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(360f, 220f);
        layout.spacing = new Vector2(20f, 20f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 4;
        layout.childAlignment = TextAnchor.UpperCenter;
        var fitter = grid.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = shelves.gameObject.AddComponent<ScrollRect>();
        scroll.content = grid;
        scroll.viewport = shelves;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.08f;
        scroll.scrollSensitivity = 40f;
        var template = BuildTile(grid);

        var shop = Get<UIShop>(panel.gameObject);
        var so = new SerializedObject(shop);
        so.FindProperty("tileTemplate").objectReferenceValue = template;
        so.FindProperty("grid").objectReferenceValue = grid;
        so.ApplyModifiedPropertiesWithoutUndo();

        Listen(card.Find("BackBTN").GetComponent<Button>(), shop.Close);
        panel.gameObject.SetActive(false);

        // HUD button: open the shop; dot while something can be bought.
        var hudButton = purchase.Find("purchaseImg/purchaseBTN");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(hudButton.gameObject);
        Listen(hudButton.GetComponent<Button>(), shop.Open);
        AddBadge(purchase.Find("purchaseImg"), new Vector2(-10f, -10f));

        BuildResultCard(canvas, shop);

        // How to play: serving now also grows the cafe, and the pulse that says "use this next" is no
        // longer teal (HighlightController).
        canvas.Find("HelpPanel/HelpCard/HelpArea/ServingHelpArea/Body").GetComponent<TMP_Text>().text =
            "Tap a waiting customer. Their stars level up the cafe and the shop.";
        canvas.Find("HelpPanel/HelpCard/HelpArea/StackHelpArea/Body").GetComponent<TMP_Text>().text =
            "Tap a plate to stack. Toss a spare ingredient at a customer to buy time!";
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("[CafeGrowth] built shop UI");
    }

    // "CAFE LEVEL 3", a stars bar and "12 / 40" next to the title.
    static void BuildLevelBlock(RectTransform card)
    {
        var block = Rect("Level", card, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -40f), new Vector2(560f, 76f));
        var label = Text(Rect("Label", block, V(0, 1), V(0, 1), V(0, 1), V(0, 0), V(360, 28)), "CAFE LEVEL 1", SemiBold, 22f, UITokens.Colors.InkMuted, TextAlignmentOptions.TopLeft, 8f);
        var track = Rect("Track", block, V(0, 1), V(0, 1), V(0, 1), V(0, -40), V(300, 18));
        Img(track.gameObject, "ui_pill", UITokens.Colors.LightPink, true, 128f / 18f);
        var fill = Rect("Fill", track, V(0, 0), V(0.4f, 1), V(0, 0.5f), V(0, 0), V(0, 0));
        Img(fill.gameObject, "ui_pill", UITokens.Colors.Yellow, true, 128f / 18f);
        var star = Rect("Star", block, V(0, 1), V(0, 1), V(0.5f, 0.5f), V(334, -49), V(30, 30));
        Img(star.gameObject, "icon_star", UITokens.Colors.DeepYellow);
        var progress = Text(Rect("Progress", block, V(0, 1), V(0, 1), V(0, 0.5f), V(356, -49), V(200, 36)), "0 / 15", SemiBold, 28f, UITokens.Colors.Ink, TextAlignmentOptions.MidlineLeft);

        var level = Get<UICafeLevel>(block.gameObject);
        var so = new SerializedObject(level);
        so.FindProperty("levelLabel").objectReferenceValue = label;
        so.FindProperty("progressLabel").objectReferenceValue = progress;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.FindProperty("showShiftStars").boolValue = false;
        so.FindProperty("levelFormat").stringValue = "CAFE LEVEL {0}";
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // 360 x 220: picture on the left; category, name, what it does and the button on the right.
    static UIShopTile BuildTile(RectTransform grid)
    {
        var old = grid.Find("TileTemplate");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var tile = Rect("TileTemplate", grid, V(0, 1), V(0, 1), V(0, 1), V(0, 0), V(360, 220));
        var surface = Img(tile.gameObject, "ui_rounded", UITokens.Colors.Cream, true, 40f / 24f);

        var icon = Rect("Icon", tile, V(0, 0.5f), V(0, 0.5f), V(0.5f, 0.5f), V(84, 6), V(140, 140));
        var iconImage = Img(icon.gameObject, null, Color.white);
        iconImage.preserveAspect = true;
        var lockBadge = Rect("Lock", tile, V(0, 0.5f), V(0, 0.5f), V(0.5f, 0.5f), V(84, 6), V(60, 60));
        Img(lockBadge.gameObject, "ui_circle", UITokens.Colors.WarmWhite);
        var lockGlyph = Rect("Glyph", lockBadge, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 1), V(34, 34));
        Img(lockGlyph.gameObject, "icon_lock", UITokens.Colors.InkSoft);

        var category = Text(Rect("Category", tile, V(0, 1), V(0, 1), V(0, 1), V(164, -18), V(180, 26)), "KITCHEN", SemiBold, 22f, UITokens.Colors.InkMuted, TextAlignmentOptions.TopLeft, 6f);
        var title = Text(Rect("Title", tile, V(0, 1), V(0, 1), V(0, 1), V(164, -44), V(184, 38)), "Second Stove", SemiBold, 30f, UITokens.Colors.Ink, TextAlignmentOptions.TopLeft);
        title.enableAutoSizing = true;
        title.fontSizeMin = 22f;
        title.fontSizeMax = 30f;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        var blurb = Text(Rect("Blurb", tile, V(0, 1), V(0, 1), V(0, 1), V(164, -84), V(184, 58)), "Cook two patties at once", Medium, 22f, UITokens.Colors.InkSoft, TextAlignmentOptions.TopLeft);
        blurb.lineSpacing = -8f;

        // Button: transparent root (Button + UIPressable) > Edge > Face > Content (icon + label).
        var button = Rect("Button", tile, V(0, 0), V(0, 0), V(0, 0), V(164, 18), V(180, 58));
        Img(button.gameObject, "ui_rounded", new Color(0, 0, 0, 0), true, 40f / 22f);
        button.GetComponent<Image>().raycastTarget = true;
        var btn = button.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        var edge = Rect("Edge", button, V(0, 0), V(1, 1), V(0.5f, 0.5f), V(0, -5), V(0, 0));
        var edgeImage = Img(edge.gameObject, "ui_rounded", UITokens.Colors.DarkTeal, true, 40f / 22f);
        var face = Rect("Face", button, V(0, 0), V(1, 1), V(0.5f, 0.5f), V(0, 0), V(0, 0));
        var faceImage = Img(face.gameObject, "ui_rounded", UITokens.Colors.Teal, true, 40f / 22f);
        var content = Rect("Content", face, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(120, 40));
        var row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 6f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var glyph = Rect("Icon", content, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(26, 26));
        var glyphImage = Img(glyph.gameObject, "icon_check", UITokens.Colors.Ink);
        var glyphSize = glyph.gameObject.AddComponent<LayoutElement>();
        glyphSize.preferredWidth = glyphSize.preferredHeight = 26f;
        var label = Text(Rect("Label", content, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(80, 36)), "$60", SemiBold, 28f, UITokens.Colors.WarmWhite, TextAlignmentOptions.Center);
        var pressable = button.gameObject.AddComponent<UIPressable>();
        var pso = new SerializedObject(pressable);
        pso.FindProperty("face").objectReferenceValue = face;
        pso.FindProperty("depth").floatValue = 5f;
        pso.ApplyModifiedPropertiesWithoutUndo();

        var component = tile.gameObject.AddComponent<UIShopTile>();
        var so = new SerializedObject(component);
        so.FindProperty("surface").objectReferenceValue = surface;
        so.FindProperty("icon").objectReferenceValue = iconImage;
        so.FindProperty("lockBadge").objectReferenceValue = lockBadge.GetComponent<Image>();
        so.FindProperty("title").objectReferenceValue = title;
        so.FindProperty("blurb").objectReferenceValue = blurb;
        so.FindProperty("category").objectReferenceValue = category;
        so.FindProperty("button").objectReferenceValue = btn;
        so.FindProperty("buttonFace").objectReferenceValue = faceImage;
        so.FindProperty("buttonEdge").objectReferenceValue = edgeImage;
        so.FindProperty("buttonLabel").objectReferenceValue = label;
        so.FindProperty("buttonIcon").objectReferenceValue = glyphImage;
        so.FindProperty("checkIcon").objectReferenceValue = UISprite("icon_check");
        so.FindProperty("lockIcon").objectReferenceValue = UISprite("icon_lock");
        so.ApplyModifiedPropertiesWithoutUndo();
        return component;
    }

    // A small yellow dot on the top-right of a button while something in the shop is affordable.
    static void AddBadge(Transform button, Vector2 offset)
    {
        var old = button.Find("Badge");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var dot = Rect("Badge", button, V(1, 1), V(1, 1), V(0.5f, 0.5f), offset, V(26, 26));
        Img(dot.gameObject, "ui_circle", UITokens.Colors.Yellow);
        var ring = Rect("Ring", dot, V(0, 0), V(1, 1), V(0.5f, 0.5f), V(0, 0), V(0, 0));
        Img(ring.gameObject, "ui_ring", UITokens.Colors.WarmWhite);
        var badge = Get<UIShopBadge>(button.gameObject);
        var so = new SerializedObject(badge);
        so.FindProperty("dot").objectReferenceValue = dot.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildResultCard(Transform canvas, UIShop shop)
    {
        var card = (RectTransform)canvas.Find("ScorePanel/BackgroundIMG/ResultCard");

        // What this shift earned (the wallet itself now carries over).
        var earned = card.Find("EarnedTile/EarningTXT").GetComponent<EarningTextController>();
        var eso = new SerializedObject(earned);
        eso.FindProperty("shiftOnly").boolValue = true;
        eso.ApplyModifiedPropertiesWithoutUndo();

        // Best rating -> cafe level: "CAFE LEVEL 3", "+27 (star)", and the bar filling up to the new total.
        var tile = First(card.Find("BestTile"), card.Find("CafeTile"));
        tile.name = "CafeTile";
        foreach (var t in tile.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        foreach (var name in new[] { "Value", "Track" })
        {
            var t = tile.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        var label = First(tile.Find("HScoreTitleTXT"), tile.Find("Label")).GetComponent<TMP_Text>();
        label.name = "Label";
        label.text = "CAFE LEVEL 1";
        var oldValue = tile.Find("HScoreTXT");
        if (oldValue != null) Object.DestroyImmediate(oldValue.gameObject);

        var value = Rect("Value", tile, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0.5f), V(0, 50), V(200, 50));
        var row = value.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 6f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        var fitter = value.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var gain = Text(Rect("Stars", value, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(100, 50)), "+0", SemiBold, 44f, UITokens.Colors.Ink, TextAlignmentOptions.Center);
        var star = Rect("Star", value, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(36, 36));
        Img(star.gameObject, "icon_star", UITokens.Colors.DeepYellow);
        var starSize = star.gameObject.AddComponent<LayoutElement>();
        starSize.preferredWidth = starSize.preferredHeight = 36f;

        var track = Rect("Track", tile, V(0.5f, 0), V(0.5f, 0), V(0.5f, 0), V(0, 12), V(240, 12));
        Img(track.gameObject, "ui_pill", UITokens.Colors.LightPink, true, 128f / 12f);
        var fill = Rect("Fill", track, V(0, 0), V(0.4f, 1), V(0, 0.5f), V(0, 0), V(0, 0));
        Img(fill.gameObject, "ui_pill", UITokens.Colors.Yellow, true, 128f / 12f);

        var level = Get<UICafeLevel>(tile.gameObject);
        var so = new SerializedObject(level);
        so.FindProperty("levelLabel").objectReferenceValue = label;
        so.FindProperty("progressLabel").objectReferenceValue = gain;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.FindProperty("showShiftStars").boolValue = true;
        so.FindProperty("levelFormat").stringValue = "CAFE LEVEL {0}";
        so.ApplyModifiedPropertiesWithoutUndo();

        // Play again on top; Shop and Exit side by side under it.
        var area = card.Find("InteractionArea");
        var exit = First(area.Find("ExitBTN"), area.Find("Row/ExitBTN"));
        var rowRect = (RectTransform)area.Find("Row");
        if (rowRect == null)
        {
            rowRect = Rect("Row", area, V(0, 1), V(0, 1), V(0.5f, 0.5f), V(0, 0), V(560, 72));
            var h = rowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 16f;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = true;
            var le = rowRect.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 72f;
        }
        exit.SetParent(rowRect, false);
        var oldShop = rowRect.Find("ShopBTN");
        if (oldShop != null) Object.DestroyImmediate(oldShop.gameObject);
        var shopButton = Object.Instantiate(exit.gameObject, rowRect);
        shopButton.name = "ShopBTN";
        shopButton.transform.SetSiblingIndex(0);
        foreach (var t in shopButton.GetComponentsInChildren<TMP_Text>(true)) t.text = "Shop";
        var face = shopButton.transform.Find("Face");
        var cart = Rect("Cart", face, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(-58, 0), V(34, 34));
        Img(cart.gameObject, "icon_cart", UITokens.Colors.Ink);
        var labelRect = (RectTransform)face.GetComponentInChildren<TMP_Text>(true).transform;
        labelRect.anchoredPosition = new Vector2(18f, labelRect.anchoredPosition.y);
        var shopBtn = shopButton.GetComponent<Button>();
        while (shopBtn.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(shopBtn.onClick, 0);
        Listen(shopBtn, shop.Open);
        AddBadge(shopButton.transform, new Vector2(-10f, -6f));
    }

    #region UI helpers
    static Vector2 V(float x, float y) => new Vector2(x, y);

    // The first that exists (re-runs find the renamed object).
    static Transform First(Transform a, Transform b) => a != null ? a : b;

    static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static Image Img(GameObject go, string sprite, Color color, bool sliced = false, float ppu = 1f)
    {
        var image = Get<Image>(go);
        image.sprite = sprite != null ? UISprite(sprite) : null;
        image.color = color;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = ppu;
        image.raycastTarget = false;
        return image;
    }

    static TextMeshProUGUI Text(RectTransform rt, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align, float spacing = 0f)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = font;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.characterSpacing = spacing;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    static void Listen(Button button, UnityAction action)
    {
        while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        button.GetComponent<Image>().raycastTarget = true;
    }
    #endregion
}
