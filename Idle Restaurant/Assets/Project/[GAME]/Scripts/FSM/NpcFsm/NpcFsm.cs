//using CrazyGames;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public enum ExecutingNpcState
{
    COME,
    ORDER,
    WAIT,
    EAT,
    REACT,
    PROTEST,
    GO
}
public class NpcFsm : MonoBehaviour
{
    #region FSM
    public ExecutingNpcState executingNpcState;
    public NpcStates currentState;
    public NpcComeState comeState = new();
    public NpcOrderState orderState = new();
    public NpcWaitState waitState = new();
    public NpcEatState eatState = new();
    public NpcReactState reactState = new();
    public NpcProtestState protestState = new();
    public NpcGoState goState = new();
    #endregion

    #region Events
    [HideInInspector]
    public UnityEvent OnNpcWalk = new();
    [HideInInspector]
    public UnityEvent OnNpcSitChairDown = new();
    [HideInInspector]
    public UnityEvent OnNpcSitChairIdle = new();
    [HideInInspector]
    public UnityEvent OnNpcSitChairStandUp = new();

    [HideInInspector]
    public UnityEvent OnNpcEatStart = new();
    [HideInInspector]
    public UnityEvent OnNpcEatEnd = new();
    [HideInInspector]
    public UnityEvent OnNpcWaitEnd = new();
    // The burger arrived: LastReview holds what they think of it (before eating).
    [HideInInspector]
    public UnityEvent OnNpcServed = new();
    // They finished eating and paid: ScoreManager.LastOrder holds the rating and the money.
    [HideInInspector]
    public UnityEvent OnNpcPaid = new();
    // Something to say that isn't about a served burger (a snack, a bonk on the head).
    [HideInInspector]
    public UnityEvent<string, Mood> OnNpcSays = new();
    // A small tip outside of a rated order (a thrown snack).
    [HideInInspector]
    public UnityEvent<int> OnNpcTipped = new();

    #endregion

    #region Components
    private NavMeshAgent agent;
    public NavMeshAgent Agent{ get { return (agent == null) ? agent = GetComponent<UnityEngine.AI.NavMeshAgent>() : agent; } }
    #endregion

    // Customers in the restaurant right now (for hints and serving; avoids scene searches every frame).
    public static readonly List<NpcFsm> Active = new();

    #region Parameters
    private Vector3 nextPos;

    // Rush-hour pacing: each customer waits this long (seconds) before leaving hungry. It used to be a flat
    // 240 s, which left no reason to hurry — and no reason to cut corners.
    // (80-110 s felt too tight in play tests: two burgers in a row already cost customers.)
    [SerializeField] private Vector2 patienceRange = new Vector2(100f, 130f);
    public float Patience { get; private set; } = 115f;
    public float WaitedSeconds => waitingTimer;
    public BurgerReview LastReview { get; private set; }

    private float waitingTimer;
    private float servedAfter;

    // Food thrown at them while they wait: the first prepared item is a snack (buys time, small tip);
    // anything unprepared, or a second item, earns the chef a tomato back.
    [SerializeField] private float snackPatience = 25f;
    [SerializeField] private float bonkPatience = 10f;
    public const int SnackTip = 1;
    private int caughtCount;

    Hamburger _hamburger;
    [HideInInspector] public ISedile chair;
    #endregion

    void OnEnable()
    {
        Active.Add(this);
        Patience = Random.Range(patienceRange.x, patienceRange.y);
        LastReview = null;
        caughtCount = 0;

        executingNpcState = ExecutingNpcState.COME;
        currentState = comeState;
        currentState.EnterState(this);
    }

    void Update()
    {
        currentState.UpdateState(this);
    }

    public void MoveNpc()
    {
        if(chair != null)
        {
            nextPos = chair.CalculateSitPos();
            Agent.SetDestination(nextPos);
            chair.IsEmpty = false;
        }

    }

    public void DoneWithPath()
    {
        // While a new path is being computed remainingDistance can still hold the old (arrived) value.
        if(!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance)
        {
            executingNpcState = ExecutingNpcState.ORDER;
        }
    }

