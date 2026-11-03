using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The level's goal (DayGoals) under the customers card: "LEVEL 5" with its three stars filling as they are earned,
// "Serve 2 perfect burgers", a small bar and "1/2". It turns teal with a check once met, and shows a cross if it can
// no longer be met (someone left hungry). The stars are made here (the break card's star sprite).
// Hidden during the first lesson (one thing at a time) and between days (the break card shows how it went).
// Built by Tools/Chibi Cafe/15 (CafeDaysBuilder).
[RequireComponent(typeof(CanvasGroup))]
public class UIDayGoal : MonoBehaviour
{
    [SerializeField] private TMP_Text label, goal, count;
    [SerializeField] private RectTransform fill;
    [SerializeField] private Image fillImage, mark;
    [SerializeField] private Sprite check, cross;

    private CanvasGroup group;
    private string shownGoal, shownCount;
    private int shownState = -1;
    private float punch;
    private Image[] starMarks;
    private int shownStars = -1;
    private float starPunch;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
    }

    private void LateUpdate()
    {
        var days = DayGoals.Instance;
        bool visible = days != null && days.Running && days.Goal != null && !Tutorial.Running;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, dt / UITokens.Motion.Slow);
        if (!visible) return;

        string goalText = days.Goal.Text;
        if (goalText != shownGoal)
        {
            shownGoal = goalText;
            label.text = DayGoals.Label(days.Day);
            goal.text = goalText;
        }

        float progress;
        string countText;
        if (days.Goal.Kind == GoalKind.Average)
        {
            float average = days.Average;
            progress = average / 5f;
            countText = average.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "<size=75%> avg</size>";
        }
        else
        {
            var (done, of) = days.Progress;
            progress = (float)done / Mathf.Max(1, of);
            countText = days.Goal.Kind == GoalKind.Earn ? $"${Mathf.Min(done, of)}/{of}" : $"{Mathf.Min(done, of)}/{of}";
        }
        if (countText != shownCount)
        {
            if (shownCount != null) punch = 1f;
            shownCount = countText;
            count.text = countText;
        }
        fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);

        // 0 under way, 1 met, 2 missed
        int state = days.Done || (days.Goal.Kind == GoalKind.Average && days.Average >= DayGoals.GoodAverage && ScoreManager.Instance.hostedCustomer > 0) ? 1
                  : days.Lost ? 2 : 0;
        if (state != shownState)
        {
            if (state == 1 && shownState == 0) punch = 1.6f;
            shownState = state;
            // Met: the card says so in words too, in teal.
            label.text = state == 1 ? "GOAL DONE!" : DayGoals.Label(days.Day);
            label.color = state == 1 ? UITokens.Colors.Teal : UITokens.Colors.InkMuted;
            mark.enabled = state != 0;
            mark.sprite = state == 1 ? check : cross;
            mark.color = state == 1 ? UITokens.Colors.Teal : UITokens.Colors.Tomato;
            fillImage.color = state == 1 ? UITokens.Colors.Teal : state == 2 ? UITokens.Colors.StarEmpty : UITokens.Colors.Yellow;
            goal.color = state == 2 ? UITokens.Colors.InkMuted : UITokens.Colors.Ink;
        }

        punch = Mathf.MoveTowards(punch, 0f, dt / UITokens.Motion.Slow);
        float s = 1f + (UITokens.Motion.PunchScale - 1f) * 0.5f * Mathf.Sin(Mathf.Clamp01(punch) * Mathf.PI);
        count.rectTransform.localScale = new Vector3(s, s, 1f);
        Stars(days, dt);
    }

    // Three small stars beside "LEVEL 5": filled (deep yellow) as they are earned, a pop when one lights up.
    private void Stars(DayGoals days, float dt)
    {
        if (starMarks == null)
        {
            var sprite = UIBreakCard.Instance != null ? UIBreakCard.Instance.starIcon : null;
            if (sprite == null) return;
            starMarks = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Star " + (i + 1), typeof(RectTransform));
                go.transform.SetParent(label.transform.parent, false);
                var image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                starMarks[i] = image;
            }
        }
        // After the label's words ("LEVEL 12"), on its line.
        var lr = label.rectTransform;
        float size = label.fontSize * 1.05f;
        float x0 = lr.anchoredPosition.x - lr.pivot.x * lr.rect.width + label.preferredWidth + size * 0.55f;
        int now = days.StarsNow;
        if (now > shownStars && shownStars >= 0) starPunch = 1f;
        shownStars = now;
        starPunch = Mathf.MoveTowards(starPunch, 0f, dt / UITokens.Motion.Slow);
        for (int i = 0; i < 3; i++)
        {
            var r = starMarks[i].rectTransform;
            r.anchorMin = lr.anchorMin;
            r.anchorMax = lr.anchorMin;
            r.pivot = new Vector2(0f, 0.5f);
            r.sizeDelta = new Vector2(size, size);
            r.anchoredPosition = new Vector2(x0 + i * size * 1.08f, lr.anchoredPosition.y + (0.5f - lr.pivot.y) * lr.rect.height);
            bool lit = i < now;
            starMarks[i].color = lit ? UITokens.Colors.DeepYellow : UITokens.Colors.StarEmpty;
            float pop = lit && i == now - 1 ? 1f + 0.45f * Mathf.Sin(Mathf.Clamp01(starPunch) * Mathf.PI) : 1f;
            r.localScale = new Vector3(pop, pop, 1f);
        }
    }
}
