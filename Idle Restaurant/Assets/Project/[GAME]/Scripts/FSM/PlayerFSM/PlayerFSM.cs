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
    // Slipped on a tomato: the animator plays the fall and getting up again.
    [HideInInspector]
    public UnityEvent OnPlayerSlip = new UnityEvent();
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
        baseSpeed = runSpeed;
        Agent.acceleration = Mathf.Max(Agent.acceleration, runAcceleration);
        ApplyPerks();

        executingState = ExecutingState.IDLE;
        currentState = playerIdleState;
        currentState.EnterState(this);

        //path = new NavMeshPath();
        //elapsed = 0.0f;
    }

    #region Cafe upgrades
    private float baseSpeed;
    // His running speed before the coffee (the scene's agent had 3.5: playtesters found him slow). The agent
    // also gets up to speed and stops quicker, so short hops between stations feel snappy.
    [SerializeField] private float runSpeed = 4.6f;
    [SerializeField] private float runAcceleration = 40f;

    private void OnEnable() => CafeShop.Changed += OnCafeChanged;
    private void OnDisable() => CafeShop.Changed -= OnCafeChanged;

    // A new table brings service points of its own; the coffee makes the chef quicker.
    private void OnCafeChanged(Upgrade upgrade)
    {
        services = null;
        reachSources = null;
        reachOvens = null;
        reachPans = null;
        reachBoards = null;
        reachPlates = null;
        reachBins = null;
        reachBlocked.Clear();
        reachOf.Clear();
        ApplyPerks();
    }

    private void ApplyPerks()
    {
        if (baseSpeed > 0f) Agent.speed = baseSpeed * CafeShop.ChefSpeedScale;
    }
    #endregion

    // The UI check only gates new taps (MovePlayer), so the chef keeps walking while the pointer is over UI.
    void Update()
    {
        Steer();
        UseWhatHeWalksUpTo();
        currentState.UpdateState(this);
    }

    public void MovePlayer()
    {
        // On a touch screen the stick sends taps itself, when the finger lifts without having dragged.
        if (ChefStick.TouchDriven) return;
        if(Input.GetMouseButtonDown(0) && !PointerUtility.IsOverUI())
            HandleScreenTap(Input.mousePosition);
    }

    #region Steering by hand
    // WASD / arrow keys, or the invisible joystick on a phone (ChefStick). Steering forgets whatever a tap was
    // sending him to: he goes where he is pointed, and nothing is used when he stops. Up on the stick is up the
    // screen. Taps still work as before, in between.
    private ChefStick stick;
    public bool Steering { get; private set; }
    [SerializeField] private float steerTurnSpeed = 14f;

    private void Steer()
    {
        if (stick == null && (stick = GetComponent<ChefStick>()) == null) return;
        Vector2 move = stick.Move;
        bool wanted = move.sqrMagnitude > 0.0025f && stick.Playing && !IsStunned
                      && executingState != ExecutingState.INTERACT && Time.timeScale > 0f;
        if (wanted)
        {
            if (!Steering)
            {
                Steering = true;
                ForgetTarget();
            }
            var camera = _playerCam.transform;
            var ahead = Vector3.ProjectOnPlane(camera.forward, Vector3.up);
            if (ahead.sqrMagnitude < 0.01f) ahead = Vector3.ProjectOnPlane(camera.up, Vector3.up);
            ahead.Normalize();
            var right = Vector3.Cross(Vector3.up, ahead);
            var heading = right * move.x + ahead * move.y;
            steerHeading = heading;

            Agent.velocity = heading * Agent.speed;
            if (heading.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(heading), Time.deltaTime * steerTurnSpeed);
            executingState = ExecutingState.RUN;
        }
        else if (Steering)
        {
            Steering = false;
            Agent.velocity = Vector3.zero;
            if (executingState == ExecutingState.RUN) executingState = ExecutingState.IDLE;
        }
    }

    private void ForgetTarget()
    {
        spawnable = null;
        placeable = null;
        edible = null;
        selectable = null;
        bin = null;
        approachTarget = null;
        walkTarget = null;
        hasApproach = false;
        if (Agent.isOnNavMesh) Agent.ResetPath();
    }
    #endregion

    #region Using what he walks up to
    // Moving by hand (WASD / the stick), walking up to something that is useful right now uses it, like a tap
    // on it would: no need to tap (the "arcade idle" feel of games like Burger Life). Taps work as before, and
    // a tap's walk is never second-guessed. What counts, by what is in his hands:
    //   empty   - an ingredient crate; a baked bun, a fried (or burnt) patty, chopped veg; a finished burger;
    //   food    - a raw bun to a free oven, a raw patty to a free pan, whole veg/cheese to a free board, anything
    //             onto a plate (a whole cheese or a burnt bun too: mess is part of the game), anything into the bin;
    //   burger  - the table spot of a customer waiting for one, an empty plate, the bin.
    // (The bin and unprepared food on plates were left out at first, so nothing could go to waste by accident;
    // the user wanted both, and "steering into it" already keeps them deliberate.) He has to be close - as close as he can get to
    // it, plus a little (some crates sit deep on the counter: ReachOf) - and be steering INTO it, for a moment
    // (ReachDwell): walking along the counter past the crates uses none of them, and standing still uses nothing
    // (a first version also used what he faced after stopping, and he grabbed crates he had only stopped near).
    // The same use then waits until he has stepped away from that thing, so pushing on into a crate gives one
    // bun, not one a frame - but the next, different use of it is fine: veg put on the board can be taken off
    // again once it is chopped, without walking away first. (Blocking everything around him stopped "take a
    // tomato, turn to the board beside the crate" - the two are neighbours in this kitchen.)
    private const float ReachNear = 0.8f, ReachSlack = 0.35f, ReachTable = 1.45f, ReachDwell = 0.2f;
    private Vector3 steerHeading;
    private readonly Dictionary<Component, float> reachOf = new();
    private readonly Dictionary<Collider, Component> reachBlocked = new();   // the use -> the thing it was on
    private readonly List<Collider> reachDrop = new();
    private Component reachCandidate;
    private float reachSince;
    private IngredientsSource[] reachSources;
    private Oven[] reachOvens;
    private Pan[] reachPans;
    private ChoppingBoard[] reachBoards;
    private Plate[] reachPlates;
    private Bin[] reachBins;

    private void UseWhatHeWalksUpTo()
    {
        if (!Steering || IsStunned || walkTarget != null || Time.timeScale == 0f || stick == null || !stick.Playing)
        {
            reachCandidate = null;
            return;
        }
        reachSources ??= FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None);
        reachOvens ??= FindObjectsByType<Oven>(FindObjectsSortMode.None);
        reachPans ??= FindObjectsByType<Pan>(FindObjectsSortMode.None);
        reachBoards ??= FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None);
        reachPlates ??= FindObjectsByType<Plate>(FindObjectsSortMode.None);
        reachBins ??= FindObjectsByType<Bin>(FindObjectsSortMode.None);
        services ??= FindObjectsByType<ServiceBase>(FindObjectsSortMode.None);

        // Things he has stepped away from can be used again.
        reachDrop.Clear();
        foreach (var pair in reachBlocked)
        {
            var place = pair.Value;
            if (pair.Key == null || place == null || !place.gameObject.activeInHierarchy || ReachDistance(place) > ReachOf(place) + 0.6f)
                reachDrop.Add(pair.Key);
        }
        foreach (var c in reachDrop) reachBlocked.Remove(c);

        Component bestPlace = null;
        Collider bestUse = null;
        float best = float.PositiveInfinity;
        void Consider(Component place, Collider use, float reach)
        {
            if (place == null || use == null || !place.gameObject.activeInHierarchy || reachBlocked.ContainsKey(use)) return;
            float d = ReachDistance(place);
            if (d > Mathf.Max(reach, ReachOf(place)) || !Meant(place, out float aim, out bool square)) return;
            // The thing he stands squarely in front of wins over one he only sees past the corner of, even if that
            // one is nearer: the crates sit side by side, some deeper on the counter than others (the patty crate
            // behind the cheese crate's corner could not be had at all), and the keyboard only has 8 directions.
            float cost = d * 0.5f + (square ? 0f : 1.2f) + (1f - aim);
            if (cost >= best) return;
            best = cost;
            bestPlace = place;
            bestUse = use;
        }

        var held = HeldFood;
        if (held == null)
        {
            foreach (var source in reachSources) Consider(source, source.GetComponent<Collider>(), ReachNear);
            foreach (var oven in reachOvens)
                if (oven.Food is Bun bun && bun.Preparation != Prep.Raw) Consider(OvenBody(oven), bun.collider, ReachNear);
            foreach (var pan in reachPans)
                if (pan.Food is Burger patty && patty.Preparation != Prep.Raw) Consider(pan, patty.collider, ReachNear);
            foreach (var board in reachBoards)
                if (board.Food is CuttableBase sliced && sliced.isSliced) Consider(board, sliced.collider, ReachNear);
            foreach (var plate in reachPlates)
                if (plate.FinishedBurger is { } burger) Consider(plate, burger.collider, ReachNear);
            // Walking into a plate that has everything an order needs finishes the burger (only then: a half-built
            // one is never closed just by passing it).
            foreach (var plate in reachPlates)
                if (plate.CoversAnOrder) Consider(plate, plate.GetComponent<Collider>(), ReachNear);
        }
        else if (held is Hamburger)
        {
            foreach (var service in services)
                if (service.IsSuitable(held) && WaitedFor(service)) Consider(service, service.GetComponent<Collider>(), ReachTable);
            foreach (var plate in reachPlates) if (plate.Accepts(held)) Consider(plate, plate.GetComponent<Collider>(), ReachNear);
            foreach (var bin in reachBins) Consider(bin, bin.GetComponent<Collider>(), ReachNear);
        }
        else
        {
            bool raw = held.Preparation == Prep.Raw || held.Preparation == Prep.Whole;
            if (held is Bun && raw)
                foreach (var oven in reachOvens) if (!oven.IsHaveFood()) Consider(OvenBody(oven), oven.GetComponent<Collider>(), ReachNear);
            if (held is Burger && raw)
                foreach (var pan in reachPans) if (!pan.IsHaveFood()) Consider(pan, pan.GetComponent<Collider>(), ReachNear);
            if (held is CuttableBase cut && !cut.isSliced)
                foreach (var board in reachBoards) if (!board.IsHaveFood()) Consider(board, board.GetComponent<Collider>(), ReachNear);
            foreach (var plate in reachPlates) if (plate.Accepts(held)) Consider(plate, plate.GetComponent<Collider>(), ReachNear);
            foreach (var bin in reachBins) Consider(bin, bin.GetComponent<Collider>(), ReachNear);
        }

        if (bestPlace != reachCandidate)
        {
            reachCandidate = bestPlace;
            reachSince = Time.time;
        }
        if (bestPlace == null || Time.time - reachSince < ReachDwell) return;

        // Exactly what a tap on it does once he is beside it.
        GatherInteractableComponents(bestUse, bestUse.bounds.center);
        PreferWaitingCustomer();
        SelectObject();
        if (selectable is Component used)
        {
            LastTapped = used;
            LastTappedAt = Time.unscaledTime;
        }
        Interact();
        spawnable = null;
        placeable = null;
        edible = null;
        selectable = null;
        bin = null;
        approachTarget = null;
        walkTarget = null;
        reachCandidate = null;

        reachBlocked[bestUse] = bestPlace;
    }

    // The oven tray is tapped through the whole oven (TapProxy): the oven body is what he walks up to.
    private static Component OvenBody(Oven oven)
    {
        foreach (var proxy in TapProxy.All)
            if (proxy != null && proxy.Target == oven) return proxy;
        return oven;
    }

    private float ReachDistance(Component c) =>
        TryBounds(c, out var bounds) ? FlatDistance(Agent.transform.position, bounds) : float.PositiveInfinity;

    // The way he is being steered (not the way he happens to slide along a counter) points into it, within about
    // 50 degrees of its nearest side. `aim` is how squarely (1 = dead ahead); `square` is whether he stands in
    // front of one of its sides rather than off one of its corners.
    private bool Meant(Component c, out float aim, out bool square)
    {
        aim = 0f;
        square = false;
        if (!TryBounds(c, out var bounds) || steerHeading.sqrMagnitude < 0.0001f) return false;
        var me = Agent.transform.position;
        var to = bounds.ClosestPoint(new Vector3(me.x, bounds.center.y, me.z)) - me;
        to.y = 0f;
        square = (me.x >= bounds.min.x && me.x <= bounds.max.x) || (me.z >= bounds.min.z && me.z <= bounds.max.z);
        if (to.sqrMagnitude < 0.0025f) { aim = 1f; return true; }   // already touching it
        aim = Vector3.Dot(steerHeading.normalized, to.normalized);
        return aim > 0.64f;
    }

    // How close he can get to it, plus a little: like a tap's approach (FindApproach), the floor around it that he
    // can actually walk to (the floor nearest its middle can be behind or under the counter). Things don't move,
    // so it is worked out once each.
    private float ReachOf(Component c)
    {
        if (reachOf.TryGetValue(c, out float reach)) return reach;
        reach = ReachNear;
        if (TryBounds(c, out var bounds))
        {
            probePath ??= new NavMeshPath();
            var filter = new NavMeshQueryFilter { agentTypeID = Agent.agentTypeID, areaMask = Agent.areaMask };
            Vector3 from = Agent.transform.position;
            float ring = Mathf.Max(bounds.extents.x, bounds.extents.z) + Agent.radius + 0.1f;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < ApproachProbes; i++)
            {
                float angle = i * Mathf.PI * 2f / ApproachProbes;
                var probe = new Vector3(bounds.center.x + Mathf.Cos(angle) * ring, from.y, bounds.center.z + Mathf.Sin(angle) * ring);
                if (!NavMesh.SamplePosition(probe, out var hit, 2.5f, filter)) continue;
                if (!NavMesh.CalculatePath(from, hit.position, filter, probePath) || probePath.status != NavMeshPathStatus.PathComplete) continue;
                nearest = Mathf.Min(nearest, FlatDistance(hit.position, bounds));
            }
            if (!float.IsPositiveInfinity(nearest)) reach = Mathf.Max(ReachNear, nearest + ReachSlack);
        }
        reachOf[c] = reach;
        return reach;
    }
    #endregion

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
        PreferWaitingCustomer();
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
            // Not in the first lesson: a new player "giving the tomato to the customer" got one back in the face.
            if (Tutorial.Running)
            {
                Tutorial.Say("They want a <b>burger</b>!");
                return true;
            }
            ThrowAt(picked);
            return true;
        }

        spawnable = null;
        edible = null;
        bin = null;
        placeable = target;
        selectable = target;
        approachTarget = target;
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

        // Something to use: walk to the reachable spot beside it with the shortest walk, or use it from here
        // if that's already as close. Just the floor: walk there unless it's a step away.
        bool approach = FindApproach(approachTarget, out var spot);
        if (approach) destination = spot;
        distance = Vector3.Distance(Agent.transform.position, destination);
        if (approach ? !ReachedApproach() : distance > 3.0f)
        {
            executingState = ExecutingState.RUN;
            Agent.SetDestination(destination);
        }
        else
            Interact();
    }

    #region Approach
    // Where to stand to use the tapped thing. It used to be the walkable point nearest to where the finger
    // landed, which for a plate in the middle of the island could be on its far side: the chef walked all
    // the way round the table instead of using the plate right in front of him. Now: probe spots around it,
    // keep the one with the shortest walk (never on a seated customer), and stop as soon as he is that close
    // to it (DoneWithPath), even if the path would go on. Only on a tap: a few navmesh queries.
    [SerializeField] private float approachSlack = 0.25f;
    private const int ApproachProbes = 12;
    private Component approachTarget;
    private bool hasApproach;
    private Bounds approachBounds;
    private float approachReach;
    private NavMeshPath probePath;   // made on first use (not in a field initializer: Unity can't make it there)
    private static readonly Vector3[] corners = new Vector3[32];

    private bool FindApproach(Component target, out Vector3 spot)
    {
        spot = default;
        hasApproach = false;
        if (target == null || !TryBounds(target, out var bounds)) return false;

        probePath ??= new NavMeshPath();
        var filter = new NavMeshQueryFilter { agentTypeID = Agent.agentTypeID, areaMask = Agent.areaMask };
        Vector3 from = Agent.transform.position;
        float ring = Mathf.Max(bounds.extents.x, bounds.extents.z) + Agent.radius + 0.1f;
        float best = float.PositiveInfinity;
        for (int i = 0; i < ApproachProbes; i++)
        {
            float angle = i * Mathf.PI * 2f / ApproachProbes;
            var probe = new Vector3(bounds.center.x + Mathf.Cos(angle) * ring, from.y, bounds.center.z + Mathf.Sin(angle) * ring);
            if (!NavMesh.SamplePosition(probe, out var hit, 2.5f, filter) || NearCustomer(hit.position)) continue;
            if (!NavMesh.CalculatePath(from, hit.position, filter, probePath) || probePath.status != NavMeshPathStatus.PathComplete) continue;
            // The shortest walk wins; among similar ones, the spot closer to the thing.
            float cost = PathLength(probePath) + 0.5f * FlatDistance(hit.position, bounds);
            if (cost < best)
            {
                best = cost;
                spot = hit.position;
            }
        }
        if (float.IsPositiveInfinity(best)) return false;

        approachBounds = bounds;
        approachReach = FlatDistance(spot, bounds) + approachSlack;
        hasApproach = true;
        return true;
    }

    public bool ReachedApproach() => hasApproach && FlatDistance(Agent.transform.position, approachBounds) <= approachReach;

    // Colliders of utensils are switched off while not usable, and a disabled collider has empty bounds.
    private static bool TryBounds(Component target, out Bounds bounds)
    {
        var col = target as Collider ?? target.GetComponent<Collider>();
        if (col != null && col.enabled)
        {
            bounds = col.bounds;
            return true;
        }
        var shape = target.GetComponentInChildren<Renderer>();
        bounds = shape != null ? shape.bounds : default;
        return shape != null;
    }

    private static float FlatDistance(Vector3 point, Bounds bounds)
    {
        var closest = bounds.ClosestPoint(new Vector3(point.x, bounds.center.y, point.z));
        return Vector2.Distance(new Vector2(point.x, point.z), new Vector2(closest.x, closest.z));
    }

    private static float PathLength(NavMeshPath path)
    {
        int n = path.GetCornersNonAlloc(corners);
        float length = 0f;
        for (int i = 1; i < n; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
        return length;
    }

    private static bool NearCustomer(Vector3 point)
    {
        foreach (var npc in NpcFsm.Active)
        {
            var offset = npc.transform.position - point;
            if (offset.x * offset.x + offset.z * offset.z < 0.8f) return true;
        }
        return false;
    }
    #endregion

    // `col` may be null (e.g. a full oven tapped with food in hand): then it's a plain walk to the point.
    private void GatherInteractableComponents(Collider col, Vector3 point)
    {
        spawnable = col != null ? col.GetComponent<ISpawnable>() : null;
        placeable = col != null ? col.GetComponent<PlaceableBase>() : null;
        edible = col != null ? col.GetComponent<EdibleBase>() : null;
        selectable = col != null ? col.GetComponent<ISelectable>() : null;
        bin = col != null ? col.GetComponent<Bin>() : null;
        approachTarget = col != null && IsInteractive(col) ? col : null;   // the floor is just walked to
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

    // Hit by a customer's tomato or arrow with empty hands: frozen on the spot for a moment, then he carries
    // on to where he was going. (While he carries something a hit never stops him: FoodFight.)
    public void Splat(float seconds)
    {
        stunnedUntil = Time.time + seconds;
        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
        // He stops dead: so do his legs (it used to keep playing the run on the spot until he moved on).
        if (executingState == ExecutingState.RUN && !slipped)
        {
            splatted = true;
            OnPlayerIdle.Invoke();
        }
        CancelInvoke(nameof(Recover));
        Invoke(nameof(Recover), seconds);
    }

    private bool splatted;

    // Only a running chef can slip (never mid-interaction: that animation ends the interaction).
    public bool IsRunning => executingState == ExecutingState.RUN && Agent.velocity.sqrMagnitude > 0.25f;

    private bool slipped;

    // Hit by a tomato while running: he slips on it, lands on his back and gets up again, still holding
    // whatever he was carrying.
    public void Slip(float seconds)
    {
        stunnedUntil = Time.time + seconds;
        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
        slipped = true;
        OnPlayerSlip.Invoke();
        CancelInvoke(nameof(Recover));
        Invoke(nameof(Recover), seconds);
    }

    private void Recover()
    {
        Agent.isStopped = false;
        if (splatted)
        {
            splatted = false;
            if (executingState == ExecutingState.RUN) OnPlayerRun.Invoke();
        }
        if (!slipped) return;
        slipped = false;
        // The animator was busy falling: back to running (if he still has somewhere to be) or standing.
        if (executingState == ExecutingState.RUN) OnPlayerRun.Invoke();
        else OnPlayerIdle.Invoke();
    }
    #endregion

    // The service point is a small white spot on the table; with a burger in hand, a tap on anything
    // non-interactive near a free one (the chair or table) serves there.
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
        approachTarget = nearest;
        destination = nearest.transform.position;
    }

    // A table has a spot for each chair: a burger taken to a table goes to the spot of the customer who is
    // waiting at it, not to whichever empty spot the finger happened to be nearest.
    private const float SameTable = 1.8f;

    private void PreferWaitingCustomer()
    {
        if (!isHolded || currentFood is not Hamburger || placeable is not ServiceBase tapped) return;
        if (WaitedFor(tapped)) return;

        ServiceBase best = null;
        float bestDistance = SameTable;
        foreach (var npc in NpcFsm.Active)
        {
            var state = npc.executingNpcState;
            if (state != ExecutingNpcState.COME && state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT) continue;
            if (npc.chair?.GetTableService() is not ServiceBase service || !service.IsSuitable(currentFood)) continue;
            Vector3 offset = service.transform.position - tapped.transform.position;
            offset.y = 0f;
            if (offset.magnitude < bestDistance)
            {
                bestDistance = offset.magnitude;
                best = service;
            }
        }
        if (best == null) return;
        placeable = best;
        selectable = best;
        approachTarget = best;
        destination = best.transform.position;
    }

    private static bool WaitedFor(ServiceBase service)
    {
        foreach (var npc in NpcFsm.Active)
        {
            var state = npc.executingNpcState;
            if ((state == ExecutingNpcState.COME || state == ExecutingNpcState.ORDER || state == ExecutingNpcState.WAIT)
                && npc.chair?.GetTableService() == (NonStackBase)service) return true;
        }
        return false;
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
            if (!isHolded && placeable is Plate plate) { CloseBurger(plate); return; }
            DropObject(placeable);
        }
    }

    // Empty hands on a plate with layers on it: "done!" - the top bun goes on, and he picks the burger up to serve it,
    // unless Mochi is free to run it out (then it waits on the plate for him).
    private void CloseBurger(Plate plate)
    {
        if (plate.HasHamburger || plate.LayerCount == 0) return;
        if (!plate.Close()) return;
        if (Tutorial.Running || !Waiter.Free) StartCoroutine(PickUpNextFrame(plate, plate.FinishedBurger));
    }

    // A burger made this frame has not run its Start yet (its collider and "can be picked up" are set there): take
    // it a frame later - unless something else has happened to it meanwhile.
    private System.Collections.IEnumerator PickUpNextFrame(Plate plate, Hamburger burger)
    {
        yield return null;
        if (burger == null || isHolded || plate.FinishedBurger != burger) yield break;
        burger.isLastPiece = true;
        PickUpObject(burger);
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
            GameSfx.Play(GameSfx.Cue.PutDown);

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
                GameSfx.Play(GameSfx.Cue.Trash);
                currentFood = null;
                isHolded = false;
            }
        }
    }
    #endregion

    public void DoneWithPath()
    {
        if (Steering || !Agent.isOnNavMesh || Agent.pathPending) return;
        if (Agent.remainingDistance <= Agent.stoppingDistance)
            executingState = ExecutingState.IDLE;
        else if (ReachedApproach())
        {
            // Close enough to use it: stop here rather than finishing a path that bends round it.
            Agent.ResetPath();
            executingState = ExecutingState.IDLE;
        }
    }

    private void UpdateStoppingDistance()  
    {
        if (approachTarget != null)
            Agent.stoppingDistance = 0f;   // he walks to a spot beside it (FindApproach)
        else if(placeable != null)
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
