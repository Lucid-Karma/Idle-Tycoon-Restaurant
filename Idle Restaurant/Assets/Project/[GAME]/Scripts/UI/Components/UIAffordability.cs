using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Makes the shop's buy button honest: disabled (and explained) until the player can pay.
public class UIAffordability : MonoBehaviour
{
    [SerializeField] private BuyButton buyButton;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text hint;

    private void OnEnable()
    {
        EventManager.OnScoreUpdate.AddListener(Refresh);
        Refresh();
    }

    private void OnDisable() => EventManager.OnScoreUpdate.RemoveListener(Refresh);

    private void Refresh()
    {
        int missing = buyButton.Price - ScoreManager.Instance.totalLevelEarning;
        button.interactable = missing <= 0;
        if (hint == null) return;
        hint.gameObject.SetActive(missing > 0);
        hint.text = $"Serve customers to earn ${missing} more";
    }
}
