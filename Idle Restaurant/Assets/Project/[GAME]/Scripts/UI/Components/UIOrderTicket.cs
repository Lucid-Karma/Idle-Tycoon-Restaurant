using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// What a customer ordered, drawn in their order bubble as a little burger made of the real layers, bottom bun
// to top bun (Tools/UI/order_layers.py): the player reads "patty, cheese, onion" from colour at a glance, no text.
// It replaces the bubble's old generic burger icon (hidden while there is an order to show).
// Layers already on the plate being made for this customer fade back a little, so the stack also says what is
// still missing. On the order bubble of Npc_WS_Canvas, set up by Tools/Chibi Cafe/13 (CafeOrdersBuilder).
public class UIOrderTicket : MonoBehaviour
{
    [SerializeField] private Image genericIcon;
    [SerializeField] private Sprite bunTop, bunBottom, patty, cheese, lettuce, tomato, onion, bacon;
    // Fries on the side (NpcFsm.WantsFries): a little carton on the bubble's corner, fading back once they are tossed.
    [SerializeField] private Sprite fries;
    [SerializeField] private Vector2 box = new(66f, 62f);   // the space inside the patience ring
    [SerializeField, Range(0f, 0.6f)] private float overlap = 0.4f;
    [SerializeField, Range(0f, 1f)] private float doneAlpha = 0.35f;

    private NpcFsm npc;
    private Recipe shown;
    private readonly List<Image> layers = new();
    private readonly List<string> names = new();
    private int lastDone = -1;
    private Image friesBadge;
    private bool friesGot;
    private float friesPop = 1f;

    public Sprite Fries => fries;

    private void Awake() => npc = GetComponentInParent<NpcFsm>();

    private void LateUpdate()
    {
        var order = npc != null ? npc.Ordered : null;
        if (order != shown)
        {
            shown = order;
            Build(order);
        }
        if (order != null) ShowProgress();
        ShowFries(order != null && npc.WantsFries);
    }

    // The fries carton: bottom right of the bubble, over its ring; a pop as they arrive, then faded like a layer done.
    private void ShowFries(bool wanted)
    {
        if (!wanted || fries == null)
        {
            if (friesBadge != null) friesBadge.enabled = false;
            return;
        }
        if (friesBadge == null)
        {
            var go = new GameObject("Fries", typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(fries.rect.width, fries.rect.height) * (box.y * 0.62f / fries.rect.height);
            rt.anchoredPosition = new Vector2(box.x * 0.56f, -box.y * 0.36f);
            friesBadge = go.GetComponent<Image>();
            friesBadge.sprite = fries;
            friesBadge.raycastTarget = false;
            friesGot = false;
        }
        friesBadge.enabled = true;
        if (npc.GotFries != friesGot)
        {
            friesGot = npc.GotFries;
            if (friesGot) friesPop = 1.45f;
        }
        friesPop = Mathf.MoveTowards(friesPop, 1f, Time.deltaTime * 2.5f);
        friesBadge.rectTransform.localScale = Vector3.one * friesPop;
        var c = friesBadge.color;
        c.a = friesGot && friesPop <= 1.01f ? doneAlpha : 1f;
        friesBadge.color = c;
    }

    public Sprite SpriteOf(string layer) => layer switch
    {
        "burger" => patty,
        "cheese" => cheese,
        "lettuce" => lettuce,
        "tomato" => tomato,
        "onion" => onion,
        "bacon" => bacon,
        "fries" => fries,
        "bun_bottom" => bunBottom,
        "bun_top" => bunTop,
        _ => null,
    };

    private void Build(Recipe order)
    {
        foreach (var image in layers) Destroy(image.gameObject);
        layers.Clear();
        names.Clear();
        lastDone = -1;
        if (genericIcon != null) genericIcon.enabled = order == null;
        if (order == null) return;

        foreach (var (image, name) in Stack(transform, order, SpriteOf, bunBottom, bunTop, box, overlap))
        {
            layers.Add(image);
            names.Add(name);
        }
    }

    // A recipe as a little burger of layer images under parent, fitted to box: (image, ingredient; null for the
    // top bun) bottom to top. Also used for a new recipe on the break card (UIBreakCard).
    public static List<(Image image, string name)> Stack(Transform parent, Recipe order, System.Func<string, Sprite> spriteOf,
                                                         Sprite bunBottom, Sprite bunTop, Vector2 box, float overlap)
    {
        // Bottom to top: the bottom bun, the fillings in the order's order, the top bun.
        var sprites = new List<(Sprite sprite, string name)>();
        bool bun = System.Array.IndexOf(order.Layers, "bun") >= 0;
        if (bun) sprites.Add((bunBottom, "bun"));
        foreach (var layer in order.Layers)
            if (layer != "bun") sprites.Add((spriteOf(layer), layer));
        if (bun) sprites.Add((bunTop, null));

        // Lay them out in sprite pixels, then scale the whole stack to fit the box.
        float height = 0f, width = 0f;
        var bottoms = new List<float>();
        for (int i = 0; i < sprites.Count; i++)
        {
            var r = sprites[i].sprite.rect;
            bottoms.Add(height);
            height += i == sprites.Count - 1 ? r.height : r.height * (1f - overlap);
            width = Mathf.Max(width, r.width);
        }
        float scale = Mathf.Min(box.x / width, box.y / height);

        var made = new List<(Image, string)>();
        for (int i = 0; i < sprites.Count; i++)
        {
            var go = new GameObject("Layer " + (sprites[i].name ?? "top"), typeof(RectTransform), typeof(Image));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            var r = sprites[i].sprite.rect;
            rt.sizeDelta = new Vector2(r.width, r.height) * scale;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, bottoms[i] * scale - height * scale * 0.5f);
            var image = go.GetComponent<Image>();
            image.sprite = sprites[i].sprite;
            image.raycastTarget = false;
            made.Add((image, sprites[i].name));
        }
        return made;
    }

    // Fade back what is already on the plate being made for them (each layer once, bottom up).
    private void ShowProgress()
    {
        List<string> onPlate = null;
        foreach (var plate in Orders.Plates)
            if (plate != null && plate.ForCustomer == npc) { onPlate = new List<string>(plate.LayerNames); break; }
        int done = onPlate?.Count ?? 0;
        if (done == lastDone) return;
        lastDone = done;
        for (int i = 0; i < layers.Count; i++)
        {
            bool have = names[i] != null && onPlate != null && onPlate.Remove(names[i]);
            var c = layers[i].color;
            c.a = have ? doneAlpha : 1f;
            layers[i].color = c;
        }
    }
}
