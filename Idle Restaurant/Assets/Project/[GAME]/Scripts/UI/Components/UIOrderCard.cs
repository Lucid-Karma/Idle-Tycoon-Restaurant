using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The order being made, as a ticket in the bottom-right corner (user, 2026-10-15: "players can't make a perfect
// burger: the burger pictures are too small, you can't tell the ingredients - but they can't be bigger either, or
// the customers disappear. Whatever the recipe, the player should easily know what to use - and still know they
// are free."). The order bubbles keep their little burgers; this ticket spells the order out:
//   (face) ORDER  Deluxe                         + Onion · extra
//   [bun]   [patty]   [cheese]  [lettuce] [tomato]      a tile per ingredient: the ingredient as it looks
//    Bun     Patty     Cheese    Lettuce   Tomato       ready, its name and how it has to be made; a tick
//   baked    cooked    sliced    fresh     sliced       when it is on the plate ready, a red badge when it is
//   Exactly this = Perfect  ·  any burger still sells    on but not (flame: raw, knife: whole, cross: burnt)
// It mirrors the "Last order" card in the bottom-left corner: what you just served on the left, what you are
// making on the right. That corner is empty floor - no station, plate, table or customer (measured, with every
// shop upgrade); a recipe column on the right edge lay over the onion crate, one on the left over the big tomato.
// Which order: the burger in the chef's hands, else the plate being stacked (most layers), else the next order to
// start (Orders.NextUp: the one a first layer will be for); the portrait wears the order's number (UIOrderTags). Its background is frosted (UIFrostedCard) so the cafe shows through; solid while the pointer
// is over it and for a moment whenever something changes (a layer goes on, the order changes). Hidden during the first lesson (Mochi's card does the talking) and the break cards. Built by step 15.
[RequireComponent(typeof(CanvasGroup))]
public class UIOrderCard : MonoBehaviour
{
    [SerializeField] private TMP_Text title, extras, footer;
    [SerializeField] private Image portrait;
    [SerializeField] private Sprite personIcon;
    [SerializeField] private RectTransform tiles;
    [SerializeField] private GameObject tileTemplate;
    [SerializeField] private UIOrderTicket layerSprites;   // fallback pictures (the order bubble's layer strips)
    // Each ingredient as it looks ready, rendered from the game's own models at the game camera's angle.
    [SerializeField] private Sprite bunIcon, pattyIcon, cheeseIcon, lettuceIcon, tomatoIcon, onionIcon;
    [SerializeField] private Sprite check, warn, chop, burnt;   // on it / raw / whole / burnt

    private CanvasGroup group;
    private UIFrostedCard frosted;   // its background steps back when nothing happens; the words stay sharp
    private string shownKey;
    private readonly List<GameObject> made = new();
    // The order's number on the portrait's corner: the same badge the customer's bubble, the plate and the burger
    // wear (UIOrderTags), so "this card is for customer 2" is read without reading.
    private UIOrderTags.Badge badge;
    private NpcFsm shownWho;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        frosted = GetComponent<UIFrostedCard>();
        tileTemplate.SetActive(false);
        UIOrderTags.Create(title.font);
        float size = BadgeSize;
        badge = UIOrderTags.MakeBadge(portrait.transform, title.font, size);
        badge.Rect.anchorMin = badge.Rect.anchorMax = new Vector2(1f, 1f);
        badge.Rect.anchoredPosition = new Vector2(-size * 0.05f, -size * 0.05f);
        badge.Hide();
    }

    private float BadgeSize => ((RectTransform)portrait.transform).rect.height * 0.55f;

    private void LateUpdate()
    {
        var (order, who, layers) = Current();
        bool visible = order != null && CafeProgress.Taught && !Tutorial.Running
                       && (UIBreakCard.Instance == null || !UIBreakCard.Instance.Showing);
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, dt / UITokens.Motion.Normal);
        group.blocksRaycasts = visible;   // for the pointer-over; it lies on empty floor, nothing to tap under it
        if (!visible) { shownKey = null; return; }
        badge.Size(BadgeSize);   // (runs its pop)

        string key = order.Name + "|" + (who != null ? who.GetInstanceID() + "#" + who.Ticket : "0") + "|" + string.Join(",", layers.Select(l => l.name + (int)l.prep));
        if (key == shownKey) return;
        if (shownKey != null && frosted != null) frosted.Flash();
        shownKey = key;
        Fill(order, who, layers);
    }

    // The order to show and what is already in it.
    private static (Recipe order, NpcFsm who, List<(string name, Prep prep)> layers) Current()
    {
        var player = FindChef();
        if (player != null && player.HeldFood is Hamburger burger && burger.ForCustomer != null && burger.ForCustomer.Ordered != null)
            return (burger.ForCustomer.Ordered, burger.ForCustomer, burger.Layers.Select(l => (l.Name, l.Prep)).ToList());

        Plate best = null;
        foreach (var plate in Orders.AllPlates)
            if (plate != null && !plate.HasHamburger && plate.LayerCount > 0 && plate.Order != null
                && (best == null || plate.LayerCount > best.LayerCount))
                best = plate;
        if (best != null) return (best.Order, best.ForCustomer, best.LayerPreps.ToList());

        // Nothing on a plate: the next order to start - the one the first layer will be for (Orders.NextUp). Only if
        // every waiting order is already being made, the one closest to giving up.
        var next = Orders.NextUp();
        if (next == null)
            foreach (var npc in NpcFsm.Active)
                if (Orders.Waiting(npc) && (next == null || npc.WaitedSeconds / npc.Patience > next.WaitedSeconds / next.Patience))
                    next = npc;
        return next != null ? (next.Ordered, next, new List<(string, Prep)>()) : (null, null, null);
    }

    private static PlayerFSM chef;
    private static PlayerFSM FindChef() => chef != null ? chef : chef = FindFirstObjectByType<PlayerFSM>();

    private static string NameOf(string layer) => layer == "burger" ? "Patty" : char.ToUpperInvariant(layer[0]) + layer.Substring(1);

    // How it has to be made to count as perfect.
    private static string PrepOf(string layer) => layer switch
    {
        "bun" => "baked",
        "burger" => "cooked",
        "lettuce" => "fresh",
        _ => "sliced",
    };

    // What is wrong with a layer that is on but not ready.
    private Sprite ProblemOf(Prep prep) => prep switch
    {
        Prep.Raw => warn,
        Prep.Burnt => burnt,
        Prep.Whole => chop,
        _ => null,
    };

    private Sprite SpriteOf(string layer)
    {
        var icon = layer switch
        {
            "bun" => bunIcon,
            "burger" => pattyIcon,
            "cheese" => cheeseIcon,
            "lettuce" => lettuceIcon,
            "tomato" => tomatoIcon,
            "onion" => onionIcon,
            _ => null,
        };
        if (icon != null) return icon;
        return layerSprites == null ? null : layerSprites.SpriteOf(layer == "bun" ? "bun_bottom" : layer);
    }

    private void Fill(Recipe order, NpcFsm who, List<(string name, Prep prep)> layers)
    {
        foreach (var go in made) Destroy(go);
        made.Clear();

        var icons = UIBreakCard.Instance != null ? UIBreakCard.Instance.customerIcons : null;
        var face = who != null && icons != null && (int)who.Kind < icons.Length ? icons[(int)who.Kind] : null;
        portrait.sprite = face != null ? face : personIcon;
        portrait.color = face != null ? Color.white : UITokens.Colors.InkMuted;
        title.text = order.Name;
        if (who != null && who.Ticket > 0)
        {
            if (who != shownWho || badge.Ticket != who.Ticket) badge.Pop();
            badge.Set(who.Ticket);
        }
        else badge.Hide();
        shownWho = who;

        // Match what is on the plate to the order, layer by layer (bottom up, as it is stacked); the rest are extras.
        var unused = new List<(string name, Prep prep)>(layers);
        int good = 0;
        foreach (var layer in order.Layers)
        {
            int at = unused.FindIndex(l => l.name == layer && l.prep == Prep.Good);
            if (at < 0) at = unused.FindIndex(l => l.name == layer);
            (string name, Prep prep)? got = at >= 0 ? unused[at] : null;
            if (at >= 0) unused.RemoveAt(at);
            var problem = got.HasValue ? ProblemOf(got.Value.prep) : null;
            if (got.HasValue && problem == null) good++;
            AddTile(layer, got.HasValue, !got.HasValue ? null : problem ?? check,
                    problem == null ? UITokens.Colors.Teal : UITokens.Colors.Tomato);
        }

        // Extras are allowed: named in grey, never in red.
        var names = unused.Select(l => NameOf(l.name)).Distinct().ToList();
        extras.text = names.Count == 0 ? ""
            : "+ " + string.Join(", ", names.Take(2)) + (names.Count > 2 ? $" +{names.Count - 2}" : "") + " <size=85%>· extra</size>";

        // (No star glyph: Fredoka has none, it showed as nothing.)
        bool perfect = good == order.Layers.Length && unused.Count == 0;
        string perfectWord = $"<color=#{ColorUtility.ToHtmlStringRGB(UITokens.Colors.DeepYellow)}>Perfect</color>";
        footer.text = perfect && layers.Count > 0
            ? $"<color=#{ColorUtility.ToHtmlStringRGB(UITokens.Colors.DarkTeal)}>All there - close it for a Perfect!</color>"
            : $"Exactly this = {perfectWord}  ·  any burger still sells";
    }

    private void AddTile(string layer, bool onPlate, Sprite mark, Color markTint)
    {
        var tile = Instantiate(tileTemplate, tiles);
        tile.SetActive(true);
        made.Add(tile);
        float alpha = onPlate ? 1f : 0.6f;
        var icon = tile.transform.Find("Icon").GetComponent<Image>();
        icon.sprite = SpriteOf(layer);
        icon.enabled = icon.sprite != null;
        icon.color = new Color(1f, 1f, 1f, alpha);
        var name = tile.transform.Find("Name").GetComponent<TMP_Text>();
        name.text = NameOf(layer);
        name.alpha = Mathf.Max(alpha, 0.75f);
        tile.transform.Find("Prep").GetComponent<TMP_Text>().text = PrepOf(layer);
        var markImage = tile.transform.Find("Mark").GetComponent<Image>();
        markImage.enabled = mark != null;
        markImage.sprite = mark;
        markImage.color = markTint;
    }
}
