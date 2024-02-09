using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    #region Components
    private NavMeshAgent agent;
    public NavMeshAgent Agent{ get { return (agent == null) ? agent = GetComponent<UnityEngine.AI.NavMeshAgent>() : agent; } }
    #endregion

    PlaceableBase placeable;
    EdibleBase edible;
    Hamburger _hamburger;

    #region Parameters
    [SerializeField] private List<Transform> targetPositions = new();
    private Vector3 nextPos;
    private int posIndex;

    Vector3 characterPosition;
    Vector3 rayDirection;

    private float waitingTimer;
    private float timeScore;
    private float totalPoint;

    [SerializeField] private GameObject holdParent;
    private bool isHolded;
    private EdibleBase currentFood;
    #endregion

    void OnEnable()
    {
        isHolded = false;

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
        posIndex = Random.Range(0, targetPositions.Count-1);
        nextPos = targetPositions[posIndex].position;
        Agent.SetDestination(nextPos);
    }

    #region Selectables'Methods
    public void GetFood()
    {
        if(edible != null)
        {
            PickUpObject(edible);
        }
    }
    public void PlaceFood()
    {
        if (placeable != null)
        {
            DropObject(placeable);
        }
    }

    void PickUpObject(EdibleBase ingredient) 
    {
        if (isHolded)   return;

        currentFood = ingredient;

        if(currentFood == null) return;

        ingredient.RemoveFromList();
        EventManager.OnFoodHolded.Invoke();

        currentFood.transform.parent = holdParent.transform;
        currentFood.transform.localRotation = Quaternion.identity;
        currentFood.transform.position = holdParent.transform.position;

        isHolded = true;
    }
    void DropObject(PlaceableBase place)
    {
        if(isHolded)
        {
            if(currentFood == null) return;

            place?.UseFood(currentFood);
            if(!place.IsSuitable(currentFood)) return;
            EventManager.OnFoodDropped.Invoke();

            currentFood = null;

            isHolded = false;
        }
    }
    #endregion

    public void DoneWithPath()
    {
        if(Agent.remainingDistance <= Agent.stoppingDistance)
        {
            executingNpcState = ExecutingNpcState.ORDER;
        }
    }

    public void Order()
    {
        Debug.Log("Hamburger please!");
        executingNpcState = ExecutingNpcState.WAIT;
    }

    public void Wait()
    {
        waitingTimer += Time.deltaTime;
        if (waitingTimer > 240f)
        {
            executingNpcState = ExecutingNpcState.PROTEST;
            return;    
        }

        characterPosition = transform.position;
        rayDirection = transform.forward * 3;

        Ray ray = new Ray(characterPosition, rayDirection);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            edible = hit.collider.GetComponent<EdibleBase>();
            if (edible != null)
            {
                _hamburger = edible.gameObject.GetComponent<Hamburger>();
                if(_hamburger != null)
                    executingNpcState = ExecutingNpcState.EAT;
            }
        }
    }

    public void Eat()
    {
        // start eating animation.
        // add an animation event at the end to start REACT.
        GetFood();
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
        // go back about wherever you came.
        Agent.SetDestination(Vector3.zero);
        gameObject.SetActive(false);
    }

    // public void SwitchState(NpcStates nextState)
    // {
    //     currentState = nextState;
    //     currentState.EnterState(this);
    // }
}
