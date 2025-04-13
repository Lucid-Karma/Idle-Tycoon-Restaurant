using UnityEngine;

public class BuyButton : MonoBehaviour
{
    [SerializeField] private GameObject PurchaseObject;

    public void BuyItem()
    {
        if(ScoreManager.Instance.totalLevelEarning >= 20)
        {
            EventManager.OnClick.Invoke();
            ProductManager.Instance.ShowPurchasedItem(0);
            PurchaseObject.SetActive(false);
            ScoreManager.Instance.SpendEarnings(20);
            EventManager.OnScoreUpdate.Invoke();
            Time.timeScale = 1;
        }
    }
}
