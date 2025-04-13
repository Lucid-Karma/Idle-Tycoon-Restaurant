using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductManager : Singleton<ProductManager>
{
    //kitchenApron etc.
    [SerializeField] private GameObject[] products;

    private void OnEnable()
    {
        for (int i = 0; i < products.Length; i++)
        {
            products[i].SetActive(false);
        }
    }

    public void ShowPurchasedItem(int purchasedItemIndex)
    {
        products[purchasedItemIndex].SetActive(true);
    }
}
