using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace CrazyGames
{
    [Serializable]
    public class GameSettings
    {
        public bool disableChat;
        public bool muteAudio;

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    public class GameModule : MonoBehaviour
    {
        private CrazySDK _crazySDK;

        [Obsolete("IsInstantJoin is deprecated, please use IsInstantMultiplayer")]
        public bool IsInstantJoin => Application.isEditor ? false : GetInviteLinkParameter("instantJoin") == "true";
        public bool IsInstantMultiplayer => Application.isEditor ? false : GetInviteLinkParameter("instantJoin") == "true";

        private readonly List<Action<GameSettings>> _settingsChangeListener = new List<Action<GameSettings>>();
        private readonly List<Action<Dictionary<string, string>>> _joinRoomListener = new List<Action<Dictionary<string, string>>>();

        public GameSettings Settings
        {
            get
            {
                if (Application.isEditor)
                {
                    return new GameSettings() { disableChat = false };
                }
                return JsonUtility.FromJson<GameSettings>(GetGameSettingsJSONSDK());
            }
        }

        /* If the game was started by invitation, this contains the invite params you passed to UpdateRoom() or InviteLink() methods */
        public Dictionary<string, string> InviteParams
        {
            get
            {
                if (Application.isEditor)
                {
                    return null;
                }
                var inviteParamsStr = GetInviteParamsSDK();
                return Utils.DeserializeCustomJsonDictionary(inviteParamsStr);
            }
        }

        public void Init(CrazySDK crazySDK)
        {
            _crazySDK = crazySDK;
        }

        public void HappyTime()
        {
            _crazySDK.DebugLog("Happy time!");
            _crazySDK.WrapSDKAction(HappyTimeSDK, () => { });
        }

        public void GameplayStart()
        {
            _crazySDK.DebugLog("Gameplay start called");
            _crazySDK.WrapSDKAction(GameplayStartSDK, () => { });
        }

        public void GameplayStop()
        {
            _crazySDK.DebugLog("Gameplay stop called");
            _crazySDK.WrapSDKAction(GameplayStopSDK, () => { });
        }

        public string GetInviteLinkParameter(string paramName)
        {
            if (Application.isEditor)
            {
                Debug.LogError("Cannot parse url in Unity editor, try running it in a browser");
                return "";
            }

            var paramValue = GetInviteLinkParamSDK(paramName);
            return string.IsNullOrEmpty(paramValue) ? null : paramValue;
        }

        public string InviteLink(Dictionary<string, string> parameters)
        {
            return _crazySDK.WrapSDKFunc(
                () =>
                {
                    var paramsJson = Utils.ConvertDictionaryToJson(parameters);
                    _crazySDK.DebugLog($"Requesting invite URL with params: {paramsJson}");
                    return RequestInviteUrlSDK(paramsJson);
                },
                () =>
                {
                    _crazySDK.DebugLog("Invite URL requested");
                    const string baseUrl = "https://crazygames.com/game/your-game";
                    return Utils.AppendQueryParameters(baseUrl, parameters);
                }
            );
        }

        public string ShowInviteButton(Dictionary<string, string> parameters)
        {
            return _crazySDK.WrapSDKFunc(
                () =>
                {
                    var paramsJson = Utils.ConvertDictionaryToJson(parameters);
                    _crazySDK.DebugLog($"Requesting invite button with params: {paramsJson}");
                    return ShowInviteButtonSDK(paramsJson);
                },
                () =>
                {
                    _crazySDK.DebugLog("Invite button called");
                    const string baseUrl = "https://crazygames.com/game/your-game";
                    return Utils.AppendQueryParameters(baseUrl, parameters);
                }
            );
        }

        public void HideInviteButton()
        {
            _crazySDK.DebugLog("Hide invite button called");
            _crazySDK.WrapSDKAction(HideInviteButtonSDK, () => { });
        }

        public void CopyToClipboard(string text)
        {
            _crazySDK.WrapSDKAction(
                () => CopyToClipboardSDK(text),
                () =>
                {
                    GUIUtility.systemCopyBuffer = text;
                }
            );
        }

        /// <summary>
        /// Register a new listener that is called everytime the game settings change.
        /// </summary>
        public void AddSettingsChangeListener(Action<GameSettings> listener)
        {
            _settingsChangeListener.Add(listener);
        }

        public void RemoveSettingsChangeListener(Action<GameSettings> listener)
        {
            _settingsChangeListener.Remove(listener);
        }

        public void AddJoinRoomListener(Action<Dictionary<string, string>> listener)
        {
            _crazySDK.WrapSDKAction(
                () =>
                {
                    _joinRoomListener.Add(listener);
                    if (_joinRoomListener.Count == 1)
                    {
                        RegisterJoinRoomListenerSDK();
                    }
                },
                () => { }
            );
        }

        public void RemoveJoinRoomListener(Action<Dictionary<string, string>> listener)
        {
            _crazySDK.WrapSDKAction(
                () =>
                {
                    _joinRoomListener.Remove(listener);
                    // in case there isn't any active join room listener in game, it should be removed from the HTML5 SDK, so that the game is reloaded with invite params
                    if (_joinRoomListener.Count == 0)
                    {
                        RemoveJoinRoomListenerSDK();
                    }
                },
                () => { }
            );
        }

        private void JSLibCallback_SettingsChangeListener(string responseStr)
        {
            var newSettings = JsonUtility.FromJson<GameSettings>(responseStr);

            var tempList = _settingsChangeListener.Select(c => c).ToList(); // use a temp list, the main list may get modified if a callback adds/removes a listener
            tempList.ForEach(c => c(newSettings));
        }

        private void JSLibCallback_JoinRoomListener(string inviteParamsStr)
        {
            Dictionary<string, string> inviteParams = Utils.DeserializeCustomJsonDictionary(inviteParamsStr);
            var tempList = _joinRoomListener.Select(c => c).ToList();
            tempList.ForEach(c => c(inviteParams));
        }

        public void UpdateRoom(UpdateRoomInput input)
        {
            var paramsJson = input.Serialize();
            _crazySDK.DebugLog($"Updating room with params: {paramsJson}");
            _crazySDK.WrapSDKAction(() => UpdateRoomSDK(paramsJson), () => { });
        }

        public void LeftRoom()
        {
            _crazySDK.DebugLog("Left room called");
            _crazySDK.WrapSDKAction(LeftRoomSDK, () => { });
        }

        public void SetGameContext(Dictionary<string, string> context)
        {
            var contextJson = context != null ? Utils.ConvertDictionaryToJson(context) : "null";
            _crazySDK.DebugLog($"Set game context: {contextJson}");
            _crazySDK.WrapSDKAction(() => SetGameContextSDK(contextJson), () => { });
        }

        public void ClearGameContext()
        {
            _crazySDK.DebugLog("Clear game context called");
            _crazySDK.WrapSDKAction(ClearGameContextSDK, () => { });
        }

#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void HappyTimeSDK();

        [DllImport("__Internal")]
        private static extern void GameplayStartSDK();

        [DllImport("__Internal")]
        private static extern void GameplayStopSDK();

        [DllImport("__Internal")]
        private static extern string RequestInviteUrlSDK(string urlParams);

        [DllImport("__Internal")]
        private static extern string GetInviteLinkParamSDK(string paramName);

        [DllImport("__Internal")]
        private static extern string ShowInviteButtonSDK(string urlParams);

        [DllImport("__Internal")]
        private static extern void HideInviteButtonSDK();

        [DllImport("__Internal")]
        private static extern void CopyToClipboardSDK(string text);

        [DllImport("__Internal")]
        private static extern string GetGameSettingsJSONSDK();

        [DllImport("__Internal")]
        private static extern void UpdateRoomSDK(string paramsJson);

        [DllImport("__Internal")]
        private static extern void LeftRoomSDK();

        [DllImport("__Internal")]
        private static extern void RegisterJoinRoomListenerSDK();

        [DllImport("__Internal")]
        private static extern void RemoveJoinRoomListenerSDK();

        [DllImport("__Internal")]
        private static extern string GetInviteParamsSDK();

        [DllImport("__Internal")]
        private static extern void SetGameContextSDK(string contextJson);

        [DllImport("__Internal")]
        private static extern void ClearGameContextSDK();
#else
        private void HappyTimeSDK() { }

        private void GameplayStartSDK() { }

        private void GameplayStopSDK() { }

        private string RequestInviteUrlSDK(string urlParams)
        {
            return "";
        }

        private string GetInviteLinkParamSDK(string paramName)
        {
            return "";
        }

        private string ShowInviteButtonSDK(string urlParams)
        {
            return "";
        }

        private void HideInviteButtonSDK() { }

        private void CopyToClipboardSDK(string text) { }

        private string GetGameSettingsJSONSDK()
        {
            return "";
        }

        private void UpdateRoomSDK(string paramsJson) { }

        private void LeftRoomSDK() { }

        private void RegisterJoinRoomListenerSDK() { }

        private void RemoveJoinRoomListenerSDK() { }

        private string GetInviteParamsSDK()
        {
            return "";
        }

        private void SetGameContextSDK(string contextJson) { }

        private void ClearGameContextSDK() { }

#endif
    }

    public class UpdateRoomInput
    {
        public string RoomId { get; set; } = null;
        public bool? IsJoinable { get; set; } = null;
        public Dictionary<string, string> InviteParams { get; set; } = null;

        public string Serialize()
        {
            // JsonUtility from Unity is lacking proper support for dictionaries
            var entries = new List<string>();
            if (RoomId != null)
                entries.Add($"\"roomId\":\"{RoomId}\"");
            if (IsJoinable != null)
                entries.Add($"\"isJoinable\":{IsJoinable.Value.ToString().ToLower()}");
            if (InviteParams != null)
                entries.Add($"\"inviteParams\":{Utils.ConvertDictionaryToJson(InviteParams)}");
            return "{" + string.Join(",", entries) + "}";
        }

        public override string ToString()
        {
            return Serialize();
        }
    }
}
