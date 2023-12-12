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
    IEdible edible;

    #region Parameters
    Camera _playerCam;
    Ray ray;
    private RaycastHit hit;

    [SerializeField] private GameObject holdParent;
    private bool isHolded;
    private GameObject currentFood;
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
                edible = hit.collider.GetComponent<SelectableBase>();


                executingState = ExecutingState.WALK;

                distance = Vector3.Distance(Agent.transform.position, hit.point);
                if(distance > 2.0f)
                    Agent.SetDestination(hit.point);
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
            currentFood = holdParent.transform.GetChild(0)? holdParent.transform.GetChild(0).gameObject: null;
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


    void PickUpObject(IEdible ingredient) 
    {
        if (isHolded)   return;

        currentFood = ingredient.SetFood(); 

        if(currentFood == null) return;

        currentFood.transform.parent = holdParent.transform;
        currentFood.transform.localRotation = Quaternion.identity;
        currentFood.transform.position = holdParent.transform.position;

        isHolded = true;
        Debug.Log("food picked up");
    }
    void DropObject(IPlaceable place)
    {
        if(!isHolded)   return;
        if(currentFood == null) return;

        currentFood.transform.parent = place.parentTransform;
        currentFood.transform.position = place.parentTransform.position;
    
        currentFood = null;

        isHolded = false;
        Debug.Log("food dropped");
    }
    #endregion
    

    // void NotifyObjectClicked(GameObject clickedObject) 
    // {
    //     IngredientsBase food = clickedObject.GetComponent<IngredientsBase>();
    //     if (food != null) 
    //     {
    //         food.OnPlayerInteract(); // Notify the food object about player interaction
    //     } 
    // }

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
