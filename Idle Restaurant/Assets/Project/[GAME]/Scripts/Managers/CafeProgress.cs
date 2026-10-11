using System;
using System.Collections.Generic;
using UnityEngine;

// What the cafe keeps between shifts and sessions: the money in the till, the stars customers gave (they
// raise the cafe level, which opens more of the shop and brings new kinds of customers) and what was
// bought. Stored through GamePlatform (the player's CrazyGames account there, PlayerPrefs elsewhere);
// everything else starts fresh with each shift.
public static class CafeProgress
{
    private const string CoinsKey = "Cafe.Coins", StarsKey = "Cafe.Stars", OwnedKey = "Cafe.Owned",
                         HatKey = "Cafe.Hat", ShiftsKey = "Cafe.Shifts", TaughtKey = "Cafe.Taught", SnackTipKey = "Cafe.SnackTip";

    // Total stars needed for level 1, 2, 3... Every served order gives its rounded rating (0-5 stars), so a
    // shift of 8 customers is worth about 20-35: level 2 after the first shift, the last level after ~10.
    public static readonly int[] LevelStars = { 0, 15, 40, 80, 130, 190 };
    public static int MaxLevel => LevelStars.Length;

    // Money, stars or purchases changed (UI refresh). Static: subscribe with named methods only.
    public static event Action Changed;
    // The cafe reached a new level (the new level).
    public static event Action<int> LeveledUp;

    private static bool loaded;
    private static int coins, stars, shifts;
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
        owned.Clear();
        foreach (var id in GamePlatform.GetString(OwnedKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            owned.Add(id);
    }

    public static int Coins { get { Load(); return coins; } }
    public static int Stars { get { Load(); return stars; } }
    public static int ShiftsPlayed { get { Load(); return shifts; } }

    public static int Level => LevelFor(Stars);

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

    // Progress towards the next level: stars into this level / stars this level spans (0/0 at the top).
    public static (int into, int span) LevelProgress
    {
        get
        {
            int level = Level;
            if (level >= MaxLevel) return (0, 0);
            int from = LevelStars[level - 1];
            return (Stars - from, LevelStars[level] - from);
        }
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

    public static void AddStars(int amount)
    {
        if (amount <= 0) return;
        Load();
        int before = Level;
        stars += amount;
        GamePlatform.SetInt(StarsKey, stars);
        Save();
        int after = Level;
        for (int level = before + 1; level <= after; level++) LeveledUp?.Invoke(level);
    }

    public static void CountShift()
    {
        Load();
        GamePlatform.SetInt(ShiftsKey, ++shifts);
        Save();
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
        foreach (var key in new[] { CoinsKey, StarsKey, OwnedKey, HatKey, ShiftsKey, TaughtKey, SnackTipKey }) GamePlatform.DeleteKey(key);
        GamePlatform.Flush();
        loaded = false;
        Changed?.Invoke();
    }
#endif
}
