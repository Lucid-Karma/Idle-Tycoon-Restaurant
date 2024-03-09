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
    }

    private void OnDisable()
    {
        EventManager.OnScoreUpdate.RemoveListener(UpdateEarningText);
        EventManager.OnLevelFinish.RemoveListener(UpdateLevelEarningText); 
    }

    private float point = 0;
    private void UpdateEarningText()
    {
        point = ScoreManager.Instance.totalLevelEarning;
        EarningText.text = point.ToString() + "$";
    }

    private void UpdateLevelEarningText()
    {
        point = ScoreManager.Instance.totalLevelEarning;
        EarningText.text = point.ToString() + " $";
    }
}
