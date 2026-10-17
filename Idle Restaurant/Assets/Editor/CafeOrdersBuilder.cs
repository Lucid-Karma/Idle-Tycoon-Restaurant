using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Step 13 (re-runnable): customers order a specific burger (Orders). Imports the layer sprites drawn by
// Tools/UI/order_layers.py and puts a UIOrderTicket in every customer's order bubble (the shared
// Npc_WS_Canvas prefab), in place of the bubble's generic burger icon.
public static partial class CafeGrowthBuilder
{
    const string OrderSprites = Game + "Graphics/Sprites/Orders/";
    const string NpcCanvasPrefab = Game + "Prefabs/CharacterPrefabs/NpcPrefabs/Npc_WS_Canvas.prefab";

    [MenuItem("Tools/Chibi Cafe/13 Orders: Order Tickets in the Bubbles")]
    public static void BuildOrders()
    {
        foreach (var name in new[] { "bun_top", "bun_bottom", "burger", "cheese", "lettuce", "tomato", "onion" })
        {
            string path = OrderSprites + "layer_" + name + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path) ?? throw new System.Exception("no " + path + " (run Tools/UI/order_layers.py)");
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(OrderSprites + "layer_" + name + ".png");

        var root = PrefabUtility.LoadPrefabContents(NpcCanvasPrefab);
        try
        {
            var bubble = root.transform.Find("OrderProgressBackground/Bubble");
            // The order bubble is drawn a little smaller than the eating bubble beside it: at 1.35x (the first
            // version) the burgers in it were so big that the customers under them could not be seen (user).
            // Absolute values, from the eating bubble's own scale, so the step can be re-run (a relative factor once
            // made it 100x too big).
            var bubbleRect = (RectTransform)bubble;
            float baseScale = root.transform.Find("EatProgressBackground/Bubble").localScale.x;   // 0.0135
            bubbleRect.localScale = new Vector3(baseScale * 0.8f, baseScale * 0.8f, 1f);
            bubbleRect.anchoredPosition = new Vector2(0f, 0.2f);
            var old = bubble.Find("Ticket");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var icon = bubble.Find("Icon").GetComponent<Image>();

            var ticket = new GameObject("Ticket", typeof(RectTransform)).GetComponent<RectTransform>();
            ticket.SetParent(bubble, false);
            ticket.SetSiblingIndex(icon.transform.GetSiblingIndex() + 1);
            ticket.anchorMin = ticket.anchorMax = ticket.pivot = new Vector2(0.5f, 0.5f);
            ticket.sizeDelta = new Vector2(100f, 100f);
            ticket.anchoredPosition = new Vector2(0f, 1f);
            var t = ticket.gameObject.AddComponent<UIOrderTicket>();
            var so = new SerializedObject(t);
            so.FindProperty("genericIcon").objectReferenceValue = icon;
            so.FindProperty("bunTop").objectReferenceValue = S("bun_top");
            so.FindProperty("bunBottom").objectReferenceValue = S("bun_bottom");
            so.FindProperty("patty").objectReferenceValue = S("burger");
            so.FindProperty("cheese").objectReferenceValue = S("cheese");
            so.FindProperty("lettuce").objectReferenceValue = S("lettuce");
            so.FindProperty("tomato").objectReferenceValue = S("tomato");
            so.FindProperty("onion").objectReferenceValue = S("onion");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, NpcCanvasPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log("[CafeGrowth] order tickets built");
    }
}
