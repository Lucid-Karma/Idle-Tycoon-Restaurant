using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EarningTextController : MonoBehaviour
{
    private TextMeshProUGUI earningText;
    public TextMeshProUGUI EarningText
    {
        get
        {
            if(earningText == null)
            earningText = GetComponent<TextMeshProUGUI>();

            return earningText;
        }
    }

    private void OnEnable()
    {
        EventManager.OnScoreUpdate.AddListener(UpdateEarningText);
        EventManager.OnLevelFinish.AddListener(UpdateLevelEarningText);
        // Labels inside panels that open later (e.g. the shop wallet) must show the current value.
        if (ScoreManager.Instance != null) UpdateEarningText();
    }

    private void OnDisable()
    {
        EventManager.OnScoreUpdate.RemoveListener(UpdateEarningText);
        EventManager.OnLevelFinish.RemoveListener(UpdateLevelEarningText); 
    }

    // The wallet shows the till (kept between shifts) minus the money still lying on the tables, which it
    // counts up as the chef picks it up (MoneyPiles); the result card shows what this shift earned.
    [SerializeField] private bool shiftOnly;

    private int shown = int.MinValue, target;
    private float countFrom, countAt = -10f, punch;
    private const float CountSeconds = 0.6f;

    private void UpdateEarningText()
    {
        var score = ScoreManager.Instance;
        int value = shiftOnly ? score.ShiftEarned : score.totalLevelEarning - MoneyPiles.Pending;
        if (shown == int.MinValue || value < target || !isActiveAndEnabled)
        {
            // First show, or spending: straight to the number.
            shown = target = value;
            EarningText.text = "$" + value;
            return;
        }
        if (value == target) return;
        countFrom = shown;
        target = value;
        countAt = Time.unscaledTime;
        punch = 1f;
    }

    private void Update()
    {
        if (shown == target) { Settle(); return; }
        float t = Mathf.Clamp01((Time.unscaledTime - countAt) / CountSeconds);
        shown = Mathf.RoundToInt(Mathf.Lerp(countFrom, target, 1f - (1f - t) * (1f - t)));
        if (t >= 1f) shown = target;
        EarningText.text = "$" + shown;
        Settle();
    }

    // A small punch as money comes in.
    private void Settle()
    {
        if (punch <= 0f) return;
        punch = Mathf.MoveTowards(punch, 0f, Mathf.Min(Time.unscaledDeltaTime, 1f / 30f) / 0.35f);
        float s = 1f + 0.18f * Mathf.Sin(punch * Mathf.PI);
        EarningText.rectTransform.localScale = new Vector3(s, s, 1f);
    }

    private void UpdateLevelEarningText() => UpdateEarningText();
}
