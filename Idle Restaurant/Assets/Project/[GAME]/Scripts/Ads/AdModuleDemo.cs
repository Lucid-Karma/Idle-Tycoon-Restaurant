using UnityEngine;

namespace CrazyGames
{
    public class AdModuleDemo : MonoBehaviour
    {
        [SerializeField] private GameObject PurchaseObject;
        [SerializeField] private RestartButton restartButton;
        private void Start()
        {
            CrazySDK.Init(() => { }); // ensure if starting this scene from editor it is initialized
        }

        public void ShowMidgameAd()
        {
            CrazySDK.Ad.RequestAd(
                CrazyAdType.Midgame,
                () =>
                {
                    Debug.Log("Midgame ad started");
                },
                (error) =>
                {
                    Debug.Log("Midgame ad error: " + error);
                    restartButton.Restart();
                },
                () =>
                {
                    Debug.Log("Midgame ad finished");
                    restartButton.Restart();
                }
            );
            //PurchaseObject.SetActive(true);
        }

        public void ShowRewardedAd()
        {
            CrazySDK.Ad.RequestAd(
                CrazyAdType.Rewarded,
                () =>
                {
                    Debug.Log("Rewarded ad started");
                },
                (error) =>
                {
                    Debug.Log("Rewarded ad error: " + error);
                    EventManager.OnLevelContine.Invoke();
                },
                () =>
                {
                    Debug.Log("Rewarded ad finished, reward the player here");
                    EventManager.OnLevelContine.Invoke();
                    PurchaseObject.SetActive(false);
                    ProductManager.Instance.ShowPurchasedItem(0);
                }
            );
        }
    }
}
