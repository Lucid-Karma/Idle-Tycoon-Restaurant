using TMPro;
using UnityEngine;

// One line on the result card about how the shift went: the burger served most ("Signature burger:
// Chaos Burger") and the best rush streak.
[RequireComponent(typeof(TMP_Text))]
public class UIShiftHighlights : MonoBehaviour
{
    private TMP_Text label;

    private void Awake() => label = GetComponent<TMP_Text>();

    private void OnEnable() => EventManager.OnLevelFinish.AddListener(Refresh);
    private void OnDisable() => EventManager.OnLevelFinish.RemoveListener(Refresh);

    private void Refresh()
    {
        var score = ScoreManager.Instance;
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
}