    public void Order()
    {
        if(chair != null)
        {
            transform.rotation = chair.GetSedileRot();
            OnNpcSitChairDown.Invoke();
            executingNpcState = ExecutingNpcState.WAIT;
        }
    }

    public void Wait()
    {
        waitingTimer += Time.deltaTime;
        if (waitingTimer > Patience)
        {
            // Out of patience: "Too slow!", and a tomato for the chef on the way out.
            OnNpcWaitEnd.Invoke();
            if (FoodFight.Instance != null) FoodFight.Instance.CustomerThrows(this);
            executingNpcState = ExecutingNpcState.PROTEST;
            return;
        }

        if(chair.GetTableService().IsHaveFood())
        {
            _hamburger = chair.GetTableService().GetHamburger();
            if(_hamburger != null)
            {
                servedAfter = waitingTimer;
                LastReview = _hamburger.Review;
                OnNpcServed.Invoke();
                OnNpcEatStart.Invoke();
                executingNpcState = ExecutingNpcState.EAT;
            }
            else
                executingNpcState = ExecutingNpcState.PROTEST;
        }
    }

    public void React()
    {
        // Quality (how it was made) and speed (how long they waited) both count; see ScoreManager.RateOrder.
        var order = ScoreManager.Instance.RateOrder(LastReview, servedAfter, Patience);

        OnNpcSitChairStandUp.Invoke();
        OnNpcPaid.Invoke();
        EventManager.OnOrderRated.Invoke();
        EventManager.OnScoreUpdate.Invoke();
        chair.GetTableService().RemoveFood(_hamburger); // free only this table
        _hamburger.gameObject.SetActive(false);

        if (order.Rating < 2.5f)
        {
            EventManager.OnScoreBad.Invoke();
        }
        else if(order.Rating >= 4)
        {
            EventManager.OnScoreGood.Invoke();
            //if (totalPoint >= 4.5f)
            //    CrazySDK.Game.HappyTime();
        }
        else
        {
            EventManager.OnScoreNotBad.Invoke();
        }

        chair.IsEmpty = true;
        executingNpcState = ExecutingNpcState.GO;
    }

    // Something the chef threw hit them (FoodFight). Only customers still waiting for their order react.
    public void CatchThrown(BurgerLayer item)
    {
        if (executingNpcState != ExecutingNpcState.WAIT) return;
        caughtCount++;

        if (caughtCount > 1)
        {
            OnNpcSays.Invoke("I want a BURGER!", Mood.Shocked);
            FoodFight.Instance.CustomerThrows(this);
        }
        else if (item.Prep == Prep.Good)
        {
            waitingTimer = Mathf.Max(0f, waitingTimer - snackPatience);
            OnNpcSays.Invoke(BurgerReview.SnackLine(item), Mood.Delighted);
            ScoreManager.Instance.AddTip(SnackTip);
            OnNpcTipped.Invoke(SnackTip);
            EventManager.OnScoreUpdate.Invoke();
        }
        else
        {
            waitingTimer = Mathf.Min(Patience - 1f, waitingTimer + bonkPatience);
            OnNpcSays.Invoke(BurgerReview.BonkLine(item), Mood.Shocked);
            FoodFight.Instance.CustomerThrows(this);
        }
    }

    public void Protest()
    {        ScoreManager.Instance.RegisterWalkout();
        executingNpcState = ExecutingNpcState.GO;
    }

    public void Go()
    {
        if(IsPathEnd())
        {
            ScoreManager.Instance.HostedCustomerCount ++;
            Agent.stoppingDistance = 0;
            gameObject.SetActive(false);
            EventManager.OnCustomerWent.Invoke();
            ScoreManager.Instance.FinishLevel();
        }
    }

    private bool IsPathEnd()
    {
        if(!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance)
            return true;
        return false;
    }

    public void SwitchState(NpcStates nextState)
    {
        currentState = nextState;
        currentState.EnterState(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
        waitingTimer = 0;
    }
}
