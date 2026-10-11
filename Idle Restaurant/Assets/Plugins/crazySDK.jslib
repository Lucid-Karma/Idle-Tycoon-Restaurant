mergeInto(LibraryManager.library, {
  InitSDK: function (version) {
    // to avoid warnings about Unity stringify beeing obsolete
    if (typeof UTF8ToString !== 'undefined') {
      window.unityStringify = UTF8ToString;
    } else {
      window.unityStringify = Pointer_stringify;
    }

    window.UnitySDK = {
      version: window.unityStringify(version),
      enableDebug: window.location.href.includes('sdk_debug=true'),
      pointerLockElement: undefined,
      unlockPointer: function () {
        this.pointerLockElement = document.pointerLockElement || null;
        if (this.pointerLockElement && document.exitPointerLock) {
          document.exitPointerLock();
          if (this.enableDebug) {
            this.log('Pointer lock released from', this.pointerLockElement);
          }
        }
      },
      lockPointer: function () {
        if (this.pointerLockElement && this.pointerLockElement.requestPointerLock) {
          this.pointerLockElement.requestPointerLock();
          if (this.enableDebug) {
            this.log('Pointer lock requested on', this.pointerLockElement);
          }
        }
      },
      logError: function () {
        var args = Array.prototype.slice.call(arguments);
        args.unshift('[JsLib]');
        console.error.apply(console, args);
      },
      log: function () {
        var args = Array.prototype.slice.call(arguments);
        args.unshift('[JsLib]');
        console.log.apply(console, args);
      },
      joinRoomListener: function (inviteParams) {
        // check c# for more info regarding why params are forwarded like this
        var entries = Object.keys(inviteParams).map(function (key) {
          return { key: key, value: inviteParams[key] };
        });
        var payload = JSON.stringify({ entries: entries });
        SendMessage('CrazySDKSingleton', 'JSLibCallback_JoinRoomListener', payload);
      }
    };

    var initOptions = {
      wrapper: {
        engine: 'unity',
        sdkVersion: window.unityStringify(version)
      }
    };

    var script = document.createElement('script');
    script.src = 'https://sdk.crazygames.com/crazygames-sdk-v3.js';
    document.head.appendChild(script);
    script.addEventListener('load', function () {
      window.CrazyGames.SDK.init(initOptions).then(function () {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_Init');
        window.CrazyGames.SDK.ad
          .hasAdblock()
          .then(function (result) {
            SendMessage('CrazySDKSingleton', 'JSLibCallback_AdblockDetectionResult', result ? 1 : 0);
          })
          .catch(function (error) {
            window.UnitySDK.logError('Error while checking adblock:', error);
            SendMessage('CrazySDKSingleton', 'JSLibCallback_AdblockDetectionResult', 0);
          });
        window.CrazyGames.SDK.game.addSettingsChangeListener(function (newSettings) {
          SendMessage('CrazySDKSingleton', 'JSLibCallback_SettingsChangeListener', JSON.stringify(newSettings));
        });
        window.CrazyGames.SDK.ad.addAdblockPopupListener(function (e) {
          if (e === 'open') {
            window.UnitySDK.unlockPointer();
          }
        });
      });
    });
  },

  RegisterAuthListenerSDK: function () {
    window.CrazyGames.SDK.user.addAuthListener(function (user) {
      SendMessage('CrazySDKSingleton', 'JSLibCallback_AuthListener', JSON.stringify({ userJson: JSON.stringify(user) }));
    });
  },
  RegisterJoinRoomListenerSDK: function () {
    window.CrazyGames.SDK.game.addJoinRoomListener(window.UnitySDK.joinRoomListener);
  },
  RemoveJoinRoomListenerSDK: function () {
    window.CrazyGames.SDK.game.removeJoinRoomListener(window.UnitySDK.joinRoomListener);
  },

  /** SDK.ad module */
  RequestAdSDK: function (adType) {
    var adTypeStr = window.unityStringify(adType);
    var callbacks = {
      adStarted: function () {
        window.UnitySDK.unlockPointer();
        SendMessage('CrazySDKSingleton', 'JSLibCallback_AdStarted');
      },
      adFinished: function () {
        window.UnitySDK.lockPointer();
        SendMessage('CrazySDKSingleton', 'JSLibCallback_AdFinished');
      },
      adError: function (error) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_AdError', JSON.stringify(error));
        // closing the adblock popup results in an 'adError' with code 'adblock'
        // in this case lock pointer back (it is unlocked when the popup is opened)
        if (error && error.code === 'adblock') {
          window.UnitySDK.lockPointer();
        }
      }
    };
    window.CrazyGames.SDK.ad.requestAd(adTypeStr, callbacks).catch(function (e) {
      window.UnitySDK.logError('Error while requesting ad:', e);
      SendMessage('CrazySDKSingleton', 'JSLibCallback_AdError', JSON.stringify({ code: 'unknown', message: e.message || 'Unknown error' }));
    });
  },

  PrefetchAdSDK: function (adType) {
    var adTypeStr = window.unityStringify(adType);
    try {
      window.CrazyGames.SDK.ad.prefetchAd(adTypeStr);
    } catch (e) {
      window.UnitySDK.logError('Error while prefetching ad:', e);
    }
  },

  /** SDK.banner module */
  RequestBannersSDK: function (bannersJSON) {
    var banners = JSON.parse(window.unityStringify(bannersJSON));
    try {
      window.CrazyGames.SDK.banner.requestOverlayBanners(banners, function (bannerId, message, error) {});
    } catch (e) {
      window.UnitySDK.logError('Error while requesting banners:', e);
    }
  },

  /** SDK.game module */
  GetInviteParamsSDK: function () {
    var inviteParams = window.CrazyGames.SDK.game.inviteParams;
    var inviteParamsJson = JSON.stringify(null);
    if (inviteParams !== null) {
      var entries = Object.keys(inviteParams).map(function (key) {
        return { key: key, value: inviteParams[key] };
      });
      inviteParamsJson = JSON.stringify({ entries: entries });
    }
    var bufferSize = lengthBytesUTF8(inviteParamsJson) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(inviteParamsJson, buffer, bufferSize);
    return buffer;
  },
  HappyTimeSDK: function () {
    try {
      window.CrazyGames.SDK.game.happytime();
    } catch (e) {
      window.UnitySDK.logError('Error while calling happytime:', e);
    }
  },
  GameplayStartSDK: function () {
    try {
      window.CrazyGames.SDK.game.gameplayStart();
    } catch (e) {
      window.UnitySDK.logError('Error while calling gameplayStart:', e);
    }
  },
  GameplayStopSDK: function () {
    try {
      window.CrazyGames.SDK.game.gameplayStop();
    } catch (e) {
      window.UnitySDK.logError('Error while calling gameplayStop:', e);
    }
  },
  RequestInviteUrlSDK: function (paramsStr) {
    var params = JSON.parse(window.unityStringify(paramsStr));
    var url = '';
    try {
      url = window.CrazyGames.SDK.game.inviteLink(params);
    } catch (e) {
      window.UnitySDK.logError('Error while requesting invite url:', e);
    }
    var bufferSize = lengthBytesUTF8(url) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(url, buffer, bufferSize);
    return buffer;
  },
  GetInviteLinkParamSDK: function (paramName) {
    var urlParams = new URLSearchParams(window.location.search);
    var paramValue = urlParams.get(window.unityStringify(paramName));
    if (paramValue === null) {
      paramValue = '';
    }
    var bufferSize = lengthBytesUTF8(paramValue) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(paramValue, buffer, bufferSize);
    return buffer;
  },
  ShowInviteButtonSDK: function (paramsStr) {
    var params = JSON.parse(window.unityStringify(paramsStr));
    var url = '';
    try {
      url = window.CrazyGames.SDK.game.showInviteButton(params);
    } catch (e) {
      window.UnitySDK.logError('Error while showing invite button:', e);
    }
    var bufferSize = lengthBytesUTF8(url) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(url, buffer, bufferSize);
    return buffer;
  },
  HideInviteButtonSDK: function () {
    try {
      window.CrazyGames.SDK.game.hideInviteButton();
    } catch (e) {
      window.UnitySDK.logError('Error while calling hideInviteButton:', e);
    }
  },
  GetGameSettingsJSONSDK: function () {
    var settingsJson = JSON.stringify(window.CrazyGames.SDK.game.settings);
    var bufferSize = lengthBytesUTF8(settingsJson) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(settingsJson, buffer, bufferSize);
    return buffer;
  },
  UpdateRoomSDK: function (paramsStr) {
    var params = JSON.parse(window.unityStringify(paramsStr));
    try {
      window.CrazyGames.SDK.game.updateRoom(params);
    } catch (e) {
      window.UnitySDK.logError('Error while updating room:', e);
    }
  },
  LeftRoomSDK: function () {
    try {
      window.CrazyGames.SDK.game.leftRoom();
    } catch (e) {
      window.UnitySDK.logError('Error while calling leftRoom:', e);
    }
  },
  SetGameContextSDK: function (contextStr) {
    var context = JSON.parse(window.unityStringify(contextStr));
    try {
      window.CrazyGames.SDK.game.setGameContext(context);
    } catch (e) {
      window.UnitySDK.logError('Error while calling setGameContext:', e);
    }
  },
  ClearGameContextSDK: function () {
    try {
      window.CrazyGames.SDK.game.clearGameContext();
    } catch (e) {
      window.UnitySDK.logError('Error while calling clearGameContext:', e);
    }
  },

  /** SDK.user module */
  IsUserAccountAvailableSDK: function () {
    return window.CrazyGames.SDK.user.isUserAccountAvailable;
  },
  ShowAuthPromptSDK: function () {
    window.CrazyGames.SDK.user
      .showAuthPrompt()
      .then(function (user) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_ShowAuthPrompt', JSON.stringify({ userJson: JSON.stringify(user) }));
      })
      .catch(function (error) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_ShowAuthPrompt', JSON.stringify({ errorJson: JSON.stringify(error) }));
      });
  },
  ShowAccountLinkPromptSDK: function () {
    window.CrazyGames.SDK.user
      .showAccountLinkPrompt()
      .then(function (response) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_ShowAccountLinkPrompt', JSON.stringify({ linkAccountResponseStr: JSON.stringify(response) }));
      })
      .catch(function (error) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_ShowAccountLinkPrompt', JSON.stringify({ errorJson: JSON.stringify(error) }));
      });
  },
  GetUserSDK: function () {
    window.CrazyGames.SDK.user
      .getUser()
      .then(function (user) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_GetUser', JSON.stringify({ userJson: JSON.stringify(user) }));
      })
      .catch(function (error) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_GetUser', JSON.stringify({ errorJson: JSON.stringify(error) }));
      });
  },
  GetUserTokenSDK: function () {
    window.CrazyGames.SDK.user
      .getUserToken()
      .then(function (token) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_GetUserToken', JSON.stringify({ token: token }));
      })
      .catch(function (error) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_GetUserToken', JSON.stringify({ errorJson: JSON.stringify(error) }));
      });
  },
  GetXsollaUserTokenSDK: function () {
    window.CrazyGames.SDK.user
      .getXsollaUserToken()
      .then(function (token) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_GetXsollaUserToken', JSON.stringify({ token: token }));
      })
      .catch(function (error) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_GetXsollaUserToken', JSON.stringify({ errorJson: JSON.stringify(error) }));
      });
  },
  AddUserScoreSDK: function (score) {
    try {
      window.CrazyGames.SDK.user.addScore(score);
    } catch (e) {
      window.UnitySDK.logError('Error while calling addScore:', e);
    }
  },
  SubmitUserScoreSDK: function (score) {
    try {
      var payload = {
        encryptedScore: window.unityStringify(score)
      };

      window.CrazyGames.SDK.user.submitScore(payload);
    } catch (e) {
      window.UnitySDK.logError('Error while calling submitScore:', e);
    }
  },
  SubmitUserScoreV2SDK: function (encryptedScore, score) {
    try {
      var payload = {
        encryptedScore: window.unityStringify(encryptedScore),
        score: score
      };

      window.CrazyGames.SDK.user.submitScore(payload);
    } catch (e) {
      window.UnitySDK.logError('Error while calling submitScore:', e);
    }
  },
  ListFriendsSDK: function (page, size) {
    page = parseInt(page);
    size = parseInt(size);
    window.CrazyGames.SDK.user
      .listFriends({ page, size })
      .then(function (friendsPage) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_ListFriends', JSON.stringify({ friendsPageJson: JSON.stringify(friendsPage) }));
      })
      .catch(function (error) {
        SendMessage('CrazySDKSingleton', 'JSLibCallback_ListFriends', JSON.stringify({ errorJson: JSON.stringify(error) }));
      });
  },

  /** SDK.data module */
  DataClearSDK: function () {
    try {
      window.CrazyGames.SDK.data.clear();
    } catch (e) {
      window.UnitySDK.logError('Error while calling clear:', e);
    }
  },
  DataGetItemSDK: function (key) {
    var data = null;
    try {
      data = window.CrazyGames.SDK.data.getItem(window.unityStringify(key));
    } catch (e) {
      window.UnitySDK.logError('Error while calling getItem:', e);
    }
    // DataModule from Unity won't call this method if the key doesn't exist, it will return the default value
    var bufferSize = lengthBytesUTF8(data + '') + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(data, buffer, bufferSize);
    return buffer;
  },
  DataHasKeySDK: function (key) {
    var data = null;
    try {
      data = window.CrazyGames.SDK.data.getItem(window.unityStringify(key));
    } catch (e) {
      window.UnitySDK.logError('Error while calling getItem:', e);
    }
    return data !== null;
  },
  DataRemoveItemSDK: function (key) {
    try {
      window.CrazyGames.SDK.data.removeItem(window.unityStringify(key));
    } catch (e) {
      window.UnitySDK.logError('Error while calling removeItem:', e);
    }
  },
  DataSetItemSDK: function (key, value) {
    try {
      window.CrazyGames.SDK.data.setItem(window.unityStringify(key), window.unityStringify(value));
    } catch (e) {
      window.UnitySDK.logError('Error while calling setItem:', e);
    }
  },

  /** SDK.analytics module */
  AnalyticsTrackOrderSDK: function (provider, order) {
    try {
      window.CrazyGames.SDK.analytics.trackOrder(window.unityStringify(provider), JSON.parse(window.unityStringify(order)));
    } catch (e) {
      window.UnitySDK.logError('Error while calling trackOrder:', e);
    }
  },

  /** SDK.store module */
  StoreBuyItemSDK: function (itemId) {
    try {
      window.CrazyGames.SDK.store
        .buyItem(window.unityStringify(itemId))
        .then(function (result) {
          SendMessage('CrazySDKSingleton', 'JSLibCallback_StoreBuyItem', JSON.stringify({ purchaseResultJson: JSON.stringify(result) }));
        })
        .catch(function (error) {
          SendMessage('CrazySDKSingleton', 'JSLibCallback_StoreBuyItem', JSON.stringify({ errorJson: JSON.stringify(error) }));
        });
    } catch (e) {
      window.UnitySDK.logError('Error while calling buyItem:', e);
      var error = { code: 'unexpectedError', message: e.message || 'Unknown error' };
      SendMessage('CrazySDKSingleton', 'JSLibCallback_StoreBuyItem', JSON.stringify({ errorJson: JSON.stringify(error) }));
    }
  },
  StoreSetSandboxSDK: function (sandbox) {
    try {
      window.CrazyGames.SDK.store.setSandbox(sandbox === 1);
    } catch (e) {
      window.UnitySDK.logError('Error while calling setSandbox:', e);
    }
  },

  /** other */
  CopyToClipboardSDK: function (text) {
    const elem = document.createElement('textarea');
    elem.value = window.unityStringify(text);
    document.body.appendChild(elem);
    elem.select();
    elem.setSelectionRange(0, 99999);
    document.execCommand('copy');
    document.body.removeChild(elem);
  },
  SyncUnityGameDataSDK: function () {
    try {
      window.CrazyGames.SDK.data.syncUnityGameData();
    } catch (e) {
      window.UnitySDK.logError('Error while calling syncUnityGameData:', e);
    }
  },
  GetEnvironmentSDK: function () {
    var environment = window.CrazyGames.SDK.environment;
    var bufferSize = lengthBytesUTF8(environment) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(environment, buffer, bufferSize);
    return buffer;
  },
  GetSystemInfoSDK: function () {
    var systemInfoJson = JSON.stringify(window.CrazyGames.SDK.user.systemInfo);
    var bufferSize = lengthBytesUTF8(systemInfoJson) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(systemInfoJson, buffer, bufferSize);
    return buffer;
  }
});
