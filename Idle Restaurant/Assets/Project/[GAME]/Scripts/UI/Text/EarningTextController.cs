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

    // The wallet shows the till (kept between shifts); the result card shows what this shift earned.
    [SerializeField] private bool shiftOnly;

    private void UpdateEarningText()
    {
        var score = ScoreManager.Instance;
        EarningText.text = "$" + (shiftOnly ? score.ShiftEarned : score.totalLevelEarning);
    }

    private void UpdateLevelEarningText() => UpdateEarningText();
}
