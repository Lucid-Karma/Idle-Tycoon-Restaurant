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

    private enum Job { Waiting, Fetching, Delivering, GoingHome }

    private NavMeshAgent agent;
    private Vector3 home;
    private Job job = Job.Waiting;
    private float jobSince;
    private Plate from;
    private ServiceBase to;
    private Hamburger carrying;
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
            return mochi != null && mochi.isActiveAndEnabled && mochi.carrying == null
                   && (mochi.job == Job.Waiting || mochi.job == Job.GoingHome);
        }
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
        home = homePoint != Vector3.zero ? homePoint : transform.position;
        if (tray != null) trayRest = tray.localPosition;
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
        if (job == Job.Waiting || job == Job.GoingHome)
        {
            if (Time.time >= nextLook)
            {
                nextLook = Time.time + LookFor;
                Look();
            }
            if (job == Job.GoingHome && Arrived(home)) SetJob(Job.Waiting);
            if (job == Job.Waiting) FaceTheViewer();
            return;
        }

        // Whatever is in his way, he doesn't carry a burger around forever: put it back, go home, try again.
        if (Time.time - jobSince > GiveUpAfter) { GiveUp(); return; }

        if (job == Job.Fetching) Fetch();
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
        bool carry = carrying != null;
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
        cat.localRotation = Quaternion.Euler(lean, 0f, roll);
        cat.localScale = new Vector3(wide, tall, wide);

        if (tail != null)
        {
            // Standing, the tail wags in bursts (it swells and fades, then rests); hopping, it streams out
            // against the way he rocks and flicks once each hop.
            float burst = 0.55f + 0.45f * Mathf.Sin(Time.time * 0.9f);
            float wag = Mathf.Sin(Time.time * WagRate) * WagDegrees * burst;
            float streaming = -rock * 1.6f + Mathf.Sin(u * Mathf.PI * 2f) * 10f;
            tail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(streaming, wag, standing));
        }

        if (tray == null) return;
        // The tray rides at his paws: it goes up and down with him and sways with the rocking, but stays level.
        float side = -Mathf.Sin(roll * Mathf.Deg2Rad) * trayRest.y;
        float ahead = Mathf.Sin(lean * Mathf.Deg2Rad) * trayRest.y;
        float bob = carry ? Mathf.Abs(nod) * 0.012f * standing : 0f;
        tray.localPosition = new Vector3(trayRest.x + side, trayRest.y * tall + lift + bob, trayRest.z + ahead);

        if (hold != null) Wobble(dt, carry, speed, air, rock, nod);
    }

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
