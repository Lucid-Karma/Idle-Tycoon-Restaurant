using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    // Read by HighlightController (tap feedback and "where can this go" hints) and CarryWobble. The top of what he
    // carries (Carried: everything, bottom first).
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
        // His agent is off in the scene and switched on here: in a build the scene's objects can be enabled before
        // the NavMeshSurface has added its navmesh, and an agent enabled then logs "Failed to create agent because
        // there is no valid NavMesh" (2026-10-17). By Start every OnEnable, the surface's included, has run.
        if (!Agent.enabled) Agent.enabled = true;
        _playerCam = Camera.main;
        isHolded = false;
        baseSpeed = runSpeed;
        Agent.acceleration = Mathf.Max(Agent.acceleration, runAcceleration);
        ApplyPerks();
        LooseDust();
        UITapQueue.Create(this);
        stack = gameObject.AddComponent<CarryStack>();
        var anyPlate = FindFirstObjectByType<Plate>();
        stack.Init(this, holdParent.transform, anyPlate != null ? anyPlate.TopBunPrefab : null);

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

    // The puffs at his feet: little clouds kicked up a few at a time, staying close behind him and fading. They used
    // to live in his own space and swung round with him whenever he turned; left in the world (2026-10-28) they
    // trailed out in long lines - so now they are thrown out in small bunches, carry most of his speed (so they stay
    // near his heels, no further than the old ones went) and die quickly, and a turn no longer swings them.
    private void LooseDust()
    {
        var dust = ParticleSystem;
        if (dust == null) return;
        var main = dust.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.36f);
        var emission = dust.emission;   // a puff every step or so
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 2, 4, -1, 0.17f) });
        var follow = dust.inheritVelocity;
        follow.enabled = true;
        follow.mode = ParticleSystemInheritVelocityMode.Initial;
        follow.curveMultiplier = 0.82f;
        var drift = dust.velocityOverLifetime;   // a little lift as they settle
        drift.enabled = true;
        drift.space = ParticleSystemSimulationSpace.World;
        drift.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        drift.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        drift.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        var fade = dust.colorOverLifetime;
        fade.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
        fade.color = g;
    }

    private void OnEnable()
    {
        CafeShop.Changed += OnCafeChanged;
        KitchenStations.Changed += OnStationsChanged;
    }

    private void OnDisable()
    {
        CafeShop.Changed -= OnCafeChanged;
        KitchenStations.Changed -= OnStationsChanged;
    }

    // A station opened: the crates and cookers he can walk up to are looked for again.
    private void OnStationsChanged() => OnCafeChanged(null);

    // A new table brings service points of its own; the coffee makes the chef quicker.
    private void OnCafeChanged(Upgrade upgrade)
    {
        services = null;
        reachSources = null;
        reachOvens = null;
        reachPans = null;
        reachHeat = null;
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
        // Taps waiting while he stands idle with nothing under way (the last one came to nothing): on with the next.
        if (queue.Count > 0 && !runningQueue && !Busy && executingState == ExecutingState.IDLE && currentState == playerIdleState && !IsStunned && !Steering)
            StartCoroutine(RunNextQueued());
    }

    public void MovePlayer()
    {
        // On a touch screen the stick sends taps itself, when the finger lifts without having dragged.
        if (ChefStick.TouchDriven) return;
        if(Input.GetMouseButtonDown(0) && (!PointerUtility.IsOverUI() || Tutorial.TapsThrough(Input.mousePosition)))
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
        // Down on the floor (or frozen by a splat): steering waits. It used to let go of him at once - "stopped,
        // so idle" - and the Idle trigger cut his fall short: he only stumbled (user, 2026-10-28).
        if (IsStunned) return;
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
        queue.Clear();
        busyAt = null;
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
    private HeatStation[] reachHeat;
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
        reachHeat ??= FindObjectsByType<HeatStation>(FindObjectsSortMode.None);
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
            foreach (var station in reachHeat)
                if (station.Food is CookedBase cooked && cooked.Preparation != Prep.Raw) Consider(station, cooked.collider, ReachNear);
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
            foreach (var station in reachHeat)
                if (station.Accepts(held)) Consider(station, station.GetComponent<Collider>(), ReachNear);
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
        TapFeedback.On(screenPosition, this);   // whatever the tap does next, what it landed on answers
        if (IsStunned) return;
        // The first lesson only takes taps on what its arrow points at (and aims them at it).
        if (!Tutorial.AcceptsTap(screenPosition, out var aim)) return;
        screenPosition = aim;
        // The lesson's planned burger says exactly which thing the tap was for (Tutorial.Planning).
        if (Tutorial.TakePlannedTap(out var planned))
        {
            if (TapWaits) { Add(planned); return; }
            queue.Clear();
            busyAt = null;
            EventManager.OnClick.Invoke();   // (Run itself is quiet: a waiting tap clicked when it was made)
            Run(planned);
            return;
        }
        // On his way to something already: the tap waits its turn (or, on bare floor, calls everything off).
        if (TapWaits && Enqueue(screenPosition)) return;
        queue.Clear();
        busyAt = null;

        ray = _playerCam.ScreenPointToRay(screenPosition);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit tapHit, 100);
        // A tap on a TapProxy (the oven body) stands for the utensil or the food inside it.
        Collider tapped = hitSomething ? ResolveTap(tapHit.collider) : null;

        // An interactive object under the finger always wins; otherwise a tap near a customer serves them
        // (burger in hand) or throws them what the chef is holding; otherwise a tap just beside a crate, a station
        // or a plate means that thing (NearestInteractive).
        Vector3 point = hitSomething ? tapHit.point : Vector3.zero;
        if (!(tapped != null && IsInteractive(tapped)))
        {
            if (TryCustomerAt(screenPosition)) return;
            var near = NearestInteractive(screenPosition);
            if (near != null) { tapped = ResolveTap(near); point = near.bounds.center; hitSomething = true; }
        }
        if (!hitSomething) return;

        GatherInteractableComponents(tapped, point);
        SnapToNearbyService();
        PreferWaitingCustomer();
        GoToTarget();
    }

    // Forgiving taps (2026-10-20): on a phone held sideways the crates, the bin and the plates are 20-30 px across,
    // well under a fingertip (44 px), and a tap that missed one walked the chef to the floor beside it. Now a tap
    // within this distance of something interactive (its box as seen on screen) counts as a tap on it - a share of the
    // screen's height: generous for fingers, small for a mouse pointer.
    private const float TouchSlop = 0.07f, MouseSlop = 0.03f;

    private Collider NearestInteractive(Vector3 screenPosition)
    {
        var cam = _playerCam != null ? _playerCam : Camera.main;
        float best = Screen.height * (ChefStick.TouchDriven ? TouchSlop : MouseSlop);
        Collider found = null;
        foreach (var col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if (!col.enabled || !col.gameObject.activeInHierarchy || col.isTrigger && col.GetComponent<TapProxy>() == null) continue;
            var resolved = ResolveTap(col);
            if (resolved == null || !IsInteractive(resolved)) continue;
            var b = col.bounds;
            if (b.size.magnitude > 6f) continue;   // a statically batched collider can report the whole kitchen
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var p = cam.WorldToScreenPoint(new Vector3(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i < 4 ? b.min.z : b.max.z));
                if (p.z < 0f) continue;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            float dx = Mathf.Max(minX - screenPosition.x, 0f, screenPosition.x - maxX);
            float dy = Mathf.Max(minY - screenPosition.y, 0f, screenPosition.y - maxY);
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            if (d < best) { best = d; found = col; }
        }
        return found;
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
        // Two things or more in his hands are a burger (CarriesBurger): it is served. One is thrown - and so are the
        // fries to whoever ordered them.
        bool serving = CarriesBurger;

        NpcFsm picked = null;
        ServiceBase target = null;
        float nearest = Screen.height * customerTapRadius;
        foreach (var npc in NpcFsm.Active)
        {
            var state = npc.executingNpcState;
            if (npc.chair == null) continue;
            ServiceBase service = null;
            bool theirFries = npc.FriesDue && Carries(f => f is Fries && f.Preparation == Prep.Good);
            if (serving && !theirFries)
            {
                if (state != ExecutingNpcState.COME && state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT) continue;
                service = npc.chair.GetTableService() as ServiceBase;
                if (service == null || !CanServe(service)) continue;
            }
            else if (state != ExecutingNpcState.WAIT && !(npc.FriesDue && Carries(f => f is Fries))) continue;

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

        if (target == null)
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
        // Their fries, if he has some in his stack; otherwise the top one.
        if (npc.FriesDue && Carried.FirstOrDefault(f => f is Fries && f.Preparation == Prep.Good) is EdibleBase fries) BringToTop(fries);
        var food = currentFood;
        walkTarget = null;
        EventManager.OnFoodDropped.Invoke();
        PopTop();

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
    // whatever he was carrying. Hit from behind, he goes down on his face instead (and hops back up).
    public bool SlipsForward { get; private set; }

    public void Slip(float seconds, bool forward = false)
    {
        SlipsForward = forward;
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
        // An oven, a pan, the griddle, the fryer or the board: everything to do there in one visit (UseStation). Steering
        // by hand keeps the old one-thing use, and never takes anything half done.
        var station = StationTapped();
        if (station != null)
        {
            if (!Steering)
            {
                StartCoroutine(UseStation(station));
                return;
            }
            if (edible != null && StillPreparing(edible)) return;
        }
        GetFoodFromSource();
        GetFood();
        PlaceFood();
        TrashEdible();
        RotateToSelectable();
        if (queue.Count > 0 && !runningQueue) StartCoroutine(RunNextQueued());
    }

    #region Tap queue
    // Taps made while he is on his way to something wait their turn (user, 2026-10-28, from the report: the walk made
    // every order slow; "Diner Dash"-style, the player taps the next two or three things and he does them in order).
    // Each tap is kept as what was under the finger - a thing, a customer, a point - and worked out only when its turn
    // comes, with what he holds THEN: tap the finished burger, then its customer, and he takes it to them. A tap on bare
    // floor calls the rest off and walks there; steering by hand calls it off too. Small numbers over the waiting
    // targets show the order (UITapQueue). Not in the first lesson, which takes one tap at a time - except the burger
    // it plans in one go (Tutorial.Planning).
    // A whole burger is one session of taps (user, 2026-10-30): crate, oven, crate, pan, ..., plate, customer - up to
    // MaxQueued waiting at once (it was 3); "take it" on something still cooking waits by it until it is ready
    // (WaitForReady); a station or plate tapped while his hands were empty still counts (Enqueue); and the same plate
    // tapped twice in a row is two steps (the patty onto it, then "done!"), only a double tap is one.
    public const int MaxQueued = 16;

    public struct QueuedTap
    {
        public Collider Thing;     // as hit (a TapProxy is resolved when its turn comes)
        public NpcFsm Customer;    // or a customer (to serve or throw to)
        public Vector3 Point;
    }

    private readonly List<QueuedTap> queue = new();
    private bool runningQueue;
    private float lastQueuedAt;
    public IReadOnlyList<QueuedTap> Queued => queue;

    // Nothing under way and nothing waiting (the lesson's planned burger checks its taps against what he did). In the
    // idle STATE, not just stopped: he stops one frame and uses the thing he walked to the next (PlayerIdleState).
    public bool Idle => !Busy && queue.Count == 0 && !runningQueue && executingState == ExecutingState.IDLE && currentState == playerIdleState;

    // On his way to something (the tapping-ahead tip, Tutorial) and how far he still has to go.
    public bool OnItsWay => Busy;
    public float WalkLeft => Agent.hasPath && !Agent.pathPending ? Agent.remainingDistance : 0f;

    private static bool Queues => !Tutorial.Running || Tutorial.Planning;

    // On the way to something to use or someone to serve (not just walking to a spot on the floor), or working at a
    // station (UseStation).
    private bool Busy => Queues && !Steering && (busyAt != null
        || executingState == ExecutingState.RUN && (approachTarget != null || selectable != null));

    // A new tap waits its turn: he is busy, or about to start the next waiting tap (in the frames between one use and
    // the next a tap used to call the whole queue off).
    private bool TapWaits => Busy || Queues && !Steering && (queue.Count > 0 || runningQueue);

    // True when the tap was taken by the queue (queued, or ignored because it is full or the same as the last one).
    private bool Enqueue(Vector3 screenPosition)
    {
        var tap = new QueuedTap();
        // What the finger is on, as it will be when its turn comes. Stations and plates switch their colliders off
        // while his hands are empty (taps go to the food on them then), so a plate tapped while he walked to a crate
        // was missed - taken for bare floor, which called the whole queue off. Here they all count.
        WakePlaceables(true);
        try
        {
            ray = _playerCam.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100) && Usable(hit.collider)) { tap.Thing = hit.collider; tap.Point = hit.point; }
            else if ((tap.Customer = CustomerNear(screenPosition)) != null) tap.Point = tap.Customer.transform.position;
            else
            {
                var near = NearestInteractive(screenPosition);
                if (near == null) return false;   // bare floor: not queued (the caller walks there, dropping the queue)
                tap.Thing = near;
                tap.Point = near.bounds.center;
            }
        }
        finally { WakePlaceables(false); }
        Add(tap);
        return true;
    }

    // Onto the end of the line - unless it is full. Every tap counts, the same thing twice too (two buns from the crate,
    // the plate and the plate again for "done!"): only one that comes in the same instant as the last (a touch screen
    // reporting one finger twice) is dropped.
    private void Add(QueuedTap tap)
    {
        if (queue.Count >= MaxQueued) return;
        if (queue.Count > 0 && Time.unscaledTime - lastQueuedAt < 0.06f)
        {
            var last = queue[queue.Count - 1];
            if (last.Thing == tap.Thing && last.Customer == tap.Customer) return;
        }
        queue.Add(tap);
        lastQueuedAt = Time.unscaledTime;
        EventManager.OnClick.Invoke();
    }

    private static readonly List<Collider> woken = new();

    private static void WakePlaceables(bool on)
    {
        if (!on)
        {
            foreach (var col in woken) if (col != null) col.enabled = false;
            woken.Clear();
            return;
        }
        foreach (var place in FindObjectsByType<PlaceableBase>(FindObjectsSortMode.None))
        {
            var col = place.GetComponent<Collider>();
            if (col != null && !col.enabled && place.gameObject.activeInHierarchy) { col.enabled = true; woken.Add(col); }
        }
    }

    // With empty hands, a station or a plate (tapped while it was empty, or for later) means what is on it now: the bun
    // in the oven, the patty on the pan, the chopped cheese, the finished burger.
    private Collider OnIt(Collider col)
    {
        if (col == null || isHolded) return col;
        Component on = col.GetComponent<NonStackBase>() is NonStackBase station && station.Food != null ? station.Food
                     : col.GetComponent<Plate>() is Plate plate && plate.FinishedBurger != null ? plate.FinishedBurger : null;
        var its = on != null ? on.GetComponent<Collider>() : null;
        return its != null ? its : col;
    }

    // Still baking, frying (raw on its cooker) or being chopped.
    private static bool StillPreparing(EdibleBase food) =>
        food.Placeable is CookingBase && food.Preparation == Prep.Raw
        || food.Placeable is ChoppingBoard && food is CuttableBase { isSliced: false };
    #endregion

    #region Carrying a stack
    // He carries a stack (user, 2026-10-30: "while we tap one thing after another he runs round collecting the
    // ingredients; tap the oven and the bun in his stack goes in; tap the board and he chops the veg he carries" - the
    // way the arcade cooking games do it; one thing at a time made every burger a walk back and forth). Up to MaxCarry
    // things, the last one taken on top (HeldFood); a finished burger is carried alone.
    //   crate      - one more on the stack;
    //   a cooker   - what is ready on it is taken (he waits for it if it is still cooking), then the first raw thing it
    //                cooks goes in (UseStation);
    //   the board  - everything whole he carries is chopped there, one after another, and back on the stack;
    //   a plate    - the whole stack goes on it, bun first; with empty hands it is "done!" as before;
    //   the bin    - burnt things if he carries any, else the top one;
    //   a customer - a burger is served; anything else thrown is the top one (their fries, if he has some).
    public const int MaxCarry = 12;   // (was 6, like a plate's six layers; user, 2026-10-30: "drop the 6-layer limit")
    private readonly List<EdibleBase> under = new();   // below the top, bottom first
    private readonly List<EdibleBase> carried = new();
    private NonStackBase busyAt;                       // working at a station (UseStation)
    private CarryStack stack;                          // how it moves

    public IReadOnlyList<EdibleBase> Carried
    {
        get
        {
            carried.Clear();
            if (!isHolded || currentFood == null) return carried;
            carried.AddRange(under);
            carried.Add(currentFood);
            return carried;
        }
    }

    public bool Carries(System.Func<EdibleBase, bool> what) => Carried.Any(what);

    // A burger in his hands: a finished one, or two or more things in his stack (fries aside, they are a side) - user,
    // 2026-10-30: "no plate needed to make a burger: more than one thing in his hands is a burger, and more can go on
    // as you like; one thing is thrown". Served by tapping its customer: it becomes a burger on their table
    // (AssembleAt), the bun at the bottom, its top on top, exactly as a plate makes it.
    public bool CarriesBurger => isHolded && (currentFood is Hamburger || BurgerParts() >= 2);

    private int BurgerParts()
    {
        int n = 0;
        foreach (var f in Carried) if (f != null && f is not Fries && f is not Hamburger) n++;
        return n;
    }

    // A customer's table that can take his burger.
    private bool CanServe(ServiceBase table) => !table.IsHaveFood() && CarriesBurger;

    // Something in his stack to leave on this plate (a plate is a place to put things down now).
    private bool CarriesSomethingFor(Plate plate) => Carried.Any(f => f != null && f is not Hamburger && plate.Accepts(f));

    // Everything on a plate back into his hands, top first.
    private void TakeFromPlate(Plate plate)
    {
        int guard = 0;
        while (plate.Top is EdibleBase top && CanTakeMore && guard++ < 16)
        {
            top.isLastPiece = true;
            PickUpObject(top);
            if (plate.Top == top) break;
        }
    }

    // The burger made from his stack on the customer's table (a hidden plate puts it together: the bun at the bottom,
    // the top bun on top, the layers and their review exactly as a plate's burger).
    private Plate assembler;

    private bool AssembleAt(ServiceBase table)
    {
        var parts = Carried.Where(f => f != null && f is not Fries && f is not Hamburger).ToList();
        if (parts.Count < 2 || table.IsHaveFood()) return false;
        if (assembler == null)
        {
            var model = FindFirstObjectByType<Plate>();
            if (model == null) return false;
            var go = Instantiate(model.gameObject);
            go.name = "Burger Assembler";
            go.SetActive(false);
            foreach (var r in go.GetComponents<Renderer>()) r.enabled = false;
            foreach (var c in go.GetComponents<Collider>()) c.enabled = false;
            assembler = go.GetComponent<Plate>();
            assembler.Start();
        }
        assembler.transform.SetPositionAndRotation(table.Slot.position, Quaternion.identity);
        assembler.gameObject.SetActive(true);
        foreach (var food in parts)
        {
            BringToTop(food);
            assembler.UseFood(food);
            EventManager.OnFoodDropped.Invoke();
            PopTop();
        }
        assembler.Close();
        var burger = assembler.FinishedBurger;
        if (burger != null)
        {
            NpcFsm theirs = null;
            foreach (var npc in NpcFsm.Active)
                if (npc.chair != null && (Object)npc.chair.GetTableService() == table) theirs = npc;
            if (theirs != null) burger.ForCustomer = theirs;
            burger.isLastPiece = true;
            burger.untouchable = false;
            burger.RemoveFromList();
            table.UseFood(burger);
            GameSfx.Play(GameSfx.Cue.Dish);
            TapJuice.Boing(table.transform, 0.07f);
        }
        assembler.gameObject.SetActive(false);
        return burger != null;
    }

    // Room for one more (nothing goes on a burger he carries, and a burger only into empty hands).
    public bool CanTakeMore => !isHolded || currentFood is not Hamburger && under.Count + 1 < MaxCarry;

    // `food` is the new top: what was on top goes under it. It flies there from `from` (a crate, a station).
    private void Push(EdibleBase food, Vector3 from)
    {
        if (isHolded && currentFood != null && currentFood != food) under.Add(currentFood);
        currentFood = food;
        isHolded = true;
        Restack();
        if (stack != null) stack.Arrive(food, from);
    }

    // The top has just been put somewhere: the next one down is the top now.
    private void PopTop()
    {
        if (under.Count > 0)
        {
            currentFood = under[under.Count - 1];
            under.RemoveAt(under.Count - 1);
            isHolded = true;
            EventManager.OnFoodHolded.Invoke();   // (placeables switched their colliders off as it was put down)
        }
        else
        {
            currentFood = null;
            isHolded = false;
        }
        Restack();
    }

    private void BringToTop(EdibleBase food)
    {
        if (food == null || food == currentFood || !under.Remove(food)) return;
        under.Add(currentFood);
        currentFood = food;
    }

    // One on another in his hands, bottom first (CarryStack moves them there). A burger in his hands (two things or
    // more) has its bun at the bottom, cut like a plate's bottom bun, and its top on top - whenever the bun came.
    private readonly List<EdibleBase> shown = new();

    private void Restack()
    {
        var hand = holdParent.transform;
        shown.Clear();
        foreach (var food in Carried)
        {
            if (food == null) continue;
            if (food.transform.parent != hand) food.transform.SetParent(hand, true);
            shown.Add(food);
        }
        Bun bottom = null;
        if (currentFood is not Hamburger && BurgerParts() >= 2)
            foreach (var food in shown) if (food is Bun bun) { bottom = bun; break; }
        foreach (var food in shown) if (food is Bun bun) bun.LookSliced(bun == bottom);
        if (bottom != null)
        {
            shown.Remove(bottom);
            shown.Insert(0, bottom);
        }
        if (stack != null) stack.Layout(shown, bottom != null);
    }

    // What a station takes from his stack: raw buns for the oven, raw patties for the pan, raw bacon / potatoes for the
    // griddle and the fryer, whole veg and cheese for the board.
    private static bool Goes(EdibleBase food, NonStackBase station) => station switch
    {
        Oven => food is Bun && food.Preparation == Prep.Raw,
        Pan => food is Burger && food.Preparation == Prep.Raw,
        HeatStation heat => heat.Takes(food) && food.Preparation == Prep.Raw,
        ChoppingBoard => food is CuttableBase { isSliced: false },
        _ => false,
    };

    private EdibleBase FirstFor(NonStackBase station) => Carried.FirstOrDefault(f => f != null && Goes(f, station));

    // The station a tap was for: the utensil itself or the food on it.
    private NonStackBase StationTapped()
    {
        var station = placeable as NonStackBase;
        if (station == null && edible != null) station = edible.Placeable as NonStackBase;
        return station is CookingBase || station is ChoppingBoard ? station : null;
    }

    // One visit to a station: take what is ready on it (waiting for it if it is still cooking and he came for it), put
    // in the first raw thing it cooks; at the board, chop everything whole he carries and keep it.
    private IEnumerator UseStation(NonStackBase station)
    {
        busyAt = station;
        RotateToSelectable();
        var on = station.Food;
        if (on != null)
        {
            float giveUp = Time.time + 40f;
            while (busyAt == station && station.Food == on && StillPreparing(on) && Time.time < giveUp)
            {
                RotateToSelectable();
                yield return null;
            }
            if (busyAt != station) yield break;   // called off: another tap, steering
            if (station.Food == on && !StillPreparing(on) && CanTakeMore && !on.untouchable)
            {
                on.isLastPiece = true;
                PickUpObject(on);
                yield return null;
            }
        }
        while (busyAt == station && !station.IsHaveFood() && FirstFor(station) is EdibleBase raw)
        {
            if (raw is Bun whole) whole.LookSliced(false);   // (the bottom of a burger in his hands goes in whole)
            BringToTop(raw);
            DropObject(station);
            if (station is not ChoppingBoard) break;
            float giveUp = Time.time + 10f;
            while (busyAt == station && station.Food == raw && StillPreparing(raw) && Time.time < giveUp) yield return null;
            if (busyAt != station) yield break;
            if (station.Food != raw || StillPreparing(raw)) break;
            raw.isLastPiece = true;
            PickUpObject(raw);
            yield return null;
        }
        if (busyAt != station) yield break;
        busyAt = null;
        if (queue.Count > 0 && !runningQueue) StartCoroutine(RunNextQueued());
    }

    // Interactive with either empty or full hands (what it will be used with is only known when its turn comes).
    private static bool Usable(Collider col)
    {
        var proxy = col.GetComponent<TapProxy>();
        if (proxy == null) return IsInteractive(col);
        var empty = proxy.Resolve(false);
        var full = proxy.Resolve(true);
        return empty != null && IsInteractive(empty) || full != null && IsInteractive(full);
    }

    private NpcFsm CustomerNear(Vector3 screenPosition)
    {
        NpcFsm picked = null;
        float nearest = Screen.height * customerTapRadius;
        foreach (var npc in NpcFsm.Active)
        {
            var state = npc.executingNpcState;
            if (npc.chair == null || state != ExecutingNpcState.COME && state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT
                && !npc.FriesDue) continue;
            float d = Vector2.Distance(_playerCam.WorldToScreenPoint(npc.transform.position + Vector3.up), screenPosition);
            if (d < nearest) { nearest = d; picked = npc; }
        }
        return picked;
    }

    private IEnumerator RunNextQueued()
    {
        runningQueue = true;
        // Two frames: a burger closed by the last use is picked up a frame later (PickUpNextFrame).
        yield return null;
        yield return null;
        runningQueue = false;
        while (queue.Count > 0 && !Busy && !IsStunned && !Steering)
        {
            var tap = queue[0];
            queue.RemoveAt(0);
            if (Run(tap)) yield break;   // on his way (or done at once: Interact starts the next)
        }
    }

    // Carries out a waiting tap with what he holds now; false if there is nothing to do with it any more.
    private bool Run(QueuedTap tap)
    {
        if (tap.Customer != null)
        {
            if (!tap.Customer.isActiveAndEnabled || !isHolded || currentFood == null) return false;
            var state = tap.Customer.executingNpcState;
            bool theirFries = tap.Customer.FriesDue && Carries(f => f is Fries && f.Preparation == Prep.Good);
            if (CarriesBurger && !theirFries)
            {
                if (state != ExecutingNpcState.COME && state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT) return false;
                if (tap.Customer.chair?.GetTableService() is not ServiceBase service || !CanServe(service)) return false;
                spawnable = null;
                edible = null;
                bin = null;
                placeable = service;
                selectable = service;
                approachTarget = service;
                destination = service.transform.position;
                quietSelect = true;
                try { GoToTarget(); } finally { quietSelect = false; }
                return true;
            }
            if (state != ExecutingNpcState.WAIT && !(tap.Customer.FriesDue && Carries(f => f is Fries)) || Tutorial.Running) return false;
            ThrowAt(tap.Customer);
            return false;   // thrown from where he stands: on to the next
        }
        if (tap.Thing == null || !tap.Thing.gameObject.activeInHierarchy) return false;
        var col = OnIt(ResolveTap(tap.Thing));
        if (col == null || !IsInteractive(col)) return false;
        GatherInteractableComponents(col, tap.Point);
        SnapToNearbyService();
        PreferWaitingCustomer();
        quietSelect = true;
        try { GoToTarget(); } finally { quietSelect = false; }
        return true;
    }
    #endregion

    public void GetFoodFromSource()
    {
        if(spawnable != null)
        {
            if (!CanTakeMore) { GameSfx.Play(GameSfx.Cue.Bonk); return; }

            spawnable?.Spawn();
            EventManager.OnFoodHolded.Invoke();
            var spawned = holdParent.transform.GetChild(holdParent.transform.childCount - 1) ?
                holdParent.transform.GetChild(holdParent.transform.childCount - 1).gameObject.GetComponent<EdibleBase>(): null;
            var crate = spawnable as Component;
            if (spawned != null) Push(spawned, crate != null ? crate.transform.position + Vector3.up * 0.3f : holdParent.transform.position);
            if (crate != null) TapJuice.Boing(crate.transform, 0.1f);   // the crate gives one up
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
            if (placeable is Plate plate && plate.Top != null && !(isHolded && currentFood is Hamburger) && (!isHolded || !CarriesSomethingFor(plate)))
            {
                TakeFromPlate(plate);
                return;
            }
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
        if (ingredient == null || ingredient.untouchable || !ingredient.isLastPiece) return;
        // Onto his stack; a finished burger only into empty hands.
        if (isHolded && (!CanTakeMore || ingredient is Hamburger)) return;

        var from = ingredient.transform.position;
        ingredient.RemoveFromList();
        EventManager.OnFoodHolded.Invoke();

        ingredient.transform.parent = holdParent.transform;
        ingredient.transform.localRotation = Quaternion.identity;
        ingredient.transform.position = holdParent.transform.position;

        Push(ingredient, from);
    }
    void DropObject(PlaceableBase place)
    {
        if (!isHolded || currentFood == null || place == null) return;
        // His stack onto a customer's table: a burger.
        if (place is ServiceBase table && currentFood is not Hamburger)
        {
            AssembleAt(table);
            return;
        }
        // A plate takes the whole stack, bottom first (the first bun goes to the bottom of the burger itself).
        if (place is Plate plate && currentFood is not Hamburger)
        {
            bool any = false;
            foreach (var food in Carried.ToList())
            {
                if (food == null || food is Hamburger || !plate.Accepts(food)) continue;
                BringToTop(food);
                plate.UseFood(food);
                EventManager.OnFoodDropped.Invoke();
                PopTop();
                if (stack != null) stack.Landed(food);
                any = true;
            }
            if (any)
            {
                GameSfx.Play(GameSfx.Cue.Dish);
                TapJuice.Boing(plate.transform, 0.08f);
                // What is put down on a plate is a burger at once (user, 2026-10-30): Mochi, if he is in the cafe,
                // runs it out to its customer; otherwise it waits there for the chef.
                plate.Close();
            }
            return;
        }
        // A station takes what it cooks from anywhere in the stack (UseStation picks it).
        // (Unless the top is already one it takes: UseStation chose it.)
        if (place is NonStackBase station && (station is CookingBase || station is ChoppingBoard) && !Goes(currentFood, station)
            && FirstFor(station) is EdibleBase raw)
            BringToTop(raw);

        place.UseFood(currentFood);
        if(!place.IsSuitable(currentFood)) return;
        EventManager.OnFoodDropped.Invoke();
        // a burger onto a customer's table: the china clinks
        GameSfx.Play(place is Plate || place is TableService ? GameSfx.Cue.Dish : GameSfx.Cue.PutDown);
        var put = currentFood;
        PopTop();
        if (stack != null) stack.Landed(put);
        TapJuice.Boing(place.transform, 0.07f);
    }

    // The click of a tap - once: a waiting tap clicked when it was made (Add), not again when its turn comes (user,
    // 2026-10-30: "extra click sounds in the onboarding").
    private bool quietSelect;

    private void SelectObject()
    {
        if (selectable != null && !quietSelect)
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
                // Burnt things if he carries any (all of them), otherwise the top one.
                var burnt = Carried.Where(f => f != null && f.Preparation == Prep.Burnt).ToList();
                if (burnt.Count == 0) burnt.Add(currentFood);
                foreach (var food in burnt)
                {
                    BringToTop(food);
                    bin.Junk(currentFood);
                    PopTop();
                }
                GameSfx.Play(GameSfx.Cue.Trash);
                TapJuice.Boing(bin.transform, 0.2f);
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
