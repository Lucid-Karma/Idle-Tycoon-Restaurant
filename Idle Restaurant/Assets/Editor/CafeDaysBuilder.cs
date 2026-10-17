using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Step 15 (re-runnable): days, goals and the break between them.
// - HUD/GoalCard (UIDayGoal): the day's goal under the customers card, same card anatomy (shadow, warm-white
//   surface, micro label), a yellow bar and a count; a teal check once met.
// - HUD/BreakCard (UIBreakCard): the card the break between two days shows (ShiftFlow): kicker, a Permanent Marker
//   title, three stars, rows (icon, label, value) and tiles (picture, name, line), both from templates.
// - Portraits of the customer kinds for the "New in the cafe!" tiles (Graphics/Sprites/Customers), rendered from
//   the Npc prefabs like the shop icons, and the banknote icon (Tools/UI/icon_money.py) for "Earned today".
// Tokens and card anatomy from .claude/skills/chibi-burger-cafe-ui (§29).
public static partial class CafeGrowthBuilder
{
    const string PortraitDir = Game + "Graphics/Sprites/Customers/";
    const string MarkerFontPath = Game + "Graphics/Font/Permanent_Marker/PermanentMarker-Regular SDF.asset";
    const string WsCanvasPrefab = Game + "Prefabs/CharacterPrefabs/NpcPrefabs/Npc_WS_Canvas.prefab";

