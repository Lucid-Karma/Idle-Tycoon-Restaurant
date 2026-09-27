using System.Collections.Generic;
using UnityEngine;

// One layer of a served burger: which ingredient, and how it was prepared.
public readonly struct BurgerLayer
{
    public readonly string Name;
    public readonly Prep Prep;

    public BurgerLayer(string name, Prep prep)
    {
        Name = name;
        Prep = prep;
    }
}

public enum Mood
{
    Delighted, // a proper burger
    Amused,    // messy, but they're laughing
    Shocked    // raw patty, or a real pile-up of mistakes
}

// What a customer makes of a burger. Mess is content, not failure: every sloppy layer lowers the
// quality stars a little, but also gives the burger a name and the customer a line to say. The worst
// mistake names the burger; three or more make it a "Chaos Burger" (the one on the poster).
public sealed class BurgerReview
{
    public float Quality01 { get; }
    public int Flaws { get; }
    public string Title { get; }
    public string Reaction { get; }
    public Mood Mood { get; }
    public bool IsPerfect => Flaws == 0;
    public bool IsChaos => Flaws >= ChaosFlaws;

    public const int ChaosFlaws = 3;

    private BurgerReview(float quality01, int flaws, string title, string reaction, Mood mood)
    {
        Quality01 = quality01;
        Flaws = flaws;
        Title = title;
        Reaction = reaction;
        Mood = mood;
    }

    // Listed from most to least noticeable: the first one found names the burger.
    private enum Flaw { RawPatty, NoPatty, BurntPatty, WholeOnion, WholeTomato, WholeCheese, NoBun, BurntBun, RawBun, Duplicate }

    private static readonly string[] Titles =
    {
        "Moo Burger", "Veggie Surprise", "Charcoal Special", "Onion Tears", "Tomato Bomb",
        "Cheese Brick", "Naked Burger", "Toasty Tower", "Doughy Deluxe", "Double Trouble"
    };

    // Short on purpose: said in a speech bubble over a seated customer, read in a glance.
    private static readonly string[] Reactions =
    {
        "It's still mooing!", "Where's the patty?!", "Extra crispy!", "A whole onion?!",
        "A WHOLE tomato?!", "A cheese brick?!", "Where's the bun?", "Too toasty!",
        "Cold bun... eh.", "Two of those?!"
    };

    public static BurgerReview Of(IReadOnlyList<BurgerLayer> layers)
    {
        var seen = new HashSet<string>();
        var flaws = new SortedSet<Flaw>();
        float total = 0f;

        foreach (var layer in layers)
        {
            bool duplicate = !seen.Add(layer.Name);
            if (duplicate)
            {
                flaws.Add(Flaw.Duplicate);
                total += 0.3f;
                continue;
            }
            total += LayerScore(layer, flaws);
        }

        float quality = layers.Count > 0 ? total / layers.Count : 0f;
        if (!seen.Contains("burger")) { flaws.Add(Flaw.NoPatty); quality *= 0.6f; }
        if (!seen.Contains("bun")) { flaws.Add(Flaw.NoBun); quality *= 0.8f; }

        if (flaws.Count == 0)
            return new BurgerReview(quality, 0, "Chef's Classic", "Chef's kiss!", Mood.Delighted);

        Flaw worst = flaws.Min;
        bool shocked = worst == Flaw.RawPatty || flaws.Count > ChaosFlaws;
        var mood = shocked ? Mood.Shocked : Mood.Amused;
        if (flaws.Count >= ChaosFlaws)
            return new BurgerReview(quality, flaws.Count, "Chaos Burger", "What IS this?!", mood);
        return new BurgerReview(quality, flaws.Count, Titles[(int)worst], Reactions[(int)worst], mood);
    }

    private static float LayerScore(BurgerLayer layer, SortedSet<Flaw> flaws)
    {
        switch (layer.Prep)
        {
            case Prep.Whole:
                if (layer.Name == "onion") flaws.Add(Flaw.WholeOnion);
                else if (layer.Name == "tomato") flaws.Add(Flaw.WholeTomato);
                else flaws.Add(Flaw.WholeCheese);
                return 0.5f;
            case Prep.Raw:
                if (layer.Name == "burger") { flaws.Add(Flaw.RawPatty); return 0.15f; }
                flaws.Add(Flaw.RawBun);
                return 0.6f;
            case Prep.Burnt:
                if (layer.Name == "burger") { flaws.Add(Flaw.BurntPatty); return 0.35f; }
                flaws.Add(Flaw.BurntBun);
                return 0.45f;
            default:
                return 1f;
        }
    }

    // How hard a stack wobbles in the chef's hands: round whole veggies and a pile of mistakes wobble more.
    public float Wobbliness => 1f + 0.25f * Mathf.Min(Flaws, 4);
}
