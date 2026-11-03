using System;
using System.Globalization;
using UnityEngine;

// Three tasks a week (from the report, 2026-10-28: a reason to come back on more than one day - Good Pizza, Great
// Pizza and Eatventure run weekly events). Picked from the week's number, so everyone gets the same three and they
// change every Monday. Each pays its reward the moment it is done (a toast says so, UIToast); the HUD's book button
// shows them (UIBookButton). Progress is counted from the game's own moments: orders rated, levels finished, snacks
// tossed (the tip's sound cue). Saved per week; a new week starts them again.
public static class WeeklyTasks
{
    public enum Kind { Serve, Perfect, ThreeStars, Chaos, Snacks, Rush, Earn, Levels }

    public sealed class Task
    {
        public Kind Kind;
        public int Target, Reward, Done;
        public bool Complete => Done >= Target;

        public string Text => Kind switch
        {
            Kind.Serve => $"Serve {Target} burgers",
            Kind.Perfect => $"Serve {Target} perfect burgers",
            Kind.ThreeStars => $"Finish {Target} levels with 3 stars",
            Kind.Chaos => $"Serve {Target} Chaos Burgers",
            Kind.Snacks => $"Toss {Target} snacks",
            Kind.Rush => $"Reach a rush of x3 {Target} times",
            Kind.Earn => $"Earn ${Target}",
            _ => $"Play {Target} levels",
        };
    }

    private const string WeekKey = "Cafe.Week", ProgressKey = "Cafe.WeekProgress";

    // A task was just done (the task). Static: subscribe with named methods only.
    public static event Action<Task> Completed;

    private static Task[] tasks;
    private static int week;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        tasks = null;
        Completed = null;
    }

    public static int ThisWeek
    {
        get
        {
            var now = DateTime.Now;
            return ISOWeek.GetYear(now) * 100 + ISOWeek.GetWeekOfYear(now);
        }
    }

    // Days until Monday's new tasks.
    public static int DaysLeft
    {
        get
        {
            int d = ((int)DayOfWeek.Monday - (int)DateTime.Now.DayOfWeek + 7) % 7;
            return d == 0 ? 7 : d;
        }
    }

    public static Task[] Tasks
    {
        get
        {
            if (tasks != null && week == ThisWeek) return tasks;
            week = ThisWeek;
            tasks = Pick(week);
            if (GamePlatform.GetInt(WeekKey, 0) == week)
            {
                var saved = GamePlatform.GetString(ProgressKey, "").Split(',');
                for (int i = 0; i < tasks.Length && i < saved.Length; i++)
                    if (int.TryParse(saved[i], out int n)) tasks[i].Done = n;
            }
            else Save();
            return tasks;
        }
    }

    // Three different kinds, the same for everyone in a given week; sizes for a few sessions of play.
    private static Task[] Pick(int weekId)
    {
        var rng = new System.Random(weekId * 7919);
        var kinds = (Kind[])Enum.GetValues(typeof(Kind));
        for (int i = kinds.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (kinds[i], kinds[j]) = (kinds[j], kinds[i]);
        }
        var picked = new Task[3];
        for (int i = 0; i < 3; i++)
        {
            var kind = kinds[i];
            var (target, reward) = kind switch
            {
                Kind.Serve => (30, 40),
                Kind.Perfect => (10, 50),
                Kind.ThreeStars => (3, 60),
                Kind.Chaos => (3, 40),
                Kind.Snacks => (5, 40),
                Kind.Rush => (3, 50),
                Kind.Earn => (300, 60),
                _ => (8, 50),
            };
            picked[i] = new Task { Kind = kind, Target = target, Reward = reward };
        }
        return picked;
    }

    public static void Add(Kind kind, int amount = 1)
    {
        if (amount <= 0) return;
        foreach (var task in Tasks)
        {
            if (task.Kind != kind || task.Complete) continue;
            task.Done = Mathf.Min(task.Target, task.Done + amount);
            if (!task.Complete) continue;
            CafeProgress.AddCoins(task.Reward);
            GoogleAnalytics.Event("weekly_task_done", "kind", task.Kind.ToString(), "week", week);
            Completed?.Invoke(task);
        }
        Save();
    }

    private static void Save()
    {
        GamePlatform.SetInt(WeekKey, week);
        GamePlatform.SetString(ProgressKey, string.Join(",", Array.ConvertAll(tasks, t => t.Done.ToString(CultureInfo.InvariantCulture))));
        GamePlatform.Flush();
    }
}
