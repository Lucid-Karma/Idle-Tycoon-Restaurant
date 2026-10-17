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
        public readonly float Hold;

        public Message(string text, Sprite sprite, Color tint, float hold)
        {
            Text = text;
            Sprite = sprite;
            Tint = tint;
            Hold = hold;
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
    private float hold;
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
        EventManager.OnLevelStart.AddListener(ShowObjectiveOnStart);
        EventManager.OnScoreUpdate.AddListener(OnScoreUpdate);
        EventManager.OnOrderRated.AddListener(OnOrderRated);
        EventManager.OnCustomerProtest.AddListener(OnProtest);
        CafeProgress.LeveledUp += OnCafeLevelUp;
        DayGoals.GoalMet += OnGoalMet;
        UIRewardedBonus.Doubled += OnDoubled;
    }

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(ShowObjectiveOnStart);
        EventManager.OnScoreUpdate.RemoveListener(OnScoreUpdate);
        EventManager.OnOrderRated.RemoveListener(OnOrderRated);
        EventManager.OnCustomerProtest.RemoveListener(OnProtest);
        CafeProgress.LeveledUp -= OnCafeLevelUp;
        DayGoals.GoalMet -= OnGoalMet;
        UIRewardedBonus.Doubled -= OnDoubled;
    }

    private void Start() => lastEarning = ScoreManager.Instance.totalLevelEarning;

    // "Day 2 · Shift 3   Serve 2 perfect burgers": the shift and its goal (DayGoals), which then stays under the
    // customers card.
    // Not over the first lesson (two things to read at once): the lesson shows it when it ends.
    private void ShowObjectiveOnStart()
    {
        if (CafeProgress.Taught) ShowObjective();
    }

    public void ShowObjective()
    {
        var (day, shift) = DayGoals.Now;
        Show($"Day {day} · Shift {shift}   <color=#{Hex(UITokens.Colors.Berry)}>{DayGoals.ForDay(DayGoals.Today).Text}</color>",
            boltIcon ? boltIcon : personIcon, UITokens.Colors.Berry, 2.6f);
    }

    // Between two shifts (ShiftFlow), instead of the old result card: "Great shift!  4.2 stars  +$58".
    public void ShiftSummary(float averageStars, int walkouts, int earned)
    {
        string title = UIShiftHighlights.TitleFor(averageStars, walkouts);
        Show($"{title}   <color=#{Hex(UITokens.Colors.DeepYellow)}>{averageStars.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)}<size=80%> stars</size></color>"
             + $"   <color=#{Hex(UITokens.Colors.DarkTeal)}>+${earned}</color>", starIcon, UITokens.Colors.DeepYellow, 3.2f);
    }

    // Spending fires OnScoreUpdate without an order; purchases show up in the kitchen itself (CafeShop),
    // and snack tips over the customer, so this only keeps the reference value in sync.
    private void OnScoreUpdate() => lastEarning = ScoreManager.Instance.totalLevelEarning;

    // "Goal done!  +$16 at closing": the day's goal was just reached (DayGoals).
    private void OnGoalMet(DayGoal goal, int bonus) =>
        Show($"Goal done!   <color=#{Hex(UITokens.Colors.DarkTeal)}>+${bonus}</color> at closing", checkIcon, UITokens.Colors.Teal, 3f);

    // "Doubled!  +$71 to your wallet": the video was watched and the shift's money paid again (UIRewardedBonus);
    // the day card counts it up as well.
    private void OnDoubled(int bonus) =>
        Show($"Doubled!   <color=#{Hex(UITokens.Colors.DarkTeal)}>+${bonus}</color> to your wallet", starIcon, UITokens.Colors.DeepYellow, 3f);

    // "Cafe level 3!  Barbarians drop by", queued behind the order that earned the stars.
    private void OnCafeLevelUp(int level)
    {
        var recipe = System.Linq.Enumerable.FirstOrDefault(Orders.NewAt(level));
        string news = recipe != null ? "New recipe: " + recipe.Name
            : Customers.ArrivesAt(level, out var kind) ? Customers.Of(kind).Plural + " drop by" : "New in the shop";
        queued.Enqueue(new Message($"Cafe level {level}!   <color=#{Hex(UITokens.Colors.DarkTeal)}>{news}</color>",
            starIcon, UITokens.Colors.DeepYellow, holdTime));
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

    private void Show(string text, Sprite sprite, Color tint, float holdFor = -1f)
    {
        label.text = text;
        icon.sprite = sprite;
        icon.color = tint;
        hold = holdFor > 0f ? holdFor : holdTime;
        age = 0f;
    }

    private void LateUpdate()
    {
        if (age > hold + UITokens.Motion.Slow)
        {
            if (queued.Count == 0) return;
            var next = queued.Dequeue();
            Show(next.Text, next.Sprite, next.Tint, next.Hold);
        }
        age += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        float fadeIn = Mathf.Clamp01(age / UITokens.Motion.Normal);
        float fadeOut = 1f - Mathf.Clamp01((age - hold) / UITokens.Motion.Slow);
        float a = Mathf.Min(fadeIn, fadeOut);
        group.alpha = a;
        float ease = 1f - Mathf.Pow(1f - fadeIn, 3f);
        rect.anchoredPosition = restPos + Vector2.up * (16f * (1f - ease));
    }

    private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
}
