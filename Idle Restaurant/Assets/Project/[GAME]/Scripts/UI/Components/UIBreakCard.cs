using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The break between two days (ShiftFlow): a few cards in a row over the empty cafe - how the day went (stars, the
// goal, the money), what the cafe just opened ("New!": recipes, customers, shop items) and the daily treat. They
// are the "something new every couple of minutes" of the first ten minutes. Each one waits for the player's "Next"
// (the last one says "Next day"): they used to go by themselves after a couple of seconds, too fast to read, with
// the "Double it" offer flashing by in a corner of the HUD (user: "it doesn't leave a nice feeling"). The offer is
// now on the day's card itself, next to "Next day", and a doubled day counts its money up on the card.
// The next customers only come in once the last card has gone.
// Rows and tiles are copies of templates the builder made (Tools/Chibi Cafe/15, CafeDaysBuilder), so the look lives
// with the rest of the UI.
// Cards opened from the HUD (the weekly tasks, the burger book) and the welcome back card close with a tap on the empty
// space around them (user, 2026-10-31: "any page except the level end ones"); the level end cards only go with "Next".
public class UIBreakCard : MonoBehaviour
{
    public sealed class Card
    {
        public string Kicker, Title, Footer;
        public int Stars = -1;                 // 0-3 stars of the day, -1 for none
        public bool Road;                      // the road of levels (drawn by the card itself)
        public int Double;                     // "Double" offered on this card: -1 the level's money, >0 this sum, 0 none
        public bool Book;                      // the burger book's names (drawn by the card itself)
        public readonly List<Row> Rows = new();
        public readonly List<Tile> Tiles = new();
        public float TileWidth = 150f;
    }

    public struct Row
    {
        public Sprite Icon;
        public Color Tint;
        public string Label, Value;
        public bool Earned;          // the day's money: counted up again when it is doubled
    }

    public struct Tile
    {
        public Sprite Icon;          // or
        public Recipe Recipe;        // a recipe drawn from its layers
        public Color Tint;
        public string Name, Sub;
        public bool Highlight;
    }

    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform card;
    [SerializeField] private TMP_Text kicker, title, footer;
    [SerializeField] private Image[] stars;
    [SerializeField] private RectTransform rows, tiles;
    [SerializeField] private GameObject rowTemplate, tileTemplate;
    [SerializeField] private UIOrderTicket layerSprites;    // the order bubble's layer sprites (Npc_WS_Canvas prefab)
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextLabel;
    [SerializeField] private GameObject doubleButton;       // UIRewardedBonus: offered on the day's card only
    [Header("Icons for the cards (ShiftFlow)")]
    public Sprite starIcon;
    public Sprite checkIcon, crossIcon, moneyIcon;
    public Sprite[] customerIcons;    // by CustomerKind (rendered by Tools/Chibi Cafe/15)
    public Sprite[] grillDishes;      // the Grill House's dishes (GrandGoal): roast dinner, stew, steak

    public static UIBreakCard Instance { get; private set; }
    public bool Showing { get; private set; }
    // What the last card's button says ("Play level 8"); the set of cards is made by ShiftFlow.
    public string LastButton = "Next shift";

    // A tile: its picture (at most) and the room under it for the name and the line below.
    public const float TileIcon = 104f, TileText = 100f;

    private readonly List<GameObject> made = new();
    private bool next, closeAll;
    private float cardShownAt;
    private Image catcher;   // the empty space around the card, taking taps while a set may be tapped away
    private TMP_Text earnedValue;
    private int earnedFrom, earnedTo;
    private float earnedAt = -10f;
    // The level's end cards (autoNext) sit aside in the bottom-right corner, small and a little see-through, like the
    // order ticket that lives there (user, 2026-11-05: in the middle they lay over the coach's tips while the next
    // level was already starting). Other cards keep the middle of the screen.
    private const float AsideScale = 0.7f, AsideAlpha = 0.88f;
    private static readonly Vector2 AsideMargin = new(-24f, 24f);
    private float cardScale = 1f, maxAlpha = 1f;

    private void Awake()
    {
        Instance = this;
        group.alpha = 0f;
        group.blocksRaycasts = false;
        rowTemplate.SetActive(false);
        tileTemplate.SetActive(false);
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        BuildCatcher();
    }

    // Transparent and not drawn (cullTransparentMesh), behind everything on the card and far bigger than the screen.
    // Taps on the card itself stop on its surface; the kitchen under the catcher gets none while it is on.
    private void BuildCatcher()
    {
        var go = new GameObject("TapOutside", typeof(RectTransform), typeof(CanvasRenderer), typeof(LayoutElement), typeof(OutsideTap));
        go.transform.SetParent(group.transform, false);
        go.transform.SetAsFirstSibling();
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        go.GetComponent<OutsideTap>().card = this;
        go.GetComponent<CanvasRenderer>().cullTransparentMesh = true;
        catcher = go.AddComponent<Image>();
        catcher.color = Color.clear;
        catcher.raycastTarget = false;
        var r = catcher.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(20000f, 20000f);
    }

    private sealed class OutsideTap : MonoBehaviour, IPointerClickHandler
    {
        public UIBreakCard card;
        public void OnPointerClick(PointerEventData eventData) => card.TapOutside();
    }

    private void TapOutside()
    {
        // Not in the card's first moment, like "Next": a tap meant for the kitchen does not close it unread.
        if (!catcher.raycastTarget || Time.unscaledTime - cardShownAt < 0.35f) return;
        closeAll = true;
        next = true;
        EventManager.OnClick.Invoke();
    }

    private void OnEnable() => UIRewardedBonus.Doubled += OnDoubled;
    private void OnDisable() => UIRewardedBonus.Doubled -= OnDoubled;

    private void OnNext()
    {
        next = true;
        GameSfx.Play(GameSfx.Cue.PutDown);
    }

    // The ad was watched: the day's money counts up to twice as much, in yellow, with a punch.
    private void OnDoubled(int bonus)
    {
        if (earnedValue == null) return;
        earnedFrom = earnedTo;
        earnedTo += bonus;
        earnedAt = Time.unscaledTime;
    }

    private void CountEarned()
    {
        if (earnedValue == null || earnedAt < 0f) return;
        float t = Mathf.Clamp01((Time.unscaledTime - earnedAt) / 0.9f);
        int shown = Mathf.RoundToInt(Mathf.Lerp(earnedFrom, earnedTo, 1f - (1f - t) * (1f - t)));
        earnedValue.text = $"+${shown}  <size=80%>x2!</size>";
        earnedValue.color = UITokens.Colors.DeepYellow;
        float s = 1f + 0.3f * Mathf.Sin(Mathf.Clamp01(t * 1.4f) * Mathf.PI);
        earnedValue.rectTransform.localScale = new Vector3(s, s, 1f);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // `autoNext`: each card moves on by itself after AutoNextSeconds, its button counting down - the shift-end cards,
    // where most players used to stop (CrazyGames 2026-11-05: average play 2.5 min, about the end of the first level).
    public void Play(List<Card> cards, Action done, bool tapOutsideCloses = false, bool autoNext = false) =>
        StartCoroutine(Run(cards, done, tapOutsideCloses, autoNext));

    public const float AutoNextSeconds = 4f;

    private IEnumerator Run(List<Card> cards, Action done, bool tapOutsideCloses, bool autoNext)
    {
        Showing = true;
        closeAll = false;
        catcher.raycastTarget = tapOutsideCloses;
        var home = (card.anchorMin, card.anchorMax, card.pivot, card.anchoredPosition);
        if (autoNext)
        {
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(1f, 0f);
            card.anchoredPosition = AsideMargin;
        }
        cardScale = autoNext ? AsideScale : 1f;
        maxAlpha = autoNext ? AsideAlpha : 1f;
        // The "Double it" offer decides for itself at the end of the day (UIRewardedBonus.Offer): let it, first.
        yield return null;
        bool offerDouble = doubleButton != null && doubleButton.activeSelf;
        var bonus = doubleButton != null ? doubleButton.GetComponent<UIRewardedBonus>() : null;
        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            Fill(c);
            next = false;
            group.blocksRaycasts = true;
            if (doubleButton != null)
            {
                if (c.Double > 0 && bonus != null) bonus.OfferAmount(c.Double);   // shows itself if it may
                else doubleButton.SetActive(offerDouble && c.Double < 0);
            }
            string label = i == cards.Count - 1 ? LastButton : "Next";
            if (nextLabel != null) nextLabel.text = label;
            int shownSecond = -1;
            if (c.Stars > 0 || c.Tiles.Count > 0) GameSfx.Play(GameSfx.Cue.LevelUp);
            float age = 0f;
            cardShownAt = Time.unscaledTime;
            // The card waits for "Next" (a tap in its first moment is not taken, so a tap meant for the kitchen does not
            // skip it unread) - or, with autoNext, for its few seconds, unless a "Double it" offer is up (that is a choice).
            bool auto = false;
            while (!(next && age > 0.35f))
            {
                // Paused with the game (the shop or help card is open on top).
                if (Time.timeScale > 0f) age += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                bool offer = doubleButton != null && doubleButton.activeSelf;
                if (autoNext && !offer)
                {
                    int left = Mathf.CeilToInt(AutoNextSeconds - age);
                    if (left != shownSecond && nextLabel != null) { shownSecond = left; nextLabel.text = $"{label} in {Mathf.Max(left, 1)}"; }
                    if (age >= AutoNextSeconds) { next = true; auto = true; }
                }
                Animate(c, age);
                CountEarned();
                yield return null;
            }
            if (autoNext) GoogleAnalytics.Event("break_card", "index", i, "of", cards.Count, "card", c.Kicker, "auto", auto);
            if (doubleButton != null) doubleButton.SetActive(false);
            group.blocksRaycasts = false;
            for (float t = 0f; t < UITokens.Motion.Normal; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
            {
                group.alpha = (1f - t / UITokens.Motion.Normal) * maxAlpha;
                yield return null;
            }
            group.alpha = 0f;
            if (closeAll) break;
        }
        catcher.raycastTarget = false;
        (card.anchorMin, card.anchorMax, card.pivot, card.anchoredPosition) = home;
        card.localScale = Vector3.one;
        cardScale = maxAlpha = 1f;
        Showing = false;
        done?.Invoke();
    }

    private void Animate(Card c, float age)
    {
        float t = Mathf.Clamp01(age / UITokens.Motion.Slow);
        group.alpha = Mathf.Clamp01(age / UITokens.Motion.Normal) * maxAlpha;
        float s = Mathf.LerpUnclamped(0.92f, 1f, BackOut(t)) * cardScale;
        card.localScale = new Vector3(s, s, 1f);
        for (int i = 0; i < stars.Length; i++)
        {
            if (!stars[i].gameObject.activeSelf) continue;
            float st = Mathf.Clamp01((age - 0.25f - 0.15f * i) / UITokens.Motion.Slow);
            float ss = i < c.Stars ? BackOut(st) : 1f;
            stars[i].rectTransform.localScale = new Vector3(ss, ss, 1f);
            stars[i].color = i < c.Stars && st > 0f ? UITokens.Colors.DeepYellow : UITokens.Colors.StarEmpty;
        }
        if (roadNext != null)
        {
            float beat = 1f + 0.1f * Mathf.Sin(age * 4f);
            roadNext.localScale = new Vector3(beat, beat, 1f);
        }
        for (int i = 0; i < made.Count; i++)
        {
            if (!made[i].transform.IsChildOf(tiles)) continue;
            float tt = Mathf.Clamp01((age - 0.15f - 0.08f * i) / UITokens.Motion.Slow);
            float ts = Mathf.LerpUnclamped(0.6f, 1f, BackOut(tt));
            made[i].transform.localScale = new Vector3(ts, ts, 1f);
        }
    }

    private static float BackOut(float t)
    {
        const float k = 1.70158f;
        t -= 1f;
        return 1f + t * t * ((k + 1f) * t + k);
    }

    private void Fill(Card c)
    {
        foreach (var go in made) Destroy(go);
        made.Clear();
        earnedValue = null;
        earnedAt = -10f;

        kicker.text = c.Kicker;
        title.text = c.Title;
        footer.text = c.Footer ?? "";
        footer.gameObject.SetActive(!string.IsNullOrEmpty(c.Footer));
        stars[0].transform.parent.gameObject.SetActive(c.Stars >= 0);
        foreach (var star in stars) star.gameObject.SetActive(c.Stars >= 0);

        rows.gameObject.SetActive(c.Rows.Count > 0);
        foreach (var r in c.Rows)
        {
            var row = Instantiate(rowTemplate, rows);
            row.SetActive(true);
            var icon = row.transform.Find("Icon").GetComponent<Image>();
            icon.sprite = r.Icon;
            icon.color = r.Tint;
            row.transform.Find("Label").GetComponent<TMP_Text>().text = r.Label;
            var value = row.transform.Find("Value").GetComponent<TMP_Text>();
            value.text = r.Value;
            if (r.Earned)
            {
                earnedValue = value;
                int.TryParse(r.Value.Replace("+", "").Replace("$", ""), out earnedTo);
            }
            made.Add(row);
        }

        tiles.gameObject.SetActive(c.Tiles.Count > 0 || c.Road || c.Book);
        roadNext = null;
        if (c.Road) BuildRoad();
        if (c.Book) BuildBook();
        foreach (var t in c.Tiles)
        {
            var tile = Instantiate(tileTemplate, tiles);
            tile.SetActive(true);
            // Narrow tiles (the daily treat's week) get a smaller picture and a shorter tile.
            float iconSize = Mathf.Min(TileIcon, c.TileWidth - 24f);
            var size = tile.GetComponent<LayoutElement>();
            size.preferredWidth = c.TileWidth;
            size.preferredHeight = iconSize + TileText;
            var back = tile.transform.Find("Back").GetComponent<Image>();
            back.color = t.Highlight ? UITokens.Colors.Yellow : UITokens.Colors.CreamSoft;
            var icon = tile.transform.Find("Icon").GetComponent<Image>();
            icon.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            if (t.Recipe != null && layerSprites != null)
            {
                icon.enabled = false;
                var box = new Vector2(iconSize, iconSize);
                UIOrderTicket.Stack(icon.transform, t.Recipe, layerSprites.SpriteOf, layerSprites.SpriteOf("bun_bottom"),
                    layerSprites.SpriteOf("bun_top"), box, 0.4f);
            }
            else
            {
                icon.sprite = t.Icon;
                icon.color = t.Tint;
                icon.preserveAspect = true;
            }
            tile.transform.Find("Name").GetComponent<TMP_Text>().text = t.Name;
            tile.transform.Find("Sub").GetComponent<TMP_Text>().text = t.Sub ?? "";
            made.Add(tile);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(card);
    }

    #region The road of levels
    // Seven stops along a line: the last three levels played with the stars each earned, the next one (big, yellow,
    // breathing) and the three after it, with a picture over any that opens something new - its new customer, recipe
    // or shop item (CafeProgress.TierOpensAt). The "one more level" pull: where you are, and what is coming.
    private const float RoadStep = 92f, RoadDot = 54f, RoadNextDot = 72f;
    private RectTransform roadNext;

    private void BuildRoad()
    {
        var root = new GameObject("Road", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(tiles, false);
        var size = root.gameObject.AddComponent<LayoutElement>();
        size.preferredWidth = RoadStep * 6f + RoadNextDot + 24f;
        size.preferredHeight = 210f;
        made.Add(root.gameObject);

        int next = CafeProgress.LevelNumber;
        int first = Mathf.Max(1, next - 3);
        // the line under the stops: done (teal) up to the next level, still to come (soft pink) after it
        for (int i = 0; i < 6; i++)
        {
            int level = first + i;
            var line = Piece(root, "Line", null, level < next ? UITokens.Colors.Teal : UITokens.Colors.StarEmpty);
            line.sizeDelta = new Vector2(RoadStep, 8f);
            line.anchoredPosition = new Vector2(X(i) + RoadStep * 0.5f, -8f);
        }
        for (int i = 0; i < 7; i++)
        {
            int level = first + i;
            float x = X(i);
            bool done = level < next, current = level == next;
            if (current)
            {
                var halo = Piece(root, "Halo", Disc(), UITokens.WithAlpha(UITokens.Colors.Yellow, 0.35f));
                halo.sizeDelta = Vector2.one * (RoadNextDot + 18f);
                halo.anchoredPosition = new Vector2(x, -8f);
                roadNext = halo;
            }
            var dot = Piece(root, "Stop " + level, Disc(), done ? UITokens.Colors.Teal : current ? UITokens.Colors.Yellow : UITokens.Colors.LightPink);
            dot.sizeDelta = Vector2.one * (current ? RoadNextDot : RoadDot);
            dot.anchoredPosition = new Vector2(x, -8f);
            var number = Words(dot, level.ToString(), current ? 30f : 22f, done ? UITokens.Colors.WarmWhite : current ? UITokens.Colors.Ink : UITokens.Colors.Berry);
            number.rectTransform.sizeDelta = dot.sizeDelta;
            if (done)
            {
                int earned = CafeProgress.StarsOf(level);
                for (int s = 0; s < 3; s++)
                {
                    var star = Piece(root, "Star", starIcon, s < earned ? UITokens.Colors.DeepYellow : UITokens.Colors.StarEmpty);
                    star.sizeDelta = Vector2.one * 20f;
                    star.anchoredPosition = new Vector2(x + (s - 1) * 21f, -56f);
                }
            }
            else if (current)
            {
                var label = Words(root, "NEXT", 16f, UITokens.Colors.Berry);
                label.rectTransform.sizeDelta = new Vector2(RoadStep, 24f);
                label.rectTransform.anchoredPosition = new Vector2(x, -64f);
            }
            int tier = CafeProgress.TierOpensAt(level);
            if (tier > 0 && !done) Unlock(root, tier, new Vector2(x, 62f));
            else if (!done && Stations.OpensOn(level, out var station)) UnlockStation(root, station, new Vector2(x, 62f));
        }
    }

    private static float X(int i) => (i - 3) * RoadStep;

    // The burger book: every name in a grid of pills - found ones in ink, the rest as "? ? ?".
    private void BuildBook()
    {
        const int columns = 5;
        const float w = 122f, h = 40f, gap = 8f;
        int rowsCount = (BurgerBook.All.Length + columns - 1) / columns;
        var root = new GameObject("Book", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(tiles, false);
        var size = root.gameObject.AddComponent<LayoutElement>();
        size.preferredWidth = columns * w + (columns - 1) * gap;
        size.preferredHeight = rowsCount * (h + gap);
        made.Add(root.gameObject);
        for (int i = 0; i < BurgerBook.All.Length; i++)
        {
            string name = BurgerBook.All[i];
            bool has = BurgerBook.Has(name);
            int col = i % columns, row = i / columns;
            var pill = Piece(root, name, UIShapes.Pill(), has ? UITokens.Colors.CreamSoft : UITokens.Colors.LightPink);
            pill.sizeDelta = new Vector2(w, h);
            pill.anchoredPosition = new Vector2((col - (columns - 1) * 0.5f) * (w + gap), ((rowsCount - 1) * 0.5f - row) * (h + gap));
            pill.GetComponent<Image>().type = Image.Type.Sliced;
            var words = Words(pill, has ? name : "? ? ?", 20f, has ? UITokens.Colors.Ink : UITokens.Colors.Berry);
            words.fontStyle = has ? FontStyles.Bold : FontStyles.Normal;
            words.enableAutoSizing = true;
            words.fontSizeMin = 13f;
            words.fontSizeMax = 20f;
            words.rectTransform.sizeDelta = new Vector2(w - 12f, h);
        }
    }

    // What a tier brings, as one picture over its stop: its new customer, else its first recipe, else a shop item.
    private void Unlock(RectTransform root, int tier, Vector2 at)
    {
        var back = Piece(root, "Unlock", Disc(), UITokens.Colors.CreamSoft);
        back.sizeDelta = Vector2.one * 64f;
        back.anchoredPosition = at;
        Sprite picture = null;
        if (Customers.ArrivesAt(tier, out var kind) && customerIcons != null && (int)kind < customerIcons.Length) picture = customerIcons[(int)kind];
        var recipe = System.Linq.Enumerable.FirstOrDefault(Orders.NewAt(tier));
        if (picture == null && recipe != null && layerSprites != null)
        {
            UIOrderTicket.Stack(back, recipe, layerSprites.SpriteOf, layerSprites.SpriteOf("bun_bottom"), layerSprites.SpriteOf("bun_top"),
                new Vector2(48f, 48f), 0.4f);
            return;
        }
        if (picture == null && CafeShop.Instance != null)
        {
            Upgrade first = null;
            foreach (var u in CafeShop.Instance.Upgrades)
                if (u.level == tier && (first == null || u.price < first.price)) first = u;
            if (first != null) picture = first.icon;
        }
        if (picture == null) return;
        var image = Piece(back, "Picture", picture, Color.white);
        image.sizeDelta = Vector2.one * 56f;
        image.GetComponent<Image>().preserveAspect = true;
    }

    // A station's level on the road: what it makes (the order bubbles' bacon strip, the fries carton).
    private void UnlockStation(RectTransform root, Stations.Kind station, Vector2 at)
    {
        var back = Piece(root, "Unlock", Disc(), UITokens.Colors.CreamSoft);
        back.sizeDelta = Vector2.one * 64f;
        back.anchoredPosition = at;
        var picture = StationPicture(station);
        if (picture == null) return;
        var image = Piece(back, "Picture", picture, Color.white);
        image.sizeDelta = Vector2.one * 46f;
        image.GetComponent<Image>().preserveAspect = true;
    }

    public Sprite StationPicture(Stations.Kind station) =>
        layerSprites == null ? null : layerSprites.SpriteOf(station == Stations.Kind.Bacon ? "bacon" : "fries");

    private static RectTransform Piece(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return (RectTransform)go.transform;
    }

    private TMP_Text Words(Transform parent, string text, float size, Color color)
    {
        var go = new GameObject("Words", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var words = go.AddComponent<TextMeshProUGUI>();
        words.font = kicker.font;
        words.text = text;
        words.fontSize = size;
        words.fontStyle = FontStyles.Bold;
        words.color = color;
        words.alignment = TextAlignmentOptions.Center;
        words.raycastTarget = false;
        return words;
    }

    private static Sprite Disc() => UIShapes.Disc();
    #endregion
}
