using System;

// The cafe's day is a real day, not a shift (user: "playing the second shift of today it says DAY 2 GOAL, as if I
// came back the next day - 12 or 24 hours should pass; shifts on the same day must not look like different days").
// A day holds as many shifts as the player likes ("Day 3 · Shift 2"). A new day begins with the first shift after
// the calendar date has changed AND at least 12 hours have passed since the day began - so a late-night session
// that runs past midnight is still the same day, and two sessions in one day are never two days.
// The daily treat (DailyTreat) goes by these days too, so "tomorrow's treat" means the cafe's next day.
// Stored with the cafe's progress (GamePlatform).
public static class CafeDay
{
    public const string NumberKey = "Cafe.Day", StartKey = "Cafe.DayStart", ShiftsKey = "Cafe.DayShifts",
                        GapKey = "Cafe.DayGap";
    public const double MinHours = 12;

    // The day being played (1 = the first).
    public static int Number => Math.Max(1, GamePlatform.GetInt(NumberKey, 1));

    // Shifts finished on this day.
    public static int ShiftsDone => GamePlatform.GetInt(ShiftsKey, 0);

    // Calendar days between the start of the previous day and this one (1 = it was yesterday): the treat's row.
    public static int Gap => GamePlatform.GetInt(GapKey, 1);

    private static bool Started(out DateTime start)
    {
        start = default;
        if (!long.TryParse(GamePlatform.GetString(StartKey, ""), out long seconds)) return false;
        start = DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
        return true;
    }

    // Would the next shift begin a new day?
    public static bool NewDayDue
    {
        get
        {
            if (!Started(out var start)) return false;   // the very first day has not begun yet: it is day 1
            var now = DateTime.Now;
            return now.Date > start.Date && (now - start).TotalHours >= MinHours;
        }
    }

    // The day and shift the next shift will be (for the start-of-shift toast, before it begins).
    public static (int day, int shift) Upcoming => NewDayDue ? (Number + 1, 1) : (Number, ShiftsDone + 1);

    // A shift begins: starts the first day, or a new one when it is due. Returns the day and the shift of the day.
    public static (int day, int shift) BeginShift()
    {
        var now = DateTime.Now;
        if (!Started(out var start))
        {
            Begin(Number, now, 1);
        }
        else if (NewDayDue)
        {
            Begin(Number + 1, now, Math.Max(1, (int)(now.Date - start.Date).TotalDays));
            GoogleAnalytics.Event("cafe_day", "day", Number);
        }
        return (Number, ShiftsDone + 1);
    }

    private static void Begin(int day, DateTime now, int gap)
    {
        GamePlatform.SetInt(NumberKey, day);
        GamePlatform.SetString(StartKey, new DateTimeOffset(now).ToUnixTimeSeconds().ToString());
        GamePlatform.SetInt(ShiftsKey, 0);
        GamePlatform.SetInt(GapKey, gap);
        GamePlatform.Flush();
    }

    // A shift was finished (CafeProgress.CountShift).
    public static void ShiftFinished() => GamePlatform.SetInt(ShiftsKey, ShiftsDone + 1);
}
