using System;
using System.Collections.Generic;
using UnityEngine;

// What the cafe keeps between shifts and sessions: the money in the till, the levels played and their stars, the
// cafe's tier (which opens more of the shop, new recipes and new kinds of customers) and what was bought. Stored
// through GamePlatform (the player's CrazyGames account there, PlayerPrefs elsewhere); everything else starts fresh
// with each shift. A shift is a LEVEL now (user, 2026-10-28: "short levels with 1-3 stars, something new at the end
// of them"): level N is the N-th shift, and the cafe's tier follows the level reached (TierLevels), so the road between
// levels (UIBreakCard's road card) can show exactly where the next new thing waits.
public static class CafeProgress
{
    private const string CoinsKey = "Cafe.Coins", StarsKey = "Cafe.Stars", OwnedKey = "Cafe.Owned",
                         HatKey = "Cafe.Hat", ShiftsKey = "Cafe.Shifts", TaughtKey = "Cafe.Taught", SnackTipKey = "Cafe.SnackTip",
                         TiersKey = "Cafe.Tiers", LegacyTierKey = "Cafe.LegacyTier", LevelStarsKey = "Cafe.LevelStars";

    // The level (shift) at which each tier opens: tier 2 from level 2, tier 3 from level 4, then 6, 9 and 12 - a new
    // thing every second or third level early on. Whatever a tier brings (recipes, customers, shop items) is listed by
    // Orders.NewAt, Customers.ArrivesAt and the shop's upgrade levels.
    public static readonly int[] TierLevels = { 1, 2, 4, 6, 9, 12 };
    public static int MaxLevel => TierLevels.Length;

    // Before levels, tiers followed the stars customers gave (total stars for tier 1, 2, 3...). Kept for saves made
    // then: the tier a player had reached is remembered once (LegacyTier) and never taken away.
    public static readonly int[] LevelStars = { 0, 8, 30, 60, 100, 150 };

    // Money, stars or purchases changed (UI refresh). Static: subscribe with named methods only.
    public static event Action Changed;
    // The cafe reached a new level (the new level).
    public static event Action<int> LeveledUp;

    private static bool loaded;
    private static int coins, stars, shifts, legacyTier;
    private static string levelStars = "";
    private static string hat;
    private static readonly HashSet<string> owned = new();

