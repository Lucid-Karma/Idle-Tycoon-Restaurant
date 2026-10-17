using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HostedCustomerTextController : MonoBehaviour
{
    private TextMeshProUGUI counterText;
    public TextMeshProUGUI CounterText
    {
        get
        {
            if(counterText == null)
            counterText = GetComponent<TextMeshProUGUI>();

            return counterText;
        }
    }

    private void OnEnable()
    {
        EventManager.OnCustomerWent.AddListener(UpdateCounterText);
        EventManager.OnLevelStart.AddListener(UpdateCounterText);   // back to 0 for the next shift
    }

    private void OnDisable()
    {
        EventManager.OnCustomerWent.RemoveListener(UpdateCounterText);
        EventManager.OnLevelStart.RemoveListener(UpdateCounterText);
    }

    public int point = 0;
    private void UpdateCounterText()
    {
        point = ScoreManager.Instance.HostedCustomerCount;
        CounterText.text = point.ToString();
    }
}
