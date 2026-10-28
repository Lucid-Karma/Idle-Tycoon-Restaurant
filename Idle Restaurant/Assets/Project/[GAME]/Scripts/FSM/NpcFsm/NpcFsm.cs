using System.Collections;
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
    private Vector3 seat;
    private bool clearingChair;
    private const float ChairClearance = 1.8f;

    // Rush-hour pacing: each customer waits this long (seconds) before leaving hungry. It used to be a flat
    // 240 s, which left no reason to hurry — and no reason to cut corners.
    // (80-110 s felt too tight in play tests: two burgers in a row already cost customers.)
    [SerializeField] private Vector2 patienceRange = new Vector2(100f, 130f);
    public float Patience { get; private set; } = 115f;
    public float WaitedSeconds => waitingTimer;
    public BurgerReview LastReview { get; private set; }
    // What they ordered as they sat down (Orders.Pick), shown in their bubble (UIOrderTicket). Null until seated.
    public Recipe Ordered { get; private set; }
    // Their order's number (1, 2, 3...: the smallest one free as they sit down), worn by their bubble, the plate and
    // the burger made for them and the order card (UIOrderTags), so who a burger is for is a matter of matching two
    // numbers (user: "clear enough for a five-year-old"). 0 until they order; kept until they leave.
    public int Ticket { get; private set; }
    public static readonly UnityEngine.Events.UnityEvent<NpcFsm> OnOrdered = new();
    // An order was rated (who, and how it went): a perfect one pops "PERFECT!" over them (UISplatPop).
    // Static: subscribe with named methods only.
    public static readonly UnityEngine.Events.UnityEvent<NpcFsm, OrderResult> OnRated = new();

    // Regular (skeleton) or an adventurer with a quirk (Customers): patience, pay, what they say.
    [SerializeField] private CustomerKind kind;
    public CustomerKind Kind => kind;
    // What they say about the burger they were served, and how they take it (their quirk can change both).
    public string ServedLine { get; private set; }
    public Mood ServedMood { get; private set; }
    public string GiveUpLine => Customers.GiveUpLine(kind);

    private float waitingTimer;
    private float servedAfter;

    // Food thrown at them while they wait: the first prepared item is a snack (buys time, small tip);
    // anything unprepared, or a second item, earns the chef a tomato back.
    [SerializeField] private float snackPatience = 25f;
    [SerializeField] private float bonkPatience = 10f;
    public const int SnackTip = 1;
    private int caughtCount;
    // Only the first thing thrown at a customer can be a snack; after that they just ask for a burger.
    public bool CanTakeSnack => executingNpcState == ExecutingNpcState.WAIT && caughtCount == 0;

    Hamburger _hamburger;
    [HideInInspector] public ISedile chair;
    #endregion

    void OnEnable()
    {
        Active.Add(this);
        warnedOffFloor = false;
        if (!Agent.enabled) Agent.enabled = true;   // switched off while they sat (Seat)
        Patience = Random.Range(patienceRange.x, patienceRange.y) * Customers.Of(kind).PatienceScale * CafeShop.PatienceScale;
        LastReview = null;
        Ordered = null;
        Ticket = 0;
        caughtCount = 0;

        executingNpcState = ExecutingNpcState.COME;
        currentState = comeState;
        currentState.EnterState(this);
        vanishing = false;
        if (Model != null) { Model.localScale = Vector3.zero; StartCoroutine(Appear()); }
        SeasonalDecor.Dress(this);   // a ghost at Halloween, a slower stroll in winter
    }

    void Update()
    {
        ShutChairBehind();
        currentState.UpdateState(this);
    }

    public void MoveNpc()
    {
        if(chair != null)
        {
            nextPos = chair.CalculateSitPos();
            chair.Open(this, true);   // everyone else walks round the chair; this customer walks into it
            Agent.SetDestination(nextPos);
            chair.IsEmpty = false;
        }

    }

    public void DoneWithPath()
    {
        if (!OnTheFloor()) return;
        // While a new path is being computed remainingDistance can still hold the old (arrived) value.
        if(!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance)
        {
            executingNpcState = ExecutingNpcState.ORDER;
        }
    }

    private int FreeTicket()
    {
        int ticket = 1;
        while (Active.Exists(n => n != this && n.Ticket == ticket)) ticket++;
        return ticket;
    }

    public void Order()
    {
        if(chair != null)
        {
            transform.rotation = chair.GetSedileRot();
            Seat();
            Ticket = FreeTicket();
            Ordered = Orders.Pick();
            OnOrdered.Invoke(this);
            OnNpcSitChairDown.Invoke();
            executingNpcState = ExecutingNpcState.WAIT;
            GameSfx.Play(GameSfx.Cue.Seated);
            // Adventurers say what they're like as they sit down ("MAKE IT MESSY!").
            var profile = Customers.Of(kind);
            if (profile.Hello != null) OnNpcSays.Invoke(profile.Hello, profile.HelloMood);
        }
    }

    public void Wait()
    {
        // No clock on the player during the first-shift lesson: nobody loses patience, so nobody leaves,
        // throws a tomato, or rushes him, however long he takes to read a card (Tutorial).
        if (!Tutorial.Running) waitingTimer += Time.deltaTime;
        if (waitingTimer > Patience)
        {
            // Out of patience: "Too slow!", and a tomato for the chef on the way out (knights just sigh).
            OnNpcWaitEnd.Invoke();
            if (FoodFight.Instance != null && Customers.ThrowsBack(kind)) FoodFight.Instance.CustomerThrows(this);
            executingNpcState = ExecutingNpcState.PROTEST;
            return;
        }

        if(chair.GetTableService().IsHaveFood())
        {
            _hamburger = chair.GetTableService().GetHamburger();
            if(_hamburger != null)
            {
                servedAfter = waitingTimer;
                LastReview = BurgerReview.Of(_hamburger.Layers, Ordered);
                (ServedLine, ServedMood) = Customers.React(kind, LastReview, ScoreManager.Speed01(servedAfter, Patience));
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
        var order = ScoreManager.Instance.RateOrder(LastReview, servedAfter, Patience, kind, Ordered);
        OnRated.Invoke(this, order);
        // The money stays on the table until the chef walks by (MoneyPiles); it is already saved.
        MoneyPiles.Drop(chair.GetTableService().transform.position, order.Earned);

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

        bool throwsBack = Customers.ThrowsBack(kind);
        if (caughtCount > 1)
        {
            OnNpcSays.Invoke("I want a BURGER!", Mood.Shocked);
            if (throwsBack) FoodFight.Instance.CustomerThrows(this);
        }
        else if (item.Prep == Prep.Good)
        {
            float scale = Customers.SnackScale(kind);
            int tip = Mathf.RoundToInt(SnackTip * scale);
            waitingTimer = Mathf.Max(0f, waitingTimer - snackPatience * scale);
            OnNpcSays.Invoke(scale > 1f ? "Snacks! My hero!" : BurgerReview.SnackLine(item), Mood.Delighted);
            ScoreManager.Instance.AddTip(tip);
            OnNpcTipped.Invoke(tip);
            GameSfx.Play(GameSfx.Cue.Tip);
            EventManager.OnScoreUpdate.Invoke();
            FoodFight.OnSnackAccepted.Invoke();
        }
        else
        {
            waitingTimer = Mathf.Min(Patience - 1f, waitingTimer + bonkPatience);
            OnNpcSays.Invoke(throwsBack ? BurgerReview.BonkLine(item) : "How rude!", Mood.Shocked);
            if (throwsBack) FoodFight.Instance.CustomerThrows(this);
        }
    }

    public void Protest()
    {        ScoreManager.Instance.RegisterWalkout();
        executingNpcState = ExecutingNpcState.GO;
    }

    // Seated: on the seat itself, out of the crowd simulation (a seated agent used to be shoved off its seat by
    // customers walking past, ending up two on one chair), and the chair shuts behind them.
    private void Seat()
    {
        seat = chair.CalculateSitPos();
        var at = new Vector3(seat.x, transform.position.y, seat.z);
        if ((at - transform.position).sqrMagnitude < 1f) transform.position = at;
        Agent.enabled = false;
        chair.Open(this, false);
    }

    // Getting up: the chair opens for them, and once the navmesh is back under their feet they walk off to the
    // door; the chair shuts again once they are clear of it (Update).
    public void LeaveChair(Vector3 to, float stoppingDistance) => StartCoroutine(Leave(to, stoppingDistance));

    private IEnumerator Leave(Vector3 to, float stoppingDistance)
    {
        chair.Open(this, true);
        var filter = new NavMeshQueryFilter { agentTypeID = Agent.agentTypeID, areaMask = Agent.areaMask };
        for (int i = 0; i < 30 && !NavMesh.SamplePosition(transform.position, out _, 0.2f, filter); i++) yield return null;
        Agent.enabled = true;
        Agent.stoppingDistance = stoppingDistance;
        if (Agent.isOnNavMesh) Agent.SetDestination(to);
        else OnTheFloor();
        clearingChair = true;
    }

    private void ShutChairBehind()
    {
        if (!clearingChair || chair == null) return;
        var away = transform.position - seat;
        away.y = 0f;
        if (away.sqrMagnitude < ChairClearance * ChairClearance) return;
        clearingChair = false;
        chair.Open(this, false);
    }

    public void Go()
    {
        if (!Agent.enabled || vanishing) return;   // still getting up (Leave), or already on the way out
        if(IsPathEnd())
            StartCoroutine(Vanish());
    }

    // Coming in and going out (user, 2026-10-27: customers popped into existence at the door and blinked out of it):
    // the model - not the root, whose scale the navmesh agent would take - grows in with a little bounce, and on the
    // way out swells a touch and shrinks away; only then is the customer gone (and counted).
    private Transform model;
    private bool vanishing;
    private Transform Model => model != null ? model : model = transform.Find("Model");

    private IEnumerator Appear()
    {
        var m = Model;
        if (m == null) yield break;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.35f)
        {
            float back = 1.70158f, x = t - 1f;   // ease out, with an overshoot
            m.localScale = Vector3.one * (1f + (back + 1f) * x * x * x + back * x * x);
            yield return null;
        }
        m.localScale = Vector3.one;
    }

    private IEnumerator Vanish()
    {
        vanishing = true;
        if (Agent.isOnNavMesh) Agent.isStopped = true;
        var m = Model;
        for (float t = 0f; m != null && t < 1f; t += Time.deltaTime / 0.28f)
        {
            float k = t < 0.3f ? 1f + 0.15f * (t / 0.3f) : 1.15f * (1f - (t - 0.3f) / 0.7f);
            m.localScale = Vector3.one * Mathf.Max(0f, k);
            yield return null;
        }
        ScoreManager.Instance.HostedCustomerCount ++;
        if (Agent.isOnNavMesh) Agent.isStopped = false;
        Agent.stoppingDistance = 0;
        gameObject.SetActive(false);
        EventManager.OnCustomerWent.Invoke();
        ScoreManager.Instance.FinishLevel();
    }

    private bool IsPathEnd()
    {
        if (!OnTheFloor()) return false;
        if(!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance)
            return true;
        return false;
    }

    // An agent switched on where there is no navmesh under it never moves, and every question about its path
    // logs an error (a build showed one every frame: a customer standing still at the door). Put it on the nearest
    // floor and send it on its way again; say so once, with where it was, to find out why.
    private bool warnedOffFloor;
    private bool OnTheFloor()
    {
        if (!Agent.enabled) return false;
        if (Agent.isOnNavMesh) return true;
        var filter = new NavMeshQueryFilter { agentTypeID = Agent.agentTypeID, areaMask = Agent.areaMask };
        bool found = NavMesh.SamplePosition(transform.position, out var hit, 5f, filter);
        if (!warnedOffFloor)
        {
            warnedOffFloor = true;
            Debug.LogWarning($"[NpcFsm] {name} off the navmesh at {transform.position} ({executingNpcState}); nearest floor " +
                             (found ? $"{hit.position}, {Vector3.Distance(hit.position, transform.position):0.00} m away" : "none within 5 m"));
        }
        if (!found || !Agent.Warp(hit.position)) return false;
        if (executingNpcState == ExecutingNpcState.COME) MoveNpc();
        else if (executingNpcState == ExecutingNpcState.GO) Agent.SetDestination(new Vector3(21.35f, 1.2f, 8.79f));
        return Agent.isOnNavMesh;
    }

    public void SwitchState(NpcStates nextState)
    {
        currentState = nextState;
        currentState.EnterState(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
        clearingChair = false;
        if (chair != null) chair.Open(this, false);
        waitingTimer = 0;
    }
}
