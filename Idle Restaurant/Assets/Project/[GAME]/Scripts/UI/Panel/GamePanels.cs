using System;
using UnityEngine;

public class GamePanels : Panel
{
    public Panel WelcomePanel;
    public Panel HelpPanel;
    public Panel InGamePanel;
    public Panel ScorePanel;
    public Panel LevelFinishPanel;

    private void Awake() 
    {
        WelcomePanel.HidePanel();
        HelpPanel.HidePanel();
        //InGamePanel.HidePanel();
        ScorePanel.HidePanel();
        LevelFinishPanel.HidePanel();
    }

    private void OnEnable()
    {
        EventManager.OnGameStart.AddListener(InitializeWelcomePanel);
        EventManager.OnLevelStart.AddListener(InitializeInGamePanel);
        EventManager.OnLevelContine.AddListener(ReinitializeInGamePanel);
        EventManager.OnLevelFinish.AddListener(InitializeScorePanel);
        EventManager.OnGameEnd.AddListener(InitializeLevelFinishPanel);
        EventManager.OnHelpRequest.AddListener(InitializeHelpPanel);
    }

    private void OnDisable()
    {
        EventManager.OnGameStart.RemoveListener(InitializeWelcomePanel);
        EventManager.OnLevelStart.RemoveListener(InitializeInGamePanel);
        EventManager.OnLevelContine.RemoveListener(ReinitializeInGamePanel);
        EventManager.OnLevelFinish.RemoveListener(InitializeScorePanel);
        EventManager.OnGameEnd.RemoveListener(InitializeLevelFinishPanel);
        EventManager.OnHelpRequest.RemoveListener(InitializeHelpPanel);
    }

    private void InitializeWelcomePanel()
    {
        InGamePanel.HidePanel();
        WelcomePanel.ShowPanel();
        ShowPanel();
    }

    private void InitializeInGamePanel()
    {
        WelcomePanel.HidePanel();
        InGamePanel.ShowPanel();
        ShowPanel();
    }
    private void ReinitializeInGamePanel()
    {
        HelpPanel.HidePanel();
        InGamePanel.ShowPanel();
        ShowPanel();
    }

    private void InitializeScorePanel()
    {
        InGamePanel.HidePanel();
        ScorePanel.ShowPanel();
        ShowPanel();
    }

    private void InitializeLevelFinishPanel()
    {
        ScorePanel.HidePanel();
        LevelFinishPanel.ShowPanel();
        ShowPanel();
    }

    private void InitializeHelpPanel()
    {
        InGamePanel.HidePanel();
        HelpPanel.ShowPanel();
        ShowPanel();
    }
}
