using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

// Service never stops (the "arcade idle" flow of games like Burger Life): when the last customer of a shift
// has left, there is no result card and nothing pauses. The cafe stands empty for a breath while the break cards
// (UIBreakCard) say how the day went - its stars, its goal (DayGoals), the money -, what the cafe just opened and,
// once a calendar day, the daily treat (DailyTreat). That is the natural break where CrazyGames allows a midgame
// ad (GamePlatform.Midgame, at most one every 8 minutes), and then the next day's customers walk in on their own.
// The shift is still the unit of play underneath: its stats, the stars average, the bigger shift after a table
// is bought, the analytics events (level_end / level_start) and the "Double it" offer all keep working.
// Added to the GameManager's object at runtime (GameManager.Awake), so the scene does not change.
// The old way - result card, "Play again", a scene reload - was where most players stopped playing
// (average session 4.5 minutes, about one shift).
public class ShiftFlow : MonoBehaviour
{
    // The shortest the empty cafe stays before the next wave is sent (the cards usually take longer).
    public const float Breather = 3.5f;

    // Tiles on a "New!" card, at most (recipes and customers first, then shop items).
    private const int MaxTiles = 4;

    private bool flowing;

    private void OnEnable()
    {
        EventManager.OnLevelFinish.AddListener(OnShiftFinished);
        EventManager.OnLevelStart.AddListener(OnLevelStart);
    }

    private void OnDisable()
    {
        EventManager.OnLevelFinish.RemoveListener(OnShiftFinished);
        EventManager.OnLevelStart.RemoveListener(OnLevelStart);
    }

    // Once a session, as the first level starts: what the cafe made while the player was away (OfflineEarnings).
    private static bool welcomed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetWelcome() => welcomed = false;

    private void OnLevelStart()
    {
        if (welcomed) return;
        welcomed = true;
        if (!CafeProgress.Taught || OfflineEarnings.Pending.amount <= 0) { OfflineEarnings.Touch(); return; }
        StartCoroutine(Welcome());
    }

    // "WELCOME BACK / +$40 / Away 2 h 10 min / Earned while away +$40", with "Double" and "Collect".
    private IEnumerator Welcome()
    {
        yield return new WaitForSeconds(1.2f);
        while (LessonIntro.Playing || Time.timeScale == 0f) yield return null;
        var ui = UIBreakCard.Instance;
        int minutes = OfflineEarnings.Pending.minutes;
        int paid = OfflineEarnings.Claim();
        if (ui == null || paid <= 0) yield break;
        var card = new UIBreakCard.Card
        {
            Kicker = "WELCOME BACK", Title = $"+${paid}", Double = paid,
            Footer = "Your regulars kept dropping by for snacks",
        };
        card.Rows.Add(new UIBreakCard.Row { Icon = ui.checkIcon, Tint = UITokens.Colors.Teal, Label = "Away", Value = Away(minutes) });
        card.Rows.Add(new UIBreakCard.Row
        {
            Icon = ui.moneyIcon, Tint = UITokens.Colors.DarkTeal, Label = "Earned while away", Value = $"+${paid}", Earned = true,
        });
        ui.LastButton = "Collect";
        GameSfx.Play(GameSfx.Cue.Coin);
        ui.Play(new List<UIBreakCard.Card> { card }, null, tapOutsideCloses: true);
    }

    private static string Away(int minutes) =>
        minutes >= 180 ? "3 h or more" : minutes >= 60 ? $"{minutes / 60} h {minutes % 60} min" : $"{minutes} min";

    private void OnShiftFinished()
    {
        if (flowing) return;
        flowing = true;
        StartCoroutine(NextShift());
    }

    private IEnumerator NextShift()
    {
        bool shown = false;
        var ui = UIBreakCard.Instance;
        var days = DayGoals.Instance;
        if (ui != null && days != null) ui.Play(Cards(days.Finish(), ui), () => shown = true);
        else
        {
            Summary();
            shown = true;
        }
        yield return new WaitForSeconds(Breather);
        while (!shown) yield return null;
        // Not on top of the shop or the help card (both pause): wait until the player is back in the kitchen.
        while (Time.timeScale == 0f) yield return null;
        if (UIRewardedBonus.WatchedThisCard) Begin();
        else GamePlatform.Midgame(Begin);
    }

