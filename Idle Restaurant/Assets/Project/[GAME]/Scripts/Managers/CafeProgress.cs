using System;
using System.Collections.Generic;
using UnityEngine;

// What the cafe keeps between shifts and sessions: the money in the till, the stars customers gave (they
// raise the cafe level, which opens more of the shop and brings new kinds of customers) and what was
// bought. Stored in PlayerPrefs; everything else starts fresh with each shift.
public static class CafeProgress
{
    private const string CoinsKey = "Cafe.Coins", StarsKey = "Cafe.Stars", OwnedKey = "Cafe.Owned",
                         HatKey = "Cafe.Hat", ShiftsKey = "Cafe.Shifts", TaughtKey = "Cafe.Taught";

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
        coins = PlayerPrefs.GetInt(CoinsKey, 0);
        stars = PlayerPrefs.GetInt(StarsKey, 0);
        shifts = PlayerPrefs.GetInt(ShiftsKey, 0);
        hat = PlayerPrefs.GetString(HatKey, "");
        owned.Clear();
        foreach (var id in PlayerPrefs.GetString(OwnedKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
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
        get => PlayerPrefs.GetInt(TaughtKey, 0) == 1;
        set { PlayerPrefs.SetInt(TaughtKey, value ? 1 : 0); PlayerPrefs.Save(); }
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
        PlayerPrefs.SetInt(CoinsKey, coins);
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
        PlayerPrefs.SetInt(StarsKey, stars);
        Save();
        int after = Level;
        for (int level = before + 1; level <= after; level++) LeveledUp?.Invoke(level);
    }

    public static void CountShift()
    {
        Load();
        PlayerPrefs.SetInt(ShiftsKey, ++shifts);
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
        PlayerPrefs.SetString(OwnedKey, string.Join(",", owned));
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
            PlayerPrefs.SetString(HatKey, hat);
            Save();
        }
    }

    private static void Save()
    {
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Chibi Cafe/Reset Cafe Progress")]
    public static void ResetAll()
    {
        foreach (var key in new[] { CoinsKey, StarsKey, OwnedKey, HatKey, ShiftsKey, TaughtKey }) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        loaded = false;
        Changed?.Invoke();
    }
#endif
}
