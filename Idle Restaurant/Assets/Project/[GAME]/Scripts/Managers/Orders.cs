using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// What customers order. Every customer asks for one recipe as they sit down (shown in their bubble as a little
// burger built from the real layers, UIOrderTicket), and a plate becomes a burger when it holds as many layers as
// the order it is being built for (Plate). This is what makes one customer different from the next: before it,
// every customer wanted "any six layers" and after a few burgers the player was only repeating himself.
// Recipes open up with the cafe's level, so something new arrives every shift or two ("New recipe!").
// Mess is still content, not failure: a burger that is not what they asked for is never refused - a missing or
// extra layer is a flaw like a burnt bun (BurgerReview), with its own line ("Where's my cheese?!").
public sealed class Recipe
{
    public readonly string Name;
    public readonly int Level;                 // cafe level it starts being ordered at
    public readonly string[] Layers;           // bottom to top; a bun means the plate adds the top bun itself

    public Recipe(string name, int level, params string[] layers)
    {
        Name = name;
        Level = level;
        Layers = layers;
    }

    public int Count => Layers.Length;

    // How many of each ingredient it takes.
    public Dictionary<string, int> Needs()
    {
        var needs = new Dictionary<string, int>();
        foreach (var layer in Layers) needs[layer] = needs.TryGetValue(layer, out int n) ? n + 1 : 1;
        return needs;
    }
}

public static class Orders
{
    // Ingredient names as EdibleBase.Name has them: bun, burger (the patty), cheese, lettuce, tomato, onion.
    public static readonly Recipe Classic = new("Classic", 1, "bun", "burger", "lettuce", "tomato");

    // The first lesson's order: the shortest real burger that still uses all three stations (oven, pan, board).
    public static readonly Recipe LessonRecipe = new("Cheeseburger", 1, "bun", "burger", "cheese");

    public static readonly Recipe[] Menu =
    {
        Classic,
        LessonRecipe,
        new("Onion Burger", 2, "bun", "burger", "onion", "cheese"),
        new("Garden", 3, "bun", "lettuce", "tomato", "onion"),
        new("Deluxe", 3, "bun", "burger", "cheese", "lettuce", "tomato"),
        new("Double", 4, "bun", "burger", "burger", "cheese"),
        new("Big Stack", 5, "bun", "burger", "cheese", "burger", "cheese"),
        new("The Tower", 6, "bun", "burger", "cheese", "lettuce", "tomato", "onion"),
    };

    public static IEnumerable<Recipe> Unlocked => Menu.Where(r => r.Level <= CafeProgress.Level);

    // The recipes a cafe level adds, for the level-up message.
    public static IEnumerable<Recipe> NewAt(int level) => Menu.Where(r => r.Level == level);

    // What a customer sitting down asks for. The newest recipes come up more often, so a new one is seen soon;
    // the first lesson always asks for its own short order (LessonRecipe, what the lesson walks through).
    public static Recipe Pick()
    {
        if (Tutorial.Running) return LessonRecipe;
        var open = Unlocked.ToList();
        int newest = open.Max(r => r.Level);
        float total = 0f;
        foreach (var r in open) total += Weight(r, newest);
        float roll = Random.value * total;
        foreach (var r in open)
        {
            roll -= Weight(r, newest);
            if (roll <= 0f) return r;
        }
        return open[open.Count - 1];
    }

    // The day's special (DayGoals: "Serve 2 Cheeseburgers") is ordered about half the time, so the goal can be met.
    public static Recipe Special;

    private static float Weight(Recipe r, int newest) => r == Special ? 4f : r.Level == newest ? 2f : 1f;

    // A customer whose order is still to be made: seated and waiting, nothing on their table yet.
    public static bool Waiting(NpcFsm npc) =>
        npc != null && npc.isActiveAndEnabled && npc.Ordered != null && npc.executingNpcState == ExecutingNpcState.WAIT
        && npc.chair != null && !npc.chair.GetTableService().IsHaveFood();

    // Someone is already making (or carrying) this customer's burger.
    public static bool Claimed(NpcFsm npc, Plate except)
    {
        foreach (var plate in Plates)
            if (plate != except && plate.ForCustomer == npc) return true;
        foreach (var burger in Burgers)
            if (burger.ForCustomer == npc) return true;
        return false;
    }

    // The order a plate with these layers should be making: one nobody is making yet, the closest match first
    // (layers the order does not want count against it, but never rule it out: a six-layer burger can still be
    // for a cheeseburger customer), then whoever has waited longest.
    public static NpcFsm BestFor(IReadOnlyCollection<string> onPlate, Plate plate)
    {
        NpcFsm best = null;
        float bestScore = float.NegativeInfinity;
        foreach (var npc in NpcFsm.Active)
        {
            if (!Waiting(npc) || Claimed(npc, plate)) continue;
            int matching = Matching(onPlate, npc.Ordered);
            float score = matching * 100f - (onPlate.Count - matching) * 30f + npc.WaitedSeconds;
            if (score > bestScore) { bestScore = score; best = npc; }
        }
        return best;
    }

    // How many layers already there are ones the recipe wants.
    public static int Matching(IEnumerable<string> layers, Recipe recipe)
    {
        var needs = recipe.Needs();
        int n = 0;
        foreach (var layer in layers)
            if (needs.TryGetValue(layer, out int left) && left > 0) { needs[layer] = left - 1; n++; }
        return n;
    }

    // What is still missing from the recipe, given what is on the plate (and anything else already under way).
    public static List<string> Missing(Recipe recipe, IEnumerable<string> have)
    {
        var needs = recipe.Needs();
        foreach (var layer in have)
            if (needs.TryGetValue(layer, out int left) && left > 0) needs[layer] = left - 1;
        var missing = new List<string>();
        foreach (var layer in recipe.Layers)
            if (needs.TryGetValue(layer, out int left) && left > 0) { missing.Add(layer); needs[layer] = left - 1; }
        return missing;
    }

    #region Live objects
    private static readonly List<Plate> plates = new();
    public static IReadOnlyList<Plate> AllPlates => plates;
    private static readonly List<Hamburger> burgers = new();
    public static IReadOnlyList<Plate> Plates => plates;
    public static IReadOnlyList<Hamburger> Burgers => burgers;
    public static void Register(Plate p) { if (!plates.Contains(p)) plates.Add(p); }
    public static void Unregister(Plate p) => plates.Remove(p);
    public static void Register(Hamburger b) { if (!burgers.Contains(b)) burgers.Add(b); }
    public static void Unregister(Hamburger b) => burgers.Remove(b);
    #endregion

    public static string Label(string ingredient) => ingredient == "burger" ? "patty" : ingredient;
}
