using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// The cafe's first member of staff: a small cat who runs finished burgers out to the tables, so the chef
// can stay in the kitchen. Bought from the shop; before that he isn't in the scene at all.
// He only ever does the delivery the player could do by hand: take a finished burger off a plate and put
// it on a waiting customer's table spot (the same ServiceBase.UseFood the chef uses, so scoring, tips and
// the customer's reaction are untouched).
// No rig and no animator: he is one mesh that hops, which is cheaper than a walk cycle and cuter.
// More of him (user, 2026-11-03): tapped, he purrs and spins (PetAt); he takes fried fries out of the fryer and
// tosses them to the customer who ordered them, as the chef does; when the chef slips, he runs over and catches what
// flew out of the chef's hands on his tray (Catch), and hands it back; and between errands he sometimes digs in the
// bin and eats what he finds (Bin.Dig).
[RequireComponent(typeof(NavMeshAgent))]
public class Waiter : MonoBehaviour
{
    [SerializeField] private Transform cat;     // the body, hopped up and down
    [SerializeField] private Transform tail;    // turns about its root (a child of the body)
    [SerializeField] private Transform tray;
    [SerializeField] private Transform hold;    // where the burger rides
    [SerializeField] private Vector3 homePoint; // world position of his corner, written by the builder

    // How he moves and how close he has to get live here, not in the scene: a number serialized into the
    // scene keeps its old value when the default changes here, and these are meant to be tuned by feel.
    private const float Speed = 3.8f;               // a little quicker than the chef (3.5): he is the one doing the running
    private const float Acceleration = 24f;
    private const float Reach = 1.5f;               // close enough to take a burger or to set it down
    private const float FarthestReach = 2.6f;       // as close as he gets when something is in the way
    private const float LookFor = 0.3f;             // seconds between looks for something to do
    private const float GiveUpAfter = 14f;          // no single errand should take longer than this

    private const float HopRate = 2.4f;             // hops a second at a run, empty-handed
    private const float CarryHopRate = 3.0f;        // ... and with a burger on the tray: quicker and springier
    private const float HopHeight = 0.13f;
    private const float CarryHopHeight = 0.21f;
    private const float SwayDegrees = 3f;           // standing about, he rocks slowly from side to side...
    private const float SwayRate = 1.7f;            // ...(that is, a swing every four seconds or so)
    private const float WagDegrees = 22f;           // ...and his tail wags, in little bursts
    private const float WagRate = 6.5f;

    private enum Job { Waiting, Fetching, Delivering, GoingHome, Catching, Rummaging, Eating, Visiting }

    private NavMeshAgent agent;
    private Vector3 home;
    private Job job = Job.Waiting;
    private float jobSince;
    private Plate from;
    private ServiceBase to;
    private Hamburger carrying;
    // Fries he is taking to a customer (instead of a burger): from which station, for whom.
    private EdibleBase fries;
    private HeatStation friesFrom;
    private NpcFsm friesFor;
    private const float ThrowFrom = 4f;             // he tosses fries from this close, as the chef does from the kitchen
    private Vector3 carriedScale;
    private float nextLook;
    private float nextAsk;
    private float closeSince = -1f;
    private readonly Dictionary<Transform, Vector3> airborne = new();   // burgers in mid-toss, and the size each ends at

    private bool moving;
    private float hopPhase;     // 0..1 through the hop he is in; 0 is standing on the floor
    private int hops;           // which hop, so he leans one way and then the other
    private float standing;     // 0 while hopping, 1 once he has settled: how much of the sway shows
    private float cheerAt = -10f;
    private Vector3 trayRest;
    private Quaternion trayTurn = Quaternion.identity;
    private Vector3 lastVelocity;
    private Vector2 wobble, wobbleVelocity;     // the burger on the tray: x forward/back, y side to side

    // The layers of the burger on the tray, each with its own little spring (Jiggle).
    private sealed class Piece
    {
        public Transform Item;
        public Vector3 RestPos;
        public Quaternion RestRot;
        public float Height01;       // 0 the bottom layer .. 1 the top one
        public Vector3 Slide, SlideVelocity;
        public float Lift, LiftVelocity;
        public float Twist;          // which way it turns as it hops (+1 / -1)
    }
    private readonly List<Piece> pieces = new();
    // The height of one layer in the burger's own space, and the same distance measured sideways: a burger is
    // squashed to a tenth in height (its layers are scaled back up), so its own up and its own sideways differ.
    private float pieceGap, sideGap;
    private int jiggledHop;

    // In the cafe and not busy with a burger: a burger the chef closes is left on its plate for him (PlayerFSM).
    public static bool Free
    {
        get
        {
            var mochi = FindFirstObjectByType<Waiter>();
            return mochi != null && mochi.isActiveAndEnabled && mochi.Idle;
        }
    }

    // Nothing in his paws and nothing he can't drop for a job (looking for a bite in the bin can wait).
    private bool Idle => carrying == null && fries == null
                         && (job == Job.Waiting || job == Job.GoingHome || job == Job.Rummaging || job == Job.Visiting);

    // Walking somewhere, for the game's watchdog (Unstuck): it calls Unstick when he has not moved for a while.
    // Not on a visit: walking up to the chef he stops short against him, which looked stuck - the watchdog put him
    // back in his corner and he came hopping over again (user, 2026-11-06). A visit gives up by itself (Visit).
    public bool OnTheMove => job != Job.Waiting && job != Job.Eating && job != Job.Visiting && agent.isOnNavMesh && agent.hasPath
                             && !agent.pathPending && agent.remainingDistance > 0.3f;

