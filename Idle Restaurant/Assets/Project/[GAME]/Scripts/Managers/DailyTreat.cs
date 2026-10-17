using System;

// A present for coming back: the first shift finished on each of the cafe's days (CafeDay: a real day, not a
// shift) pays a treat, bigger every day in a row (a week of them, then the week starts again), and the card says
// what tomorrow's will be. Missing a day only starts the row again; nothing is taken away. Shown on the break after
// the shift (UIBreakCard), not when the game opens: the game opens straight into the kitchen and a pop-up there
// would be in the way.
// Today's play also works for tomorrow (user: "the days should really work for the next day, so there is a reason
// to come back - but keep the player in the game as long as possible today"): every shift finished today puts
// +$10 into tomorrow's treat (Save, up to $100 a day), paid out with the treat the next time the player comes back.
// Playing on today makes tomorrow's present bigger; the shift's card shows it growing.
// Stored with the cafe's progress (GamePlatform: the player's CrazyGames account there).
public static class DailyTreat
{
    public const string DateKey = "Cafe.DailyDate", StreakKey = "Cafe.DailyStreak",
                        SavedKey = "Cafe.TreatSaved", SavedDateKey = "Cafe.TreatSavedDate",
                        TreatDayKey = "Cafe.TreatDay", SavedDayKey = "Cafe.TreatSavedDay";

    public static readonly int[] Rewards = { 20, 30, 40, 50, 70, 90, 150 };
    public const int PerDayPlayed = 10, MaxSaved = 100;

    // The cafe day whose treat was paid last (0: none yet). Saves from before the cafe had real days kept the
    // calendar date instead: a treat paid today counts as this day's.
    private static int PaidDay
    {
        get
        {
            int day = GamePlatform.GetInt(TreatDayKey, 0);
            if (day == 0 && GamePlatform.GetString(DateKey, "") == DateTime.Now.ToString("yyyyMMdd")) return CafeDay.Number;
            return day;
        }
    }

    // Read at the end of a shift (the day has begun).
    public static bool Due => PaidDay != CafeDay.Number;

    public static int Streak => GamePlatform.GetInt(StreakKey, 0);

    // The treat for a day of the row (1 = the first day).
    public static int RewardFor(int streakDay) => Rewards[(Math.Max(1, streakDay) - 1) % Rewards.Length];

    // Put aside on this cafe day for the next one.
    public static int SavedToday => GamePlatform.GetInt(SavedDayKey, 0) == CafeDay.Number ? GamePlatform.GetInt(SavedKey, 0) : 0;

    // What tomorrow's treat will be if the player comes back tomorrow: the next day of the row plus today's savings.
    public static int Tomorrow => RewardFor(Streak + 1) + SavedToday;

    // Pays today's treat: (day of the row, coins in all, of which saved on an earlier day). Call only when Due.
    public static (int streakDay, int amount, int saved) Claim()
    {
        int today = CafeDay.Number;
        // The row goes on when the last treat was the previous cafe day and that day began the calendar day before.
        bool kept = PaidDay == today - 1 && CafeDay.Gap <= 1;
        int streak = kept ? Streak + 1 : 1;
        // Savings from an earlier day come with it (and are spent); today's own savings wait for tomorrow.
        int savedOn = GamePlatform.GetInt(SavedDayKey, 0);
        bool earlier = savedOn != 0 && savedOn != today;
        int saved = earlier ? GamePlatform.GetInt(SavedKey, 0) : 0;
        if (earlier) GamePlatform.SetInt(SavedKey, 0);
        int amount = RewardFor(streak) + saved;
        GamePlatform.SetInt(TreatDayKey, today);
        GamePlatform.SetInt(StreakKey, streak);
        CafeProgress.AddCoins(amount);   // saves (Flush)
        GoogleAnalytics.Event("daily_treat", "streak", streak, "value", amount, "saved", saved);
        return (streak, amount, saved);
    }

    // A shift was finished today: +$10 for tomorrow's treat. Returns (how much was added, saved so far today).
    public static (int added, int saved) Save()
    {
        int saved = SavedToday;
        int added = Math.Min(PerDayPlayed, MaxSaved - saved);
        saved += added;
        GamePlatform.SetInt(SavedDayKey, CafeDay.Number);
        GamePlatform.SetInt(SavedKey, saved);
        GamePlatform.Flush();
        return (added, saved);
    }
}
