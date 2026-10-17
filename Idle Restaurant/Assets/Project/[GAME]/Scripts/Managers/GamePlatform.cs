using System;
using CrazyGames;
using UnityEngine;

// The one place the game talks to the portal it runs on. On CrazyGames (and in the editor, which simulates it)
// that is the CrazyGames SDK: progress is saved to the player's CrazyGames account (Data module), gameplay
// start/stop is reported, and video ads are shown. Anywhere else (beyzosh.com, whose domains are whitelisted
// in the SDK's sitelock) it is plain PlayerPrefs and no ads.
// Rules followed (docs.crazygames.com/requirements/ads): midgame ads only between shifts, rewarded ads only
// when the player asks, the game blocked while an ad is up (the SDK pauses time and mutes audio), a failed or
// missing ad never stops the game and never pays out a reward.
public static class GamePlatform
{
    public static bool Ready { get; private set; }

    // CrazySDK.IsAvailable is true on CrazyGames, localhost and in the editor; false on whitelisted domains.
    private static bool OnCrazyGames => Ready && CrazySDK.IsAvailable && CrazySDK.IsInitialized;
    public static bool IsCrazyGames => OnCrazyGames;

    // Started once, before the game scene loads (Boot). Calls back when saving and ads can be used.
    public static void Init(Action done)
    {
        if (Ready) { done(); return; }
        bool supported = Application.isEditor || Application.platform == RuntimePlatform.WebGLPlayer;
        if (!supported || !CrazySDK.IsAvailable)
        {
            Ready = true;
            done();
            return;
        }
        CrazySDK.Init(() =>
        {
            if (Ready) return;
            Ready = true;
            done();
        });
    }

    // The SDK never answered (blocked script, offline): play on with local saves rather than hang on boot.
    public static void GiveUpWaiting()
    {
        Ready = true;
    }

    #region Saving
    // Every value read once and remembered (a missing key too): some are asked for every frame (GrandGoal.Teased
    // and CafeProgress.Taught from the HUD), and on CrazyGames each read is a call into the SDK's JavaScript - the
    // first build with the Grill House card logged a read of "Cafe.GrillTeased" every frame. Writes and deletes
    // go through here too, so the copy never goes stale. Starts over if the storage changes (the SDK becoming ready
    // after an early read from PlayerPrefs), and with each play session in the editor.
    private static readonly System.Collections.Generic.Dictionary<string, int?> ints = new();
    private static readonly System.Collections.Generic.Dictionary<string, string> strings = new();
    private static readonly System.Collections.Generic.HashSet<string> stringsRead = new();
    private static bool cachedForCrazy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ForgetCache()
    {
        ints.Clear();
        strings.Clear();
        stringsRead.Clear();
    }

    private static bool Storage()
    {
        bool crazy = OnCrazyGames;
        if (crazy != cachedForCrazy) { ForgetCache(); cachedForCrazy = crazy; }
        return crazy;
    }

    private static bool Has(bool crazy, string key) => crazy ? CrazySDK.Data.HasKey(key) : PlayerPrefs.HasKey(key);

    public static int GetInt(string key, int fallback)
    {
        bool crazy = Storage();
        if (!ints.TryGetValue(key, out var value))
        {
            value = Has(crazy, key) ? (crazy ? CrazySDK.Data.GetInt(key, fallback) : PlayerPrefs.GetInt(key, fallback)) : (int?)null;
            ints[key] = value;
        }
        return value ?? fallback;
    }

    public static string GetString(string key, string fallback)
    {
        bool crazy = Storage();
        if (!stringsRead.Contains(key))
        {
            strings[key] = Has(crazy, key) ? (crazy ? CrazySDK.Data.GetString(key, fallback) : PlayerPrefs.GetString(key, fallback)) : null;
            stringsRead.Add(key);
        }
        return strings[key] ?? fallback;
    }

    public static void SetInt(string key, int value)
    {
        if (Storage()) CrazySDK.Data.SetInt(key, value);
        else PlayerPrefs.SetInt(key, value);
        ints[key] = value;
    }

    public static void SetString(string key, string value)
    {
        if (Storage()) CrazySDK.Data.SetString(key, value);
        else PlayerPrefs.SetString(key, value);
        strings[key] = value;
        stringsRead.Add(key);
    }

    public static void DeleteKey(string key)
    {
        if (Storage()) CrazySDK.Data.DeleteKey(key);
        else PlayerPrefs.DeleteKey(key);
        ints[key] = null;
        strings[key] = null;
        stringsRead.Add(key);
    }

    // The Data module saves by itself (debounced); PlayerPrefs needs telling.
    public static void Flush()
    {
        if (!OnCrazyGames) PlayerPrefs.Save();
    }
    #endregion

    #region Gameplay events
    private static bool playing;

    // A shift is running and nothing is covering it (called on start and on every resume).
    public static void GameplayStart()
    {
        if (playing || !OnCrazyGames) return;
        playing = true;
        CrazySDK.Game.GameplayStart();
    }

    // A break: the result card, the shop, the help card.
    public static void GameplayStop()
    {
        if (!playing || !OnCrazyGames) return;
        playing = false;
        CrazySDK.Game.GameplayStop();
    }

    // For something special only (a new cafe level), not routine events.
    public static void HappyTime()
    {
        if (OnCrazyGames) CrazySDK.Game.HappyTime();
    }
    #endregion

    #region Ads
    public static bool AdsSupported => OnCrazyGames;
    public static bool AdShowing { get; private set; }

    // Why a rewarded ad did not pay out, in a line the result card can show.
    public enum AdOutcome { Watched, Unavailable, AdBlock, AdsOff }

    // Forced ads are kept rare (CrazyGames' advice for idle/tycoon games is "very sparingly"): at most one midgame
    // ad per MidgameGap of play - with shifts of 5-6 minutes that is about every other shift - and never in the
    // first one. The SDK has its own cooldown on top.
    public const float MidgameGap = 8f * 60f;
    private static float lastMidgameAt;

    // Between shifts. `then` always runs, ad or no ad.
    public static void Midgame(Action then)
    {
        if (!OnCrazyGames || AdShowing || Time.realtimeSinceStartup - lastMidgameAt < MidgameGap) { then(); return; }
        lastMidgameAt = Time.realtimeSinceStartup;
        AdShowing = true;
        CrazySDK.Ad.RequestAd(CrazyAdType.Midgame,
            null,
            error => { AdShowing = false; GoogleAnalytics.Event("ad_midgame", "outcome", error.code); then(); },
            () => { AdShowing = false; GoogleAnalytics.Event("ad_midgame", "outcome", "watched"); then(); });
    }

    // Asked for by the player. The reward is granted only on Watched.
    public static void Rewarded(Action<AdOutcome> then)
    {
        if (!OnCrazyGames || AdShowing) { then(AdOutcome.Unavailable); return; }
        AdShowing = true;
        CrazySDK.Ad.RequestAd(CrazyAdType.Rewarded,
            null,
            error =>
            {
                AdShowing = false;
                GoogleAnalytics.Event("ad_rewarded", "outcome", error.code);
                then(error.code switch
                {
                    "adblock" => AdOutcome.AdBlock,
                    "adsDisabledBasicLaunch" => AdOutcome.AdsOff,
                    _ => AdOutcome.Unavailable,
                });
            },
            () => { AdShowing = false; GoogleAnalytics.Event("ad_rewarded", "outcome", "watched"); then(AdOutcome.Watched); });
    }
    #endregion
}
