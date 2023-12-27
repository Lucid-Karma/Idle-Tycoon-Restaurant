using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum ExecutingState
{
    IDLE,
    WALK
}
public class PlayerFSM : MonoBehaviour
{
    #region FSM
    public ExecutingState executingState;
    public PlayerStates currentState;

    public PlayerIdleState playerIdleState = new PlayerIdleState();
    public PlayerWalkState playerWalkState = new PlayerWalkState();
    #endregion

    #region Components
    private NavMeshAgent agent;
    public NavMeshAgent Agent{ get { return (agent == null) ? agent = GetComponent<UnityEngine.AI.NavMeshAgent>() : agent; } }
    #endregion

    ISpawnable spawnable;
    IPlaceable placeable;
    EdibleBase edible;

    #region Parameters
    Camera _playerCam;
    Ray ray;
    private RaycastHit hit;

    [SerializeField] private GameObject holdParent;
    private bool isHolded;
    private EdibleBase currentFood;
    private float distance;
    #endregion

    void Start()
    {
        _playerCam = Camera.main;
        isHolded = false;

        executingState = ExecutingState.IDLE;
        currentState = playerIdleState;
        currentState.EnterState(this);
    }

    void Update()
    {
        currentState.UpdateState(this);
    }

    public void MovePlayer()
    {
        if(Input.GetMouseButtonDown(0))
        {
            ray = _playerCam.ScreenPointToRay(Input.mousePosition);

            if(Physics.Raycast(ray, out hit, 100))
            {
                spawnable = hit.collider.GetComponent<ISpawnable>();
                placeable = hit.collider.GetComponent<IPlaceable>();
                edible = hit.collider.GetComponent<EdibleBase>();

                executingState = ExecutingState.WALK;

                distance = Vector3.Distance(Agent.transform.position, hit.point);
                if(distance > 3.0f)
                    Agent.SetDestination(hit.point);

                Debug.Log(hit.collider.gameObject.name);
            }
        }
    }

    #region Selectables'Methods
    public void GetFoodFromSource()
    {
        if(spawnable != null)
        {
            if (isHolded)   return;

            spawnable?.Spawn();
            EventManager.OnFoodHolded.Invoke();
            currentFood = holdParent.transform.GetChild(0)? holdParent.transform.GetChild(0).gameObject.GetComponent<EdibleBase>(): null;
            isHolded = true;
        }
    }
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

        if(currentFood == null || !ingredient.isLastPiece) return;

        EventManager.OnFoodHolded.Invoke();

        currentFood.transform.parent = holdParent.transform;
        currentFood.transform.localRotation = Quaternion.identity;
        currentFood.transform.position = holdParent.transform.position;

        ingredient.RemoveFromList();

        isHolded = true;
    }
    void DropObject(IPlaceable place)
    {
        if(isHolded)
        {
            if(currentFood == null) return;

            place?.UseFood(currentFood);
            EventManager.OnFoodDropped.Invoke();

            currentFood = null;

            isHolded = false;
            Debug.Log("dropped");
        }
    }
    #endregion

    public void DoneWithPath()
    {
        if(Agent.remainingDistance <= Agent.stoppingDistance)
        {
            executingState = ExecutingState.IDLE;
        }
    }

    public void SwitchState(PlayerStates nextState)
    {
        currentState = nextState;
        currentState.EnterState(this);
    }
}
