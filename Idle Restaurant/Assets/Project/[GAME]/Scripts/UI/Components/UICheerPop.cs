using TMPro;
using UnityEngine;
using UnityEngine.UI;

// A success over the customer's head: a perfect burger shows a warm-white pill with a star - "Perfect!" - that
// springs up, floats a little higher and fades, with small stars bursting out of it; a rush of three or more
// serves in a row shows the same pill with a bolt - "Rush x3!". It is deliberately not the tomato "SPLAT!" (user:
// that one is great for a splat, not for these): a calm card from the HUD's own family, not a comic burst.
// Built by Tools/Chibi Cafe/15 (CafeDaysBuilder) on the HUD canvas.
[RequireComponent(typeof(CanvasGroup))]
public class UICheerPop : MonoBehaviour
{
    [SerializeField] private RectTransform pill;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image[] sparks;
    [SerializeField] private Sprite star, bolt;

    private const float Rise = 70f, Life = 1.6f, Fade = 0.35f, SparkTime = 0.7f;

    private CanvasGroup group;
    private RectTransform rect, parent;
    private Vector3 world;
    private float age = 99f;
    private Vector2[] sparkDirections;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        rect = (RectTransform)transform;
        parent = (RectTransform)rect.parent;
        group.alpha = 0f;
        sparkDirections = new Vector2[sparks.Length];
    }

    private void OnEnable() => NpcFsm.OnRated.AddListener(OnRated);
    private void OnDisable() => NpcFsm.OnRated.RemoveListener(OnRated);

    private void OnRated(NpcFsm npc, OrderResult order)
    {
        if (npc == null) return;
        bool perfect = order.Review.IsPerfect;
        bool rush = order.RushStreak >= 3;
        if (!perfect && !rush) return;
        string rushText = $"Rush x{order.RushStreak}!";
        label.text = perfect
            ? "Perfect!" + (rush ? $"  <size=75%><color=#{ColorUtility.ToHtmlStringRGB(UITokens.Colors.DarkTeal)}>{rushText}</color></size>" : "")
            : rushText;
        icon.sprite = perfect ? star : bolt;
        icon.color = perfect ? UITokens.Colors.DeepYellow : UITokens.Colors.Teal;
        foreach (var s in sparks)
        {
            s.sprite = perfect ? star : bolt;
            s.color = perfect ? UITokens.Colors.Yellow : UITokens.Colors.Teal;
        }
        for (int i = 0; i < sparks.Length; i++)
        {
            float a = (i / (float)sparks.Length) * Mathf.PI * 2f + Random.Range(-0.25f, 0.25f);
            sparkDirections[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(90f, 140f);
        }
        world = npc.transform.position + Vector3.up * 2.3f;
        age = 0f;
        if (perfect) GameSfx.Play(GameSfx.Cue.Ready);   // a bright ding on top of the coin
    }

    private void LateUpdate()
    {
        if (age > Life) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        age += dt;
        var cam = Camera.main;
        if (cam == null) return;

        // Over the customer's head on screen, floating up as it goes.
        Vector2 screen = cam.WorldToScreenPoint(world);
        float t = Mathf.Clamp01(age / Life);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local))
            rect.anchoredPosition = local + Vector2.up * (60f + Rise * (1f - (1f - t) * (1f - t)));

        // Springs in (back-out), holds, fades.
        float pop = Mathf.Clamp01(age / 0.28f);
        const float k = 1.70158f;
        float p = pop - 1f;
        float s = Mathf.LerpUnclamped(0.5f, 1f, 1f + p * p * ((k + 1f) * p + k));
        pill.localScale = new Vector3(s, s, 1f);
        group.alpha = Mathf.Clamp01(age / 0.12f) * (1f - Mathf.Clamp01((age - (Life - Fade)) / Fade));

        // The sparks burst out of the pill and fade.
        float st = Mathf.Clamp01(age / SparkTime);
        for (int i = 0; i < sparks.Length; i++)
        {
            var r = sparks[i].rectTransform;
            r.anchoredPosition = sparkDirections[i] * (1f - (1f - st) * (1f - st));
            float ss = Mathf.Sin(st * Mathf.PI) * 1.1f;
            r.localScale = new Vector3(ss, ss, 1f);
            r.localRotation = Quaternion.Euler(0f, 0f, st * 160f * (i % 2 == 0 ? 1f : -1f));
            var c = sparks[i].color;
            c.a = 1f - st;
            sparks[i].color = c;
        }
    }
}
