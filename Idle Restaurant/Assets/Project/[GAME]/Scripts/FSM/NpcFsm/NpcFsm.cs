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
    //public NpcStates currentState;
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
    
    #endregion

    #region Components
    private NavMeshAgent agent;
    public NavMeshAgent Agent{ get { return (agent == null) ? agent = GetComponent<UnityEngine.AI.NavMeshAgent>() : agent; } }
    #endregion

    #region Parameters
    private Vector3 nextPos;

    private float waitingTimer;
    private float timeScore;
    private float totalPoint;

    Hamburger _hamburger;
    [HideInInspector] public ISedile chair;
    #endregion

    void OnEnable()
    {
        executingNpcState = ExecutingNpcState.COME;
        MoveNpc();
        // currentState = NpcIdleState;
        // currentState.EnterState(this);
    }

    void Update()
    {
        //currentState.UpdateState(this);
        switch (executingNpcState)
        {
            case ExecutingNpcState.COME:
            DoneWithPath();
            break;

            case ExecutingNpcState.ORDER:
            Order();
            break;

            case ExecutingNpcState.WAIT:
            Wait();
            break;

            case ExecutingNpcState.EAT:
            Eat();
            break;

            case ExecutingNpcState.REACT:
            React();
            break;

            case ExecutingNpcState.PROTEST:
            Protest();
            break;

            case ExecutingNpcState.GO:
            Go();
            break;
        }
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
            Debug.Log("Hamburger please!");
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
                executingNpcState = ExecutingNpcState.EAT;
            else
                executingNpcState = ExecutingNpcState.PROTEST;
        }
    }

    public void Eat()
    {
        // start eating animation.
        // add an animation event at the end to start REACT.
        //GetFood();
        Debug.Log("Mmmh hamburger!");
        executingNpcState = ExecutingNpcState.REACT;
    }

    public void React()
    {
        if(waitingTimer <= 75f)
        {
            timeScore = 2;
        }
        else
        {
            timeScore = (240f - waitingTimer) * 2 / 165f;
        }
        totalPoint = (_hamburger.CalculateScore() + timeScore);
        Debug.Log("time point: " + timeScore);
        Debug.Log("not bad. " + totalPoint);

        OnNpcSitChairStandUp.Invoke();
        
        // start a new animation for a reaction.

        executingNpcState = ExecutingNpcState.GO;
    }

    public void Protest()
    {
        Debug.Log("WTF!!");
        Debug.Log("time so far: " + waitingTimer);
        executingNpcState = ExecutingNpcState.GO;
    }

    public void Go()
    {
        Debug.Log("byeee");
        OnNpcWalk.Invoke();
        // go back about wherever you came.
        Agent.SetDestination(Vector3.zero);
        gameObject.SetActive(false);
        EventManager.OnCustomerWent.Invoke();
    }

    // public void SwitchState(NpcStates nextState)
    // {
    //     currentState = nextState;
    //     currentState.EnterState(this);
    // }
}
