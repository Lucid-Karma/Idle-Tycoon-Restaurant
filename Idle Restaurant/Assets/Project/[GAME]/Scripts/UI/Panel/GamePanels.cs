using UnityEngine;

public class GamePanels : Panel
{
    public Panel InGamePanel;
    public Panel LevelFinishPanel;

    private void Awake() 
    {
        InGamePanel.HidePanel();
        LevelFinishPanel.HidePanel();
    }

    private void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(InitializeInGamePanel);
        EventManager.OnLevelFinish.AddListener(InitializeLevelFinishPanel);
    }
    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(InitializeInGamePanel);
        EventManager.OnLevelFinish.RemoveListener(InitializeLevelFinishPanel);
    }

    private void InitializeInGamePanel()
    {
        LevelFinishPanel.HidePanel();
        InGamePanel.ShowPanel();
        ShowPanel();
    }

    private void InitializeLevelFinishPanel()
    {
        InGamePanel.HidePanel();
        LevelFinishPanel.ShowPanel();
        ShowPanel();
    }
}