    // Without the cards (a scene without them): the summary rides the toast line.
    private static void Summary()
    {
        var score = ScoreManager.Instance;
        var toast = FindFirstObjectByType<UIToast>();
        if (toast != null) toast.ShiftSummary(score.GetLevelFinalScore(), score.WalkoutCount, score.ShiftEarned);
    }

    private void Begin()
    {
        flowing = false;
        ScoreManager.Instance.BeginNextShift();
        var spawner = FindFirstObjectByType<NpcSpawnController>();
        if (spawner != null) spawner.ResetForNextShift();
        EventManager.OnLevelStart.Invoke();
    }

    #region Cards
    private static List<UIBreakCard.Card> Cards(DayResult day, UIBreakCard ui)
    {
        // Today's treat first (it pays what an earlier day saved), then this shift's +$10 for tomorrow.
        var treat = DailyTreat.Due ? TreatCard(ui) : null;
        var (added, _) = DailyTreat.Save();
        var cards = new List<UIBreakCard.Card> { DayCard(day, ui, added) };
        foreach (int level in day.LevelsGained) cards.Add(NewCard(level, ui));
        if (Stations.OpensOn(CafeProgress.LevelNumber, out var station)) cards.Add(StationCard(station, ui));
        if (treat != null) cards.Add(treat);
        // The long goal: introduced after the second shift, celebrated once when the wallet holds enough.
        if (GrandGoal.TeaseDue) { cards.Add(GrillTeaser(ui)); GrandGoal.MarkTeased(); }
        else if (GrandGoal.Teased && GrandGoal.Ready && !GrandGoal.Announced) { cards.Add(GrillReady(ui)); GrandGoal.MarkAnnounced(); }
        // Last, the road: the levels played with their stars, the next one, and what waits further on.
        cards.Add(RoadCard());
        ui.LastButton = $"Play level {CafeProgress.LevelNumber}";
        return cards;
    }

    // "YOUR ROAD / Level 8 / Goal: Serve 2 perfect burgers" over the road itself (UIBreakCard draws it).
    private static UIBreakCard.Card RoadCard()
    {
        int next = CafeProgress.LevelNumber;
        return new UIBreakCard.Card
        {
            Kicker = "YOUR ROAD",
            Title = $"Level {next}",
            Footer = $"Goal: {DayGoals.ForDay(next).Text}",
            Road = true,
        };
    }

    // "LEVEL 4 COMPLETE / Great shift! / 2 of 3 stars / goal, rating, money / Level 6: Mages · Double"
    private static UIBreakCard.Card DayCard(DayResult day, UIBreakCard ui, int savedForTomorrow)
    {
        var card = new UIBreakCard.Card
        {
            Kicker = $"LEVEL {day.Day} COMPLETE",
            Title = UIShiftHighlights.TitleFor(day.Average, day.Walkouts),
            Stars = day.DayStars,
            Double = -1,
        };
        card.Rows.Add(new UIBreakCard.Row
        {
            Icon = day.Met ? ui.checkIcon : ui.crossIcon,
            Tint = day.Met ? UITokens.Colors.Teal : UITokens.Colors.StarEmpty,
            Label = day.Goal.Text,
            Value = day.Met ? $"+${day.Bonus}" : "<alpha=#8C>missed",
        });
        if (day.StarBonus > 0)
            card.Rows.Add(new UIBreakCard.Row
            {
                Icon = ui.starIcon, Tint = UITokens.Colors.DeepYellow, Label = "All three stars", Value = $"+${day.StarBonus}",
            });
        card.Rows.Add(new UIBreakCard.Row
        {
            Icon = ui.starIcon, Tint = UITokens.Colors.DeepYellow, Label = "Average rating",
            Value = day.Average.ToString("0.0", CultureInfo.InvariantCulture),
        });
        card.Rows.Add(new UIBreakCard.Row
        {
            Icon = ui.moneyIcon, Tint = UITokens.Colors.DarkTeal, Label = "Earned this level", Value = $"+${day.Earned + day.Bonus + day.StarBonus}",
            Earned = true,
        });
        // What coming back tomorrow is worth - and that this day just made it bigger.
        card.Rows.Add(new UIBreakCard.Row
        {
            Icon = ui.moneyIcon, Tint = UITokens.Colors.Berry, Label = "Tomorrow's treat",
            Value = savedForTomorrow > 0
                ? $"<size=75%><color=#{ColorUtility.ToHtmlStringRGB(UITokens.Colors.Berry)}>+{savedForTomorrow}</color></size>  ${DailyTreat.Tomorrow}"
                : $"${DailyTreat.Tomorrow}",
        });
        if (GrandGoal.Teased)
            card.Rows.Add(new UIBreakCard.Row
            {
                Icon = ui.grillDishes != null && ui.grillDishes.Length > 0 ? ui.grillDishes[0] : ui.moneyIcon, Tint = Color.white,
                Label = GrandGoal.Name, Value = GrandGoal.Ready ? "Ready!" : GrandGoal.Amount,
            });
        card.Footer = NextUnlockLine();
        return card;
    }

