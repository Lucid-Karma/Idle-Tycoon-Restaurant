using System.Globalization;
using UnityEngine;

// The game's long goal (2026-10-14, user: "an ultimate goal - say, opening a pizzeria, a grill restaurant or an
// ice-cream shop with money you can only save up by playing all the days"): save $2,000 to open a second
// restaurant, a Grill House (roast dinner, stew, steak - all of it already in the KayKit pack, so it adds
// nothing to the download). The cafe reaches its top level after about ten shifts and the shop runs out soon after;
// this gives the money that keeps coming in somewhere to go, and every day a visible step towards it.
// The savings are the wallet itself (the user's choice): buying from the shop sets it back, which is the choice.
// Introduced by a card after the second shift (ShiftFlow), then a small card under the wallet (UIGrandGoal) and a
// row on each shift's card. Reaching it is celebrated once. The Grill House itself is the next step (not built
// yet): until then the goal says it opens soon and takes no money.
public static class GrandGoal
{
    public const string Name = "Grill House";
    public const int Price = 2000;
    public const string TeasedKey = "Cafe.GrillTeased", ReachedKey = "Cafe.GrillReached";

    public static bool Teased => GamePlatform.GetInt(TeasedKey, 0) == 1;
    public static bool Announced => GamePlatform.GetInt(ReachedKey, 0) == 1;

    // Shown after the player's second shift: by then they know the kitchen, and the goal gives the next one a point.
    public static bool TeaseDue => !Teased && CafeProgress.ShiftsPlayed >= 2;

    public static bool Ready => CafeProgress.Coins >= Price;
    public static float Progress01 => Mathf.Clamp01(CafeProgress.Coins / (float)Price);

    // "$1,240 / $2,000"
    public static string Amount =>
        $"${Mathf.Min(CafeProgress.Coins, Price).ToString("N0", CultureInfo.InvariantCulture)} / ${Price.ToString("N0", CultureInfo.InvariantCulture)}";

    public static void MarkTeased()
    {
        GamePlatform.SetInt(TeasedKey, 1);
        GamePlatform.Flush();
        GoogleAnalytics.Event("grand_goal_teased", "coins", CafeProgress.Coins);
    }

    public static void MarkAnnounced()
    {
        GamePlatform.SetInt(ReachedKey, 1);
        GamePlatform.Flush();
        GoogleAnalytics.Event("grand_goal_reached", "shifts", CafeProgress.ShiftsPlayed, "cafe_day", CafeDay.Number);
    }
}
