using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Every shift has one goal of its own, so that shift after shift is not the same
// eight burgers again (the player's own words: "after about the fifth customer I got bored, it's always the same").
// The goal sits under the customers card (UIDayGoal); when the day ends it is part of the day's card (UIBreakCard):
// up to three stars for the day - one for finishing it, one for the goal, one for doing it well - and coins for a
// met goal. A missed goal costs nothing (cozy: nothing is ever lost), it only leaves a star empty.
// The first days are fixed so a new player meets every kind of goal early; after that a day's goal is drawn from
// the shift's number (the same shift always has the same goal), never the same kind twice in a row.
// The shifts used to be called days ("DAY 2 GOAL" on the second shift of the same afternoon - user: it must not look
// like the next day), then "DAY 1 · SHIFT 2"; since 2026-10-28 every shift is a LEVEL ("LEVEL 7"), short, with its
// three stars on show from the start (UIDayGoal) - one for finishing, one for the goal, one for the goal and an average
// of 4 or more - and all three pay a little extra. `Day` below is the level's number: it picks the goal and sizes its
// bonus. The real day (CafeDay) is still kept, for the daily treat.
// Added to the GameManager's object at runtime (GameManager.Awake), like ShiftFlow.
// (Fries, 2026-10-28: the fryer's level - level 5 - has "Toss 2 fries" as its goal, and it joins the others after that.)
public enum GoalKind { Perfect, Earn, Special, NoWalkouts, Rush, Average, Fries }

public sealed class DayGoal
{
    public GoalKind Kind;
    public int Target;
    public Recipe Special;     // GoalKind.Special: the recipe of the day (ordered more often that day)

    public string Text => Kind switch
    {
        GoalKind.Perfect => Target == 1 ? "Serve a perfect burger" : $"Serve {Target} perfect burgers",
        GoalKind.Earn => $"Earn ${Target}",
        GoalKind.Special => $"Serve {Target} {Plural(Special.Name)}",
        GoalKind.NoWalkouts => "Nobody leaves hungry",
        GoalKind.Rush => $"Reach a rush of x{Target}",
        GoalKind.Fries => $"Toss {Target} fries",
        _ => "Average 4 stars or more",
    };

    private static string Plural(string name) => name switch
    {
        "Garden" => "Garden burgers",
        "Cheese Lover" => "Cheese Lovers",
        "Veggie Deluxe" => "Veggie Deluxes",
        "Onion Double" => "Onion Doubles",
        "The Tower" => "Towers",
        _ => name + "s",
    };
}

public struct DayResult
{
    public int Day;            // the shift's number over the whole game (the goal)
    public int CafeDay, Shift; // the real day (CafeDay) and the shift of that day
    public DayGoal Goal;
    public bool Met;
    public int DayStars;       // 1-3
    public int Bonus;          // coins for a met goal
    public int StarBonus;      // coins for all three stars
    public float Average;
    public int Earned;         // this shift's orders and tips, without the bonus
    public int Walkouts;
    public List<int> LevelsGained;
}

public class DayGoals : MonoBehaviour
{
    public static DayGoals Instance { get; private set; }

    // The first days, in order: every kind of goal is met within the first seven levels. Level 3's special is the new
    // griddle's Bacon Burger (SpecialFor) and level 5 - the fryer's - is about fries: each new station is the goal of the
    // level it opens at (Stations).
    private static readonly GoalKind[] FirstDays =
        { GoalKind.Perfect, GoalKind.Earn, GoalKind.Special, GoalKind.NoWalkouts, GoalKind.Fries, GoalKind.Rush, GoalKind.Average };

    public const float GoodAverage = 4f;

    public DayGoal Goal { get; private set; }
    public int Day { get; private set; }
    public int CafeDayNumber { get; private set; }
    public int ShiftOfDay { get; private set; }
    public bool Running { get; private set; }

    // "LEVEL 7"
    public static string Label(int level) => $"LEVEL {level}";

    // The level being played, or (between levels) the next one.
    public static int LevelNow => Instance != null && Instance.Running ? Instance.Day : Today;

    private int specialServed;
    private bool announced;

    // A counting goal was reached during the day (the goal, its bonus): a toast and the goal card say so - it used to
    // sit in its corner without a word (user). Static: subscribe with named methods only.
    public static event System.Action<DayGoal, int> GoalMet;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => GoalMet = null;

    // Paid at the day's end when the goal is met.
    public int Bonus => Mathf.Min(40, 10 + 2 * Day);
    // And a little more for all three stars.
    public int StarBonus => Mathf.Max(5, Bonus / 2);

