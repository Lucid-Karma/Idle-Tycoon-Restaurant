using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Top-center, short-lived feedback line: order verdict + money earned, purchases,
// customers leaving, and the shift objective. One message at a time; a new one replaces the old.
[RequireComponent(typeof(CanvasGroup))]
public class UIToast : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite starIcon, checkIcon, crossIcon, cartIcon, personIcon;
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
        EventManager.OnScoreGood.AddListener(OnGreat);
        EventManager.OnScoreNotBad.AddListener(OnGood);
        EventManager.OnScoreBad.AddListener(OnPoor);
        EventManager.OnCustomerProtest.AddListener(OnProtest);
    }

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(ShowObjective);
        EventManager.OnScoreUpdate.RemoveListener(OnScoreUpdate);
        EventManager.OnScoreGood.RemoveListener(OnGreat);
        EventManager.OnScoreNotBad.RemoveListener(OnGood);
        EventManager.OnScoreBad.RemoveListener(OnPoor);
        EventManager.OnCustomerProtest.RemoveListener(OnProtest);
    }

    private void Start() => lastEarning = ScoreManager.Instance.totalLevelEarning;

    private void ShowObjective() =>
        Show($"Serve {ScoreManager.Instance.CustomersPerLevel} customers to finish the shift", personIcon, UITokens.Colors.Berry);

    // Spending fires OnScoreUpdate without a verdict event, so a negative delta means a purchase.
    private void OnScoreUpdate()
    {
        int delta = ScoreManager.Instance.totalLevelEarning - lastEarning;
        if (delta >= 0) return;
        lastEarning += delta;
        Show($"Purchased  <color=#{Hex(UITokens.Colors.Tomato)}>-${-delta}</color>", cartIcon, UITokens.Colors.Teal);
    }

    private void OnGreat() => ShowVerdict("Loved it!", starIcon, UITokens.Colors.DeepYellow);
    private void OnGood() => ShowVerdict("Tasty!", checkIcon, UITokens.Colors.Teal);
    private void OnPoor() => ShowVerdict("Not quite right", crossIcon, UITokens.Colors.Tomato);
    private void OnProtest() => Show("A customer left hungry", personIcon, UITokens.Colors.Tomato);

    private void ShowVerdict(string verdict, Sprite sprite, Color tint)
    {
        int earning = ScoreManager.Instance.totalLevelEarning;
        int delta = earning - lastEarning;
        lastEarning = earning;
        string reward = delta > 0 ? $"   <color=#{Hex(UITokens.Colors.DarkTeal)}>+${delta}</color>" : "";
        Show(verdict + reward, sprite, tint);
    }

    private void Show(string text, Sprite sprite, Color tint)
    {
        label.text = text;
        icon.sprite = sprite;
        icon.color = tint;
        age = 0f;
    }

    private void LateUpdate()
    {
        if (age > holdTime + UITokens.Motion.Slow) return;
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
