using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// The outcome of one served order, for the toast, the customer's money pop and the chef card.
public struct OrderResult
{
    public BurgerReview Review;
    public float Rating;      // 0..5 stars: up to 3 for how it was made + up to 2 for how fast it came
    public float Speed01;     // 1 = served almost at once, 0 = at the end of their patience
    public int Earned;        // base + tip + perfect bonus + rush bonus + the customer's quirk + tip jar
    public int Stars;         // rounded rating: what the order adds to the cafe's stars (CafeProgress)
    public Mood Mood;         // how the customer took it (their quirk can change the burger's own mood)
    public int RushStreak;    // orders served in a row, each within RushWindow of the previous one
    public bool Speedy => Speed01 >= 0.8f;
}

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
    [HideInInspector] public float totalLevelScore;
    [HideInInspector] public int hostedCustomer;

    // The money in the till. It is kept between shifts (CafeProgress); what this shift added is ShiftEarned.
    public int totalLevelEarning => CafeProgress.Coins;
    public int ShiftEarned { get; private set; }
    public int ShiftStars { get; private set; }

    // 8 customers a shift, more with bigger dining upgrades. Fixed for the whole shift on first use, so a
    // table bought mid-shift adds its customer from the next shift (the pips and the end stay consistent).
    public const int BaseCustomers = 8;
    private int customersThisShift;
    public int CustomersPerLevel
    {
        get
        {
            if (customersThisShift == 0) customersThisShift = BaseCustomers + CafeShop.ExtraCustomers;
            return customersThisShift;
        }
    }

    #region Rush rules
    // Speed vs. care is the core choice: a sloppy burger served fast can out-earn a perfect one served late.
    public const float QualityStars = 3f, SpeedStars = 2f;
    // Waiting up to this share of their patience still counts as instant service.
    private const float SpeedGrace = 0.2f;
    public const int BasePay = 3, PerfectBonus = 2, MaxRushBonus = 3;
    // Serve the next order within this many seconds to keep the rush streak going. 45, not 30: a careful
    // player makes a Chef's Classic every ~35-40 s (measured with a play-test bot), so at 30 the streak - and
    // its bonus and its "RUSH x2" - only ever happened to someone pipelining two burgers at once.
    public const float RushWindow = 45f;
    #endregion

    #region Shift stats
    public OrderResult LastOrder { get; private set; }
    public int ServedCount { get; private set; }
    public int PerfectCount { get; private set; }
    public int ChaosCount { get; private set; }
    public int WalkoutCount { get; private set; }
    public int BestStreak { get; private set; }
    public int RushStreak { get; private set; }
    private float lastServeTime = float.NegativeInfinity;
    private readonly Dictionary<string, int> titleCounts = new();
    // The burger this shift served most (latest wins a tie) — "Signature burger" on the result card.
    public string SignatureBurger { get; private set; }
    // Seconds left to keep the rush streak alive (0 = no streak running).
    public float RushTimeLeft => RushStreak > 0 ? Mathf.Max(0f, RushWindow - (Time.time - lastServeTime)) : 0f;
    #endregion

    // 1 = served within the grace share of their patience, falling to 0 at the end of it.
    public static float Speed01(float waitedSeconds, float patience)
    {
        float grace = patience * SpeedGrace;
        return 1f - Mathf.Clamp01((waitedSeconds - grace) / Mathf.Max(1f, patience - grace));
    }

    public OrderResult RateOrder(BurgerReview review, float waitedSeconds, float patience, CustomerKind kind = CustomerKind.Regular)
    {
        float speed01 = Speed01(waitedSeconds, patience);
        // Stars are how happy this customer was: their own taste (a Barbarian loves a mess, a Mage only a
        // perfect burger) and how fast it came. A customer who reacts with delight never leaves fewer than
        // four, so what they say, what they pay and what they rate agree.
        var mood = Customers.React(kind, review, speed01).mood;
        float rating = QualityStars * Customers.Taste01(kind, review) + SpeedStars * speed01;
        if (mood == Mood.Delighted) rating = Mathf.Max(rating, 4f);

        RushStreak = Time.time - lastServeTime <= RushWindow ? RushStreak + 1 : 1;
        lastServeTime = Time.time;
        BestStreak = Mathf.Max(BestStreak, RushStreak);

        int earned = BasePay + Mathf.RoundToInt(rating)
                   + (review.IsPerfect ? PerfectBonus : 0)
                   + Mathf.Min(RushStreak - 1, MaxRushBonus)
                   + Customers.Bonus(kind, review, speed01)
                   + (rating >= 4f ? CafeShop.TipJar : 0);
        earned = Mathf.Max(1, earned);
        int stars = Mathf.RoundToInt(rating);

        ServedCount++;
        if (review.IsPerfect) PerfectCount++;
        if (review.IsChaos) ChaosCount++;
        titleCounts.TryGetValue(review.Title, out int n);
        titleCounts[review.Title] = ++n;
        if (SignatureBurger == null || n >= titleCounts[SignatureBurger]) SignatureBurger = review.Title;

        LastOrder = new OrderResult
        {
            Review = review, Rating = rating, Speed01 = speed01, Earned = earned, Stars = stars,
            Mood = mood, RushStreak = RushStreak
        };

        hostedCustomer++;
        totalLevelScore += rating;
        currentBurgerScore = rating;
        ShiftEarned += earned;
        ShiftStars += stars;
        CafeProgress.AddCoins(earned);
        CafeProgress.AddStars(stars);   // may level the cafe up (UIToast announces it after this order)
        DoPointExpression();
        return LastOrder;
    }

    // A customer ran out of patience: counts as a 0-star order and ends the rush streak.
    public void RegisterWalkout()
    {
        hostedCustomer++;
        WalkoutCount++;
        RushStreak = 0;
    }

    // Money outside of a rated order (a snack thrown to a waiting customer).
    public void AddTip(int amount)
    {
        ShiftEarned += amount;
        CafeProgress.AddCoins(amount);
    }

    private bool levelFinished;

    // Called whenever a customer walks out. The shift ends once, when every customer of the shift has left.
    // (It used to end as soon as 8 customers had been *rated* while another was still walking out, dividing
    // the 8 scores by 7 — averages above 5 — and could fire again for later departures.)
    public void FinishLevel()
    {
        if (levelFinished || HostedCustomerCount < CustomersPerLevel) return;
        levelFinished = true;

        // Average over every customer that was rated or left unserved (a protest adds 0 to the total).
        totalLevelScore /= Mathf.Max(1, hostedCustomer);
        CafeProgress.CountShift();
        EventManager.OnLevelFinish.Invoke();
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
}
