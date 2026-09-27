using UnityEngine;
using UnityEngine.UI;

//[ExecuteInEditMode()]
public class CookingProgressBar : MonoBehaviour, IProgress01
{
    private Image mask;
    public Color color;
    public Color mainColor;
    private float minumum = 10f;
    private float maximum = 20f;
    private float current = 0;

    // Mirrors the Pan/Oven timing: cooked at `minumum`, burned at `maximum`.
    public float Progress01 => Mathf.Clamp01(current / maximum);
    public float DoneAt01 => minumum / maximum;

    private float fillAmount;

    void Start()
    {
        mask = GetComponent<Image>();
    }

    void Update()
    {
        if(current < maximum)
            UpdateProgressBarAmount();
    }

    private void UpdateProgressBarAmount()
    {
        current += Time.deltaTime;
        fillAmount = current / maximum;
        mask.fillAmount = fillAmount;

        if(current >= minumum)
        {
            mask.color = color;
        }
    }
    public void ResetProgressBar()
    {
        current = 0;
        mask = GetComponent<Image>();
        mask.fillAmount = 0f;
        mask.color = mainColor;
    }
}
