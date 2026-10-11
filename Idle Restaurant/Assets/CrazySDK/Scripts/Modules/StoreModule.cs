using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CrazyGames
{
    public class StoreModule : MonoBehaviour
    {
        private CrazySDK _crazySDK;
        private Action<SdkError, PurchaseResult> _buyItemCallback;
        private bool _sandbox;

        public void Init(CrazySDK crazySDK)
        {
            _crazySDK = crazySDK;
        }

        /// <summary>
        /// Toggles whether the purchases run against Xsolla's sandbox (test) environment.
        /// </summary>
        public void SetSandbox(bool sandbox)
        {
            _sandbox = sandbox;
            _crazySDK.WrapSDKAction(
                () =>
                {
                    StoreSetSandboxSDK(sandbox ? 1 : 0);
                },
                () =>
                {
                    _crazySDK.DebugLog("Setting sandbox mode to " + sandbox);
                }
            );
        }

        /// <summary>
        /// Opens the Xsolla payment widget, allowing the user to buy a single item.
        /// Only one purchase is allowed at a time, concurrent calls will receive an error.
        /// Callback param1 = error (null if everything is fine), param2 = purchase result.
        /// Possible error codes: "userNotAuthenticated", "purchaseInitFailed", "purchaseInProgress", "purchaseCancelled", "unexpectedError".<br/><br/>
        /// You can customize the response returned in the editor in the CrazyGamesSettings file.
        /// </summary>
        public void BuyItem(string itemId, Action<SdkError, PurchaseResult> callback)
        {
            _crazySDK.WrapSDKAction(
                () =>
                {
                    if (_buyItemCallback != null)
                    {
                        callback(new SdkError("purchaseInProgress", "A purchase is already in progress."), null);
                        return;
                    }

                    _buyItemCallback = callback;
                    StoreBuyItemSDK(itemId);
                },
                () =>
                {
                    var settings = _crazySDK.Settings;
                    _crazySDK.DebugLog(
                        $"Buy item \"{itemId}\" simulation for Unity editor (sandbox: {_sandbox}), returning: {settings.buyItemResponse}"
                    );
                    switch (settings.buyItemResponse)
                    {
                        case CrazySettingsBuyItemResponse.Success:
                            callback(null, new PurchaseResult { itemId = itemId });
                            return;
                        case CrazySettingsBuyItemResponse.PurchaseCancelled:
                            callback(new SdkError("purchaseCancelled", "The purchase was cancelled."), null);
                            return;
                        case CrazySettingsBuyItemResponse.UserLoggedOut:
                            callback(new SdkError("userNotAuthenticated", "User must be logged in to perform this action."), null);
                            return;
                        case CrazySettingsBuyItemResponse.UnexpectedError:
                            callback(new SdkError("unexpectedError", "Simulated error in editor"), null);
                            return;
                        default:
                            Debug.LogError("Unhandled case " + settings.buyItemResponse + ", returning 'unexpectedError'");
                            callback(new SdkError("unexpectedError", "Simulated error in editor"), null);
                            return;
                    }
                }
            );
        }

        /// <summary>
        /// Opens the Xsolla payment widget, allowing the user to buy a single item.
        /// Only one purchase is allowed at a time, concurrent calls will throw.
        /// Possible error codes: "userNotAuthenticated", "purchaseInitFailed", "purchaseInProgress", "purchaseCancelled", "unexpectedError".<br/><br/>
        /// You can customize the response returned in the editor in the CrazyGamesSettings file.
        /// </summary>
        public System.Threading.Tasks.Task<PurchaseResult> BuyItemAsync(string itemId)
        {
            return CrazyTaskUtil.FromCallback<PurchaseResult>(cb => BuyItem(itemId, cb));
        }

        private void JSLibCallback_StoreBuyItem(string responseStr)
        {
            var response = JsonUtility.FromJson<PurchaseCallbackReply>(responseStr);
            var callback = _buyItemCallback;
            // clear the callback before invoking it, the game may start a new purchase from the callback
            _buyItemCallback = null;
            callback?.Invoke(response.Error, response.PurchaseResult);
        }

#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void StoreBuyItemSDK(string itemId);

        [DllImport("__Internal")]
        private static extern void StoreSetSandboxSDK(int sandbox);
#else
        // Preventing build to fail when using another platform than WebGL
        private void StoreBuyItemSDK(string itemId) { }

        private void StoreSetSandboxSDK(int sandbox) { }
#endif

        [Serializable]
        private class PurchaseCallbackReply
        {
            public string errorJson; // JsonUtility initializes null class with empty fields, so pass them as JSON strings and parse them here
            public string purchaseResultJson;

            public SdkError Error =>
                string.IsNullOrEmpty(errorJson) || errorJson == "null" || errorJson == "undefined"
                    ? null
                    : JsonUtility.FromJson<SdkError>(errorJson);

            public PurchaseResult PurchaseResult =>
                string.IsNullOrEmpty(purchaseResultJson) || purchaseResultJson == "null" || purchaseResultJson == "undefined"
                    ? null
                    : JsonUtility.FromJson<PurchaseResult>(purchaseResultJson);
        }
    }

    [Serializable]
    public class PurchaseResult
    {
        public string itemId;

        public override string ToString()
        {
            return base.ToString() + "ItemId = " + itemId;
        }
    }
}
