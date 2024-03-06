using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//[ExecuteInEditMode]
public class ProgressBar : MonoBehaviour
{
    //public int minumum;
    public int maximum;
    [Range(0.0f, 5.0f)]
    public float current;
    public Image mask;
    private float fillAmount;

    void OnEnable()
    {
        EventManager.OnScoreUpdate.AddListener(GetCurrentFill);
    }
    void OnDisable()
    {
        EventManager.OnScoreUpdate.RemoveListener(GetCurrentFill);
    }

    void GetCurrentFill()
    {
        current = ScoreManager.Instance.totalLevelScore;
        // float currentOffset = current - minumum;
        // float maximumOffset = maximum - minumum;
        // float fillAmount = currentOffset / maximumOffset;
        fillAmount = (float)current / (float)maximum;
        mask.fillAmount = fillAmount;
    }
}
