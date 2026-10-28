using System;
using UnityEngine;

public class GamePanels : Panel
{
    public Panel WelcomePanel;
    public Panel HelpPanel;
    public Panel InGamePanel;
    // (The old result card - ScorePanel - and the goodbye screen behind its Exit button - FarewellPanel, the
    // ExitPanel picture - were removed on 2026-10-17: neither had been shown since the shifts flow into each other.)

    private void Awake() 
    {
        WelcomePanel.HidePanel();
        HelpPanel.HidePanel();
        //InGamePanel.HidePanel();
    }

    private void OnEnable()
    {
        EventManager.OnGameStart.AddListener(InitializeWelcomePanel);
        EventManager.OnLevelStart.AddListener(InitializeInGamePanel);
        EventManager.OnLevelContine.AddListener(ReinitializeInGamePanel);
        // No result card between shifts any more: service flows on (ShiftFlow).
        EventManager.OnHelpRequest.AddListener(InitializeHelpPanel);
    }

    private void OnDisable()
    {
        EventManager.OnGameStart.RemoveListener(InitializeWelcomePanel);
        EventManager.OnLevelStart.RemoveListener(InitializeInGamePanel);
        EventManager.OnLevelContine.RemoveListener(ReinitializeInGamePanel);
        EventManager.OnHelpRequest.RemoveListener(InitializeHelpPanel);
    }

    // The title screen is no longer shown: the game opens straight into the kitchen (GameManager.StartLevel).
    private void InitializeWelcomePanel()
    {
        WelcomePanel.HidePanel();
        InGamePanel.ShowPanel();
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

    private void InitializeHelpPanel()
    {
        InGamePanel.HidePanel();
        HelpPanel.ShowPanel();
        ShowPanel();
    }
}