    // "Level 6: Mages · Double" - the next level that opens something new (after the one about to start, whose news
    // has its own card), or nothing once everything is open.
    public static string NextUnlockLine()
    {
        int from = CafeProgress.LevelNumber + 1;
        for (int level = from; level < from + 30; level++)
        {
            int tier = CafeProgress.TierOpensAt(level);
            if (tier > 0) return $"Level {level}: {NewsAt(tier)}";
            if (Stations.OpensOn(level, out var station)) return $"Level {level}: {Stations.Title(station)}";
        }
        return null;
    }

    // What a cafe tier brings, in a few words ("Barbarians · Garden & Deluxe", "new shop items").
    public static string NewsAt(int tier)
    {
        var parts = new List<string>();
        if (Customers.ArrivesAt(tier, out var kind)) parts.Add(Customers.Of(kind).Plural);
        var recipes = Orders.NewAt(tier).Select(r => r.Name).ToList();
        if (recipes.Count > 0) parts.Add(string.Join(" & ", recipes));
        return parts.Count > 0 ? string.Join("  ·  ", parts) : "new shop items";
    }

    // "NEW FROM LEVEL 4 / New in the cafe!" and a tile for each new recipe, customer and shop item of the tier.
    private static UIBreakCard.Card NewCard(int level, UIBreakCard ui)
    {
        var card = new UIBreakCard.Card { Kicker = $"NEW FROM LEVEL {CafeProgress.LevelNumber}", Title = "New in the cafe!" };
        foreach (var recipe in Orders.NewAt(level))
            card.Tiles.Add(new UIBreakCard.Tile { Recipe = recipe, Name = recipe.Name, Sub = "New recipe" });
        if (Customers.ArrivesAt(level, out var kind))
        {
            var profile = Customers.Of(kind);
            var icons = ui.customerIcons;
            card.Tiles.Add(new UIBreakCard.Tile
            {
                Icon = icons != null && (int)kind < icons.Length ? icons[(int)kind] : null, Tint = Color.white,
                Name = profile.Plural, Sub = profile.Quirk,
            });
        }
        var shop = CafeShop.Instance != null
            ? CafeShop.Instance.Upgrades.Where(u => u.level == level).OrderBy(u => u.price).ToList()
            : new List<Upgrade>();
        int room = Mathf.Max(0, MaxTiles - card.Tiles.Count);
        foreach (var upgrade in shop.Take(room))
            card.Tiles.Add(new UIBreakCard.Tile { Icon = upgrade.icon, Tint = Color.white, Name = upgrade.title, Sub = $"In the shop  ${upgrade.price}" });
        if (shop.Count > room) card.Footer = $"+{shop.Count - room} more in the shop";
        return card;
    }

