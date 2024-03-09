using UnityEngine;

public class GamePanels : Panel
{
    public Panel WelcomePanel;
    public Panel InGamePanel;
    public Panel ScorePanel;
    public Panel LevelFinishPanel;

    private void Awake() 
    {
        WelcomePanel.HidePanel();
        //InGamePanel.HidePanel();
        ScorePanel.HidePanel();
        LevelFinishPanel.HidePanel();
    }

    private void OnEnable()
    {
        EventManager.OnGameStart.AddListener(InitializeWelcomePanel);
        EventManager.OnLevelStart.AddListener(InitializeInGamePanel);
        EventManager.OnLevelFinish.AddListener(InitializeScorePanel);
        EventManager.OnGameEnd.AddListener(InitializeLevelFinishPanel);
    }
    private void OnDisable()
    {
        EventManager.OnGameStart.RemoveListener(InitializeWelcomePanel);
        EventManager.OnLevelStart.RemoveListener(InitializeInGamePanel);
        EventManager.OnLevelFinish.RemoveListener(InitializeScorePanel);
        EventManager.OnGameEnd.RemoveListener(InitializeLevelFinishPanel);
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
}
