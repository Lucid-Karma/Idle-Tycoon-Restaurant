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
    ISelectable selectable;

    Bin bin;

    // Serving with a finished burger in hand: a tap within this fraction of the screen height of a customer,
    // or within serveSnapRadius (world units) of a free service point on their chair/table, serves there.
    [SerializeField] private float customerTapRadius = 0.09f;
    [SerializeField] private float serveSnapRadius = 2.2f;
    private ServiceBase[] services;
    private Vector3 destination;

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

    // The UI check only gates new taps (MovePlayer), so the chef keeps walking while the pointer is over UI.
    void Update()
    {
        currentState.UpdateState(this);
    }

    public void MovePlayer()
    {
        if(Input.GetMouseButtonDown(0) && !PointerUtility.IsOverUI())
            HandleScreenTap(Input.mousePosition);
    }

    public void HandleScreenTap(Vector3 screenPosition)
    {
        ray = _playerCam.ScreenPointToRay(screenPosition);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit tapHit, 100);

        // An interactive object under the finger always wins; otherwise a tap near a customer serves them.
        if (!(hitSomething && IsInteractive(tapHit.collider)) && TryServeCustomerAt(screenPosition)) return;
        if (!hitSomething) return;

        hit = tapHit;
        GatherInteractableComponents();
        SnapToNearbyService();
        GoToTarget();
    }

    private static bool IsInteractive(Collider col) =>
        col.GetComponent<PlaceableBase>() != null || col.GetComponent<ISpawnable>() != null ||
        col.GetComponent<EdibleBase>() != null || col.GetComponent<Bin>() != null;

    // Customers have no collider (a tap on one hits nothing), so with a burger in hand a tap close to a
    // customer on screen serves them: the chef walks to that customer's service point.
    private bool TryServeCustomerAt(Vector3 screenPosition)
    {
        if (!isHolded || currentFood is not Hamburger) return false;

        ServiceBase target = null;
        float nearest = Screen.height * customerTapRadius;
        foreach (var npc in FindObjectsByType<NpcFsm>(FindObjectsSortMode.None))
        {
            var state = npc.executingNpcState;
            if (npc.chair == null || (state != ExecutingNpcState.COME && state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT))
                continue;
            if (npc.chair.GetTableService() is not ServiceBase service || !service.IsSuitable(currentFood))
                continue;

            Vector3 onScreen = _playerCam.WorldToScreenPoint(npc.transform.position + Vector3.up);
            float distanceOnScreen = Vector2.Distance(onScreen, screenPosition);
            if (distanceOnScreen < nearest)
            {
                nearest = distanceOnScreen;
                target = service;
            }
        }
        if (target == null) return false;

        spawnable = null;
        edible = null;
        bin = null;
        placeable = target;
        selectable = target;
        destination = target.transform.position;
        GoToTarget();
        return true;
    }

    private void GoToTarget()
    {
        SelectObject();
        UpdateStoppingDistance();

        distance = Vector3.Distance(Agent.transform.position, destination);
        if (distance > 3.0f)
        {
            executingState = ExecutingState.RUN;
            Agent.SetDestination(destination);
        }
        else
            Interact();
    }

    private void GatherInteractableComponents()
    {
        spawnable = hit.collider.GetComponent<ISpawnable>();
        placeable = hit.collider.GetComponent<PlaceableBase>();
        edible = hit.collider.GetComponent<EdibleBase>();
        selectable = hit.collider.GetComponent<ISelectable>();
        bin = hit.collider.GetComponent<Bin>();
        destination = hit.point;
    }

    // The service point is a small white spot; with a burger in hand, a tap on anything non-interactive
    // near a free one (the chair or table) serves there.
    private void SnapToNearbyService()
    {
        if (!isHolded || currentFood is not Hamburger) return;
        if (placeable != null || spawnable != null || edible != null || bin != null) return;

        services ??= FindObjectsByType<ServiceBase>(FindObjectsSortMode.None);
        ServiceBase nearest = null;
        float nearestDistance = serveSnapRadius;
        foreach (var service in services)
        {
            if (!service.IsSuitable(currentFood)) continue;
            Vector3 offset = service.transform.position - hit.point;
            offset.y = 0f;
            if (offset.magnitude < nearestDistance)
            {
                nearestDistance = offset.magnitude;
                nearest = service;
            }
        }
        if (nearest == null) return;

        placeable = nearest;
        selectable = nearest;
        destination = nearest.transform.position;
    }

    #region Selectables'Methods

    public void Interact()
    {
        GetFoodFromSource();
        GetFood();
        PlaceFood();
        TrashEdible();
        RotateToSelectable();
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

        //Debug.Log("name: " + currentFood.gameObject.name);
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
        if(!Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance)
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
