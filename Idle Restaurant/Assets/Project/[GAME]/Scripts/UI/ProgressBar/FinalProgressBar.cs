using UnityEngine;
using UnityEngine.UI;

public class FinalProgressBar : MonoBehaviour
{
    public int maximum;
    [Range(0.0f, 5.0f)]
    public float current;
    public Image mask;
    private float fillAmount;

    void OnEnable()
    {
        EventManager.OnLevelFinish.AddListener(GetCurrentFill);
    }
    void OnDisable()
    {
        EventManager.OnLevelFinish.RemoveListener(GetCurrentFill);
    }

    void GetCurrentFill()
    {
        current = ScoreManager.Instance.GetLevelFinalScore();

        fillAmount = (float)current / 5f; //(float)maximum;
        mask.fillAmount = fillAmount;
    }
}
