using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : Singleton<ScoreManager>
{
    [HideInInspector] public int HostedCustomerCount;
    [HideInInspector] public float totalLevelScore;
    [HideInInspector] public int hostedCustomer;
    private int _levelUpdateCount = 15;

    public void CalculateLevelScore(float point)
    {
        hostedCustomer ++;
        totalLevelScore += point;
        totalLevelScore /= hostedCustomer;
    }

    public void FinishLevel()
    {
        if(hostedCustomer >= _levelUpdateCount)
        {
            EventManager.OnLevelFinish.Invoke();
        }
    }
}


