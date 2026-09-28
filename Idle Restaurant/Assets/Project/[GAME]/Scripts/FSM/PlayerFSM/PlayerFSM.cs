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

    // A tap within this fraction of the screen height of a customer picks them (serve a burger / throw the
    // held ingredient); with a burger, a tap within serveSnapRadius (world units) of a free service point
    // on their chair/table also serves there.
    [SerializeField] private float customerTapRadius = 0.09f;
    [SerializeField] private float serveSnapRadius = 2.2f;
    private ServiceBase[] services;
    private Vector3 destination;

    // Read by HighlightController (tap feedback and "where can this go" hints) and CarryWobble.
    public EdibleBase HeldFood => isHolded ? currentFood : null;
    public Transform Hand => holdParent.transform;
    public Component LastTapped { get; private set; }       // selectable the chef was last sent to
    public float LastTappedAt { get; private set; }         // unscaled time of that tap
    public bool IsHeadingTo(Component target) => target != null && target == walkTarget;
    private Component walkTarget;                           // cleared once the chef interacts

    #region Parameters
    Camera _playerCam;
    Ray ray;
    private Vector3 tapPoint;

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
        if (IsStunned) return;

        ray = _playerCam.ScreenPointToRay(screenPosition);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit tapHit, 100);
        // A tap on a TapProxy (the oven body) stands for the utensil or the food inside it.
        Collider tapped = hitSomething ? ResolveTap(tapHit.collider) : null;

        // An interactive object under the finger always wins; otherwise a tap near a customer serves them
        // (burger in hand) or throws them what the chef is holding.
        if (!(tapped != null && IsInteractive(tapped)) && TryCustomerAt(screenPosition)) return;
        if (!hitSomething) return;

        GatherInteractableComponents(tapped, tapHit.point);
        SnapToNearbyService();
        GoToTarget();
    }

    public Collider ResolveTap(Collider col)
    {
        var proxy = col.GetComponent<TapProxy>();
        return proxy != null ? proxy.Resolve(isHolded) : col;
    }

    private static bool IsInteractive(Collider col) =>
        col.GetComponent<PlaceableBase>() != null || col.GetComponent<ISpawnable>() != null ||
        col.GetComponent<EdibleBase>() != null || col.GetComponent<Bin>() != null;

    // Customers have no collider (a tap on one hits nothing), so a tap close to a customer on screen picks
    // them: with a burger in hand the chef walks to their service point; with anything else he throws it
    // to them (a snack if it's prepared, a bonk on the head if it isn't — see NpcFsm.CatchThrown).
    private bool TryCustomerAt(Vector3 screenPosition)
    {
        if (!isHolded || currentFood == null) return false;
        bool serving = currentFood is Hamburger;

        NpcFsm picked = null;
        ServiceBase target = null;
        float nearest = Screen.height * customerTapRadius;
        foreach (var npc in NpcFsm.Active)
        {
            var state = npc.executingNpcState;
            if (npc.chair == null) continue;
            ServiceBase service = null;
            if (serving)
            {
                if (state != ExecutingNpcState.COME && state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT) continue;
                service = npc.chair.GetTableService() as ServiceBase;
                if (service == null || !service.IsSuitable(currentFood)) continue;
            }
            else if (state != ExecutingNpcState.WAIT) continue;

            Vector3 onScreen = _playerCam.WorldToScreenPoint(npc.transform.position + Vector3.up);
            float distanceOnScreen = Vector2.Distance(onScreen, screenPosition);
            if (distanceOnScreen < nearest)
            {
                nearest = distanceOnScreen;
                picked = npc;
                target = service;
            }
        }
        if (picked == null) return false;

        if (!serving)
        {
            ThrowAt(picked);
            return true;
        }

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

        walkTarget = selectable as Component;
        if (walkTarget != null)
        {
            LastTapped = walkTarget;
            LastTappedAt = Time.unscaledTime;
        }

        distance = Vector3.Distance(Agent.transform.position, destination);
        if (distance > 3.0f)
        {
            executingState = ExecutingState.RUN;
            Agent.SetDestination(destination);
        }
        else
            Interact();
    }

    // `col` may be null (e.g. a full oven tapped with food in hand): then it's a plain walk to the point.
    private void GatherInteractableComponents(Collider col, Vector3 point)
    {
        spawnable = col != null ? col.GetComponent<ISpawnable>() : null;
        placeable = col != null ? col.GetComponent<PlaceableBase>() : null;
        edible = col != null ? col.GetComponent<EdibleBase>() : null;
        selectable = col != null ? col.GetComponent<ISelectable>() : null;
        bin = col != null ? col.GetComponent<Bin>() : null;
        tapPoint = point;
        destination = point;
    }

    #region Food fight
    private float stunnedUntil;
    public bool IsStunned => Time.time < stunnedUntil;

    // Throw whatever is in hand to a seated customer; the chef doesn't walk there.
    private void ThrowAt(NpcFsm npc)
    {
        var food = currentFood;
        currentFood = null;
        isHolded = false;
        walkTarget = null;
        EventManager.OnFoodDropped.Invoke();

        var toward = npc.transform.position - transform.position;
        toward.y = 0f;
        if (toward.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toward);
        FoodFight.Instance.ChefThrows(food, npc);
    }

    // Hit by a customer's tomato: frozen on the spot for a moment (he keeps what he's holding and then
    // carries on to where he was going).
    public void Splat(float seconds)
    {
        stunnedUntil = Time.time + seconds;
        Agent.isStopped = true;
        CancelInvoke(nameof(Recover));
        Invoke(nameof(Recover), seconds);
    }

    private void Recover() => Agent.isStopped = false;
    #endregion

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
            Vector3 offset = service.transform.position - tapPoint;
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
        walkTarget = null;
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
