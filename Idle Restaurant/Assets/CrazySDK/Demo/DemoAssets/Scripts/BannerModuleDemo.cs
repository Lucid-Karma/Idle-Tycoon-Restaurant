using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CrazyGames
{
    public class BannerModuleDemo : MonoBehaviour
    {
        public CrazyBanner bannerPrefab;

        private void Start()
        {
            CrazySDK.Init(UpdateBannersDisplay);
        }

        public void UpdateBannersDisplay()
        {
            CrazySDK.Banner.RefreshBanners();
        }

        public void RequestVideo()
        {
            CrazySDK.Ad.RequestAd(
                CrazyAdType.Rewarded,
                () =>
                {
                    Debug.Log("Ad started");
                },
                (error) =>
                {
                    Debug.LogError($"Ad failed: {error}");
                },
                () =>
                {
                    Debug.Log("Ad finished");
                }
            );
        }

        public void DisableLastBanner()
        {
            var banners = new List<CrazyBanner>(CrazySDK.Banner.Banners);
            banners.Reverse();
            var bannerToDisable = banners.FirstOrDefault(b => b.IsVisible());
            if (bannerToDisable != null)
            {
                bannerToDisable.gameObject.SetActive(false);
            }
        }

        public void AddBanner()
        {
            var banner = Instantiate(bannerPrefab, new Vector3(), new Quaternion(), GameObject.Find("Banners").transform);
            banner.Size = (CrazyBanner.BannerSize)Random.Range(0, 3);
            banner.Position = new Vector2(Random.Range(-461, 461), Random.Range(-243, 243));
        }

        public void HideAllBanners()
        {
            CrazySDK.Banner.Banners.ForEach(b => b.gameObject.SetActive(false));
        }
    }
}
