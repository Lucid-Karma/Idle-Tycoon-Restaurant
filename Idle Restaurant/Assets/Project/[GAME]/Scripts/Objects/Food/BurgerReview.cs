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
// Judged against what they ordered (Orders, Recipe): a missing or an extra layer is a flaw too ("Where's my
// cheese?!", "I didn't order onion!"), and a burger made exactly as ordered is named after the recipe.
public sealed class BurgerReview
{
    public float Quality01 { get; }
    public int Flaws { get; }
    public string Title { get; }
    public string Reaction { get; }
    public Mood Mood { get; }
    public bool IsPerfect => Flaws == 0;
    public bool IsChaos => Flaws >= ChaosFlaws;
    // Made of exactly the layers the order asked for (however well each was made).
    public bool AsOrdered { get; private set; }

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
    private enum Flaw { RawPatty, NoPatty, BurntPatty, Missing, Extra, WholeOnion, WholeTomato, WholeCheese, NoBun, BurntBun, RawBun, Duplicate }

    private static readonly string[] Titles =
    {
        "Moo Burger", "Veggie Surprise", "Charcoal Special", "Mix-up Burger", "Surprise Burger", "Onion Tears",
        "Tomato Bomb", "Cheese Brick", "Naked Burger", "Toasty Tower", "Doughy Deluxe", "Double Trouble"
    };

    // Short on purpose: said in a speech bubble over a seated customer, read in a glance. ({0}: the ingredient.)
    private static readonly string[] Reactions =
    {
        "It's still mooing!", "Where's the patty?!", "Extra crispy!", "Where's my {0}?!", "I didn't order {0}!",
        "A whole onion?!", "A WHOLE tomato?!", "A cheese brick?!", "Where's the bun?", "Too toasty!",
        "Cold bun... eh.", "Two of those?!"
    };

    // Against an order. Every layer the recipe wants and gets scores by how it was made; a layer it did not ask
    // for scores 0.3 and is a flaw; a layer it wanted but did not get scores 0 and is a flaw.
    public static BurgerReview Of(IReadOnlyList<BurgerLayer> layers, Recipe order)
    {
        if (order == null) return Of(layers);
        var needs = order.Needs();
        var flaws = new SortedSet<Flaw>();
        string missingName = null, extraName = null;
        float total = 0f;
        int extras = 0;

        foreach (var layer in layers)
        {
            if (needs.TryGetValue(layer.Name, out int left) && left > 0)
            {
                needs[layer.Name] = left - 1;
                total += LayerScore(layer, flaws);
            }
            else
            {
                extras++;
                total += 0.3f;
                bool doubled = order.Needs().ContainsKey(layer.Name);
                flaws.Add(doubled ? Flaw.Duplicate : Flaw.Extra);
                extraName ??= doubled ? null : layer.Name;
                LayerScore(layer, flaws);   // a raw patty is still a raw patty
            }
        }
        int missing = 0;
        foreach (var pair in needs)
        {
            if (pair.Value <= 0) continue;
            missing += pair.Value;
            if (pair.Key == "burger") flaws.Add(Flaw.NoPatty);
            else if (pair.Key == "bun") flaws.Add(Flaw.NoBun);
            else { flaws.Add(Flaw.Missing); missingName ??= pair.Key; }
        }

        float quality = total / Mathf.Max(1, order.Count + extras);
        bool asOrdered = missing == 0 && extras == 0;
        // Each wrong layer counts (capped: past a handful it is chaos either way).
        int count = Mathf.Min(6, flaws.Count + Mathf.Max(0, missing - 1) + Mathf.Max(0, extras - 1));

        if (flaws.Count == 0)
            return Ordered(new BurgerReview(quality, 0, order.Name, "Just what I ordered!", Mood.Delighted), asOrdered);

        Flaw worst = flaws.Min;
        bool shocked = worst == Flaw.RawPatty || count > ChaosFlaws;
        var mood = shocked ? Mood.Shocked : Mood.Amused;
        if (count >= ChaosFlaws)
            return Ordered(new BurgerReview(quality, count, "Chaos Burger", "What IS this?!", mood), asOrdered);
        string reaction = string.Format(Reactions[(int)worst],
            Orders.Label(worst == Flaw.Missing ? missingName : extraName ?? ""));
        return Ordered(new BurgerReview(quality, count, Titles[(int)worst], reaction, mood), asOrdered);
    }

    private static BurgerReview Ordered(BurgerReview review, bool asOrdered)
    {
        review.AsOrdered = asOrdered;
        return review;
    }

    // Without an order (a burger nobody ordered: its wobble in the chef's hands).
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
        return new BurgerReview(quality, flaws.Count, Titles[(int)worst], string.Format(Reactions[(int)worst], ""), mood);
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

    // A single ingredient thrown to a waiting customer (NpcFsm.CatchThrown): prepared = a snack...
    public static string SnackLine(BurgerLayer item) => item.Name switch
    {
        "tomato" => "Tomato snack? OK!",
        "onion" => "Onion rings? Sure!",
        "cheese" => "Cheese! Yes!",
        "lettuce" => "Salad? Fine.",
        "burger" => "Patty snack!",
        "bun" => "Just bread? Eh.",
        _ => "Snack time!",
    };

    // ...unprepared = a bonk on the head (and a tomato comes back).
    public static string BonkLine(BurgerLayer item) => item.Name switch
    {
        "tomato" => "OW! A tomato?!",
        "onion" => "OW! My head!",
        "cheese" => "BONK! Cheese?!",
        "burger" => item.Prep == Prep.Raw ? "EWW, raw meat!" : "HOT! Charcoal!",
        "bun" => item.Prep == Prep.Burnt ? "Charcoal?!" : "Hey! Cold bread!",
        _ => "HEY!",
    };

    // How hard a stack wobbles in the chef's hands: round whole veggies and a pile of mistakes wobble more.
    public float Wobbliness => 1f + 0.25f * Mathf.Min(Flaws, 4);
}
