using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

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
    ISelectable selectable;
    ISedile sedile;

    Bin bin;

    #region Parameters
    Camera _playerCam;
    Ray ray;
    private RaycastHit hit;

    [SerializeField] private GameObject holdParent;
    private bool isHolded;
    private EdibleBase currentFood;
    private float distance;

    Vector3 _selectablePos;
    Vector3 direction;
    Quaternion lookRotation;
    [SerializeField] private float rotationSpeed = 50f;
    #endregion

    void Start()
    {
        _playerCam = Camera.main;
        isHolded = false;

        executingState = ExecutingState.IDLE;
        currentState = playerIdleState;
        currentState.EnterState(this);

        //path = new NavMeshPath();
        //elapsed = 0.0f;
    }

    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        currentState.UpdateState(this);
    }
    //public Transform target;
    //private NavMeshPath path;
    //private float elapsed = 0.0f;

    public void MovePlayer()
    {
        if(Input.GetMouseButtonDown(0))
        {
            ray = _playerCam.ScreenPointToRay(Input.mousePosition);

            if(Physics.Raycast(ray, out hit, 100))
            {
                GatherInteractableComponents();

                //target = hit.collider.transform;
                SelectObject();
                UpdateStoppingDistance();

                //executingState = ExecutingState.RUN;

                distance = Vector3.Distance(Agent.transform.position, hit.point);
                if (distance > 3.0f)
                {
                    executingState = ExecutingState.RUN;
                    Agent.SetDestination(hit.point);
                }
                else
                    Interact();

                //elapsed += Time.deltaTime;
                //if (elapsed > 1.0f)
                //{
                //    elapsed -= 1.0f;
                //    NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);
                //}
                //for (int i = 0; i < path.corners.Length - 1; i++)
                //    Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
            }
        }
    }

    private void GatherInteractableComponents()
    {
        spawnable = hit.collider.GetComponent<ISpawnable>();
        placeable = hit.collider.GetComponent<PlaceableBase>();
        edible = hit.collider.GetComponent<EdibleBase>();
        selectable = hit.collider.GetComponent<ISelectable>();
        sedile = hit.collider.GetComponent<ISedile>();
        bin = hit.collider.GetComponent<Bin>();
    }

    #region Selectables'Methods

    public void Interact()
    {
        GetFoodFromSource();
        GetFood();
        PlaceFood();
        TrashEdible();
        RotateToSelectable();
        UpdateSedile();
    }

    public void GetFoodFromSource()
    {
        if(spawnable != null)
        {
            if (isHolded)   return;

            spawnable?.Spawn();
            EventManager.OnFoodHolded.Invoke();
            currentFood = holdParent.transform.GetChild(holdParent.transform.childCount - 1) ? 
                holdParent.transform.GetChild(holdParent.transform.childCount - 1).gameObject.GetComponent<EdibleBase>(): null;
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

        Debug.Log("name: " + currentFood.gameObject.name);
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
    
    private void SelectObject()
    {
        if(selectable != null)
        {
            EventManager.OnClick.Invoke();
        }
    }

    public void UpdateSedile()
    {
        if(sedile != null)
        {
            sedile.UpdateChairState();
        }
    }

    public void TrashEdible()
    {
        if (bin != null)
        {
            if (isHolded)
            {
                if (currentFood == null) return;
                bin.Junk(currentFood);
                currentFood = null;
                isHolded = false;
            }
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

    public void RotateToSelectable()
    {
        if(selectable != null)
        {
            _selectablePos = selectable.SelectablePos();
            direction = (_selectablePos - Agent.transform.position).normalized;
            lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));
            Agent.transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
        
    }

    public void SwitchState(PlayerStates nextState)
    {
        currentState = nextState;
        currentState.EnterState(this);
    }
}
