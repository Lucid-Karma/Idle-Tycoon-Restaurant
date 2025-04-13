using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrazyGames
{
    public class AnalyticsModuleDemo : MonoBehaviour
    {
        private void Start()
        {
            CrazySDK.Init(() => { }); // ensure if starting this scene from editor it is initialized
        }

        public void TrackOrder()
        {
            // in a real world scenario use the order from the payment provider, this is just a demonstration
            // for more details, check our docs: https://docs.crazygames.com/sdk/unity/in-game-purchases/
            CrazySDK.Analytics.TrackOrder(
                PaymentProvider.Xsolla,
                new DemoOrderOrder
                {
                    id = "demo_id",
                    totalPrice = 100,
                    items = new List<DemoOrderItem>
                    {
                        new DemoOrderItem { id = "demo_item_1", price = 100 },
                    },
                }
            );
        }
    }

    [Serializable]
    public class DemoOrderOrder
    {
        public string id;
        public float totalPrice;
        public List<DemoOrderItem> items;
    }

    [Serializable]
    public class DemoOrderItem
    {
        public string id;
        public float price;
    }
}
