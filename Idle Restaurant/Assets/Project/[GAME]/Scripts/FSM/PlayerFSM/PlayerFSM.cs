using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public enum ExecutingState
{
    IDLE,
    RUN,
    INTERACT
}
public class PlayerFSM : MonoBehaviour
{
    #region FSM
    public ExecutingState executingState;
    public PlayerStates currentState;

    public PlayerIdleState playerIdleState = new();
    public PlayerRunState playerRunState = new();
    public PlayerInteractState playerInteractState = new();
    #endregion

    #region Events
    [HideInInspector]
    public UnityEvent OnPlayerIdle = new UnityEvent();
    [HideInInspector]
    public UnityEvent OnPlayerRun = new UnityEvent();
    [HideInInspector]
    public UnityEvent OnPlayerInteract = new UnityEvent();
    #endregion

    #region Components
    private NavMeshAgent agent;
    public NavMeshAgent Agent{ get { return (agent == null) ? agent = GetComponent<UnityEngine.AI.NavMeshAgent>() : agent; } }
    
    private ParticleSystem _particleSystem;
    public ParticleSystem ParticleSystem{ get { return (_particleSystem == null)? _particleSystem = GetComponentInChildren<ParticleSystem>(): _particleSystem;}}
    
    #endregion

    ISpawnable spawnable;
    PlaceableBase placeable;
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
        EventManager.OnLevelStart.Invoke();
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
                placeable = hit.collider.GetComponent<PlaceableBase>();
                edible = hit.collider.GetComponent<EdibleBase>();

                UpdateStoppingDistance();

                executingState = ExecutingState.RUN;

                distance = Vector3.Distance(Agent.transform.position, hit.point);
                if(distance > 3.0f)
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
        if(ingredient.untouchable)  return;

        currentFood = ingredient;

        if(currentFood == null || !ingredient.isLastPiece) return;

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
            // Add a new line to choose idle or interact after adding the anim. event to the interact anim.
            executingState = ExecutingState.IDLE;
        }
    }

    private void UpdateStoppingDistance()  
    {
        if(placeable != null)
        {
            if(placeable.gameObject.GetComponent<ServiceBase>() != null)
            {
                Agent.stoppingDistance = 1.34f;
            }
            else 
                Agent.stoppingDistance = 0f;
        }
        else
        {
            Agent.stoppingDistance = 0f;
        }
    }

    public void SwitchState(PlayerStates nextState)
    {
        currentState = nextState;
        currentState.EnterState(this);
    }
}
