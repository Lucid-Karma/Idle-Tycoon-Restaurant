using TMPro;
using UnityEngine;

// The cafe's level and the stars towards the next one (shop header, result card). On the result card it
// also shows the stars this shift added, and the bar fills up to the new total.
public class UICafeLevel : MonoBehaviour
{
    [SerializeField] private TMP_Text levelLabel;      // "CAFE LEVEL 3"
    [SerializeField] private TMP_Text progressLabel;   // "12 / 40" (or "+27" on the result card)
    [SerializeField] private RectTransform fill;       // stretched from the left inside its track
    [SerializeField] private bool showShiftStars;
    [SerializeField] private string levelFormat = "Cafe level {0}";

    private float shown, velocity, target;
    private int shownLevel;

    private void OnEnable()
    {
        // The bar counts levels now, not stars: the star in front of the shop's count went (the result card's star
        // stays - it is the stars this level earned).
        if (!showShiftStars && transform.Find("Star") is Transform star && star.gameObject.activeSelf)
        {
            star.gameObject.SetActive(false);
            if (progressLabel.transform is RectTransform text) text.anchoredPosition += Vector2.left * 34f;   // into its place
        }
        CafeProgress.Changed += Refresh;
        EventManager.OnLevelFinish.AddListener(OnShiftEnd);
        Refresh();
        shown = target;
        Apply();
    }

    private void OnDisable()
    {
        CafeProgress.Changed -= Refresh;
        EventManager.OnLevelFinish.RemoveListener(OnShiftEnd);
    }

    // The result card appears now: fill the bar from where it was before this level was counted (or from empty after
    // the cafe went up a tier).
    private void OnShiftEnd()
    {
        Refresh();
        if (!showShiftStars) return;
        int played = CafeProgress.ShiftsPlayed;
        var (into, span) = CafeProgress.ProgressAfter(played - 1);
        bool sameTier = played > 0 && Mathf.Max(CafeProgress.TierAt(played), CafeProgress.LegacyTier) == CafeProgress.Level;
        shown = sameTier && span > 0 ? (float)into / span : 0f;
        velocity = 0f;
        Apply();
    }

    private void Refresh()
    {
        int level = CafeProgress.Level;
        // A new level starts the bar again from the left.
        if (shownLevel != 0 && level != shownLevel) shown = 0f;
        shownLevel = level;
        var (into, span) = CafeProgress.LevelProgress;
        levelLabel.text = string.Format(levelFormat, level);
        if (showShiftStars && ScoreManager.Instance != null)
            progressLabel.text = "+" + ScoreManager.Instance.ShiftStars;
        else
            progressLabel.text = span > 0 ? $"{into} / {span} levels" : "Max";
        target = span > 0 ? (float)into / span : 1f;
    }

    private void LateUpdate()
    {
        if (Mathf.Approximately(shown, target) && velocity == 0f) return;
        UITokens.Spring(ref shown, ref velocity, target, Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
        if (Mathf.Abs(shown - target) < 0.001f && Mathf.Abs(velocity) < 0.01f) { shown = target; velocity = 0f; }
        Apply();
    }

    private void Apply()
    {
        if (fill == null) return;
        fill.anchorMax = new Vector2(Mathf.Clamp01(shown), fill.anchorMax.y);
    }
}
