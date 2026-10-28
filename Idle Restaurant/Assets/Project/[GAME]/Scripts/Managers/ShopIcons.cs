using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// The shop's pictures are not in the build's data file (2026-10-17, to cut the first load): they were 3.3 MB of
// uncompressed pixels there (sizes not multiples of 4 can't be compressed) for 380 KB of PNGs. They are
// StreamingAssets/shop/shop_<id>.png (Tools/Chibi Cafe/4 writes them there), fetched in the background a moment
// after the cafe is on screen, and handed to the catalogue (Upgrade.icon) as they arrive; Loaded tells the open
// shop to show them. Kept for the session (a Replay reloads the scene, whose catalogue has no pictures again).
public static class ShopIcons
{
    public static event Action Loaded;

    private static readonly Dictionary<string, Sprite> cache = new();
    private static bool loading;

    // From CafeShop.Start: pictures already fetched go on at once; the rest are fetched on an object that survives
    // a Replay, and go onto whichever catalogue is in the scene when they arrive.
    public static void Begin(IReadOnlyList<Upgrade> upgrades, float delay)
    {
        Apply(upgrades);
        if (loading || AllCached(upgrades)) return;
        loading = true;
        var runner = new GameObject("ShopIcons") { hideFlags = HideFlags.HideInHierarchy };
        UnityEngine.Object.DontDestroyOnLoad(runner);
        runner.AddComponent<Runner>().StartCoroutine(Load(new List<Upgrade>(upgrades), delay, runner));
    }

    private class Runner : MonoBehaviour { }

    private static IEnumerator Load(List<Upgrade> upgrades, float delay, GameObject runner)
    {
        yield return new WaitForSecondsRealtime(delay);   // the first frames of the game first
        foreach (var upgrade in upgrades)
        {
            if (upgrade == null || cache.ContainsKey(upgrade.id)) continue;
            string url = Path.Combine(Application.streamingAssetsPath, "shop", "shop_" + upgrade.id + ".png");
            if (!url.Contains("://")) url = "file://" + url;
            using var request = UnityWebRequestTexture.GetTexture(url, true);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ShopIcons] {upgrade.id}: {request.error}");
                continue;
            }
            var texture = DownloadHandlerTexture.GetContent(request);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "shop_" + upgrade.id;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            cache[upgrade.id] = sprite;
        }
        loading = false;
        if (CafeShop.Instance != null) Apply(CafeShop.Instance.Upgrades);
        UnityEngine.Object.Destroy(runner);
    }

    private static bool AllCached(IReadOnlyList<Upgrade> upgrades)
    {
        foreach (var upgrade in upgrades) if (upgrade != null && !cache.ContainsKey(upgrade.id)) return false;
        return true;
    }

    private static void Apply(IReadOnlyList<Upgrade> upgrades)
    {
        bool any = false;
        foreach (var upgrade in upgrades)
            if (upgrade != null && upgrade.icon == null && cache.TryGetValue(upgrade.id, out var sprite)) { upgrade.icon = sprite; any = true; }
        if (any) Loaded?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Loaded = null; loading = false; }   // (domain reload is off in the editor)
}
