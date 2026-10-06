using UnityEngine;

public enum CustomerKind
{
    Regular,    // the skeletons: there from day one
    Knight,
    Barbarian,
    Mage,
    Rogue,
    Ranger
}

// Who walks in. The regulars are there from the start; adventurers begin to drop by as the cafe grows
// (CafeProgress.Level). Each has one quirk that is easy to read mid-rush, and says it as they sit down, so
// the player knows how to treat them without opening any menu.
public static class Customers
{
    public readonly struct Profile
    {
        public readonly string Plural;
        public readonly int Level;           // cafe level they start coming at
        public readonly float PatienceScale;
        public readonly string Hello;        // said when they sit down (null: nothing)
        public readonly Mood HelloMood;
        public readonly string Quirk;        // for the "new customers" message

        public Profile(string plural, int level, float patienceScale, string hello, Mood helloMood, string quirk)
        {
            Plural = plural;
            Level = level;
            PatienceScale = patienceScale;
            Hello = hello;
            HelloMood = helloMood;
            Quirk = quirk;
        }
    }

    private static readonly Profile[] Profiles =
    {
        new("Regulars", 1, 1f, null, Mood.Amused, ""),
        new("Knights", 2, 1.4f, "I can wait!", Mood.Delighted, "patient and polite"),
        new("Barbarians", 3, 1f, "MAKE IT MESSY!", Mood.Amused, "tip for every mistake"),
        new("Mages", 4, 1f, "Impress me.", Mood.Amused, "pay big for perfect burgers"),
        new("Rogues", 5, 0.6f, "Quick, I'm late!", Mood.Shocked, "in a hurry, tip for speed"),
        new("Rangers", 6, 1f, "Got any snacks?", Mood.Delighted, "love a thrown snack"),
    };

    public static Profile Of(CustomerKind kind) => Profiles[(int)kind];

    public static bool IsUnlocked(CustomerKind kind) => CafeProgress.Level >= Of(kind).Level;

    // The kind that starts coming at this cafe level, if any.
    public static bool ArrivesAt(int level, out CustomerKind kind)
    {
        for (int i = 1; i < Profiles.Length; i++)
            if (Profiles[i].Level == level)
            {
                kind = (CustomerKind)i;
                return true;
            }
        kind = CustomerKind.Regular;
        return false;
    }

    // Extra money on top of the normal pay (can be negative: a disappointed critic).
    public static int Bonus(CustomerKind kind, BurgerReview review, float speed01) => kind switch
    {
        CustomerKind.Barbarian => Mathf.Min(review.Flaws, 3),
        CustomerKind.Mage => review.IsPerfect ? 4 : review.Flaws >= 2 ? -2 : 0,
        CustomerKind.Rogue => speed01 >= 0.8f ? 3 : 0,
        _ => 0,
    };

    // What they say about the burger: their quirk speaks where it has an opinion, otherwise the burger's own line.
    public static (string line, Mood mood) React(CustomerKind kind, BurgerReview review, float speed01)
    {
        switch (kind)
        {
            case CustomerKind.Barbarian:
                if (review.IsChaos) return ("GLORIOUS CHAOS!", Mood.Delighted);
                if (!review.IsPerfect) return ("Messy! GOOD!", Mood.Delighted);
                return ("Too neat...", Mood.Amused);
            case CustomerKind.Mage:
                if (review.IsPerfect) return ("Exquisite.", Mood.Delighted);
                if (review.Flaws >= 2) return ("Hmph. Sloppy.", Mood.Shocked);
                break;
            case CustomerKind.Rogue:
                if (speed01 >= 0.8f) return ("That was quick!", Mood.Delighted);
                break;
        }
        return (review.Reaction, review.Mood);
    }

    // Knights are too polite to throw tomatoes at the chef.
    public static bool ThrowsBack(CustomerKind kind) => kind != CustomerKind.Knight;

    public static string GiveUpLine(CustomerKind kind) => kind == CustomerKind.Knight ? "Farewell, chef..." : "Too slow!";

    // How much a thrown snack is worth to them (Rangers love snacks: twice the time, twice the tip).
    public static float SnackScale(CustomerKind kind) => kind == CustomerKind.Ranger ? 2f : 1f;
}
