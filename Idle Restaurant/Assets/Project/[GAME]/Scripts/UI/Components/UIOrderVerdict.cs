using TMPro;
using UnityEngine;

// One-word verdict for the last served order, shown next to the star rating on the chef card.
// Driven by the same events the chef portrait reacts to, so text, stars and chef always agree.
[RequireComponent(typeof(TMP_Text))]
public class UIOrderVerdict : MonoBehaviour
{
    [SerializeField] private string waitingText = "No orders yet";

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        Set(waitingText, UITokens.Colors.InkMuted);
    }

    private void OnEnable()
    {
        EventManager.OnScoreGood.AddListener(OnGreat);
        EventManager.OnScoreNotBad.AddListener(OnGood);
        EventManager.OnScoreBad.AddListener(OnPoor);
    }

    private void OnDisable()
    {
        EventManager.OnScoreGood.RemoveListener(OnGreat);
        EventManager.OnScoreNotBad.RemoveListener(OnGood);
        EventManager.OnScoreBad.RemoveListener(OnPoor);
    }

    private void OnGreat() => Set("Loved it!", UITokens.Colors.Ink);
    private void OnGood() => Set("Tasty!", UITokens.Colors.Ink);
    private void OnPoor() => Set("Not quite", UITokens.Colors.Tomato);

    private void Set(string text, Color color)
    {
        label.text = text;
        label.color = color;
    }
}