    // "NEW STATION / Bacon Griddle!" with what it makes and the recipes it brings, before the level it opens at: the
    // station pops into the kitchen as that level starts (KitchenStations), and its first order comes in with a guided
    // first go (Tutorial.BeginStationTip).
    private static UIBreakCard.Card StationCard(Stations.Kind station, UIBreakCard ui)
    {
        var card = new UIBreakCard.Card
        {
            Kicker = $"NEW STATION  ·  LEVEL {CafeProgress.LevelNumber}",
            Title = Stations.Title(station) + "!",
            Footer = station == Stations.Kind.Bacon ? "The kitchen grows a new corner: grab bacon, crisp it, stack it."
                                                    : "Potato into the fryer, then toss the fries to whoever ordered them.",
        };
        card.Tiles.Add(new UIBreakCard.Tile
        {
            Icon = ui.StationPicture(station), Tint = Color.white, Name = Stations.Word(station), Sub = Stations.Blurb(station),
            Highlight = true,
        });
        foreach (var recipe in Orders.WithStation(station).Take(MaxTiles - 1))
            card.Tiles.Add(new UIBreakCard.Tile { Recipe = recipe, Name = recipe.Name, Sub = "New recipe" });
        if (station == Stations.Kind.Fries)
            card.Tiles.Add(new UIBreakCard.Tile
            {
                Icon = ui.moneyIcon, Tint = UITokens.Colors.DarkTeal, Name = $"+${NpcFsm.FriesPay}", Sub = "for each fries tossed",
            });
        return card;
    }

    // "NEXT BIG GOAL / Open a Grill House!" and the dishes it will serve.
    private static UIBreakCard.Card GrillTeaser(UIBreakCard ui)
    {
        var card = new UIBreakCard.Card
        {
            Kicker = "NEXT BIG GOAL",
            Title = "Open a Grill House!",
            Footer = $"Save ${GrandGoal.Price.ToString("N0", CultureInfo.InvariantCulture)} - the money in your wallet counts",
        };
        AddDishes(card, ui);
        return card;
    }

    // "GRILL HOUSE / You saved $2,000!"
    private static UIBreakCard.Card GrillReady(UIBreakCard ui)
    {
        var card = new UIBreakCard.Card
        {
            Kicker = "GRILL HOUSE",
            Title = $"You saved ${GrandGoal.Price.ToString("N0", CultureInfo.InvariantCulture)}!",
            Stars = 3,
            Footer = "Your Grill House opens soon - the money stays yours",
        };
        AddDishes(card, ui);
        return card;
    }

    private static void AddDishes(UIBreakCard.Card card, UIBreakCard ui)
    {
        string[] names = { "Roast dinner", "Hearty stew", "Big steak" };
        for (int i = 0; i < names.Length; i++)
            card.Tiles.Add(new UIBreakCard.Tile
            {
                Icon = ui.grillDishes != null && i < ui.grillDishes.Length ? ui.grillDishes[i] : null, Tint = Color.white,
                Name = names[i], Sub = "On the menu",
            });
    }

    // "DAILY TREAT  ·  2 DAYS IN A ROW / +$30", the week's treats in a row (today's in yellow), and what tomorrow's is.
    private static UIBreakCard.Card TreatCard(UIBreakCard ui)
    {
        var (streak, amount, saved) = DailyTreat.Claim();
        int week = DailyTreat.Rewards.Length;
        int today = (streak - 1) % week + 1;
        var card = new UIBreakCard.Card
        {
            Kicker = streak > 1 ? $"DAILY TREAT  ·  {streak} DAYS IN A ROW" : "DAILY TREAT",
            Title = $"+${amount}",
            Double = amount,
            Footer = saved > 0 ? $"Includes +${saved} you saved by playing yesterday"
                               : $"Every level you play today adds +${DailyTreat.PerDayPlayed} to tomorrow's",
            TileWidth = 76f,
        };
        for (int d = 1; d <= week; d++)
            card.Tiles.Add(new UIBreakCard.Tile
            {
                Icon = d < today ? ui.checkIcon : ui.starIcon,
                Tint = d < today ? UITokens.Colors.Teal : d == today ? UITokens.Colors.WarmWhite : UITokens.Colors.StarEmpty,
                Name = $"Day {d}", Sub = $"${DailyTreat.Rewards[d - 1]}", Highlight = d == today,
            });
        return card;
    }
    #endregion
}
