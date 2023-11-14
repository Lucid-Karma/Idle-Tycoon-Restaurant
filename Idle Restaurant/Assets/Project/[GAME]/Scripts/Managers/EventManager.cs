using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    public static UnityEvent OnGameStart = new UnityEvent();
    public static UnityEvent OnGameEnd = new UnityEvent();

    public static UnityEvent OnLevelStart = new UnityEvent();
    public static UnityEvent OnLevelContine = new UnityEvent();
    public static UnityEvent OnLevelFinish = new UnityEvent();

    public static UnityEvent OnLevelSuccess = new UnityEvent();
    public static UnityEvent OnLevelFail = new UnityEvent();

    public static UnityEvent OnRestart = new UnityEvent();

    public static UnityEvent OnObstacleCreated = new UnityEvent();

    public static UnityEvent OnPlayerStartedRunning = new UnityEvent();

    public static UnityEvent OnMusicOn = new UnityEvent();
    public static UnityEvent OnMusicOff = new UnityEvent();
}