    private void Update()
    {
        if (!Running || announced || !Done) return;
        announced = true;
        GameSfx.Play(GameSfx.Cue.LevelUp);
        GoalMet?.Invoke(Goal, Bonus);
        GoogleAnalytics.Event("day_goal_met", "day", Day, "goal", Goal.Kind.ToString());
    }
    private readonly List<int> levelsGained = new();

    // The shift being played (or about to start) over the whole game: the one after the last one counted.
    public static int Today => CafeProgress.ShiftsPlayed + 1;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(OnDayStart);
        EventManager.OnOrderRated.AddListener(OnOrderRated);
        CafeProgress.LeveledUp += OnLeveledUp;
    }

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(OnDayStart);
        EventManager.OnOrderRated.RemoveListener(OnOrderRated);
        CafeProgress.LeveledUp -= OnLeveledUp;
    }

    public static DayGoal ForDay(int day)
    {
        day = Mathf.Max(1, day);
        var kind = KindFor(day);
        var goal = new DayGoal { Kind = kind };
        switch (kind)
        {
            // The very first shift has three customers and the lesson's burger is made right, step by step: one
            // perfect burger, so the first goal is met as the lesson ends ("Goal done!") instead of a red "missed"
            // on the first card a new player sees.
            case GoalKind.Perfect: goal.Target = day == 1 ? 1 : Mathf.Min(4, 2 + (day - 1) / 4); break;
            case GoalKind.Earn:
                // About what a good shift of this size makes, a little more every day. (From the level's own size, so
                // the road card can name the next level's goal before it starts.)
                int customers = ScoreManager.CustomersForShift(day - 1) + CafeShop.ExtraCustomers;
                goal.Target = Mathf.Min(35 + 5 * day, 90, customers * 7);
                break;
            case GoalKind.Special:
                goal.Target = day > 10 ? 3 : 2;
                goal.Special = SpecialFor(day);
                break;
            case GoalKind.Rush: goal.Target = day > 10 ? 4 : 3; break;
            case GoalKind.Fries: goal.Target = day > 10 ? 3 : 2; break;
        }
        return goal;
    }

    private static GoalKind KindFor(int day)
    {
        if (day <= FirstDays.Length) return FirstDays[day - 1];
        var kind = FirstDays[FirstDays.Length - 1];
        for (int d = FirstDays.Length + 1; d <= day; d++)
        {
            int roll = new System.Random(d * 7919).Next(FirstDays.Length - 1);
            kind = (GoalKind)(((int)kind + 1 + roll) % FirstDays.Length);
        }
        return kind;
    }

    // One of the newest recipes the cafe has (the one the player most needs to meet), picked by the day.
    private static Recipe SpecialFor(int day)
    {
        if (Stations.OpensOn(day, out var station) && station == Stations.Kind.Bacon) return Orders.BaconBurger;
        var open = Orders.Unlocked.Where(r => r != Orders.Classic).ToList();
        if (open.Count == 0) return Orders.Classic;
        int newest = open.Max(r => r.Level);
        var newestOnes = open.Where(r => r.Level == newest).ToList();
        return newestOnes[day % newestOnes.Count];
    }

    // OnLevelStart can be raised twice for one shift: the day starts once.
    private void OnDayStart()
    {
        if (Running) return;
        Running = true;
        Day = Today;
        (CafeDayNumber, ShiftOfDay) = CafeDay.BeginShift();
        Goal = ForDay(Day);
        Orders.Special = Goal.Special;
        specialServed = 0;
        announced = false;
        levelsGained.Clear();
    }

    private void OnOrderRated()
    {
        if (!Running || Goal.Kind != GoalKind.Special) return;
        var order = ScoreManager.Instance.LastOrder;
        if (order.Order == Goal.Special && order.Review.AsOrdered) specialServed++;
    }

    private void OnLeveledUp(int level)
    {
        if (!levelsGained.Contains(level)) levelsGained.Add(level);
    }

    // Average of the orders so far (a walkout counts as 0); ScoreManager turns its total into the average itself
    // once the shift has finished.
    private static float AverageSoFar(bool finished)
    {
        var score = ScoreManager.Instance;
        return finished ? score.GetLevelFinalScore() : score.totalLevelScore / Mathf.Max(1, score.hostedCustomer);
    }

    // How far the goal is (for the HUD): count of target. NoWalkouts and Average are only settled at the end.
    public (int done, int of) Progress
    {
        get
        {
            var score = ScoreManager.Instance;
            if (Goal == null || score == null) return (0, 1);
            return Goal.Kind switch
            {
                GoalKind.Perfect => (score.PerfectCount, Goal.Target),
                GoalKind.Earn => (score.ShiftEarned, Goal.Target),
                GoalKind.Special => (specialServed, Goal.Target),
                GoalKind.Rush => (score.BestStreak, Goal.Target),
                GoalKind.Fries => (score.FriesServed, Goal.Target),
                GoalKind.NoWalkouts => (score.hostedCustomer - score.WalkoutCount, score.CustomersPerLevel),
                _ => (0, 1),
            };
        }
    }

    public float Average => ScoreManager.Instance != null ? AverageSoFar(false) : 0f;

    // A fries goal still needs more fries than anyone in the cafe is waiting for: the next customer wants some too
    // (Orders.WantsFries), so the goal can always be met.
    public static bool FriesShort
    {
        get
        {
            var days = Instance;
            if (days == null || !days.Running || days.Goal == null || days.Goal.Kind != GoalKind.Fries || ScoreManager.Instance == null)
                return false;
            int coming = NpcFsm.Active.Count(n => n.FriesDue);
            return ScoreManager.Instance.FriesServed + coming < days.Goal.Target;
        }
    }

    // The level's stars as they stand (the HUD shows them filling): the first is there from the start (finishing the
    // level always earns it), the second once the goal is met, the third while the goal is met and the average holds.
    public int StarsNow
    {
        get
        {
            if (Goal == null || ScoreManager.Instance == null) return 1;
            bool met = Goal.Kind == GoalKind.Average
                ? ScoreManager.Instance.hostedCustomer > 0 && Average >= GoodAverage && !Lost
                : Done;
            if (Goal.Kind == GoalKind.NoWalkouts) met = !Lost && ScoreManager.Instance.hostedCustomer > 0;
            bool well = Goal.Kind == GoalKind.Average ? ScoreManager.Instance.WalkoutCount == 0
                                                      : ScoreManager.Instance.hostedCustomer > 0 && Average >= GoodAverage;
            return 1 + (met ? 1 : 0) + (met && well ? 1 : 0);
        }
    }

    // Reached already (the counting goals), or cannot be reached any more (someone left hungry).
    public bool Done => Goal != null && Goal.Kind is GoalKind.Perfect or GoalKind.Earn or GoalKind.Special or GoalKind.Rush or GoalKind.Fries
                        && Progress.done >= Progress.of;
    public bool Lost => Goal != null && Goal.Kind == GoalKind.NoWalkouts && ScoreManager.Instance.WalkoutCount > 0;

    // The day is over (ShiftFlow, on OnLevelFinish): settles the goal, pays the bonus, reports it.
    public DayResult Finish()
    {
        var score = ScoreManager.Instance;
        if (!Running) OnDayStart();
        Running = false;
        Orders.Special = null;

        float average = AverageSoFar(true);
        bool met = Goal.Kind switch
        {
            GoalKind.NoWalkouts => score.WalkoutCount == 0,
            GoalKind.Average => average >= GoodAverage,
            _ => Progress.done >= Progress.of,
        };
        // The third star: the goal and a good day besides (for the stars goal itself, nobody left hungry too).
        bool well = Goal.Kind == GoalKind.Average ? score.WalkoutCount == 0 : average >= GoodAverage;
        int dayStars = 1 + (met ? 1 : 0) + (met && well ? 1 : 0);
        int bonus = met ? Bonus : 0;
        int starBonus = dayStars == 3 ? StarBonus : 0;
        if (bonus + starBonus > 0) CafeProgress.AddCoins(bonus + starBonus);
        CafeProgress.RecordLevelStars(Day, dayStars);
        WeeklyTasks.Add(WeeklyTasks.Kind.Levels);
        if (dayStars == 3) WeeklyTasks.Add(WeeklyTasks.Kind.ThreeStars);

        GoogleAnalytics.Event("day_end", "day", Day, "cafe_day", CafeDayNumber, "shift_of_day", ShiftOfDay,
            "goal", Goal.Kind.ToString(), "goal_met", met, "day_stars", dayStars);
        if (dayStars == 3) GamePlatform.HappyTime();

        return new DayResult
        {
            Day = Day, CafeDay = CafeDayNumber, Shift = ShiftOfDay, Goal = Goal, Met = met, DayStars = dayStars, Bonus = bonus,
            StarBonus = starBonus, Average = average,
            Earned = score.ShiftEarned, Walkouts = score.WalkoutCount, LevelsGained = new List<int>(levelsGained),
        };
    }
}