    [MenuItem("Tools/Chibi Cafe/15 Days: Goals, Break Cards and Daily Treat")]
    public static void BuildDays()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != MainScene) throw new System.Exception("Open " + MainScene + " first");
        ImportUISprite(UIDir + "icon_money.png");
        var portraits = RenderPortraits();
        var hud = GameObject.Find(Canvas + "/InGamePanel/HUD").transform;
        BuildGoalCard(hud);
        BuildBreakCard(hud, portraits);
        BuildCheerPop(hud);
        var dishes = RenderDishes();
        BuildGrandGoal(hud, dishes[0]);
        BuildOrderCard(hud);
        var breakCard = new SerializedObject(hud.GetComponentInChildren<UIBreakCard>(true));
        var dishList = breakCard.FindProperty("grillDishes");
        dishList.arraySize = dishes.Length;
        for (int i = 0; i < dishes.Length; i++) dishList.GetArrayElementAtIndex(i).objectReferenceValue = dishes[i];
        breakCard.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CafeGrowth] days: goal card, break card, customer portraits");
    }

    static void ImportUISprite(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 512;
        imp.SaveAndReimport();
    }

    #region Portraits
    // One picture per kind of customer (by CustomerKind; the Regulars have none: they are there from the start).
    static Sprite[] RenderPortraits()
    {
        Directory.CreateDirectory(PortraitDir);
        var idle = AssetDatabase.LoadAllAssetsAtPath(SkullFbx).OfType<AnimationClip>().First(c => c.name == "Idle");
        int kinds = System.Enum.GetValues(typeof(CustomerKind)).Length;
        var sprites = new Sprite[kinds];
        var view = Quaternion.Euler(10f, 180f, 0f);
        AnimationMode.StartAnimationMode();
        try
        {
            foreach (var (model, kind) in Adventurers)
            {
                if (sprites[(int)kind] != null) continue;   // the Rogue's first look
                string path = PortraitDir + "customer_" + kind.ToString().ToLowerInvariant() + ".png";
                var stage = new GameObject("__IconStage") { hideFlags = HideFlags.DontSave };
                try
                {
                    var subject = new GameObject("Subject").transform;
                    subject.SetParent(stage.transform, false);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcPath(model));
                    var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, subject);
                    npc.hideFlags = HideFlags.DontSave;
                    PrefabUtility.UnpackPrefabInstance(npc, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    foreach (var b in npc.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
                    foreach (var c in npc.GetComponentsInChildren<Canvas>(true)) c.gameObject.SetActive(false);
                    var agent = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (agent != null) agent.enabled = false;
                    npc.transform.SetPositionAndRotation(IconStage, Quaternion.Euler(0f, -20f, 0f));
                    var body = npc.transform.Find("Model").gameObject;
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(body, idle, 0.2f);
                    AnimationMode.EndSampling();
                    var cam = IconCamera(stage.transform, view);
                    File.WriteAllBytes(path, RenderMatted(cam, 360, 360));
                }
                finally
                {
                    Object.DestroyImmediate(stage);
                }
                ImportUISprite(path);
                sprites[(int)kind] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        finally
        {
            AnimationMode.StopAnimationMode();
        }
        return sprites;
    }
    #endregion

    #region Goal card
    static void BuildGoalCard(Transform hud)
    {
        var old = hud.Find("GoalCard");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var customers = hud.Find("CustomersCard");

        // Under the customers card, same width and margins.
        var card = Rect("GoalCard", hud, V(0, 1), V(0, 1), V(0, 1), V(32, -188), V(280, 150));
        card.SetSiblingIndex(customers.GetSiblingIndex() + 1);
        card.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        Object.Instantiate(customers.Find("Shadow"), card).name = "Shadow";
        Object.Instantiate(customers.Find("Surface"), card).name = "Surface";
        var label = Object.Instantiate(customers.Find("Label"), card).GetComponent<TextMeshProUGUI>();
        label.name = "Label";
        label.text = "DAY 1  ·  SHIFT 1";

        var goal = Text(Rect("Goal", card, V(0, 1), V(0, 1), V(0, 1), V(26, -50), V(232, 64)),
            "Serve 2 perfect burgers", SemiBold, UITokens.Typography.Body, UITokens.Colors.Ink, TextAlignmentOptions.TopLeft);
        goal.lineSpacing = -12f;

        var track = Rect("Track", card, V(0, 1), V(0, 1), V(0, 0.5f), V(26, -124), V(140, 12));
        Img(track.gameObject, "ui_pill", UITokens.Colors.LightPink, true, 128f / 12f);
        var fill = Rect("Fill", track, V(0, 0), V(0.4f, 1), V(0, 0.5f), V(0, 0), V(0, 0));
        var fillImage = Img(fill.gameObject, "ui_pill", UITokens.Colors.Yellow, true, 128f / 12f);

        var count = Text(Rect("Count", card, V(0, 1), V(0, 1), V(1, 0.5f), V(256, -124), V(86, 36)),
            "0/2", SemiBold, 28f, UITokens.Colors.Ink, TextAlignmentOptions.Right);
        count.textWrappingMode = TextWrappingModes.NoWrap;

        var mark = Rect("Mark", card, V(1, 1), V(1, 1), V(1, 1), V(-18, -16), V(32, 32));
        var markImage = Img(mark.gameObject, "icon_check", UITokens.Colors.Teal);
        markImage.enabled = false;

        var ui = card.gameObject.AddComponent<UIDayGoal>();
        var so = new SerializedObject(ui);
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("goal").objectReferenceValue = goal;
        so.FindProperty("count").objectReferenceValue = count;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.FindProperty("fillImage").objectReferenceValue = fillImage;
        so.FindProperty("mark").objectReferenceValue = markImage;
        so.FindProperty("check").objectReferenceValue = UISprite("icon_check");
        so.FindProperty("cross").objectReferenceValue = UISprite("icon_close");
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    #endregion

    #region Grand goal
    // The Grill House's dishes (GrandGoal), rendered from the KayKit models like the shop icons: the steak dinner,
    // the stew and a steak.
    const string DishDir = Game + "Graphics/Sprites/Grill/";
    static readonly string[] DishModels = { "food_dinner", "food_stew", "food_ingredient_steak" };

    static Sprite[] RenderDishes()
    {
        Directory.CreateDirectory(DishDir);
        var view = Camera.main.transform.rotation;
        var sprites = new Sprite[DishModels.Length];
        for (int i = 0; i < DishModels.Length; i++)
        {
            string path = DishDir + "grill_" + DishModels[i].Replace("food_ingredient_", "").Replace("food_", "") + ".png";
            var stage = new GameObject("__IconStage") { hideFlags = HideFlags.DontSave };
            try
            {
                var subject = new GameObject("Subject").transform;
                subject.SetParent(stage.transform, false);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/KayKit_Restaurant_Bits_1.0_FREE/Assets/fbx (unity)/" + DishModels[i] + ".fbx");
                var copy = (GameObject)Object.Instantiate(model, subject);
                copy.hideFlags = HideFlags.DontSave;
                copy.transform.SetPositionAndRotation(IconStage, Quaternion.Euler(0f, 20f, 0f));
                var cam = IconCamera(stage.transform, view);
                File.WriteAllBytes(path, RenderMatted(cam, 360, 360));
            }
            finally
            {
                Object.DestroyImmediate(stage);
            }
            ImportUISprite(path);
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return sprites;
    }

    // The Grill House savings (UIGrandGoal): its first design - the dish on the left, "NEXT: GRILL HOUSE",
    // "$660 / $2,000", a bar - in the top-right corner, its right edge on the screen margin (the buttons' edge, as
    // the left column's cards are on theirs) and a clear gap under the buttons. Frosted (UIFrostedCard). Tried and
    // dropped: right-aligned to the wallet (off-axis over the kitchen), a tab under the wallet (tiny), a twin of the
    // day's goal card (the user kept the first design). Nothing tappable lies there.
    static void BuildGrandGoal(Transform hud, Sprite dish)
    {
        var old = hud.Find("GrandGoal");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var customers = hud.Find("CustomersCard");

        var card = Rect("GrandGoal", hud, V(1, 1), V(1, 1), V(1, 1), V(-32, -132), V(300, 100));
        card.SetSiblingIndex(hud.Find("Wallet").GetSiblingIndex() + 1);
        var group = card.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        var shadow = Object.Instantiate(customers.Find("Shadow"), card);
        shadow.name = "Shadow";
        var surface = Object.Instantiate(customers.Find("Surface"), card);
        surface.name = "Surface";
        surface.GetComponent<Image>().raycastTarget = true;   // the pointer over it makes it solid

        var iconRect = Rect("Dish", card, V(0, 1), V(0, 1), V(0, 1), V(12, -12), V(76, 76));
        var icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = dish;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        var label = Object.Instantiate(customers.Find("Label"), card).GetComponent<TextMeshProUGUI>();
        label.name = "Label";
        var labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = V(0, 1);
        labelRect.anchoredPosition = V(96, -14);
        labelRect.sizeDelta = V(190, 24);
        label.text = "NEXT: GRILL HOUSE";
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = label.fontSize;

        var count = Text(Rect("Count", card, V(0, 1), V(0, 1), V(0, 1), V(96, -38), V(190, 32)),
            "$0 / $2,000", SemiBold, 24f, UITokens.Colors.Ink, TextAlignmentOptions.MidlineLeft);
        count.textWrappingMode = TextWrappingModes.NoWrap;
        count.enableAutoSizing = true;
        count.fontSizeMin = 16f;
        count.fontSizeMax = 24f;

        var track = Rect("Track", card, V(0, 1), V(0, 1), V(0, 0.5f), V(96, -80), V(186, 12));
        Img(track.gameObject, "ui_pill", UITokens.Colors.LightPink, true, 128f / 12f);
        var fill = Rect("Fill", track, V(0, 0), V(0.3f, 1), V(0, 0.5f), V(0, 0), V(0, 0));
        var fillImage = Img(fill.gameObject, "ui_pill", UITokens.Colors.Berry, true, 128f / 12f);

        var ui = card.gameObject.AddComponent<UIGrandGoal>();
        var so = new SerializedObject(ui);
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("count").objectReferenceValue = count;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.FindProperty("fillImage").objectReferenceValue = fillImage;
        so.ApplyModifiedPropertiesWithoutUndo();
        Frost(card.gameObject, surface.GetComponent<Image>(), shadow.GetComponent<Image>(), 0.7f);
    }

    static void Frost(GameObject card, Image surface, Image shadow, float idle)
    {
        var frost = card.AddComponent<UIFrostedCard>();
        var so = new SerializedObject(frost);
        so.FindProperty("surface").objectReferenceValue = surface;
        so.FindProperty("shadow").objectReferenceValue = shadow;
        so.FindProperty("idleSurface").floatValue = idle;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    #endregion

    #region Order card
    // The order being made, as a ticket in the bottom-right corner (UIOrderCard), mirroring the "Last order" card
    // in the bottom-left: header (face, ORDER, recipe, extras), a tile per ingredient (picture, name, how it is
    // made, a badge), the perfect rule. Tried and moved: a column on the right edge (over the onion crate and the
    // second stove's spot - it takes the pointer for its hover) and one in the left column (over the big tomato
    // statue the user likes). The bottom-right corner is empty floor with every upgrade bought (measured).
    const float OrderCardWidth = 440f, TileWidth = 64f;
    static void BuildOrderCard(Transform hud)
    {
        var old = hud.Find("OrderCard");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var customers = hud.Find("CustomersCard");
        var lastOrder = (RectTransform)hud.Find("ChefCard");

        var card = Rect("OrderCard", hud, V(1, 0), V(1, 0), V(1, 0), V(-32, 32), V(OrderCardWidth, 190));
        if (lastOrder != null) card.SetSiblingIndex(lastOrder.GetSiblingIndex() + 1);
        var group = card.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 12, 14);
        layout.spacing = 4f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fit = card.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var shadow = Object.Instantiate(customers.Find("Shadow"), card);
        shadow.name = "Shadow";
        shadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var surface = Object.Instantiate(customers.Find("Surface"), card);
        surface.name = "Surface";
        surface.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        surface.GetComponent<Image>().raycastTarget = true;   // the pointer over the card makes it solid
        Frost(card.gameObject, surface.GetComponent<Image>(), shadow.GetComponent<Image>(), 0.55f);

        // Header: the customer's face, "ORDER", the recipe's name, and on the right any extras.
        float inner = OrderCardWidth - 36f;
        var header = Rect("Header", card, V(0, 1), V(0, 1), V(0, 1), V(0, 0), V(inner, 36));
        var headerRow = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerRow.spacing = 8f;
        headerRow.childAlignment = TextAnchor.MiddleLeft;
        headerRow.childControlWidth = headerRow.childControlHeight = true;
        headerRow.childForceExpandWidth = headerRow.childForceExpandHeight = false;
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
        var portraitRect = Rect("Portrait", header, V(0, 0.5f), V(0, 0.5f), V(0.5f, 0.5f), V(0, 0), V(34, 34));
        var portrait = portraitRect.gameObject.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        portrait.sprite = UISprite("icon_person");
        var portraitSize = portraitRect.gameObject.AddComponent<LayoutElement>();
        portraitSize.preferredWidth = portraitSize.preferredHeight = 34f;
        var kicker = Object.Instantiate(customers.Find("Label"), header).GetComponent<TextMeshProUGUI>();
        kicker.name = "Kicker";
        kicker.text = "ORDER";
        kicker.textWrappingMode = TextWrappingModes.NoWrap;
        kicker.alignment = TextAlignmentOptions.MidlineLeft;
        kicker.enableAutoSizing = false;
        var kickerSize = kicker.gameObject.AddComponent<LayoutElement>();
        kickerSize.preferredWidth = kicker.GetPreferredValues("ORDER").x + 4f;
        kickerSize.preferredHeight = 34f;
        var title = Text(Rect("Title", header, V(0, 0.5f), V(0, 0.5f), V(0, 0.5f), V(0, 0), V(150, 34)), "Cheeseburger",
            SemiBold, 24f, UITokens.Colors.Ink, TextAlignmentOptions.MidlineLeft);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.enableAutoSizing = true;
        title.fontSizeMin = 16f;
        title.fontSizeMax = 24f;
        var titleSize = title.gameObject.AddComponent<LayoutElement>();
        titleSize.preferredWidth = 150f;
        titleSize.preferredHeight = 34f;
        var extras = Text(Rect("Extras", header, V(0, 0.5f), V(0, 0.5f), V(0, 0.5f), V(0, 0), V(140, 34)), "",
            Medium, 16f, UITokens.Colors.InkMuted, TextAlignmentOptions.MidlineRight);
        extras.textWrappingMode = TextWrappingModes.NoWrap;
        extras.enableAutoSizing = true;
        extras.fontSizeMin = 12f;
        extras.fontSizeMax = 16f;
        var extrasSize = extras.gameObject.AddComponent<LayoutElement>();
        extrasSize.flexibleWidth = 1f;
        extrasSize.preferredHeight = 34f;

        // The ingredient tiles, left to right as the burger is stacked (made from the template at runtime).
        var tiles = Rect("Tiles", card, V(0, 1), V(0, 1), V(0, 1), V(0, 0), V(inner, 92));
        var tilesRow = tiles.gameObject.AddComponent<HorizontalLayoutGroup>();
        tilesRow.spacing = 4f;
        tilesRow.childAlignment = TextAnchor.UpperLeft;
        tilesRow.childControlWidth = tilesRow.childControlHeight = true;
        tilesRow.childForceExpandWidth = tilesRow.childForceExpandHeight = false;
        tiles.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;

        var tile = Rect("Tile", tiles, V(0, 1), V(0, 1), V(0, 1), V(0, 0), V(TileWidth, 92));
        var tileSize = tile.gameObject.AddComponent<LayoutElement>();
        tileSize.preferredWidth = TileWidth;
        tileSize.preferredHeight = 92f;
        var iconRect = Rect("Icon", tile, V(0.5f, 1), V(0.5f, 1), V(0.5f, 1), V(-4, -4), V(50, 42));
        var icon = iconRect.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        var name = Text(Rect("Name", tile, V(0.5f, 1), V(0.5f, 1), V(0.5f, 1), V(0, -48), V(TileWidth, 22)), "Patty",
            SemiBold, 17f, UITokens.Colors.Ink, TextAlignmentOptions.Center);
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.enableAutoSizing = true;
        name.fontSizeMin = 12f;
        name.fontSizeMax = 17f;
        var prep = Text(Rect("Prep", tile, V(0.5f, 1), V(0.5f, 1), V(0.5f, 1), V(0, -69), V(TileWidth, 18)), "cooked",
            Medium, 14f, UITokens.Colors.InkMuted, TextAlignmentOptions.Center);
        prep.textWrappingMode = TextWrappingModes.NoWrap;
        var markRect = Rect("Mark", tile, V(1, 1), V(1, 1), V(1, 1), V(2, 2), V(26, 26));
        var mark = Img(markRect.gameObject, "icon_check", UITokens.Colors.Teal);
        mark.preserveAspect = true;
        tile.gameObject.SetActive(false);

        var footer = Text(Rect("Footer", card, V(0, 1), V(0, 1), V(0, 1), V(0, 0), V(inner, 22)),
            "Exactly this = Perfect  ·  any burger still sells", Medium, 15f, UITokens.Colors.InkMuted, TextAlignmentOptions.MidlineLeft);
        footer.textWrappingMode = TextWrappingModes.NoWrap;
        footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        var ui = card.gameObject.AddComponent<UIOrderCard>();
        var so = new SerializedObject(ui);
        so.FindProperty("title").objectReferenceValue = title;
        so.FindProperty("extras").objectReferenceValue = extras;
        so.FindProperty("footer").objectReferenceValue = footer;
        so.FindProperty("portrait").objectReferenceValue = portrait;
        so.FindProperty("personIcon").objectReferenceValue = UISprite("icon_person");
        so.FindProperty("tiles").objectReferenceValue = tiles;
        so.FindProperty("tileTemplate").objectReferenceValue = tile.gameObject;
        so.FindProperty("layerSprites").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(WsCanvasPrefab).GetComponentInChildren<UIOrderTicket>(true);
        so.FindProperty("check").objectReferenceValue = UISprite("icon_check");
        so.FindProperty("warn").objectReferenceValue = UISprite("icon_flame");
        so.FindProperty("chop").objectReferenceValue = UISprite("icon_cutlery");
        so.FindProperty("burnt").objectReferenceValue = UISprite("icon_close");
        var icons = RenderIngredientIcons();
        foreach (var (field, sprite) in icons) so.FindProperty(field).objectReferenceValue = sprite;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // The ingredients as they go on a perfect burger, from the game camera's angle, for the order card.
    const string IngredientDir = Game + "Graphics/Sprites/Ingredients/";
    static readonly (string field, string file, string prefab, string visual)[] IngredientIcons =
    {
        ("bunIcon", "bun", "Hamburger", "finishBun"),
        ("pattyIcon", "patty", "Uncooked_Burger", "cookedBurger"),
        ("cheeseIcon", "cheese", "Cheese", "slicedCheese"),
        ("lettuceIcon", "lettuce", "Lettuce", "purePrefab"),
        ("tomatoIcon", "tomato", "Tomato", "slicedTomato"),
        ("onionIcon", "onion", "Onion", "slicedOnion"),
    };

    static List<(string field, Sprite sprite)> RenderIngredientIcons()
    {
        Directory.CreateDirectory(IngredientDir);
        var view = Camera.main.transform.rotation;
        var result = new List<(string, Sprite)>();
        foreach (var (field, file, prefab, visual) in IngredientIcons)
        {
            string path = IngredientDir + "ing_" + file + ".png";
            var stage = new GameObject("__IconStage") { hideFlags = HideFlags.DontSave };
            try
            {
                var subject = new GameObject("Subject").transform;
                subject.SetParent(stage.transform, false);
                var item = KeyArtRenderer.Place(KeyArtRenderer.Visual(prefab, visual), subject, file);
                item.SetPositionAndRotation(IconStage, Quaternion.identity);
                var cam = IconCamera(stage.transform, view);
                File.WriteAllBytes(path, RenderMatted(cam, 160, 160));
            }
            finally
            {
                Object.DestroyImmediate(stage);
            }
            ImportUISprite(path);
            result.Add((field, AssetDatabase.LoadAssetAtPath<Sprite>(path)));
        }
        return result;
    }
    #endregion

    #region Cheer pop
    // "Perfect!" / "Rush x3!" over the customer (UICheerPop): a warm-white pill with an icon, sparks behind it.
    static void BuildCheerPop(Transform hud)
    {
        var old = hud.Find("CheerPop");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = Rect("CheerPop", hud, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(10, 10));
        root.SetSiblingIndex(hud.Find("SplatPop").GetSiblingIndex() + 1);
        root.gameObject.AddComponent<Canvas>();                 // its own canvas: it moves every frame
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var sparks = new Image[6];
        for (int i = 0; i < sparks.Length; i++)
        {
            var spark = Rect("Spark", root, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(30, 30));
            sparks[i] = Img(spark.gameObject, "icon_star", UITokens.Colors.Yellow);
            sparks[i].preserveAspect = true;
        }

        var pill = Rect("Pill", root, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(260, 72));
        var row = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(20, 26, 12, 12);
        row.spacing = 10f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = row.childControlHeight = true;
        row.childForceExpandWidth = row.childForceExpandHeight = false;
        var fit = pill.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var shadow = Rect("Shadow", pill, V(0, 0), V(1, 1), V(0.5f, 0.5f), V(0, -6), V(0, 0));
        Img(shadow.gameObject, "ui_pill", UITokens.Colors.Shadow, true, 128f / 72f);
        shadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        var surface = Rect("Surface", pill, V(0, 0), V(1, 1), V(0.5f, 0.5f), V(0, 0), V(0, 0));
        Img(surface.gameObject, "ui_pill", UITokens.Colors.WarmWhite, true, 128f / 72f);
        surface.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        var iconRect = Rect("Icon", pill, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(44, 44));
        var icon = Img(iconRect.gameObject, "icon_star", UITokens.Colors.DeepYellow);
        icon.preserveAspect = true;
        var iconSize = iconRect.gameObject.AddComponent<LayoutElement>();
        iconSize.preferredWidth = iconSize.preferredHeight = 44f;
        var label = Text(Rect("Label", pill, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(200, 48)), "Perfect!",
            SemiBold, 38f, UITokens.Colors.Ink, TextAlignmentOptions.MidlineLeft);
        label.textWrappingMode = TextWrappingModes.NoWrap;

        var ui = root.gameObject.AddComponent<UICheerPop>();
        var so = new SerializedObject(ui);
        so.FindProperty("pill").objectReferenceValue = pill;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("label").objectReferenceValue = label;
        var list = so.FindProperty("sparks");
        list.arraySize = sparks.Length;
        for (int i = 0; i < sparks.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = sparks[i];
        so.FindProperty("star").objectReferenceValue = UISprite("icon_star");
        so.FindProperty("bolt").objectReferenceValue = UISprite("icon_bolt");
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    #endregion

    #region Break card
    static void BuildBreakCard(Transform hud, Sprite[] portraits)
    {
        var old = hud.Find("BreakCard");
        // The "Double it" offer lives on the day's card now (it used to be a HUD button that flashed by after each
        // shift): the HUD's BonusBTN is moved in - or, on a re-run, taken back out of the old card first.
        var bonus = hud.Find("BonusBTN") ?? (old != null ? old.Find("Buttons/DoubleBTN") : null);
        if (bonus != null) bonus.SetParent(hud, false);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var customers = hud.Find("CustomersCard");
        var marker = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MarkerFontPath);

        // Centred a little above the middle, over the empty cafe; above the toast line's layer, under the coach.
        var root = Rect("BreakCard", hud, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 40), V(720, 400));
        root.SetSiblingIndex(hud.Find("Coach").GetSiblingIndex());
        var canvas = root.gameObject.AddComponent<Canvas>();
        root.gameObject.AddComponent<GraphicRaycaster>();
        var group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        var column = root.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(44, 44, 30, 30);
        column.spacing = 10f;
        column.childAlignment = TextAnchor.UpperCenter;
        column.childControlWidth = column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        foreach (var part in new[] { "Shadow", "Surface" })
        {
            var copy = Object.Instantiate(customers.Find(part), root);
            copy.name = part;
            copy.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        }
        root.Find("Surface").GetComponent<Image>().raycastTarget = true;   // a tap anywhere on the card moves on

        var kicker = Text(Rect("Kicker", root, V(0, 1), V(0, 1), V(0.5f, 1), V(0, 0), V(0, 28)), "SHIFT 1 DONE  ·  DAY 1",
            SemiBold, UITokens.Typography.Micro, UITokens.Colors.Berry, TextAlignmentOptions.Center, 8f);
        var title = Text(Rect("Title", root, V(0, 1), V(0, 1), V(0.5f, 1), V(0, 0), V(0, 72)), "Great shift!",
            marker, UITokens.Typography.Heading, UITokens.Colors.Ink, TextAlignmentOptions.Center);

        var starsRow = Rect("Stars", root, V(0, 1), V(0, 1), V(0.5f, 1), V(0, 0), V(0, 72));
        var starsLayout = starsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        starsLayout.spacing = 14f;
        starsLayout.childAlignment = TextAnchor.MiddleCenter;
        starsLayout.childControlWidth = starsLayout.childControlHeight = true;
        starsLayout.childForceExpandWidth = starsLayout.childForceExpandHeight = false;
        var stars = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var star = Rect("Star", starsRow, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(64, 64));
            stars[i] = Img(star.gameObject, "icon_star", UITokens.Colors.StarEmpty);
            var le = star.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = le.preferredHeight = i == 1 ? 72f : 60f;   // the middle one a little bigger
        }

        var rows = Rect("Rows", root, V(0, 1), V(0, 1), V(0.5f, 1), V(0, 0), V(0, 0));
        var rowsLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
        rowsLayout.spacing = 2f;
        rowsLayout.padding = new RectOffset(16, 16, 6, 6);
        rowsLayout.childControlWidth = rowsLayout.childControlHeight = true;
        rowsLayout.childForceExpandWidth = true;
        rowsLayout.childForceExpandHeight = false;
        var rowTemplate = BuildRowTemplate(rows);

        var tiles = Rect("Tiles", root, V(0, 1), V(0, 1), V(0.5f, 1), V(0, 0), V(0, 0));
        var tilesLayout = tiles.gameObject.AddComponent<HorizontalLayoutGroup>();
        tilesLayout.spacing = 12f;
        tilesLayout.padding = new RectOffset(0, 0, 4, 4);
        tilesLayout.childAlignment = TextAnchor.MiddleCenter;
        tilesLayout.childControlWidth = tilesLayout.childControlHeight = true;
        tilesLayout.childForceExpandWidth = tilesLayout.childForceExpandHeight = false;
        var tileTemplate = BuildTileTemplate(tiles);

        var footer = Text(Rect("Footer", root, V(0, 1), V(0, 1), V(0.5f, 1), V(0, 0), V(0, 30)), "Tap to continue",
            Medium, UITokens.Typography.Micro, UITokens.Colors.InkMuted, TextAlignmentOptions.Center);

        // The buttons along the bottom: "Double it" (secondary, video icon, the day's card only) and "Next day"
        // (the one teal button: the card waits for it).
        var buttons = Rect("Buttons", root, V(0, 1), V(0, 1), V(0.5f, 1), V(0, 0), V(0, 84));
        var buttonsLayout = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.spacing = 16f;
        buttonsLayout.padding = new RectOffset(0, 0, 8, 0);
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlWidth = buttonsLayout.childControlHeight = false;
        buttonsLayout.childForceExpandWidth = buttonsLayout.childForceExpandHeight = false;
        var buttonsSize = buttons.gameObject.AddComponent<LayoutElement>();
        buttonsSize.preferredHeight = 84f;

        GameObject doubleButton = null;
        if (bonus != null)
        {
            bonus.SetParent(buttons, false);
            bonus.name = "DoubleBTN";
            var bonusRect = (RectTransform)bonus;
            bonusRect.anchorMin = bonusRect.anchorMax = bonusRect.pivot = V(0.5f, 0.5f);
            bonusRect.sizeDelta = V(260, 72);
            // The video icon at the left edge and the words in the rest of the face: the icon used to sit at a
            // fixed -66 from the centre, made for "Double it", and ran into the "D" of "Double +$71" (user).
            var face = bonus.Find("Face");
            var video = face != null ? face.Find("Video") as RectTransform : null;
            if (video != null)
            {
                video.anchorMin = video.anchorMax = video.pivot = V(0f, 0.5f);
                video.sizeDelta = V(34, 34);
                video.anchoredPosition = V(22, 0);
            }
            var words = face != null ? face.GetComponentInChildren<TMP_Text>(true) : null;
            if (words != null)
            {
                var wordsRect = words.rectTransform;
                wordsRect.anchorMin = V(0, 0);
                wordsRect.anchorMax = V(1, 1);
                wordsRect.pivot = V(0.5f, 0.5f);
                wordsRect.offsetMin = V(66, 0);
                wordsRect.offsetMax = V(-14, 0);
                words.alignment = TextAlignmentOptions.Center;
                words.textWrappingMode = TextWrappingModes.NoWrap;
                words.enableAutoSizing = true;
                words.fontSizeMin = 20f;
                words.fontSizeMax = 30f;
                EditorUtility.SetDirty(words);
            }
            var offer = new SerializedObject(bonus.GetComponent<UIRewardedBonus>());
            offer.FindProperty("offerSeconds").floatValue = 0f;      // it waits with the card
            offer.FindProperty("card").objectReferenceValue = group;  // the card is held still while the ad plays
            offer.ApplyModifiedPropertiesWithoutUndo();
            doubleButton = bonus.gameObject;
        }

        var back = GameObject.Find(Canvas + "/HelpPanel/HelpCard/BackBTN");
        var nextGo = Object.Instantiate(back, buttons);
        nextGo.name = "NextBTN";
        nextGo.SetActive(true);
        var nextButtonBack = nextGo.GetComponent<BackToLevelButton>();
        if (nextButtonBack != null) Object.DestroyImmediate(nextButtonBack);
        var nextButton = nextGo.GetComponent<Button>();
        while (nextButton.onClick.GetPersistentEventCount() > 0) UnityEditor.Events.UnityEventTools.RemovePersistentListener(nextButton.onClick, 0);
        var nextRect = (RectTransform)nextGo.transform;
        nextRect.anchorMin = nextRect.anchorMax = nextRect.pivot = V(0.5f, 0.5f);
        nextRect.sizeDelta = V(260, 72);
        var nextLabel = nextGo.GetComponentInChildren<TMP_Text>(true);
        nextLabel.text = "Next shift";

        var ui = root.gameObject.AddComponent<UIBreakCard>();
        var so = new SerializedObject(ui);
        so.FindProperty("group").objectReferenceValue = group;
        so.FindProperty("card").objectReferenceValue = root;
        so.FindProperty("kicker").objectReferenceValue = kicker;
        so.FindProperty("title").objectReferenceValue = title;
        so.FindProperty("footer").objectReferenceValue = footer;
        var starsProp = so.FindProperty("stars");
        starsProp.arraySize = stars.Length;
        for (int i = 0; i < stars.Length; i++) starsProp.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
        so.FindProperty("rows").objectReferenceValue = rows;
        so.FindProperty("tiles").objectReferenceValue = tiles;
        so.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
        so.FindProperty("tileTemplate").objectReferenceValue = tileTemplate;
        so.FindProperty("nextButton").objectReferenceValue = nextButton;
        so.FindProperty("nextLabel").objectReferenceValue = nextLabel;
        so.FindProperty("doubleButton").objectReferenceValue = doubleButton;
        so.FindProperty("layerSprites").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(WsCanvasPrefab).GetComponentInChildren<UIOrderTicket>(true);
        so.FindProperty("starIcon").objectReferenceValue = UISprite("icon_star");
        so.FindProperty("checkIcon").objectReferenceValue = UISprite("icon_check");
        so.FindProperty("crossIcon").objectReferenceValue = UISprite("icon_close");
        so.FindProperty("moneyIcon").objectReferenceValue = UISprite("icon_money");
        var icons = so.FindProperty("customerIcons");
        icons.arraySize = portraits.Length;
        for (int i = 0; i < portraits.Length; i++) icons.GetArrayElementAtIndex(i).objectReferenceValue = portraits[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        canvas.overrideSorting = false;
    }

    // Icon, label, value: "(check) Serve 2 perfect burgers ......... +$16"
    static GameObject BuildRowTemplate(Transform rows)
    {
        var row = Rect("RowTemplate", rows, V(0, 1), V(1, 1), V(0.5f, 1), V(0, 0), V(0, 46));
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
        var icon = Rect("Icon", row, V(0, 0.5f), V(0, 0.5f), V(0, 0.5f), V(0, 0), V(34, 34));
        var image = Img(icon.gameObject, "icon_check", UITokens.Colors.Teal);
        image.preserveAspect = true;
        var label = Text(Rect("Label", row, V(0, 0), V(1, 1), V(0, 0.5f), V(50, 0), V(-170, 0)), "Goal",
            SemiBold, UITokens.Typography.Body, UITokens.Colors.Ink, TextAlignmentOptions.Left);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        var value = Text(Rect("Value", row, V(1, 0), V(1, 1), V(1, 0.5f), V(0, 0), V(120, 0)), "+$0",
            SemiBold, 30f, UITokens.Colors.DarkTeal, TextAlignmentOptions.Right);
        value.textWrappingMode = TextWrappingModes.NoWrap;
        return row.gameObject;
    }

    // Picture, name, a line under it, on a cream tile (yellow when it is the one that matters: today's treat).
    static GameObject BuildTileTemplate(Transform tiles)
    {
        var tile = Rect("TileTemplate", tiles, V(0.5f, 0.5f), V(0.5f, 0.5f), V(0.5f, 0.5f), V(0, 0), V(150, 204));
        var le = tile.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 150f;
        le.preferredHeight = UIBreakCard.TileIcon + UIBreakCard.TileText;
        var back = Rect("Back", tile, V(0, 0), V(1, 1), V(0.5f, 0.5f), V(0, 0), V(0, 0));
        Img(back.gameObject, "ui_rounded", UITokens.Colors.CreamSoft, true, 2f);
        var icon = Rect("Icon", tile, V(0.5f, 1), V(0.5f, 1), V(0.5f, 1), V(0, -12), V(UIBreakCard.TileIcon, UIBreakCard.TileIcon));
        Img(icon.gameObject, "icon_star", UITokens.Colors.DeepYellow).preserveAspect = true;
        var name = Text(Rect("Name", tile, V(0, 0), V(1, 0), V(0.5f, 0), V(0, 50), V(-12, 34)), "Name",
            SemiBold, 24f, UITokens.Colors.Ink, TextAlignmentOptions.Center);
        name.textWrappingMode = TextWrappingModes.NoWrap;
        name.enableAutoSizing = true;
        name.fontSizeMin = 16f;
        name.fontSizeMax = 24f;
        var sub = Text(Rect("Sub", tile, V(0, 0), V(1, 0), V(0.5f, 0), V(0, 6), V(-14, 44)), "New recipe",
            Medium, 18f, UITokens.Colors.InkSoft, TextAlignmentOptions.Top);
        sub.lineSpacing = -14f;
        return tile.gameObject;
    }
    #endregion
}
