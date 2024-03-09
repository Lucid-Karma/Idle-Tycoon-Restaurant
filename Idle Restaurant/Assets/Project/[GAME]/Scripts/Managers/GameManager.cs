using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private static bool IsGameStarted;
    [HideInInspector] public static bool IsGameRestarted;

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

    public void EndGame()
    {
        if (!IsGameStarted || applicationIsQuitting == true)
            return;

        IsGameStarted = false;
        EventManager.OnGameEnd.Invoke();
    }

    private void StartLevel()
    {
        if(IsGameStarted && IsGameRestarted)
        {
            EventManager.OnLevelStart.Invoke();
        }
    }

    private void OnEnable()
    {
        EventManager.OnRestart.AddListener(ContinueGame);
        EventManager.OnLevelFinish.AddListener(PauseGame);
    }
    private void OnDisable()
    {
        EventManager.OnRestart.RemoveListener(ContinueGame);
        EventManager.OnLevelFinish.RemoveListener(PauseGame);
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
