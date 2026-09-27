using TMPro;
using UnityEngine;
using UnityEngine.UI;

// World-space speech bubble over a customer (Npc_WS_Canvas):
// - burger served: what they say about it (BurgerReview.Reaction) with a mood face, while they eat;
// - paid: a "+$7" pill that floats up and fades;
// - out of patience: "Too slow!" as they get up.
// Presentation only: it listens to the customer's events and never changes gameplay state.
public class UICustomerReaction : MonoBehaviour
{
    [SerializeField] private RectTransform bubble;      // Shadow, Surface, Tail, Face, Line
    [SerializeField] private RectTransform surface, shadow;
    [SerializeField] private Image face;
    [SerializeField] private TMP_Text line;
    [SerializeField] private RectTransform moneyPill;
    [SerializeField] private TMP_Text moneyLabel;
    [SerializeField] private Sprite faceLove, faceLaugh, faceShock;
    [SerializeField] private float lineHold = 3.1f, moneyTime = 1.7f, moneyRise = 70f;
    [SerializeField] private float padding = 26f, gap = 12f, iconSize = 52f, minWidth = 150f;

    private NpcFsm npc;
    private CanvasGroup bubbleGroup, moneyGroup;
    private Vector2 moneyRest;
    private float lineAge = float.MaxValue, moneyAge = float.MaxValue, hold;
    private float pop = 1f, popVelocity;

    private void Awake()
    {
        npc = GetComponentInParent<NpcFsm>();
        bubbleGroup = bubble.GetComponent<CanvasGroup>();
        moneyGroup = moneyPill.GetComponent<CanvasGroup>();
        moneyRest = moneyPill.anchoredPosition;
    }

    private void OnEnable()
    {
        npc.OnNpcServed.AddListener(OnServed);
        npc.OnNpcPaid.AddListener(OnPaid);
        npc.OnNpcWaitEnd.AddListener(OnGaveUp);
        HideAll();
    }

    private void OnDisable()
    {
        npc.OnNpcServed.RemoveListener(OnServed);
        npc.OnNpcPaid.RemoveListener(OnPaid);
        npc.OnNpcWaitEnd.RemoveListener(OnGaveUp);
    }

    private void OnServed()
    {
        var review = npc.LastReview;
        if (review == null) return;
        Say(review.Reaction, review.Mood, lineHold);
    }

    private void OnPaid()
    {
        lineAge = float.MaxValue;
        bubble.gameObject.SetActive(false);

        var order = ScoreManager.Instance.LastOrder;
        moneyLabel.text = "+$" + order.Earned;
        moneyPill.sizeDelta = new Vector2(moneyLabel.GetPreferredValues(moneyLabel.text).x + 2f * padding, moneyPill.sizeDelta.y);
        moneyPill.gameObject.SetActive(true);
        moneyAge = 0f;
    }

    private void OnGaveUp() => Say("Too slow!", Mood.Shocked, 2.2f);

    private void Say(string text, Mood mood, float seconds)
    {
        line.text = text;
        face.sprite = mood == Mood.Delighted ? faceLove : mood == Mood.Amused ? faceLaugh : faceShock;
        face.color = UITokens.MoodColor(mood);

        float width = Mathf.Max(minWidth, line.GetPreferredValues(text).x + iconSize + gap + 2f * padding);
        surface.sizeDelta = new Vector2(width, surface.sizeDelta.y);
        shadow.sizeDelta = new Vector2(width + 24f, shadow.sizeDelta.y);
        float left = -width * 0.5f + padding;
        ((RectTransform)face.transform).anchoredPosition = new Vector2(left + iconSize * 0.5f, 0f);
        ((RectTransform)line.transform).anchoredPosition = new Vector2(left + iconSize + gap, 0f);

        bubble.gameObject.SetActive(true);
        lineAge = 0f;
        hold = seconds;
        pop = 0.4f;
        popVelocity = 0f;
    }

    private void HideAll()
    {
        lineAge = moneyAge = float.MaxValue;
        bubble.gameObject.SetActive(false);
        moneyPill.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);

        if (bubble.gameObject.activeSelf)
        {
            lineAge += dt;
            UITokens.Spring(ref pop, ref popVelocity, 1f, dt);
            bubble.localScale = Vector3.one * pop;
            bubbleGroup.alpha = 1f - Mathf.Clamp01((lineAge - hold) / UITokens.Motion.Slow);
            if (lineAge > hold + UITokens.Motion.Slow) bubble.gameObject.SetActive(false);
        }

        if (moneyPill.gameObject.activeSelf)
        {
            moneyAge += dt;
            float t = Mathf.Clamp01(moneyAge / moneyTime);
            float rise = 1f - Mathf.Pow(1f - t, 3f);
            moneyPill.anchoredPosition = moneyRest + Vector2.up * (moneyRise * rise);
            moneyPill.localScale = Vector3.one * (t < 0.12f ? Mathf.Lerp(0.6f, 1.08f, t / 0.12f) : Mathf.Lerp(1.08f, 1f, Mathf.Clamp01((t - 0.12f) / 0.1f)));
            moneyGroup.alpha = 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
            if (t >= 1f) moneyPill.gameObject.SetActive(false);
        }
    }
}
