using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : Singleton<ScoreManager>
{
    #region Events
    [HideInInspector]
    public static UnityEvent OnTotalScoreBad = new();
    [HideInInspector]
    public static UnityEvent OnTotalScoreGood = new();
    [HideInInspector]
    public static UnityEvent OnTotalScoreNotBad = new();
    #endregion
    
    [HideInInspector] public int HostedCustomerCount;
    [HideInInspector] public int totalLevelEarning;
    [HideInInspector] public float totalLevelScore;
    [HideInInspector] public int hostedCustomer;
    private int _levelUpdateCount = 8;

    public void CalculateLevelScore(float point)
    {
        hostedCustomer ++;
        totalLevelScore += point;
        totalLevelScore /= hostedCustomer;

        CalculateIncome();
        DoPointExpression();
    }

    public void FinishLevel()
    {
        if(hostedCustomer >= _levelUpdateCount)
        {
            EventManager.OnLevelFinish.Invoke();
        }
    }

    private void CalculateIncome()
    {
        totalLevelEarning += 5;
    }

    private void DoPointExpression()
    {
        if(totalLevelScore < 1.5f)
        {
            OnTotalScoreBad.Invoke();
        }
        else if(totalLevelScore >= 3.5)
        {
            OnTotalScoreGood.Invoke();
        }
        else
        {
            OnTotalScoreNotBad.Invoke();
        }
    }
}


