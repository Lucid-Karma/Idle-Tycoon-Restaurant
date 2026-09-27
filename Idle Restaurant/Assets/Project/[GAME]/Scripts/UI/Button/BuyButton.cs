using UnityEngine;

public class BuyButton : MonoBehaviour
{
    [SerializeField] private GameObject PurchaseObject;
    [SerializeField] private int price = 20;

    public int Price => price;

    public void BuyItem()
    {
        if(ScoreManager.Instance.totalLevelEarning >= price)
        {
            EventManager.OnClick.Invoke();
            ProductManager.Instance.ShowPurchasedItem(0);
            PurchaseObject.SetActive(false);
            ScoreManager.Instance.SpendEarnings(price);
            EventManager.OnScoreUpdate.Invoke();
            Time.timeScale = 1;
        }
    }
}
