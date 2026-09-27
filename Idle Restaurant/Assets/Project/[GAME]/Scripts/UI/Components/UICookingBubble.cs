using UnityEngine;
using UnityEngine.UI;

// World-space cooking indicator above a patty on the pan or a bun in the oven.
// Cooking: the ring fills in teal around a flame. Done: the bubble pops, shows a check, and the
// ring becomes a "take it out" countdown (teal → yellow → tomato, pulsing at the end) until it burns.
// It only reads CookingProgressBar's timer; it never changes cooking state.
public class UICookingBubble : MonoBehaviour
{
    [SerializeField] private CookingProgressBar timer;
    [SerializeField] private Image ring;
    [SerializeField] private Image icon;
    [SerializeField] private Sprite cookingIcon, readyIcon;

    private Vector3 baseScale;
    private float pop = 1f, popVelocity;
    private bool ready;

    private void Awake() => baseScale = transform.localScale;

    private void OnEnable()
    {
        pop = 0.4f;
        popVelocity = 0f;
        ready = timer.Progress01 >= timer.DoneAt01; // no "ready" punch when re-shown mid-countdown
        LateUpdate();
    }

    private void LateUpdate()
    {
        float progress = timer.Progress01;
        float doneAt = timer.DoneAt01;
        bool isReady = progress >= doneAt;
        if (isReady && !ready) { pop = 1.25f; popVelocity = 0f; }
        ready = isReady;

        float pulse = 1f;
        if (!ready)
        {
            ring.fillAmount = progress / doneAt;
            ring.color = UITokens.Colors.Teal;
            icon.sprite = cookingIcon;
            icon.color = UITokens.Colors.Ink;
        }
        else
        {
            float left = 1f - (progress - doneAt) / (1f - doneAt);
            ring.fillAmount = left;
            ring.color = UITokens.UrgencyColor(left);
            icon.sprite = readyIcon;
            icon.color = UITokens.Colors.DarkTeal;
            if (left < 0.25f) pulse = 1f + 0.06f * (0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2.6f));
        }

        UITokens.Spring(ref pop, ref popVelocity, 1f, Mathf.Min(Time.deltaTime, 1f / 30f));
        transform.localScale = baseScale * (pop * pulse);
    }
}
