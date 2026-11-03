using System;
using UnityEngine;

// Money the cafe made while the player was away (from the report, 2026-10-28: a reason to come back, as in the idle
// games that keep players for months). The regulars keep dropping by for snacks: a little money for every hour away,
// up to MaxHours, more for a bigger cafe (its tier and the tables bought). Paid once, on the first level of the next
// session, by a "Welcome back" card (ShiftFlow) that can double it with a video. Nothing is paid for less than
// MinMinutes away, nor on the very first visit.
public static class OfflineEarnings
{
    public const string LastSeenKey = "Cafe.LastSeen";
    private const int MinMinutes = 15;
    private const float MaxHours = 3f;

    private static bool counted;
    private static int pending, minutesAway;

    // What the time away earned (worked out once a session, before the first Touch moves the clock on).
    public static (int amount, int minutes) Pending
    {
        get
        {
            Count();
            return (pending, minutesAway);
        }
    }

    // Per hour away: 12 + 6 for each tier + 5 for each extra table.
    public static int PerHour => 12 + 6 * CafeProgress.Level + 5 * CafeShop.ExtraCustomers;

    private static void Count()
    {
        if (counted) return;
        counted = true;
        long last = GamePlatform.GetInt(LastSeenKey, 0);
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        minutesAway = last > 0 ? (int)Math.Max(0, (now - last) / 60) : 0;
        pending = minutesAway >= MinMinutes ? Mathf.RoundToInt(Mathf.Min(minutesAway / 60f, MaxHours) * PerHour) : 0;
    }

    // Paid: into the wallet, once.
    public static int Claim()
    {
        Count();
        int amount = pending;
        pending = 0;
        if (amount > 0) CafeProgress.AddCoins(amount);
        Touch();
        return amount;
    }

    // The player is here now (called every half minute while the game runs and when the page loses them).
    public static void Touch()
    {
        Count();   // (what the last absence earned is known before the clock moves on)
        GamePlatform.SetInt(LastSeenKey, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        GamePlatform.Flush();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => counted = false;
}
