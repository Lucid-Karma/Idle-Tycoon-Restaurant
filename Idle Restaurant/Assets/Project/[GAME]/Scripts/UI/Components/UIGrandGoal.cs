using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The long goal (GrandGoal) in the top-right corner, under the buttons: the dish it will serve, "NEXT: GRILL HOUSE",
// "$660 / $2,000" and a bar that fills with the wallet - and drops back when the shop is bought from, since it is the
// same money. Its background is frosted (UIFrostedCard): a slow, secondary goal, it steps back behind the game and
// comes forward when money comes in. Shown once the goal has been introduced (the card after the second shift),
// never during the first lesson. Full, it turns teal: "GRILL HOUSE · READY!". Built by Tools/Chibi Cafe/15.
[RequireComponent(typeof(CanvasGroup))]
public class UIGrandGoal : MonoBehaviour
{
    [SerializeField] private TMP_Text label, count;
    [SerializeField] private RectTransform fill;
    [SerializeField] private Image fillImage;

    private CanvasGroup group;
    private float shown = -1f;
    private int shownCoins = -1;
    private bool shownReady;
    private float punch;
    private UIFrostedCard frosted;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        frosted = GetComponent<UIFrostedCard>();
    }

    private void LateUpdate()
    {
        bool visible = GrandGoal.Teased && CafeProgress.Taught && !Tutorial.Running;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        group.alpha = Mathf.MoveTowards(group.alpha, visible ? 1f : 0f, dt / UITokens.Motion.Slow);
        group.blocksRaycasts = visible;   // for the pointer-over (nothing to tap under it)
        if (!visible && group.alpha <= 0f) return;

        int coins = CafeProgress.Coins;
        if (coins != shownCoins)
        {
            if (shownCoins >= 0 && coins > shownCoins) { punch = 1f; if (frosted != null) frosted.Flash(); }
            shownCoins = coins;
            count.text = GrandGoal.Amount;
        }
        float target = GrandGoal.Progress01;
        shown = shown < 0f ? target : Mathf.MoveTowards(shown, target, dt * 0.6f);
        fill.anchorMax = new Vector2(Mathf.Max(0.04f, shown), 1f);

        bool ready = GrandGoal.Ready;
        if (ready != shownReady || label.text.Length == 0)
        {
            shownReady = ready;
            label.text = ready ? "GRILL HOUSE  ·  READY!" : "NEXT: GRILL HOUSE";
            label.color = ready ? UITokens.Colors.Teal : UITokens.Colors.InkMuted;
            count.text = GrandGoal.Amount;
            count.color = ready ? UITokens.Colors.Teal : UITokens.Colors.Ink;
            fillImage.color = ready ? UITokens.Colors.Teal : UITokens.Colors.Berry;
        }

        punch = Mathf.MoveTowards(punch, 0f, dt / UITokens.Motion.Slow);
        float s = 1f + (UITokens.Motion.PunchScale - 1f) * 0.5f * Mathf.Sin(Mathf.Clamp01(punch) * Mathf.PI);
        count.rectTransform.localScale = new Vector3(s, s, 1f);
    }
}
