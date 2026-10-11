using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CrazyGames
{
    /// <summary>
    /// Contains public data that can be modified by the game developers that use the SDK.
    /// </summary>
    public class CrazySettings : ScriptableObject
    {
        [Tooltip("SiteLock doesn't block these domains. Here you can add the domains where your game is hosted.")]
        public string[] whitelistedDomains;

        [Tooltip(
            "If you don't want the game to be paused when video ads are played (for example to avoid multiplayer disconnection) you can turn this off. The Audio will still be muted."
        )]
        public bool pauseGameDuringAd = true;

        [Header("User account testing (editor only)")]
        [Tooltip("Select the response you want to receive when calling CrazyUser.Instance.IsUserAccountAvailable")]
        public CrazySettingsAccountAvailableResponse accountAvailableResponse;

        [Tooltip("Select the response you want to receive when calling CrazyUser.Instance.ShowAccountLinkPrompt")]
        public CrazySettingsLinkAccountResponse linkAccountResponse;

        [Tooltip("Select the response you want to receive when calling CrazyUser.Instance.GetUser")]
        public CrazySettingsTestUser getUserResponse;

        [Tooltip("Select the response you want to receive when calling CrazyUser.Instance.ShowAuthPrompt")]
        public CrazySettingsAuthPromptResponse authPromptResponse;

        [Tooltip("Select the response you want to receive when calling CrazyUser.Instance.GetToken")]
        public CrazySettingsGetTokenResponse getTokenResponse;

        [Header("Store testing (editor only)")]
        [Tooltip("Select the response you want to receive when calling CrazySDK.Store.BuyItem")]
        public CrazySettingsBuyItemResponse buyItemResponse;

        [Header("Other settings (editor only)")]
        [Tooltip("You can disable the SDK logs in the editor if you find them annoying. Some important logs will still be displayed.")]
        public bool disableSdkLogs;

        [Tooltip("You can disable the video ad previews in the editor if you find them annoying.")]
        public bool disableAdPreviews;

        [Tooltip("Simulate ad loading failures for testing. All ad requests will return errors when enabled.")]
        public bool alwaysThrowAdError = false;

        [Tooltip("You can change what device type the systemInfo.device.type returns in the editor. See the docs for more info.")]
        public CrazySettingsDeviceType deviceType;

        [Tooltip("You can change what application type the systemInfo.applicationType returns in the editor. See the docs for more info.")]
        public CrazySettingsApplicationType applicationType;
    }

    public enum CrazySettingsAccountAvailableResponse
    {
        Yes,
        No,
    }

    public enum CrazySettingsLinkAccountResponse
    {
        Yes,
        No,
        UserLoggedOut,
    }

    public enum CrazySettingsTestUser
    {
        User1,
        User2,
        UserLoggedOut,
    }

    public enum CrazySettingsAuthPromptResponse
    {
        User1,
        User2,
        UserCancelled,
    }

    public enum CrazySettingsGetTokenResponse
    {
        User1,
        User2,
        UserLoggedOut,
        ExpiredToken,
    }

    public enum CrazySettingsBuyItemResponse
    {
        Success,
        PurchaseCancelled,
        UserLoggedOut,
        UnexpectedError,
    }

    public enum CrazySettingsDeviceType
    {
        desktop,
        tablet,
        mobile,
    }

    public enum CrazySettingsApplicationType
    {
        [InspectorName("Google Play Store")]
        google_play_store,

        [InspectorName("Apple App Store")]
        apple_store,

        [InspectorName("Progressive Web App")]
        pwa,

        [InspectorName("Web Browser")]
        web,
    }
}
