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
public class UIBreakCard : MonoBehaviour
{
    public sealed class Card
    {
        public string Kicker, Title, Footer;
        public int Stars = -1;                 // 0-3 stars of the day, -1 for none
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

    // A tile: its picture (at most) and the room under it for the name and the line below.
    public const float TileIcon = 104f, TileText = 100f;

    private readonly List<GameObject> made = new();
    private bool next;
    private TMP_Text earnedValue;
    private int earnedFrom, earnedTo;
    private float earnedAt = -10f;

    private void Awake()
    {
        Instance = this;
        group.alpha = 0f;
        group.blocksRaycasts = false;
        rowTemplate.SetActive(false);
        tileTemplate.SetActive(false);
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
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

    public void Play(List<Card> cards, Action done) => StartCoroutine(Run(cards, done));

    private IEnumerator Run(List<Card> cards, Action done)
    {
        Showing = true;
        // The "Double it" offer decides for itself at the end of the day (UIRewardedBonus.Offer): let it, first.
        yield return null;
        bool offerDouble = doubleButton != null && doubleButton.activeSelf;
        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            Fill(c);
            next = false;
            group.blocksRaycasts = true;
            if (doubleButton != null) doubleButton.SetActive(offerDouble && i == 0);
            if (nextLabel != null) nextLabel.text = i == cards.Count - 1 ? "Next shift" : "Next";
            if (c.Stars > 0 || c.Tiles.Count > 0) GameSfx.Play(GameSfx.Cue.LevelUp);
            float age = 0f;
            // No timer: the card waits for "Next" (a tap in its first moment is not taken, so a tap meant for the
            // kitchen does not skip it unread).
            while (!(next && age > 0.35f))
            {
                // Paused with the game (the shop or help card is open on top).
                if (Time.timeScale > 0f) age += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                Animate(c, age);
                CountEarned();
                yield return null;
            }
            if (doubleButton != null) doubleButton.SetActive(false);
            group.blocksRaycasts = false;
            for (float t = 0f; t < UITokens.Motion.Normal; t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f))
            {
                group.alpha = 1f - t / UITokens.Motion.Normal;
                yield return null;
            }
            group.alpha = 0f;
        }
        Showing = false;
        done?.Invoke();
    }

    private void Animate(Card c, float age)
    {
        float t = Mathf.Clamp01(age / UITokens.Motion.Slow);
        group.alpha = Mathf.Clamp01(age / UITokens.Motion.Normal);
        float s = Mathf.LerpUnclamped(0.92f, 1f, BackOut(t));
        card.localScale = new Vector3(s, s, 1f);
        for (int i = 0; i < stars.Length; i++)
        {
            if (!stars[i].gameObject.activeSelf) continue;
            float st = Mathf.Clamp01((age - 0.25f - 0.15f * i) / UITokens.Motion.Slow);
            float ss = i < c.Stars ? BackOut(st) : 1f;
            stars[i].rectTransform.localScale = new Vector3(ss, ss, 1f);
            stars[i].color = i < c.Stars && st > 0f ? UITokens.Colors.DeepYellow : UITokens.Colors.StarEmpty;
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

        tiles.gameObject.SetActive(c.Tiles.Count > 0);
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
}
