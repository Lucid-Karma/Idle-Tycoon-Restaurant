using System;
using System.Collections.Generic;
using UnityEngine;

// The burger book (from the report, 2026-10-28: a collection to complete, a reason to try things): every burger the
// game can name - the recipes made as ordered, and the funny names a mistake gives (Moo Burger, Tomato Bomb, the Chaos
// Burger...). Mess is content: the book is filled as much by getting it wrong as by getting it right. The first time
// a name is served it goes in the book, with a small tip (NewEntryTip) and a toast (UIToast); the book itself is a
// card from the HUD's book button (UIBookButton). Saved as a list of names.
public static class BurgerBook
{
    public const string Key = "Cafe.Book";
    public const int NewEntryTip = 3;

    // In the book's order: the menu first, then what mistakes make, then the Chaos Burger.
    public static readonly string[] All =
    {
        "Hamburger", "Classic", "Cheeseburger", "Onion Burger", "Cheese Lover", "Garden", "Deluxe", "Double",
        "Veggie Deluxe", "Big Stack", "Onion Double", "The Tower", "Bacon Burger", "BLT", "Bacon Deluxe", "Bacon Double",
        "Moo Burger", "Oink Burger", "Veggie Surprise", "Charcoal Special", "Bacon Jerky", "Mix-up Burger", "Surprise Burger",
        "Onion Tears", "Tomato Bomb", "Cheese Brick", "Naked Burger", "Toasty Tower", "Doughy Deluxe", "Spud Surprise",
        "Double Trouble", "Chaos Burger",
    };

    // A new name went into the book (the name, how many the book holds now). Static: subscribe with named methods.
    public static event Action<string, int> Discovered;

    private static HashSet<string> found;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        found = null;
        Discovered = null;
    }

    private static HashSet<string> Found
    {
        get
        {
            if (found != null) return found;
            found = new HashSet<string>(GamePlatform.GetString(Key, "").Split(',', StringSplitOptions.RemoveEmptyEntries));
            return found;
        }
    }

    public static int Count
    {
        get
        {
            int n = 0;
            foreach (var name in All) if (Found.Contains(name)) n++;
            return n;
        }
    }

    public static bool Has(string name) => Found.Contains(name);

    // A served burger's name (ScoreManager.RateOrder): into the book if it is one of its names and new.
    public static void Serve(string name)
    {
        if (string.IsNullOrEmpty(name) || Array.IndexOf(All, name) < 0 || !Found.Add(name)) return;
        GamePlatform.SetString(Key, string.Join(",", Found));
        CafeProgress.AddCoins(NewEntryTip);
        GoogleAnalytics.Event("book_entry", "name", name, "count", Count);
        Discovered?.Invoke(name, Count);
    }
}
