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

    [HideInInspector] public float currentBurgerScore;
    [HideInInspector] public int HostedCustomerCount;
    [HideInInspector] public int totalLevelEarning;
    [HideInInspector] public float totalLevelScore;
    [HideInInspector] public int hostedCustomer;
    private int _levelUpdateCount = 8;

    public int CustomersPerLevel => _levelUpdateCount;

    public void CalculateLevelScore(float point)
    {
        hostedCustomer ++;
        totalLevelScore += point;

        currentBurgerScore = point;

        CalculateIncome();
        DoPointExpression();
    }

    private bool levelFinished;

    // Called whenever a customer walks out. The shift ends once, when every customer of the shift has left.
    // (It used to end as soon as 8 customers had been *rated* while another was still walking out, dividing
    // the 8 scores by 7 — averages above 5 — and could fire again for later departures.)
    public void FinishLevel()
    {
        if (levelFinished || HostedCustomerCount < _levelUpdateCount) return;
        levelFinished = true;

        // Average over every customer that was rated or left unserved (a protest adds 0 to the total).
        totalLevelScore /= Mathf.Max(1, hostedCustomer);
        EventManager.OnLevelFinish.Invoke();
    }

    private void CalculateIncome()
    {
        totalLevelEarning += 5;
    }

    private void DoPointExpression()
    {
        if(currentBurgerScore < 1.5f)
        {
            OnTotalScoreBad.Invoke();
        }
        else if(currentBurgerScore >= 3.5)
        {
            OnTotalScoreGood.Invoke();
        }
        else
        {
            OnTotalScoreNotBad.Invoke();
        }
    }

    public float GetLevelFinalScore()
    {
        //print("Total point: " + totalLevelScore);
        return totalLevelScore;
    }

    public void SpendEarnings(int amount)
    {
        totalLevelEarning -= amount;
    }

    //private void DoPointExpression()
    //{
    //    if (totalLevelScore < 1.5f)
    //    {
    //        OnTotalScoreBad.Invoke();
    //    }
    //    else if (totalLevelScore >= 3.5)
    //    {
    //        OnTotalScoreGood.Invoke();
    //    }
    //    else
    //    {
    //        OnTotalScoreNotBad.Invoke();
    //    }
    //}
}


