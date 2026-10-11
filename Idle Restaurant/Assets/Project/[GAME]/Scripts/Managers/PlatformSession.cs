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
        CafeProgress.LeveledUp += OnLevelUp;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(OnShiftStart);
        EventManager.OnLevelFinish.RemoveListener(OnShiftEnd);
        EventManager.OnLevelFinish.RemoveListener(OnShiftFinished);
        EventManager.OnRestart.RemoveListener(OnShiftEnd);
        CafeProgress.LeveledUp -= OnLevelUp;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnShiftStart()
    {
        if (shiftRunning) return;   // OnLevelStart can be raised twice for one shift
        shiftRunning = true;
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
        GoogleAnalytics.Event("game_ready", "load_seconds", Time.realtimeSinceStartup,
            "returning_player", CafeProgress.ShiftsPlayed > 0, "cafe_level", CafeProgress.Level);
    }

    private void Update()
    {
        if (shiftRunning && Time.timeScale > 0f && !GamePlatform.AdShowing) GamePlatform.GameplayStart();
        else GamePlatform.GameplayStop();
    }
}
