using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private static bool IsGameStarted;
    [HideInInspector] public static bool IsGameRestarted;

    // Shifts follow each other without a result card or a pause (ShiftFlow), each one a day with a goal (DayGoals).
    private void Awake()
    {
        if (GetComponent<ShiftFlow>() == null) gameObject.AddComponent<ShiftFlow>();
        if (GetComponent<DayGoals>() == null) gameObject.AddComponent<DayGoals>();
    }

    void Start()
    {
        StartGame();
        StartLevel();
    }

    public void StartGame()
    {
        if (IsGameStarted || applicationIsQuitting == true)
            return;

        IsGameStarted = true;
        EventManager.OnGameStart.Invoke();
    }

    //public void EndGame()
    //{
    //    if (!IsGameStarted || applicationIsQuitting == true)
    //        return;

    //    IsGameStarted = false;
    //    EventManager.OnGameEnd.Invoke();
    //}

    // The game opens straight into the kitchen: no title screen with a Start button. On CrazyGames a player
    // only counts as having played once gameplay starts (PlatformSession -> GameplayStart), and a title screen
    // lost about half the players before they ever cooked (gameplay conversion 37%). One frame later, so every
    // listener has run its Start first.
    private void StartLevel()
    {
        if (IsGameStarted) StartCoroutine(StartNextFrame());
    }

    private System.Collections.IEnumerator StartNextFrame()
    {
        yield return null;
        EventManager.OnLevelStart.Invoke();
    }

    private void OnEnable()
    {
        EventManager.OnRestart.AddListener(ContinueGame);
        EventManager.OnHelpRequest.AddListener(PauseGame);
        EventManager.OnLevelContine.AddListener(ContinueGame);
    }
    private void OnDisable()
    {
        EventManager.OnRestart.RemoveListener(ContinueGame);
        EventManager.OnHelpRequest.RemoveListener(PauseGame);
        EventManager.OnLevelContine.RemoveListener(ContinueGame);
    }

    void PauseGame()
    {
        Time.timeScale = 0;
    }

    void ContinueGame()
    {
        Time.timeScale = 1;
    }
}
