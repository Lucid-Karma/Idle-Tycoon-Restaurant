using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top-center, short-lived feedback line: the served burger's name + money earned (+ rush streak),
// customers leaving, the shift objective and the cafe levelling up. One message at a time; a new one
// replaces the old, except queued ones (level up), which wait for the current message to finish.
[RequireComponent(typeof(CanvasGroup))]
public class UIToast : MonoBehaviour
{
    private readonly struct Message
    {
        public readonly string Text;
        public readonly Sprite Sprite;
        public readonly Color Tint;

        public Message(string text, Sprite sprite, Color tint)
        {
            Text = text;
            Sprite = sprite;
            Tint = tint;
        }
    }

    private readonly System.Collections.Generic.Queue<Message> queued = new();

    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite starIcon, checkIcon, crossIcon, cartIcon, personIcon;
    [SerializeField] private Sprite faceLove, faceLaugh, faceShock, boltIcon;
    [SerializeField] private float holdTime = 1.8f;

    private CanvasGroup group;
    private RectTransform rect;
    private Vector2 restPos;
    private float age = float.MaxValue;
    private int lastEarning;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        rect = (RectTransform)transform;
        restPos = rect.anchoredPosition;
        group.alpha = 0f;
    }

    private void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(ShowObjective);
        EventManager.OnScoreUpdate.AddListener(OnScoreUpdate);
        EventManager.OnOrderRated.AddListener(OnOrderRated);
        EventManager.OnCustomerProtest.AddListener(OnProtest);
        CafeProgress.LeveledUp += OnCafeLevelUp;
    }

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(ShowObjective);
        EventManager.OnScoreUpdate.RemoveListener(OnScoreUpdate);
        EventManager.OnOrderRated.RemoveListener(OnOrderRated);
        EventManager.OnCustomerProtest.RemoveListener(OnProtest);
        CafeProgress.LeveledUp -= OnCafeLevelUp;
    }

    private void Start() => lastEarning = ScoreManager.Instance.totalLevelEarning;

    private void ShowObjective() =>
        Show($"Rush hour! Serve {ScoreManager.Instance.CustomersPerLevel} customers", boltIcon ? boltIcon : personIcon, UITokens.Colors.Berry);

    // Spending fires OnScoreUpdate without an order; purchases show up in the kitchen itself (CafeShop),
    // and snack tips over the customer, so this only keeps the reference value in sync.
    private void OnScoreUpdate() => lastEarning = ScoreManager.Instance.totalLevelEarning;

    // "Cafe level 3!  Barbarians drop by", queued behind the order that earned the stars.
    private void OnCafeLevelUp(int level)
    {
        string news = Customers.ArrivesAt(level, out var kind) ? Customers.Of(kind).Plural + " drop by" : "New in the shop";
        queued.Enqueue(new Message($"Cafe level {level}!   <color=#{Hex(UITokens.Colors.DarkTeal)}>{news}</color>",
            starIcon, UITokens.Colors.DeepYellow));
    }

    // "Charcoal Special  +$7  RUSH x2"
    private void OnOrderRated()
    {
        var order = ScoreManager.Instance.LastOrder;
        lastEarning = ScoreManager.Instance.totalLevelEarning;

        string text = order.Review.Title + $"   <color=#{Hex(UITokens.Colors.DarkTeal)}>+${order.Earned}</color>";
        if (order.RushStreak >= 2)
            text += $"   <color=#{Hex(UITokens.Colors.DeepYellow)}>RUSH x{order.RushStreak}</color>";
        else if (order.Speedy)
            text += $"   <color=#{Hex(UITokens.Colors.DeepYellow)}>SPEEDY</color>";

        var (sprite, tint) = MoodIcon(order.Mood);
        Show(text, sprite, tint);
    }

    private (Sprite, Color) MoodIcon(Mood mood) => mood switch
    {
        Mood.Delighted => (faceLove ? faceLove : starIcon, UITokens.MoodColor(mood)),
        Mood.Amused => (faceLaugh ? faceLaugh : checkIcon, UITokens.MoodColor(mood)),
        _ => (faceShock ? faceShock : crossIcon, UITokens.MoodColor(mood)),
    };

    private void OnProtest() => Show("A customer left hungry", personIcon, UITokens.Colors.Tomato);

    private void Show(string text, Sprite sprite, Color tint)
    {
        label.text = text;
        icon.sprite = sprite;
        icon.color = tint;
        age = 0f;
    }

    private void LateUpdate()
    {
        if (age > holdTime + UITokens.Motion.Slow)
        {
            if (queued.Count == 0) return;
            var next = queued.Dequeue();
            Show(next.Text, next.Sprite, next.Tint);
        }
        age += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        float fadeIn = Mathf.Clamp01(age / UITokens.Motion.Normal);
        float fadeOut = 1f - Mathf.Clamp01((age - holdTime) / UITokens.Motion.Slow);
        float a = Mathf.Min(fadeIn, fadeOut);
        group.alpha = a;
        float ease = 1f - Mathf.Pow(1f - fadeIn, 3f);
        rect.anchoredPosition = restPos + Vector2.up * (16f * (1f - ease));
    }

    private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
}
