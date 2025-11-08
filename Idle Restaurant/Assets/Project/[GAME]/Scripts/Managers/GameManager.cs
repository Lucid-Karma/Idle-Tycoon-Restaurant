using UnityEngine;
//using CrazyGames;

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

    //public void EndGame()
    //{
    //    if (!IsGameStarted || applicationIsQuitting == true)
    //        return;

    //    IsGameStarted = false;
    //    EventManager.OnGameEnd.Invoke();
    //}

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
        EventManager.OnHelpRequest.AddListener(PauseGame);
        EventManager.OnLevelContine.AddListener(ContinueGame);
    }
    private void OnDisable()
    {
        EventManager.OnRestart.RemoveListener(ContinueGame);
        EventManager.OnLevelFinish.RemoveListener(PauseGame);
        EventManager.OnHelpRequest.RemoveListener(PauseGame);
        EventManager.OnLevelContine.RemoveListener(ContinueGame);
    }

    void PauseGame()
    {
        Time.timeScale = 0;
        //CrazySDK.Game.GameplayStop();
    }

    void ContinueGame()
    {
        Time.timeScale = 1;
        //CrazySDK.Game.GameplayStart();
    }
}
