using System;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace CrazyGames
{
    public class CrazyBanner : MonoBehaviour
    {
        public enum BannerSize
        {
            Leaderboard_728x90,
            Medium_300x250,
            Mobile_320x50,
            Main_Banner_468x60,
            Large_Mobile_320x100,
        }

        private Image backgroundImage;

        public string id;

        [SerializeField]
        private BannerSize size;

        public BannerSize Size
        {
            get => size;
            set
            {
                size = value;
                var banner = (RectTransform)transform.Find("Banner");
                switch (value)
                {
                    case BannerSize.Mobile_320x50:
                        banner.sizeDelta = new Vector2(320, 50);
                        break;
                    case BannerSize.Medium_300x250:
                        banner.sizeDelta = new Vector2(300, 250);
                        break;
                    case BannerSize.Leaderboard_728x90:
                        banner.sizeDelta = new Vector2(728, 90);
                        break;
                    case BannerSize.Main_Banner_468x60:
                        banner.sizeDelta = new Vector2(468, 60);
                        break;
                    case BannerSize.Large_Mobile_320x100:
                        banner.sizeDelta = new Vector2(320, 100);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }

        public Vector2 Position
        {
            get
            {
                var banner = (RectTransform)transform.Find("Banner");
                return banner.anchoredPosition;
            }
            set
            {
                var banner = (RectTransform)transform.Find("Banner");
                banner.anchoredPosition = value;
            }
        }

        private void Awake()
        {
            backgroundImage = transform.GetComponentInChildren<Image>();
            if (Application.platform == RuntimePlatform.WebGLPlayer && !Application.isEditor)
            {
                backgroundImage.color = new Color(0, 0, 0, 0);
            }

            RegenerateId();
            CrazySDK.Banner.RegisterBanner(this);
        }

        private void OnDestroy()
        {
            if (!CrazySDK.IsShutDown)
            {
                // in editor when leaving play mode avoid calling CrazySDK.Banner which will
                // create a new SDK instance and then generate a console error about improper shut down
                CrazySDK.Banner.UnregisterBanner(this);
            }
        }

        public void SimulateRefresh()
        {
            backgroundImage.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }

        public void RegenerateId()
        {
            id = Guid.NewGuid().ToString();
        }

        public bool IsVisible() => gameObject.activeInHierarchy;

        /// <summary>
        /// Some banners are partially visible (clipped by a screen edge), or completely off-screen
        /// The JS SDK won't render banners that fall (partially) outside the screen, so this helps debug missing banners
        /// Returns null for fully visible banners, since those don't need debugging.
        /// </summary>
        public string GetVisibilityDebugInfo()
        {
            var banner = (RectTransform)transform.Find("Banner");

            var corners = new Vector3[4];
            banner.GetWorldCorners(corners); // corners are ordered: [0] bottom-left, [2] top-right
            var min = new Vector2(corners[0].x, corners[0].y);
            var max = new Vector2(corners[2].x, corners[2].y);

            var screenRect = new Rect(0, 0, Screen.width, Screen.height);
            var bannerRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);

            string visibility;
            if (screenRect.Contains(min) && screenRect.Contains(max))
                return null; // fully visible, nothing to debug
            else if (bannerRect.Overlaps(screenRect))
                visibility = "partially visible (clipped by screen edge, won't be rendered)";
            else
                visibility = "off-screen (won't be rendered)";

            return $"Banner '{id}' [{size}] screen bounds x:[{min.x:F0}, {max.x:F0}] y:[{min.y:F0}, {max.y:F0}], "
                + $"screen size {Screen.width}x{Screen.height} -> {visibility}";
        }
    }
}