    // Stuck on the way somewhere: whatever he carried goes back, and he is home again.
    public void Unstick()
    {
        if (carrying != null) Drop(false);
        if (fries != null) DropFries();
        (from, to) = (null, null);
        (friesFrom, friesFor) = (null, null);
        rummageIn = null;
        Claimed = null;
        agent.speed = Speed;
        Unstuck.Floor(agent, 6f, home);
        if (agent.isOnNavMesh) agent.ResetPath();
        SetJob(Job.Waiting);
    }

    // What the chef shouldn't bother with any more: a burger the waiter has already claimed.
    public static Hamburger Claimed { get; private set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = Speed;
        agent.acceleration = Acceleration;
        agent.angularSpeed = 900f;
        agent.stoppingDistance = 0.15f;
        // The least important agent in the cafe: everyone else walks on as if he weren't there and he steps aside
        // (user, 2026-11-06: coming over to rub against the chef he pushed him about and got in his way).
        agent.avoidancePriority = 99;
        home =homePoint != Vector3.zero ? homePoint : transform.position;
        if (tray != null) (trayRest, trayTurn) = (tray.localPosition, tray.localRotation);
        nextRummage = Time.time + 20f;
        nextVisit = Time.time + 12f;
    }

    // The shop reveals something bought by growing its group from scale zero. A NavMeshAgent switched on in
    // the middle of that has its whole group - and so itself - collapsed onto the group's pivot, which is
    // not floor he can stand on: it fails to find the NavMesh and never tries again on its own, so he stood
    // exactly where he appeared and never did anything (reported as "Mochi doesn't take the burgers").
    // Scenes that load with him already bought skipped the reveal, which is why it worked in every test
    // that did not go through the shop. So he is put on the floor by his corner explicitly, once the group
    // is full size, rather than relying on the agent having placed itself when it was enabled.
    private bool EnsureOnMesh()
    {
        if (agent.isOnNavMesh) return true;
        if (transform.parent != null && transform.parent.lossyScale.x < 0.95f) return false;

        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(home, out var floor, 6f, filter)) return false;
        if (!agent.Warp(floor.position))
        {
            // Warp can refuse an agent that never got a NavMesh; switching it off and on again makes it look.
            agent.enabled = false;
            transform.position = floor.position;
            agent.enabled = true;
        }
        home = floor.position;
        return agent.isOnNavMesh;
    }

    private void OnDisable()
    {
        // Put down whatever he was holding, so a burger is never lost with him.
        if (carrying != null && from != null) Drop(false);
        if (fries != null) DropFries();
        // A burger caught in the air when he switched off would stay there: settle it where it was going.
        foreach (var (item, size) in airborne)
        {
            if (item == null) continue;
            item.localPosition = Vector3.zero;
            item.localScale = size;
        }
        airborne.Clear();
        Claimed = null;
        SetJob(Job.Waiting);
    }

    private void Update()
    {
        Hop(Time.deltaTime);
        if (!EnsureOnMesh()) return;

        // Looking costs a search of the plates, so it is done a few times a second; heading for something
        // is checked every frame, or he runs past the table before noticing he has got there. He keeps
        // looking on the way home, so a burger that comes up meanwhile does not wait for him to sit down.
        if (job == Job.Waiting || job == Job.GoingHome || job == Job.Rummaging || job == Job.Visiting)
        {
            if (Time.time >= nextLook)
            {
                nextLook = Time.time + LookFor;
                Look();
            }
            if (job == Job.GoingHome && Arrived(home)) SetJob(Job.Waiting);
            if (job == Job.Waiting) FaceTheViewer();
            if (job == Job.Rummaging) Rummage();
            if (job == Job.Visiting) Visit();
            return;
        }
        if (job == Job.Catching) { Catching(); return; }
        if (job == Job.Eating) return;   // (Eat runs it)

        // Whatever is in his way, he doesn't carry a burger around forever: put it back, go home, try again.
        if (Time.time - jobSince > GiveUpAfter) { GiveUp(); return; }

        if (job == Job.Fetching) { if (friesFor != null) FetchFries(); else Fetch(); }
        else if (friesFor != null) DeliverFries();
        else Deliver();
    }

    // Waiting in his corner he turns round to face the player (the camera), rather than showing his back to
    // them for as long as there is nothing to carry.
    private Transform viewer;
    private void FaceTheViewer()
    {
        if (standing < 0.5f) return;
        if (viewer == null) { var main = Camera.main; if (main == null) return; viewer = main.transform; }
        var toward = -viewer.forward;
        toward.y = 0f;
        if (toward.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(toward), 240f * Time.deltaTime);
    }

    private void SetJob(Job next)
    {
        job = next;
        jobSince = Time.time;
        closeSince = -1f;
        rubUntil = -1f;   // (a visit cut short by a burger to deliver starts afresh next time)
    }

    #region The job
    // A finished burger on a plate, and a customer still waiting for one with a free table spot.
    private void Look()
    {
        foreach (var plate in Object.FindObjectsByType<Plate>(FindObjectsSortMode.None))
        {
            var burger = plate.FinishedBurger;
            if (burger == null || burger.untouchable) continue;
            var table = Table(burger);
            if (table == null) continue;
            (from, to) = (plate, table);
            SetJob(Job.Fetching);
            Claimed = burger;
            GoTo(plate.transform.position, false);
            return;
        }
        if (LookForFries()) return;
        if (job == Job.Waiting && Time.time >= nextVisit && FindFirstObjectByType<PlayerFSM>() is PlayerFSM chef && chef.Idle
            && !Tutorial.Running && Flat(chef.transform.position) < 9f)
        {
            visiting = chef;
            SetJob(Job.Visiting);
            GoTo(BesideChef(), true);
            return;
        }
        if (job == Job.Waiting && Time.time >= nextRummage && Object.FindFirstObjectByType<Bin>() is Bin bin && bin.HasLeftovers)
        {
            rummageIn = bin;
            SetJob(Job.Rummaging);
            GoTo(bin.Mouth, false);
        }
    }

    // Someone is still sitting at that spot waiting for a burger (their patience can run out while he is on
    // his way, and a burger put down on an empty chair's spot just sat there for good).
    private static bool Awaited(ServiceBase spot)
    {
        foreach (var npc in NpcFsm.Active)
        {
            var state = npc.executingNpcState;
            if ((state == ExecutingNpcState.COME || state == ExecutingNpcState.ORDER || state == ExecutingNpcState.WAIT)
                && npc.chair != null && npc.chair.GetTableService() == (NonStackBase)spot) return true;
        }
        return false;
    }

    private ServiceBase Table(Hamburger burger)
    {
        // The customer it was made for first (their order is what it is).
        var owner = burger.ForCustomer;
        if (Orders.Waiting(owner) && owner.chair.GetTableService() is ServiceBase mine && !mine.IsHaveFood() && mine.IsSuitable(burger))
            return mine;
        // Otherwise whoever's order it fits best (it used to be the first customer in the list, whatever they had
        // ordered), then whoever has waited longest; someone still sitting down or ordering last.
        var layers = new List<string>();
        foreach (var layer in burger.Layers) layers.Add(layer.Name);
        ServiceBase best = null;
        float bestScore = float.NegativeInfinity;
        foreach (var npc in NpcFsm.Active)
        {
            var state = npc.executingNpcState;
            if (state != ExecutingNpcState.COME && state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT) continue;
            if (npc.chair == null || npc.chair.GetTableService() is not ServiceBase service) continue;
            if (service.IsHaveFood() || !service.IsSuitable(burger)) continue;
            float score = state == ExecutingNpcState.WAIT && npc.Ordered != null
                ? 10000f + Orders.Fit(layers, npc.Ordered) + npc.WaitedSeconds * 0.5f
                : 0f;
            if (score > bestScore) { bestScore = score; best = service; }
        }
        return best;
    }

    private void Fetch()
    {
        // The chef may have taken it, or the customer may have given up, while the cat was on his way.
        var burger = from != null ? from.FinishedBurger : null;
        if (burger == null || burger != Claimed) { GoHome(); return; }
        if (to == null || to.IsHaveFood() || !Awaited(to))
        {
            to = Table(burger);
            if (to == null) { GoHome(); return; }   // nobody waiting any more: the burger stays on its plate
        }
        if (!Arrived(from.transform.position)) return;

        var (start, size) = (burger.transform.position, burger.transform.lossyScale.x);
        burger.RemoveFromList();
        burger.transform.SetParent(hold, false);
        burger.transform.localPosition = Vector3.zero;
        burger.transform.localRotation = Quaternion.identity;
        // The burger keeps its size on the tray (it used to shrink to half, and the user did not like it): a burger
        // as big as he is, swaying on his tray with every hop (Wobble), is the joke.
        carriedScale = burger.transform.localScale;
        burger.transform.localScale = carriedScale * (size / Mathf.Max(0.0001f, burger.transform.lossyScale.x));
        carrying = burger;
        TakePieces(burger);
        SetJob(Job.Delivering);
        GoTo(to.transform.position, false);
        StartCoroutine(Toss(burger.transform, start, size, hold, 0.22f, 0.35f));
    }

    private void Deliver()
    {
        if (carrying == null) { GoHome(); return; }
        // Their patience ran out on the way: another customer who is waiting gets it, or it goes back to the pass,
        // never onto an empty chair's spot.
        if (to == null || to.IsHaveFood() || !Awaited(to))
        {
            var other = Table(carrying);
            if (other == null) { Drop(false); GoHome(); return; }
            to = other;
            GoTo(to.transform.position, false);
            return;
        }
        if (!Arrived(to.transform.position)) return;
        Drop(true);
        cheerAt = Time.time;
        GoHome();
    }

    private void GiveUp()
    {
        if (carrying != null) Drop(false);
        if (fries != null) DropFries();
        GoHome();
    }

    // Puts the burger wherever it should go: on the table if that is still where it was headed, otherwise
    // back on the plate it came from. It is thrown there in a little arc rather than appearing: the table
    // spot can be a metre and a half from where he stands.
    private void Drop(bool onTable)
    {
        var burger = carrying;
        PlaceableBase place = onTable && to != null && !to.IsHaveFood() && Awaited(to) ? to : ReturnPlate(burger);
        SettlePieces();
        var (start, size) = (burger.transform.position, burger.transform.lossyScale.x);
        burger.transform.SetParent(null, true);
        burger.transform.localScale = carriedScale;
        // Nowhere to put it (every plate busy, its plate taken meanwhile): it went nowhere and floated at tray height
        // for good (user's phone, 2026-11-04). Then into the bin with a toss, like anything else nobody can use.
        if (place == null || place is Plate full && !full.Accepts(burger))
        {
            var bin = Object.FindFirstObjectByType<Bin>();
            if (bin != null) bin.Junk(burger);
            else burger.gameObject.SetActive(false);
            carrying = null;
            Claimed = null;
            return;
        }
        place.UseFood(burger);
        // (Not when he is being switched off: a coroutine can't start then, and the burger just lands.)
        if (isActiveAndEnabled && burger.transform.parent != null)
            StartCoroutine(Toss(burger.transform, start, size, burger.transform.parent, 0.22f, 0.45f));
        carrying = null;
        Claimed = null;
    }

    // Where a burger goes back to: the plate it came from if it can still take it (the chef may have started a
    // new burger there meanwhile), otherwise any plate that can.
    private Plate ReturnPlate(Hamburger burger)
    {
        if (from != null && from.Accepts(burger)) return from;
        foreach (var plate in Object.FindObjectsByType<Plate>(FindObjectsSortMode.None))
            if (plate.Accepts(burger)) return plate;
        return from;
    }

    // Carries `item` from `start` to where `destination` is, in an arc, growing or shrinking from the size it
    // had (`fromSize`, in the world) to the size it has been given there. If it has been moved on to something
    // else in the meantime (the customer ate it, he was switched off) it is left alone.
    private IEnumerator Toss(Transform item, Vector3 start, float fromSize, Transform destination, float seconds, float arc)
    {
        var end = item.localScale;
        float ratio = fromSize / Mathf.Max(0.0001f, item.lossyScale.x);
        airborne[item] = end;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            if (item == null || item.parent != destination || !item.gameObject.activeInHierarchy) { airborne.Remove(item); yield break; }
            float s = t / seconds;
            s = s * s * (3f - 2f * s);
            item.position = Vector3.Lerp(start, destination.position, s) + Vector3.up * (Mathf.Sin(s * Mathf.PI) * arc);
            item.localScale = end * Mathf.Lerp(ratio, 1f, s);
            yield return null;
        }
        if (item != null && item.parent == destination)
        {
            item.localPosition = Vector3.zero;
            item.localScale = end;
        }
        airborne.Remove(item);
    }

    private void GoHome()
    {
        (from, to) = (null, null);
        (friesFrom, friesFor) = (null, null);
        agent.speed = Speed;
        SetJob(Job.GoingHome);
        Claimed = null;
        GoTo(home, true);
    }

    // Plates and table spots sit on furniture, a metre above the floor he walks on: head for the walkable
    // floor beside them, not for the thing itself, or the path comes back invalid and he never sets off.
    // On an errand he doesn't slow down for the last metre (he is done as soon as he is near enough, and
    // braking made him crawl up to every table); coming home he does.
    private void GoTo(Vector3 target, bool brake)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        agent.autoBraking = brake;
        agent.SetDestination(NavMesh.SamplePosition(target, out var floor, 4f, filter) ? floor.position : target);
        nextAsk = Time.time + 1f;
    }

    private float Flat(Vector3 at) => new Vector2(at.x - transform.position.x, at.z - transform.position.z).magnitude;

    private bool Arrived(Vector3 at)
    {
        if (!agent.isOnNavMesh || agent.pathPending) return false;
        float flat = Flat(at);
        if (flat <= Reach) return true;

        // The floor beside a table can be a metre from the spot on it, and a customer, a chair or the chef
        // may be standing on the last bit of that floor. He then ends up a few steps short of the end of
        // his path, shuffling against it and never quite stopping: it used to be that he waited there for
        // good, burger in hand, because "arrived" needed the path to be finished. So being near the end of
        // his path and within reach of the spot for a moment is as close as he is going to get, and counts.
        bool nearEnd = agent.remainingDistance <= 0.9f;
        if (flat <= FarthestReach && nearEnd)
        {
            if (closeSince < 0f) closeSince = Time.time;
            if (Time.time - closeSince >= 0.4f) return true;
        }
        else closeSince = -1f;

        // Nudged off course (another body, a bought table): ask again, but not every frame.
        if (nearEnd && Time.time >= nextAsk) GoTo(at, agent.autoBraking);
        return false;
    }
    #endregion

    #region Moving
    // A hop instead of a walk cycle. Hopping he rises and falls like a ball: stretched in the air, squashed
    // as he lands, leaning forward and rocking a little to one side and then the other. With a burger on the
    // tray he hops higher and quicker. Standing, he sways slowly from side to side and wags his tail, so he
    // is never frozen; the tray stays level so the burger doesn't slide off.
    private void Hop(float dt)
    {
        if (cat == null) return;
        float speed = agent.isOnNavMesh ? agent.velocity.magnitude : 0f;
        moving = moving ? speed > 0.25f : speed > 0.5f;     // a gap between starting and stopping, so he doesn't flicker
        bool carry = carrying != null || fries != null;
        float rate = carry ? CarryHopRate : HopRate;

        if (moving)
        {
            hopPhase += dt * rate * Mathf.Clamp(speed / Speed, 0.5f, 1.15f);
            if (hopPhase >= 1f) { hopPhase %= 1f; hops++; }
        }
        else if (hopPhase > 0f)
        {
            // Stopping: finish the hop he is in, so he never hangs in the air.
            hopPhase += dt * rate;
            if (hopPhase >= 1f) { hopPhase = 0f; hops++; }
        }
        standing = Mathf.MoveTowards(standing, moving || hopPhase > 0f ? 0f : 1f, dt * 4f);

        float u = hopPhase;
        float air = 4f * u * (1f - u);                                      // 0 on the floor, 1 at the top of the hop
        float landing = Mathf.Max(0f, 1f - Mathf.Min(u, 1f - u) / 0.18f);   // 1 at the instant of landing and of take-off
        float lift = air * (carry ? CarryHopHeight : HopHeight);

        // The happy little jump after setting a burger down.
        float cheer = (Time.time - cheerAt) / 0.5f;
        if (cheer >= 0f && cheer < 1f) lift += 0.17f * 4f * cheer * (1f - cheer);
        // Petted: a higher jump with a full turn in the air.
        float pet = (Time.time - pettedAt) / PetSeconds;
        float spin = 0f;
        if (pet >= 0f && pet < 1f)
        {
            lift += 0.26f * 4f * pet * (1f - pet);
            spin = 360f * pet * pet * (3f - 2f * pet);
        }

        float nod = Mathf.Sin(Time.time * SwayRate);
        // A good squash as he lands is what makes the hop read as bouncy (it was tried at a few percent, and
        // looked stiff, especially carrying a burger).
        float stretch = (air * 0.08f + (cheer >= 0f && cheer < 1f ? 0.06f : 0f)) - landing * (carry ? 0.17f : 0.13f);
        stretch = Mathf.Lerp(stretch, Mathf.Abs(nod) * 0.02f, standing);    // the sway swells him a touch
        float tall = 1f + stretch;
        float wide = 1f / Mathf.Sqrt(tall);                                 // squash and stretch keep the same volume

        float lean = Mathf.Clamp01(speed / Speed) * (carry ? 9f : 6f);
        float rock = (hops % 2 == 0 ? 1f : -1f) * Mathf.Sin(u * Mathf.PI) * (carry ? 8f : 6f);
        float roll = Mathf.Lerp(rock, nod * SwayDegrees, standing);

        Puff(ref wide, ref tall, ref roll);
        cat.localPosition = new Vector3(0f, lift, 0f);
        cat.localRotation = Quaternion.Euler(lean, spin, roll);
        cat.localScale = new Vector3(wide, tall, wide);

        if (tail != null)
        {
            // Standing, the tail wags in bursts (it swells and fades, then rests); hopping, it streams out
            // against the way he rocks and flicks once each hop.
            float burst = 0.55f + 0.45f * Mathf.Sin(Time.time * 0.9f);
            bool happy = Time.time - pettedAt < 2.5f;   // a petted cat's tail goes like mad for a while
            float wag = Mathf.Sin(Time.time * WagRate * (happy ? 2.2f : 1f)) * WagDegrees * (happy ? 1.3f : burst);
            float streaming = -rock * 1.6f + Mathf.Sin(u * Mathf.PI * 2f) * 10f;
            tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(streaming, wag, standing));
        }

        if (tray == null) return;
        // The tray rides at his paws: it goes up and down with him and sways with the rocking, but stays level.
        float side = -Mathf.Sin(roll * Mathf.Deg2Rad) * trayRest.y;
        float ahead = Mathf.Sin(lean * Mathf.Deg2Rad) * trayRest.y;
        float bob = carry ? Mathf.Abs(nod) * 0.012f * standing : 0f;
        var turn = Quaternion.Euler(0f, spin, 0f);   // (turning round with him when he is petted)
        tray.localPosition = turn * new Vector3(trayRest.x + side, trayRest.y * tall + lift + bob, trayRest.z + ahead);
        tray.localRotation = turn * trayTurn;

        if (hold != null) Wobble(dt, carrying != null, speed, air, rock, nod);
    }

    #region Fries
    // Fries done in a fryer (not burnt) and a customer still waiting for theirs: he fetches them and tosses them over.
    private bool LookForFries()
    {
        foreach (var station in Object.FindObjectsByType<HeatStation>(FindObjectsSortMode.None))
        {
            if (station.Food is not Fries done || done.Preparation != Prep.Good || done.untouchable) continue;
            var who = FriesCustomer();
            if (who == null) return false;
            (friesFrom, friesFor) = (station, who);
            SetJob(Job.Fetching);
            GoTo(station.transform.position, false);
            return true;
        }
        return false;
    }

    // Whoever has waited longest for their fries.
    private static NpcFsm FriesCustomer()
    {
        NpcFsm best = null;
        foreach (var npc in NpcFsm.Active)
            if (npc.FriesDue && (best == null || npc.WaitedSeconds > best.WaitedSeconds)) best = npc;
        return best;
    }

    private void FetchFries()
    {
        // The chef may have taken them meanwhile, or burnt them.
        if (friesFrom == null || friesFrom.Food is not Fries done || done.Preparation != Prep.Good) { GoHome(); return; }
        if (!friesFor.FriesDue) friesFor = FriesCustomer();
        if (friesFor == null) { GoHome(); return; }
        if (!Arrived(friesFrom.transform.position)) return;
        var (start, size) = (done.transform.position, done.transform.lossyScale.x);
        done.RemoveFromList();
        done.transform.SetParent(hold, false);
        done.transform.localPosition = Vector3.zero;
        done.transform.localRotation = Quaternion.identity;
        carriedScale = done.transform.localScale;
        fries = done;
        SetJob(Job.Delivering);
        GoTo(friesFor.transform.position, false);
        StartCoroutine(Toss(done.transform, start, size, hold, 0.22f, 0.35f));
    }

    private void DeliverFries()
    {
        if (fries == null) { GoHome(); return; }
        if (friesFor == null || !friesFor.FriesDue)
        {
            friesFor = FriesCustomer();
            if (friesFor == null) { DropFries(); GoHome(); return; }
            GoTo(friesFor.transform.position, false);
        }
        if (Flat(friesFor.transform.position) > ThrowFrom && !Arrived(friesFor.transform.position)) return;
        // Close enough: a toss, as the chef does.
        var toward = friesFor.transform.position - transform.position;
        toward.y = 0f;
        if (toward.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toward);
        agent.ResetPath();
        var food = fries;
        fries = null;
        food.transform.localScale = carriedScale;
        FoodFight.Instance.MochiThrows(food, friesFor);
        cheerAt = Time.time;
        GoHome();
    }

    // Nobody wants them any more: back into the fryer if it is free, otherwise into the bin.
    private void DropFries()
    {
        var food = fries;
        fries = null;
        if (food == null) return;
        food.transform.SetParent(null, true);
        food.transform.localScale = carriedScale;
        if (friesFrom != null && friesFrom.isActiveAndEnabled && !friesFrom.IsHaveFood()) { friesFrom.UseFood(food); return; }
        var bin = Object.FindFirstObjectByType<Bin>();
        if (bin != null) bin.Junk(food);
        else food.gameObject.SetActive(false);
    }
    #endregion

    #region Petting, catching, the bin
    private const float PetSeconds = 0.7f;
    private static readonly string[] PetLines = { "Purr!", "Mrrp!", "Meow!", "Prrrr!" };
    private float pettedAt = -10f;

    // A tap on him (PlayerFSM.HandleScreenTap asks first): he purrs and spins, and the tap goes no further.
    public static bool PetAt(Vector3 screen)
    {
        var mochi = FindFirstObjectByType<Waiter>();
        var cam = Camera.main;
        if (mochi == null || !mochi.isActiveAndEnabled || cam == null) return false;
        var p = cam.WorldToScreenPoint(mochi.transform.position + Vector3.up * 0.45f);
        float d = Vector2.Distance(p, screen);
        if (p.z <= 0f || d > Screen.height * 0.055f) return false;
        // A customer nearer the finger is who the tap is for.
        foreach (var npc in NpcFsm.Active)
        {
            if (npc == null) continue;
            var q = cam.WorldToScreenPoint(npc.transform.position + Vector3.up * 0.9f);
            if (q.z > 0f && Vector2.Distance(q, screen) < d) return false;
        }
        mochi.Petted();
        return true;
    }

    private void Petted()
    {
        if (Time.time - pettedAt < PetSeconds * 0.6f) return;
        pettedAt = Time.time;
        GameSfx.Play(GameSfx.Cue.Seated);
        UICheerPop.OnSay.Invoke(Head - Vector3.up * 0.2f, PetLines[Random.Range(0, PetLines.Length)]);
    }

    // The chef slipped (PlayerFSM.Slip): if he is free and near enough to get there before what flew out of the chef's
    // hands comes down, he dashes over and it lands on his tray; it goes back to the chef at `until`. The function says
    // where the i-th thing lands (null: he can't).
    private float catchUntil;
    public static System.Func<int, Vector3> Catch(Vector3 chef, float within, float until)
    {
        var mochi = FindFirstObjectByType<Waiter>();
        if (mochi == null || !mochi.isActiveAndEnabled || !mochi.Idle || !mochi.agent.isOnNavMesh) return null;
        const float Dash = 2f;   // a dash: twice his running speed
        if (mochi.Flat(chef) / (Speed * Dash) > within * 0.8f) return null;
        mochi.rummageIn = null;
        mochi.SetJob(Job.Catching);
        mochi.catchUntil = until;
        mochi.agent.speed = Speed * Dash;
        mochi.GoTo(chef, false);
        var tray = mochi.hold != null ? mochi.hold : mochi.transform;
        return i => tray.position + Vector3.up * (0.08f + 0.14f * i);
    }

    private void Catching()
    {
        if (Time.time < catchUntil + 0.3f) return;
        cheerAt = Time.time;
        GoHome();
    }

    // Now and then, with nothing to do, he trots over to the chef and rubs against his legs, purring (user,
    // 2026-11-04: little life in the quiet moments), then goes back to his corner.
    // It shows what he is up to (user, 2026-11-06): a heart now and then over his head on the way, and once there he
    // stays a moment rubbing against him, hearts floating up, before he trots home.
    private float nextVisit;
    private PlayerFSM visiting;
    private float rubUntil = -1f, nextHeart;
    private const float RubSeconds = 1.6f, HeartEvery = 0.55f, RubHeartEvery = 0.28f;

    private void Visit()
    {
        if (visiting == null || (rubUntil < 0f && Time.time - jobSince > 8f)) { EndVisit(30f); return; }
        if (rubUntil < 0f)
        {
            // There once he is at the chef's legs: near him, or as near as the floor lets him (the spot beside the chef
            // can be cut off by a counter - he stopped short and only ever gave up).
            bool there = Flat(visiting.transform.position) <= 1.5f
                         || agent.isOnNavMesh && !agent.pathPending && agent.hasPath && agent.remainingDistance < 0.3f && Flat(visiting.transform.position) < 2.2f;
            if (!there)
            {
                if (Time.time >= nextAsk) GoTo(BesideChef(), true);
                if (Time.time >= nextHeart) { nextHeart = Time.time + HeartEvery; KitchenFx.Hearts(Head, 1); }
                return;
            }
            // There: a burst of hearts, the purr, and the chef gives a little hop.
            agent.ResetPath();
            rubUntil = Time.time + RubSeconds;
            nextHeart = Time.time + RubHeartEvery;
            KitchenFx.Hearts(Head, 6);
            pettedAt = Time.time;   // the happy jump and spin, and the tail going like mad
            UICheerPop.OnSay.Invoke(Head - Vector3.up * 0.2f, "Mrrrp~");
            var body = visiting.GetComponentInChildren<Animator>();
            if (body != null && body.transform != visiting.transform) TapJuice.Hop(body.transform, 0.1f);
        }
        // Rubbing: side on to him, swaying against his legs; off again if he walks away.
        if (Flat(visiting.transform.position) > 2.6f) { EndVisit(Random.Range(35f, 60f)); return; }
        var toward = visiting.transform.position - transform.position;
        toward.y = 0f;
        float rub = Mathf.Sin(Time.time * 9f) * 14f;
        if (toward.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(Quaternion.Euler(0f, 70f + rub, 0f) * toward);
        if (Time.time >= nextHeart) { nextHeart = Time.time + RubHeartEvery; KitchenFx.Hearts(Head, 1); }
        if (Time.time >= rubUntil) EndVisit(Random.Range(35f, 60f));
    }

    // A spot at the chef's legs on Mochi's side of him, not the chef's own spot (he can't stand there).
    private Vector3 BesideChef()
    {
        var chef = visiting.transform.position;
        var away = transform.position - chef;
        away.y = 0f;
        return chef + (away.sqrMagnitude > 0.01f ? away.normalized : visiting.transform.right) * 0.7f;
    }

    private void EndVisit(float nextIn)
    {
        nextVisit = Time.time + nextIn;
        visiting = null;
        rubUntil = -1f;
        GoHome();
    }

    // Now and then, with nothing to do, he goes and digs in the bin: a thing or two flies out and he eats it.
    private float nextRummage;
    private Bin rummageIn;

    private void Rummage()
    {
        if (rummageIn == null || !rummageIn.HasLeftovers) { GoHome(); return; }
        if (Time.time - jobSince > GiveUpAfter) { GoHome(); return; }
        if (!Arrived(rummageIn.Mouth)) return;
        agent.ResetPath();
        var found = rummageIn.Dig(transform.position, Random.value < 0.4f ? 2 : 1);
        rummageIn = null;
        SetJob(Job.Eating);
        StartCoroutine(Eat(found));
    }

    private IEnumerator Eat(List<Transform> pieces)
    {
        yield return new WaitForSeconds(0.75f);   // they land first
        bool said = false;
        foreach (var piece in pieces)
        {
            if (piece == null) continue;
            var toward = piece.position - transform.position;
            toward.y = 0f;
            if (toward.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toward);
            yield return new WaitForSeconds(0.2f);
            if (!said) { UICheerPop.OnSay.Invoke(Head - Vector3.up * 0.2f, "Nom!"); said = true; }
            GameSfx.Play(GameSfx.Cue.Pickup);
            // Into his mouth, shrinking as it goes; then a happy little hop.
            var from = piece.position;
            var size = piece.localScale;
            for (float t = 0f; t < 1f;)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / 0.35f);
                if (piece == null) break;
                piece.position = Vector3.Lerp(from, Head + transform.forward * 0.25f - Vector3.up * 0.2f, t);
                piece.localScale = size * (1f - t);
                yield return null;
            }
            if (piece != null) Destroy(piece.gameObject);
            cheerAt = Time.time;
            yield return new WaitForSeconds(0.45f);
        }
        nextRummage = Time.time + Random.Range(25f, 45f);
        GoHome();
    }
    #endregion

    #region Tomato fight
    // A customer's tomato can land on him instead of the chef (FoodFight). He does not take it quietly: his fur
    // puffs up and he shakes, then he throws tomatoes back at whoever did it. (He had a synthesised voice for a
    // while; the user found it unnatural and too loud, and he is silent again.)
    private const float AngrySeconds = 0.9f;
    private float angryAt = -10f;
    private float headHeight = -1f;

    public Vector3 Head
    {
        get
        {
            if (headHeight < 0f)
            {
                var renderers = cat != null ? cat.GetComponentsInChildren<Renderer>() : GetComponentsInChildren<Renderer>();
                float top = transform.position.y + 0.8f;
                foreach (var r in renderers) top = Mathf.Max(top, r.bounds.max.y);
                headHeight = top - transform.position.y - 0.15f;
            }
            return transform.position + Vector3.up * headHeight;
        }
    }

    public bool Angry => Time.time - angryAt < AngrySeconds;

    // Hit by a tomato: he always puffs up and shakes, and only sometimes throws back (user: "only sometimes").
    private const float ThrowBackChance = 0.4f;
    public void Struck(NpcFsm thrower)
    {
        angryAt = Time.time;
        if (thrower != null && FoodFight.Instance != null && Random.value < ThrowBackChance)
            FoodFight.Instance.MochiThrowsBack(this, thrower);
    }

    // Puffed up and shaking for a moment.
    private void Puff(ref float wide, ref float tall, ref float roll)
    {
        float a = (Time.time - angryAt) / AngrySeconds;
        if (a < 0f || a >= 1f) return;
        float puff = Mathf.Sin(Mathf.Min(1f, a * 3f) * Mathf.PI * 0.5f) * (1f - Mathf.Max(0f, a - 0.7f) / 0.3f);
        wide *= 1f + 0.24f * puff;
        tall *= 1f + 0.1f * puff;
        roll += Mathf.Sin(Time.time * 55f) * 7f * puff;
    }
    #endregion

    // The burger on his tray wobbles the way it does in the chef's hands (CarryWobble): it leans back when he
    // sets off and forward when he stops, sways with every hop, gets a jolt as he lands, and settles with a
    // little spring. A messy burger wobbles more (BurgerReview.Wobbliness). Only the spot it rides on turns,
    // about the tray's surface, so it tips on the tray rather than swinging through it.
    private void Wobble(float dt, bool carry, float speed, float air, float rock, float nod)
    {
        dt = Mathf.Min(dt, 1f / 30f);
        if (dt <= 0f) return;
        var velocity = agent.isOnNavMesh ? agent.velocity : Vector3.zero;
        var local = transform.InverseTransformDirection((velocity - lastVelocity) / dt);
        lastVelocity = velocity;
        if (!carry) { wobble = wobbleVelocity = Vector2.zero; hold.localRotation = Quaternion.identity; return; }

        float wobbly = carrying.Review.Wobbliness;
        float run01 = Mathf.Clamp01(speed / Speed);
        // A gentle sway (user, twice: "still swings side to side far too much"): it was rock x1.8 with a 22 degree
        // limit and a loose spring that swung past and back; the hop now tips it a few degrees and it settles.
        var target = new Vector2(-local.z, local.x) * 0.8f                      // leans against his acceleration
                   + new Vector2(-1.5f * run01 + air * 2f, rock * 0.5f)          // tips back running, sways a little with the hop
                   + new Vector2(0f, nod * 1.2f * standing);                       // and sways with him standing
        target = Vector2.ClampMagnitude(target * wobbly, 8f * wobbly);
        wobbleVelocity += (target - wobble) * (150f * dt);
        wobbleVelocity *= Mathf.Exp(-10f * dt);   // well damped: no swinging past and back
        wobble += wobbleVelocity * dt;
        // A tall burger tipping as one stiff block looked wrong (user): the taller it is, the less the whole stack
        // leans - its layers wobble on their own instead (Jiggle).
        float block = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01((pieces.Count - 3) / 4f));
        hold.localRotation = Quaternion.Euler(wobble.x * block, 0f, wobble.y * block);
        Jiggle(dt, wobbly);
    }

    // Each layer rides on the one under it a little loosely (user: "the pieces could hop with the sway, as if
    // about to fall"): every time Mochi lands, the layers are thrown up a touch - the higher, the more - and drop
    // back with a small tilt, and they slide a little towards the side the tray leans to and spring back. The
    // bottom layer stays put; nothing ever comes off. A messy burger (BurgerReview.Wobbliness) is looser.
    private void Jiggle(float dt, float wobbly)
    {
        if (pieces.Count < 2) return;
        bool landed = hops != jiggledHop;
        jiggledHop = hops;
        float gap = pieceGap;
        float gravity = gap * 90f;
        foreach (var piece in pieces)
        {
            if (piece.Item == null) continue;
            float h = piece.Height01;
            if (landed && h > 0f)
            {
                piece.LiftVelocity += gap * 8f * h * Mathf.Min(wobbly, 1.6f) * Random.Range(0.55f, 1f);
                piece.Twist = Random.value < 0.5f ? -1f : 1f;
            }
            piece.LiftVelocity -= gravity * dt;
            piece.Lift += piece.LiftVelocity * dt;
            if (piece.Lift < 0f) { piece.Lift = 0f; piece.LiftVelocity = -piece.LiftVelocity * 0.2f; }

            // Downhill on the tray: the tray's tilt (z about the side, x about forward) moves it sideways.
            var downhill = new Vector3(-wobble.y, 0f, wobble.x) * (sideGap * 0.03f * h * wobbly);
            piece.SlideVelocity += (downhill - piece.Slide) * (110f * dt);
            piece.SlideVelocity *= Mathf.Exp(-5f * dt);
            piece.Slide += piece.SlideVelocity * dt;
            piece.Slide = Vector3.ClampMagnitude(piece.Slide, sideGap * 0.3f);

            // Up in the air it turns a little about its own axis. (Never tipped about a level axis: in the
            // burger's squashed space that would shear the layer instead of tilting it.)
            float twist = Mathf.Min(piece.Lift / gap, 1f) * 14f * piece.Twist;
            piece.Item.localPosition = piece.RestPos + piece.Slide + Vector3.up * piece.Lift;
            piece.Item.localRotation = Quaternion.AngleAxis(twist, Vector3.up) * piece.RestRot;
        }
    }

    private void TakePieces(Hamburger burger)
    {
        SettlePieces();
        var items = new List<Transform>();
        // The layers only: not the plate the burger sits on (a child of it too) nor the empty bun holder.
        foreach (Transform child in burger.transform)
            if (child.gameObject.activeSelf && !child.name.StartsWith("plate", System.StringComparison.OrdinalIgnoreCase)
                && child.GetComponentInChildren<Renderer>() != null)
                items.Add(child);
        items.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));
        if (items.Count < 2) return;
        pieceGap = Mathf.Max(0.001f, (items[^1].localPosition.y - items[0].localPosition.y) / (items.Count - 1));
        var scale = burger.transform.lossyScale;
        sideGap = pieceGap * Mathf.Abs(scale.y) / Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        for (int i = 0; i < items.Count; i++)
            pieces.Add(new Piece
            {
                Item = items[i], RestPos = items[i].localPosition, RestRot = items[i].localRotation,
                Height01 = i / (float)(items.Count - 1), Twist = 1f,
            });
        jiggledHop = hops;
    }

    // Every layer back exactly where it was stacked, before the burger leaves the tray.
    private void SettlePieces()
    {
        foreach (var piece in pieces)
        {
            if (piece.Item == null) continue;
            piece.Item.localPosition = piece.RestPos;
            piece.Item.localRotation = piece.RestRot;
        }
        pieces.Clear();
    }
    #endregion
}
