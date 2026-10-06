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

    // The result card appears now: fill the bar from where the shift started (or from empty after a level up).
    private void OnShiftEnd()
    {
        Refresh();
        if (!showShiftStars || ScoreManager.Instance == null) return;
        int before = CafeProgress.Stars - ScoreManager.Instance.ShiftStars;
        int level = CafeProgress.LevelFor(before);
        shown = level == CafeProgress.Level && level < CafeProgress.MaxLevel
            ? (float)(before - CafeProgress.LevelStars[level - 1]) / (CafeProgress.LevelStars[level] - CafeProgress.LevelStars[level - 1])
            : 0f;
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
            progressLabel.text = span > 0 ? $"{into} / {span}" : "Max";
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
