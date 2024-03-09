using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    public static UnityEvent OnGameStart = new();
    public static UnityEvent OnGameEnd = new();

    public static UnityEvent OnLevelStart = new();
    public static UnityEvent OnLevelContine = new();
    public static UnityEvent OnLevelFinish = new();

    public static UnityEvent OnLevelSuccess = new();
    public static UnityEvent OnLevelFail = new();

    public static UnityEvent OnRestart = new();

    public static UnityEvent OnCustomerWent = new();

    //public static HamburgerEvent OnHamburgerPrep = new HamburgerEvent();

    public static UnityEvent OnFoodHolded = new();
    public static UnityEvent OnFoodDropped = new();

    public static UnityEvent OnScoreUpdate = new();
    public static UnityEvent OnScoreBad = new();
    public static UnityEvent OnScoreNotBad = new();
    public static UnityEvent OnScoreGood = new();
    public static UnityEvent OnCustomerProtest = new();

    public static UnityEvent OnPlayerStartedRunning = new();

    public static UnityEvent OnMusicOn = new();
    public static UnityEvent OnMusicOff = new();

    public static UnityEvent OnClick = new();
}

//public class HamburgerEvent : UnityEvent<Transform> { }
