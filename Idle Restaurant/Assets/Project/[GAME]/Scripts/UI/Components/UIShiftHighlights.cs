using TMPro;
using UnityEngine;

// One line on the result card about how the shift went: the burger served most ("Signature burger:
// Chaos Burger") and the best rush streak. It also gives the card's title the shift's mood: it said "Shift
// complete!" whether every customer left happy or nobody got fed.
[RequireComponent(typeof(TMP_Text))]
public class UIShiftHighlights : MonoBehaviour
{
    private TMP_Text label;
    private TMP_Text title;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        title = transform.parent.Find("Title")?.GetComponent<TMP_Text>();
    }

    private void OnEnable() => EventManager.OnLevelFinish.AddListener(Refresh);
    private void OnDisable() => EventManager.OnLevelFinish.RemoveListener(Refresh);

    private void Refresh()
    {
        var score = ScoreManager.Instance;
        if (title != null) title.text = TitleFor(score.GetLevelFinalScore(), score.WalkoutCount);
        if (score.ServedCount == 0)
        {
            label.text = "Nobody got fed... yet!";
            return;
        }
        string ink = ColorUtility.ToHtmlStringRGB(UITokens.Colors.Ink);
        string text = $"Signature burger: <color=#{ink}>{score.SignatureBurger}</color>";
        if (score.BestStreak >= 2) text += $"   ·   Best rush <color=#{ink}>x{score.BestStreak}</color>";
        label.text = text;
    }

    // Cozy even when it went badly: a rough shift is a story, not a failure.
    private static string TitleFor(float average, int walkouts) =>
        average >= 4.5f && walkouts == 0 ? "Perfect shift!"
        : average >= 3.5f ? "Great shift!"
        : average >= 2.5f ? "Nice shift!"
        : average >= 1.5f ? "Busy shift!"
        : "Rough shift!";
}
