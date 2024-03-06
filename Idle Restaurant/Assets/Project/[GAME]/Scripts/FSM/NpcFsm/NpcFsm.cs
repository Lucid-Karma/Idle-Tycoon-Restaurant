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
    
    #endregion

    #region Components
    private NavMeshAgent agent;
    public NavMeshAgent Agent{ get { return (agent == null) ? agent = GetComponent<UnityEngine.AI.NavMeshAgent>() : agent; } }
    #endregion

    #region Parameters
    private Vector3 nextPos;

    private float waitingTimer;
    private float timeScore;
    private float hamburgerPoint;
    private float totalPoint;

    Hamburger _hamburger;
    [HideInInspector] public ISedile chair;
    #endregion

    void OnEnable()
    {
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
        if(Agent.remainingDistance <= Agent.stoppingDistance)
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
        if (waitingTimer > 240f)
        {
            executingNpcState = ExecutingNpcState.PROTEST;
            return;    
        }

        if(chair.GetTableService().IsHaveFood())
        {
            _hamburger = chair.GetTableService().GetHamburger();
            if(_hamburger != null)
            {
                OnNpcEatStart.Invoke();
                executingNpcState = ExecutingNpcState.EAT;
            }
            else
                executingNpcState = ExecutingNpcState.PROTEST;
        }
    }

    public void Eat()
    {
        // start eating animation.
        // add an animation event at the end to start REACT.
        
        //executingNpcState = ExecutingNpcState.REACT;
    }

    public void React()
    {
        hamburgerPoint = _hamburger.CalculateScore();
        if(waitingTimer <= 75f)
        {
            if(hamburgerPoint < 1f)
            {
                timeScore = 0.6f;
            }
            else
                timeScore = 2;
        }
        else
        {
            timeScore = (240f - waitingTimer) * 2 / 165f;
        }
        totalPoint = (hamburgerPoint + timeScore);

        OnNpcSitChairStandUp.Invoke();
        ScoreManager.Instance.CalculateLevelScore(totalPoint);
        EventManager.OnScoreUpdate.Invoke();

        if(totalPoint < 2.5f)
        {
            EventManager.OnScoreBad.Invoke();
        }
        else if(totalPoint >= 4)
        {
            EventManager.OnScoreGood.Invoke();
        }
        else
        {
            EventManager.OnScoreNotBad.Invoke();
        }
        
        // start a new animation for a reaction.

        executingNpcState = ExecutingNpcState.GO;
    }

    public void Protest()
    {
        // Add a event to trigger chef's sad anim.

        //Debug.Log("time so far: " + waitingTimer);
        ScoreManager.Instance.hostedCustomer ++;
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
        if(Agent.remainingDistance <= Agent.stoppingDistance)
            return true;
        return false;
    }

    public void SwitchState(NpcStates nextState)
    {
        currentState = nextState;
        currentState.EnterState(this);
    }
}
