using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    private bool isGameStarted;
    public bool IsGameStarted { get { return isGameStarted; } private set { isGameStarted = value; } }

    public void StartGame()
    {
        if (IsGameStarted || applicationIsQuitting == false)
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


    private void OnEnable()
    {
        EventManager.OnRestart.AddListener(ContinueGame);
        EventManager.OnLevelFail.AddListener(PauseGame);
        EventManager.OnLevelSuccess.AddListener(PauseGame);
        //Timer.OnTimeOut += PauseGame;
    }
    private void OnDisable()
    {
        EventManager.OnRestart.RemoveListener(ContinueGame);
        EventManager.OnLevelFail.RemoveListener(PauseGame);
        EventManager.OnLevelSuccess.RemoveListener(PauseGame);
        //Timer.OnTimeOut -= PauseGame;
    }

    void PauseGame()
    {
        Time.timeScale = 0.5f;
    }

    void ContinueGame()
    {
        Time.timeScale = 1;
    }
}
