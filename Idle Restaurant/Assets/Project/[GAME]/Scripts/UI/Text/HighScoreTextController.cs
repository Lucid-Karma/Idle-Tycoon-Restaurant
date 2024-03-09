using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HighScoreTextController : MonoBehaviour
{
    private TextMeshProUGUI highScoreText;
    public TextMeshProUGUI HighScoreText
    {
        get
        {
            if(highScoreText == null)
            highScoreText = GetComponent<TextMeshProUGUI>();

            return highScoreText;
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

    void Start()
    {
        HighScoreText.text = PlayerPrefs.GetFloat("HighScore", 0).ToString();
    }

    private float point = 0;
    private void UpdateScoreText()
    {
        point = ScoreManager.Instance.totalLevelScore;

        if(point > PlayerPrefs.GetFloat("HighScore", 0))
        {
            PlayerPrefs.SetFloat("HighScore", point);
            HighScoreText.text = point.ToString("F3");
        }
    }
}
