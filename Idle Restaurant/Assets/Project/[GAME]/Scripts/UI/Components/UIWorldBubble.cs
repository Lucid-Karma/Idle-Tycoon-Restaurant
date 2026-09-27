using UnityEngine;
using UnityEngine.UI;

// World-space speech bubble above a customer (order / eating). It only *shows* a gameplay timer:
// patience is drawn as time left, shifting teal → yellow → tomato and gently pulsing when almost
// gone; eating fills up in teal. Pops in whenever the bubble is shown.
public class UIWorldBubble : MonoBehaviour
{
    [SerializeField] private MonoBehaviour progressSource; // implements IProgress01 (owns the timer)
    [SerializeField] private Image ring;
    [SerializeField] private bool showRemaining;           // true = patience (time left), false = progress
    [SerializeField] private float urgentBelow = 0.25f;

    private IProgress01 source;
    private Vector3 baseScale;
    private float pop = 1f, popVelocity;

    private void Awake()
    {
        source = progressSource as IProgress01;
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        pop = 0.4f;
        popVelocity = 0f;
        LateUpdate();
    }

    private void LateUpdate()
    {
        float progress = source != null ? source.Progress01 : 0f;
        float shown = showRemaining ? 1f - progress : progress;
        ring.fillAmount = shown;
        ring.color = showRemaining ? UITokens.UrgencyColor(shown) : UITokens.Colors.Teal;

        UITokens.Spring(ref pop, ref popVelocity, 1f, Mathf.Min(Time.deltaTime, 1f / 30f));
        float pulse = 1f;
        if (showRemaining && shown < urgentBelow)
            pulse = 1f + 0.06f * (0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2.6f));
        transform.localScale = baseScale * (pop * pulse);
    }
}
