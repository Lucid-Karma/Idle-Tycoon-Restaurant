using UnityEngine;
using UnityEngine.SceneManagement;

// Lives for the whole session (made by Boot, survives "Play again"): tells the portal when the player is actually
// playing. That is a shift that has started and not finished, with nothing paused over it - the shop, the help
// card, the result card and ads all stop time, so the clock is the simplest honest signal. A new cafe level is
// the game's "happy time". The same moments go to Google Analytics (GoogleAnalytics, CrazyGames only).
public class PlatformSession : MonoBehaviour
{
    private static PlatformSession instance;
    private bool shiftRunning;
    private bool reportedReady;
    private float readyAt, shiftStartedAt, lastPauseReport = -100f;

    // Measuring what the report asked for (2026-10-28): how long a new player takes to serve their first order, at
    // which moment players put the game down, and how many days after their first visit they come back.
    private const string FirstDayKey = "Cafe.FirstDay", FirstServeKey = "Cafe.FirstServe";

    public static void Ensure()
    {
        if (instance != null) return;
        var go = new GameObject("PlatformSession");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PlatformSession>();
    }

    // Named methods: the events are static and outlive scene reloads.
    private void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(OnShiftStart);
        EventManager.OnLevelFinish.AddListener(OnShiftEnd);
        EventManager.OnLevelFinish.AddListener(OnShiftFinished);
        EventManager.OnRestart.AddListener(OnShiftEnd);
        EventManager.OnOrderRated.AddListener(OnOrderRated);
        CafeProgress.LeveledUp += OnLevelUp;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(OnShiftStart);
        EventManager.OnLevelFinish.RemoveListener(OnShiftEnd);
        EventManager.OnLevelFinish.RemoveListener(OnShiftFinished);
        EventManager.OnRestart.RemoveListener(OnShiftEnd);
        EventManager.OnOrderRated.RemoveListener(OnOrderRated);
        CafeProgress.LeveledUp -= OnLevelUp;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnShiftStart()
    {
        if (shiftRunning) return;   // OnLevelStart can be raised twice for one shift
        shiftRunning = true;
        shiftStartedAt = Time.realtimeSinceStartup;
        GoogleAnalytics.Event("level_start", "shift_number", CafeProgress.ShiftsPlayed + 1, "cafe_level", CafeProgress.Level);
    }

    private void OnShiftEnd() => shiftRunning = false;

    // A finished shift (not a restart): what the player managed, to see where shifts get hard.
    private void OnShiftFinished()
    {
        var score = ScoreManager.Instance;
        if (score == null) return;
        GoogleAnalytics.Event("level_end", "shift_number", CafeProgress.ShiftsPlayed, "cafe_level", CafeProgress.Level,
            "stars", score.totalLevelScore, "earned", score.ShiftEarned, "perfect", score.PerfectCount,
            "chaos", score.ChaosCount, "walkouts", score.WalkoutCount, "best_streak", score.BestStreak, "success", true);
    }

    private void OnLevelUp(int level)
    {
        GamePlatform.HappyTime();
        GoogleAnalytics.Event("level_up", "level", level);
    }

    // The first time the cafe is on screen: how long the load took and who is playing.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (reportedReady || ScoreManager.Instance == null) return;
        reportedReady = true;
        readyAt = Time.realtimeSinceStartup;
        GoogleAnalytics.Event("game_ready", "load_seconds", Time.realtimeSinceStartup,
            "returning_player", CafeProgress.ShiftsPlayed > 0, "cafe_level", CafeProgress.Level,
            "days_since_first", DaysSinceFirst(), "levels_played", CafeProgress.ShiftsPlayed);
    }

    // Days since the player's first visit (0 on the first day), by the calendar.
    private static int DaysSinceFirst()
    {
        int today = (int)(System.DateTime.Now.Date - new System.DateTime(2000, 1, 1)).TotalDays;
        int first = GamePlatform.GetInt(FirstDayKey, 0);
        if (first == 0)
        {
            GamePlatform.SetInt(FirstDayKey, today);
            GamePlatform.Flush();
            return 0;
        }
        return Mathf.Max(0, today - first);
    }

    // The first order a player ever serves: how long after the cafe opened, and whether the lesson was running.
    private void OnOrderRated()
    {
        if (GamePlatform.GetInt(FirstServeKey, 0) == 1) return;
        GamePlatform.SetInt(FirstServeKey, 1);
        GamePlatform.Flush();
        GoogleAnalytics.Event("first_serve", "seconds", Time.realtimeSinceStartup - readyAt, "tutorial", Tutorial.Running);
    }

    // The page lost the player (another tab, the window, a phone's home button): where they were in the game.
    private void OnApplicationFocus(bool focus)
    {
        if (!focus) OfflineEarnings.Touch();
        if (focus || !reportedReady || Time.realtimeSinceStartup - lastPauseReport < 20f) return;
        lastPauseReport = Time.realtimeSinceStartup;
        var score = ScoreManager.Instance;
        GoogleAnalytics.Event("session_pause", "level", DayGoals.LevelNow, "in_level", shiftRunning,
            "served", score != null ? score.ServedCount : 0, "customers", score != null ? score.CustomersPerLevel : 0,
            "seconds_in_level", shiftRunning ? Time.realtimeSinceStartup - shiftStartedAt : 0f,
            "session_seconds", Time.realtimeSinceStartup - readyAt, "lesson", Tutorial.Running,
            "break_cards", UIBreakCard.Instance != null && UIBreakCard.Instance.Showing);
    }

    private float touchedAt;
    private int minutesReported;

    private void Update()
    {
        // The clock for money made while away (OfflineEarnings): "the player was here" every half minute.
        if (reportedReady && Time.realtimeSinceStartup - touchedAt > 30f)
        {
            touchedAt = Time.realtimeSinceStartup;
            OfflineEarnings.Touch();
        }
        if (reportedReady && minutesReported < 10 && Time.realtimeSinceStartup - readyAt >= (minutesReported + 1) * 60f)
        {
            minutesReported++;
            var score = ScoreManager.Instance;
            GoogleAnalytics.Event("play_minute", "minute", minutesReported, "level", DayGoals.LevelNow, "lesson", Tutorial.Running,
                "served", score != null ? score.ServedCount : 0, "break_cards", UIBreakCard.Instance != null && UIBreakCard.Instance.Showing);
        }
        if (shiftRunning && Time.timeScale > 0f && !GamePlatform.AdShowing) GamePlatform.GameplayStart();
        else GamePlatform.GameplayStop();
    }
}
