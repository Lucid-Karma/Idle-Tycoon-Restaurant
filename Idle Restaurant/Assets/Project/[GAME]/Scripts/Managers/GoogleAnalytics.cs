using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

// Google Analytics 4 (property "Chibi Burger Cafe - CrazyGames"), on the CrazyGames build only: started by Boot
// once the CrazyGames SDK is up, never on beyzosh.com, in the editor or on localhost. The browser side (gtag.js,
// Consent Mode, no ad features) is Plugins/WebGL/ChibiAnalytics.jslib.
// Events use GA4's recommended game names where one fits, so the GA reports know them:
//   game_ready            the cafe is on screen: load_seconds, returning_player, cafe_level
//   tutorial_begin / tutorial_complete
//   level_start           a shift starts: shift_number, cafe_level
//   level_end             a shift ends: shift_number, stars (average 0-5), earned, perfect, chaos, walkouts, best_streak
//   level_up              the cafe levels up: level
//   spend_virtual_currency  a shop upgrade bought: item_name, value, virtual_currency_name = coins
//   ad_midgame / ad_rewarded  outcome
// Sent from PlatformSession (session events), Tutorial, CafeShop and GamePlatform (ads).
public static class GoogleAnalytics
{
    const string MeasurementId = "G-F01FFEBQRP";

    private static bool on;
    // Editor testing: log what would be sent (set from the editor bridge).
    public static bool EditorLog { get; set; }

    public static void Start()
    {
        if (on || !GamePlatform.IsCrazyGames) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        on = ChibiGaInit(MeasurementId) == 1;
#endif
    }

    // Parameters as name, value pairs: Event("level_up", "level", 3).
    public static void Event(string name, params object[] pairs)
    {
        if (!on && !(Application.isEditor && EditorLog)) return;
        var json = new StringBuilder("{");
        for (int i = 0; i + 1 < pairs.Length; i += 2)
        {
            if (i > 0) json.Append(',');
            json.Append('"').Append(pairs[i]).Append("\":");
            json.Append(pairs[i + 1] switch
            {
                bool b => b ? "true" : "false",
                int n => n.ToString(CultureInfo.InvariantCulture),
                float f => f.ToString("0.##", CultureInfo.InvariantCulture),
                var v => "\"" + v.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"",
            });
        }
        json.Append('}');
        if (Application.isEditor) { Debug.Log("[GA] " + name + " " + json); return; }
#if UNITY_WEBGL && !UNITY_EDITOR
        ChibiGaEvent(name, json.ToString());
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern int ChibiGaInit(string id);
    [DllImport("__Internal")] private static extern void ChibiGaEvent(string name, string json);
#endif
}
