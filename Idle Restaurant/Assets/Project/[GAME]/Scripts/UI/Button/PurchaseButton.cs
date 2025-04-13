using UnityEngine;

public class PurchaseButton : MonoBehaviour
{
    [SerializeField] private GameObject purchasePanel;

    private void OnEnable()
    {
        purchasePanel.SetActive(false);
    }

    public void ShowPurchasePanel()
    {
        EventManager.OnClick.Invoke();
        purchasePanel.SetActive(true);
        PauseGame();
    }

    void PauseGame()
    {
        Time.timeScale = 0;
    }

    public void ContinueGame()
    {
        EventManager.OnClick.Invoke();
        Time.timeScale = 1;
    }
}
