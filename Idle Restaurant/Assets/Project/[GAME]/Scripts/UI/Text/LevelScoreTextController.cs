using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelScoreTextController : MonoBehaviour
{
    private TextMeshProUGUI scoreText;
    public TextMeshProUGUI ScoreText
    {
        get
        {
            if(scoreText == null)
            scoreText = GetComponent<TextMeshProUGUI>();

            return scoreText;
        }
    }

    private void OnEnable()
    {
        EventManager.OnLevelFinish.AddListener(UpdateScoreText);
    }

    private void OnDisable()
    {
        EventManager.OnLevelFinish.RemoveListener(UpdateScoreText); 
    }

    private float point = 0;
    private void UpdateScoreText()
    {
        point = ScoreManager.Instance.totalLevelScore;
        ScoreText.text = point.ToString("F2") + " pt";
    }
}