    // Statics survive a scene reload (Replay) but not a domain reload; start from the saved values either way.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
        Changed = null;
        LeveledUp = null;
    }

    private static void Load()
    {
        if (loaded) return;
        loaded = true;
        coins = GamePlatform.GetInt(CoinsKey, 0);
        stars = GamePlatform.GetInt(StarsKey, 0);
        shifts = GamePlatform.GetInt(ShiftsKey, 0);
        hat = GamePlatform.GetString(HatKey, "");
        levelStars = GamePlatform.GetString(LevelStarsKey, "");
        if (GamePlatform.GetInt(TiersKey, 0) == 0)
        {
            legacyTier = LevelFor(stars);
            GamePlatform.SetInt(LegacyTierKey, legacyTier);
            GamePlatform.SetInt(TiersKey, 1);
            GamePlatform.Flush();
        }
        else legacyTier = GamePlatform.GetInt(LegacyTierKey, 1);
        owned.Clear();
        foreach (var id in GamePlatform.GetString(OwnedKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            owned.Add(id);
    }

    public static int Coins { get { Load(); return coins; } }
    public static int Stars { get { Load(); return stars; } }
    public static int ShiftsPlayed { get { Load(); return shifts; } }

    // The cafe's tier (1-6): what it has opened so far.
    public static int Level { get { Load(); return Mathf.Max(TierAt(shifts + 1), legacyTier); } }

    // The tier a save from before levels had reached (kept, never taken away).
    public static int LegacyTier { get { Load(); return legacyTier; } }

    // The level being played, or the next one between levels.
    public static int LevelNumber => ShiftsPlayed + 1;

    // The tier the cafe has on reaching level `levelNumber` (from the levels alone).
    public static int TierAt(int levelNumber)
    {
        int tier = 0;
        while (tier < TierLevels.Length && levelNumber >= TierLevels[tier]) tier++;
        return tier;
    }

    // The tier that opens on reaching this level for this player (0: nothing new there).
    public static int TierOpensAt(int levelNumber)
    {
        Load();
        int tier = TierAt(levelNumber);
        return tier > TierAt(levelNumber - 1) && tier > legacyTier ? tier : 0;
    }

    // The stars (1-3) a played level earned; 0 for one not played (or played before levels were counted).
    public static int StarsOf(int levelNumber)
    {
        Load();
        int i = levelNumber - 1;
        return i >= 0 && i < levelStars.Length ? Mathf.Clamp(levelStars[i] - '0', 0, 3) : 0;
    }

    public static void RecordLevelStars(int levelNumber, int count)
    {
        Load();
        int i = levelNumber - 1;
        if (i < 0) return;
        var chars = levelStars.PadRight(i + 1, '0').ToCharArray();
        chars[i] = (char)('0' + Mathf.Clamp(count, 0, 3));
        levelStars = new string(chars);
        GamePlatform.SetString(LevelStarsKey, levelStars);
        Save();
    }

    // Whether the first shift has been taught (Tutorial). Not part of the cafe, but it belongs with the
    // other things that have to survive closing the tab.
    public static bool Taught
    {
        get => GamePlatform.GetInt(TaughtKey, 0) == 1;
        set { GamePlatform.SetInt(TaughtKey, value ? 1 : 0); GamePlatform.Flush(); }
    }

    // The snack toss is taught once, the first time a customer gets impatient (Tutorial.BeginSnackTip).
    public static bool SnackTipShown
    {
        get => GamePlatform.GetInt(SnackTipKey, 0) == 1;
        set { GamePlatform.SetInt(SnackTipKey, value ? 1 : 0); GamePlatform.Flush(); }
    }

    public static int LevelFor(int starCount)
    {
        int level = 0;
        while (level < LevelStars.Length && starCount >= LevelStars[level]) level++;
        return level;
    }

    // Progress towards the cafe's next tier, in levels played: levels into this tier / levels it spans (0/0 at the
    // top). The tier follows the levels (TierLevels); this was still counted in stars against the old star
    // thresholds and showed "-4 / 50" (user, 2026-11-06).
    public static (int into, int span) LevelProgress => ProgressAfter(ShiftsPlayed);

    // The same after `played` levels.
    public static (int into, int span) ProgressAfter(int played)
    {
        Load();
        int tier = Mathf.Max(TierAt(played + 1), legacyTier);
        if (tier >= MaxLevel) return (0, 0);
        // Level n opens tier t when n >= TierLevels[t]: so many levels played since the tier's first one. A save from
        // before levels may sit at a tier its levels have not reached yet: its bar starts there.
        int from = Mathf.Min(TierLevels[tier - 1] - 1, played);
        return (played - from, TierLevels[tier] - 1 - from);
    }

    public static void AddCoins(int amount)
    {
        if (amount == 0) return;
        Load();
        coins = Mathf.Max(0, coins + amount);
        GamePlatform.SetInt(CoinsKey, coins);
        Save();
    }

    public static bool TrySpend(int amount)
    {
        Load();
        if (coins < amount) return false;
        AddCoins(-amount);
        return true;
    }

    // The stars customers gave, all time (a score; the tier no longer follows it).
    public static void AddStars(int amount)
    {
        if (amount <= 0) return;
        Load();
        stars += amount;
        GamePlatform.SetInt(StarsKey, stars);
        Save();
    }

    // A level was played to its end: the next one is reached, and with it maybe a new tier (LeveledUp, at the end of
    // the level, so the break cards can show what it brings).
    public static void CountShift()
    {
        Load();
        int before = Level;
        GamePlatform.SetInt(ShiftsKey, ++shifts);
        CafeDay.ShiftFinished();
        Save();
        int after = Level;
        for (int tier = before + 1; tier <= after; tier++) LeveledUp?.Invoke(tier);
    }

    public static bool Owns(string id)
    {
        Load();
        return owned.Contains(id);
    }

    public static void AddOwned(string id)
    {
        Load();
        if (!owned.Add(id)) return;
        GamePlatform.SetString(OwnedKey, string.Join(",", owned));
        Save();
    }

    // The chef's hat ("" = none). Only one at a time.
    public static string Hat
    {
        get { Load(); return hat; }
        set
        {
            Load();
            hat = value ?? "";
            GamePlatform.SetString(HatKey, hat);
            Save();
        }
    }

    private static void Save()
    {
        GamePlatform.Flush();
        Changed?.Invoke();
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Chibi Cafe/Reset Cafe Progress")]
    public static void ResetAll()
    {
        foreach (var key in new[] { CoinsKey, StarsKey, OwnedKey, HatKey, ShiftsKey, TaughtKey, SnackTipKey, DailyTreat.DateKey, DailyTreat.StreakKey,
                                     DailyTreat.SavedKey, DailyTreat.SavedDateKey, DailyTreat.TreatDayKey, DailyTreat.SavedDayKey,
                                     CafeDay.NumberKey, CafeDay.StartKey, CafeDay.ShiftsKey, CafeDay.GapKey,
                                     GrandGoal.TeasedKey, GrandGoal.ReachedKey, TiersKey, LegacyTierKey, LevelStarsKey,
                                     "Cafe.FirstDay", "Cafe.FirstServe", OfflineEarnings.LastSeenKey, BurgerBook.Key,
                                     "Cafe.Week", "Cafe.WeekProgress", Stations.TaughtKey(Stations.Kind.Bacon),
                                     Stations.TaughtKey(Stations.Kind.Fries), "Cafe.QueueTip", "Cafe.ChopTip", "Cafe.BinTip",
                                     "Cafe.ShopTip", "Cafe.MochiTip" })
            GamePlatform.DeleteKey(key);
        GamePlatform.Flush();
        loaded = false;
        Changed?.Invoke();
    }
#endif
}
