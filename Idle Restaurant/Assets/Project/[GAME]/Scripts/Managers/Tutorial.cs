using System;
using System.Linq;
using UnityEngine;

// The first shift, taught the way a Nintendo game teaches: one idea at a time, learned by doing it rather
// than by reading it, nothing taken away from the player, and a little cheer at the end.
// Mochi the waiter cat does the talking from a card at the bottom of the screen, an arrow bobs over the
// one thing that matters right now, and each step waits for the player to actually do it. There is no
// "skip" because there is nothing to sit through: every step but the first and the last ends the moment
// the player acts, and the one optional trick (tossing a snack) offers a "tap to skip" after a while, so the
// lesson can never trap anyone. There is no clock on the player anywhere in it: customers do not lose
// patience (NpcFsm.Wait) and a baked bun or patty does not burn (Oven, Pan) while it runs.
// Runs once (CafeProgress.Taught); the Help screen can start it again.
public class Tutorial : MonoBehaviour
{
    public static Tutorial Instance { get; private set; }
    public static bool Running => Instance != null && Instance.step >= 0;

    // What the arrow is pointing at right now, so the hints can agree with it (HighlightController).
    public static Component Focus { get; private set; }

    [SerializeField] private UICoachCard card;
    [SerializeField] private Transform arrow;
    [SerializeField] private float arrowBob = 0.22f;
    [SerializeField] private float arrowGap = 0.12f;      // clear air between the arrow's TIP and the top of the thing
    [SerializeField] private float aboveCustomer = 1.05f; // a customer's order bubble floats over their head: clear it

    private PlayerFSM chef;
    private int step = -1;
    private bool rated, snacked;
    private float stepStarted;
    private bool skipOffered;
    private string shownLine;
    private Component pointedAt;
    private Vector3 above;
    // The arrow hangs from its own origin, which is the top of the arrowhead: its tip is this far below it.
    private float tipDepth = 0.62f;

    private sealed class Beat
    {
        public string Line;
        public Func<Tutorial, string> LineNow;           // the line can follow what the player is doing
        public Func<Tutorial, Component> Point;          // what to point at (null: nothing)
        public Func<Tutorial, bool> Done;                // null: wait for a tap
        public float SkipAfter;                          // seconds before "tap to skip" is offered; 0 = never
    }

    private static readonly Beat[] Beats =
    {
        new() { Line = "Hi! I'm Mochi.\nLet's cook your first burger!" },
        new()
        {
            Line = "Tap the <b>buns</b>.\nAnything that pulses is waiting for you.",
            Point = t => t.Source("bun"), Done = t => t.chef.HeldFood is Bun,
        },
        new()
        {
            Line = "Nice! Now pop it in the <b>oven</b>.",
            Point = t => t.Nearest<Oven>(), Done = t => Any<Oven>(o => o.IsHaveFood()),
        },
        new()
        {
            Line = "Buns bake, patties fry.\nGrab a <b>patty</b> next.",
            Point = t => t.Source("burger"), Done = t => t.chef.HeldFood is Burger,
        },
        new()
        {
            Line = "On the <b>pan</b> it goes.",
            Point = t => t.Nearest<Pan>(), Done = t => Any<Pan>(p => p.IsHaveFood()),
        },
        // Veggies are the third station, and the one that used to go untaught: a whole tomato is a flaw
        // ("A WHOLE tomato?!"), so it has to be chopped first.
        new()
        {
            Line = "Now a veggie!\nGrab a <b>tomato</b>.",
            Point = t => t.Source("tomato"), Done = t => t.chef.HeldFood is Tomato,
        },
        new()
        {
            Line = "Veggies need a trim first.\nPop it on the <b>chopping board</b>.",
            Point = t => t.Nearest<ChoppingBoard>(), Done = t => Any<ChoppingBoard>(b => b.IsHaveFood()),
        },
        // The rest of the burger, walked through one ingredient at a time. The arrow always points at the
        // single next tap (see WorkOutStack), and the line says what it is.
        new()
        {
            Line = "Stack six layers on a plate.\nOne of each is the classic -\nbut any mix works!",
            LineNow = t => t.StackStep().Line,
            Point = t => t.StackStep().Target, Done = t => Any<Plate>(p => p.FinishedBurger != null),
        },
        // Picking the burger up comes first: the customer is only the target once it is in his hands. (The
        // arrow used to jump straight to the customer, and a tap on a customer with empty hands does nothing.)
        new()
        {
            Line = "A burger! <b>Pick it up</b>\nfrom the plate.",
            LineNow = t => t.ServeStep().Line,
            Point = t => t.ServeStep().Target, Done = t => t.rated,
        },
        // The trick, taught right after the first serve: a customer who is still waiting takes anything
        // that is ready (cooked, baked, sliced) as a snack, which buys time and earns a tip. Raw food is
        // not a snack - it bonks them and they throw a tomato back - so the arrow only ever leads to
        // food that is ready, and the line follows whether the player is holding something yet.
        new()
        {
            Line = "Customers love a snack!\nGrab something <b>ready</b> to toss them.",
            LineNow = t => t.SnackStep().Line,
            Point = t => t.SnackStep().Target, Done = t => t.snacked, SkipAfter = 25f,
        },
        new() { Line = "That's it - you're a chef!\nStars make the cafe grow. Have fun!" },
    };

    private void Awake()
    {
        Instance = this;
        chef = FindFirstObjectByType<PlayerFSM>();
        if (arrow != null)
        {
            var mesh = arrow.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh != null) tipDepth = -mesh.bounds.min.y;
            arrow.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Named methods: these events are static and outlive a Replay.
    private void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(OnShiftStart);
        EventManager.OnLevelFinish.AddListener(Stop);
        EventManager.OnOrderRated.AddListener(OnRated);
        FoodFight.OnSnackAccepted.AddListener(OnSnack);
    }

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(OnShiftStart);
        EventManager.OnLevelFinish.RemoveListener(Stop);
        EventManager.OnOrderRated.RemoveListener(OnRated);
        FoodFight.OnSnackAccepted.RemoveListener(OnSnack);
        Stop();
    }

    private void OnRated() => rated = true;
    private void OnSnack() => snacked = true;
    private void OnShiftStart()
    {
        if (!CafeProgress.Taught) Begin();
    }

    public void Begin()
    {
        rated = snacked = false;
        step = 0;
        Show();
    }

    public void Stop()
    {
        step = -1;
        Focus = null;
        if (card != null) card.Hide();
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

    private void Show()
    {
        var beat = Beats[step];
        stepStarted = Time.time;
        skipOffered = false;
        Focus = beat.Point?.Invoke(this);
        shownLine = beat.LineNow != null ? beat.LineNow(this) : beat.Line;
        card.Show(shownLine, beat.Done == null ? Next : (Action)null);
    }

    private void Next()
    {
        if (step < 0) return;
        if (++step < Beats.Length) { Show(); return; }
        CafeProgress.Taught = true;
        Stop();
    }

    private void Update()
    {
        if (step < 0) return;
        var beat = Beats[step];
        // The thing to point at can appear late (a customer still walking in) or move on by itself.
        Focus = beat.Point?.Invoke(this);
        PointArrow();

        if (beat.LineNow != null)
        {
            var now = beat.LineNow(this);
            if (now != shownLine) { shownLine = now; card.SetLine(now); }
        }
        if (beat.Done != null && beat.Done(this)) Next();
        // An optional trick is never forced on anyone, and nothing in the lesson hurries them: after a while
        // the card just offers to move on, and the player decides. (This used to skip the step by itself
        // after a timer, which is the opposite of "no time pressure". Scaled time, so the Help screen,
        // which pauses the game, does not count.)
        else if (beat.SkipAfter > 0f && !skipOffered && Time.time - stepStarted > beat.SkipAfter)
        {
            skipOffered = true;
            card.OfferSkip(Next);
        }
    }

    private void PointArrow()
    {
        if (arrow == null) return;
        bool on = Focus != null;
        if (arrow.gameObject.activeSelf != on) arrow.gameObject.SetActive(on);
        if (!on) return;
        // A customer walks to their seat; everything else stays where it is, so it is measured once.
        if (Focus != pointedAt || Focus is NpcFsm)
        {
            pointedAt = Focus;
            above = TopOf(Focus);
        }
        float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)) * arrowBob;
        // The tip - not the origin - is what has to clear the top of the thing, by `arrowGap` at the
        // bottom of every bounce. (Placing the origin there buried the tip 30 cm inside everything.)
        arrow.position = above + Vector3.up * (tipDepth + arrowGap + bob);
    }

    // Every object's own origin is somewhere different - an oven's is on the floor, a plate's is up on the
    // counter - so the arrow goes above what you can actually see: the top of the thing's renderers, plus
    // whatever big piece of furniture is the tap target for it (the oven's tray is tapped through the whole
    // oven), plus, for a customer, the order bubble over their head.
    private Vector3 TopOf(Component target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>()
            .Where(r => r.enabled && r.gameObject.activeInHierarchy && r is not ParticleSystemRenderer).ToList();
        foreach (var proxy in TapProxy.All)
            if (proxy != null && proxy.Target == target)
                renderers.AddRange(proxy.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r is not ParticleSystemRenderer));

        Vector3 top;
        if (renderers.Count == 0)
        {
            top = target.transform.position + Vector3.up * 0.9f;
        }
        else
        {
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            // A statically batched renderer can report the bounds of the whole combined mesh, which can be
            // the entire kitchen; fall back to the object itself when the box is clearly not about it.
            top = bounds.size.magnitude > 6f
                ? target.transform.position + Vector3.up * 0.9f
                : new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }
        if (target is NpcFsm) top.y += aboveCustomer;
        return top;
    }

    #region Finding what to point at
    private Component Source(string food)
    {
        foreach (var source in FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None))
            if (source.name.ToLower().Contains(food)) return source;
        return null;
    }

    private Component Nearest<T>() where T : Component
    {
        var all = FindObjectsByType<T>(FindObjectsSortMode.None);
        return all.Length == 0 ? null
            : all.OrderBy(x => Vector3.Distance(x.transform.position, chef.transform.position)).First();
    }

    #region Working out the next tap
    // The six ingredients, one crate each. BurgerReview has no opinion about their ORDER, only about what is
    // on the plate: raw, burnt or whole things, no patty, no bun, and the same ingredient twice ("Double
    // Trouble"); the top bun is added by the plate itself. So the classic burger is one of each, in any
    // order, and the lesson walks through whichever are still missing - never the same one twice.
    private static readonly string[] Classic = { "bun", "burger", "tomato", "lettuce", "cheese", "onion" };
    private static string Label(string ingredient) => ingredient == "burger" ? "patty" : ingredient;

    private struct Step
    {
        public Component Target;
        public string Line;
    }

    // Each beat asks for its target AND its line, several times a frame: work it out once a frame.
    private Step Cached(ref int frame, ref Step cache, Func<Step> work)
    {
        if (frame != Time.frameCount)
        {
            frame = Time.frameCount;
            cache = work();
        }
        return cache;
    }

    private Step stackStep, snackStep, serveStep;
    private int stackFrame = -1, snackFrame = -1, serveFrame = -1;
    private Step StackStep() => Cached(ref stackFrame, ref stackStep, WorkOutStack);
    private Step SnackStep() => Cached(ref snackFrame, ref snackStep, WorkOutSnack);
    private Step ServeStep() => Cached(ref serveFrame, ref serveStep, WorkOutServe);

    // --- what is going on in the kitchen -------------------------------------------------------

    // Something finished and waiting to be taken: baked bun, fried patty, chopped veg.
    private static Component Ready()
    {
        foreach (var oven in FindObjectsByType<Oven>(FindObjectsSortMode.None))
            if (oven.Food is Bun bun && bun.Preparation == Prep.Good) return oven;
        foreach (var pan in FindObjectsByType<Pan>(FindObjectsSortMode.None))
            if (pan.Food is Burger patty && patty.Preparation == Prep.Good) return patty;
        foreach (var board in FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None))
            if (board.Food is CuttableBase sliced && sliced.isSliced) return sliced;
        return null;
    }

    // Something on its way to ready: baking, frying or being chopped. The station is what to point at.
    private static Component Cooking()
    {
        foreach (var oven in FindObjectsByType<Oven>(FindObjectsSortMode.None))
            if (oven.Food is Bun bun && bun.Preparation == Prep.Raw) return oven;
        foreach (var pan in FindObjectsByType<Pan>(FindObjectsSortMode.None))
            if (pan.Food is Burger patty && patty.Preparation == Prep.Raw) return pan;
        foreach (var board in FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None))
            if (board.Food is CuttableBase whole && !whole.isSliced) return board;
        return null;
    }

    // A burnt thing blocks the oven or the pan until it is taken out.
    private static Component BurntOnTheStove()
    {
        foreach (var oven in FindObjectsByType<Oven>(FindObjectsSortMode.None))
            if (oven.Food is Bun bun && bun.Preparation == Prep.Burnt) return oven;
        foreach (var pan in FindObjectsByType<Pan>(FindObjectsSortMode.None))
            if (pan.Food is Burger patty && patty.Preparation == Prep.Burnt) return patty;
        return null;
    }

    private static (Component free, Component any) Pick<T>() where T : NonStackBase
    {
        var all = FindObjectsByType<T>(FindObjectsSortMode.None);
        return (all.FirstOrDefault(station => !station.IsHaveFood()), all.FirstOrDefault());
    }

    // Food in his hands that is not ready yet needs a station: the oven (bun), the pan (patty), the chopping
    // board (veg and cheese). If that station is already busy, "put it in the oven" while the oven is full
    // is exactly the confusing thing - and with his hands full he cannot take the finished one out either -
    // so the honest next tap is the bin. `then` is added after the instruction when there is one.
    private Step StationStep(EdibleBase held, string then = null)
    {
        var (free, name, doIt) = held is Bun ? (Pick<Oven>().free, "oven", "Bake it in the <b>oven</b> first.")
            : held is Burger ? (Pick<Pan>().free, "pan", "Fry it in the <b>pan</b> first.")
            : (Pick<ChoppingBoard>().free, "chopping board", "Veggies and cheese need\n<b>chopping</b> first!");
        if (free != null) return new Step { Target = free, Line = then == null ? doIt : doIt + "\n" + then };
        return new Step { Target = Nearest<Bin>(), Line = $"The {name} is busy -\nbin this one for now." };
    }

    // --- the stack step ------------------------------------------------------------------------

    // The one tap that moves the burger along, from where things stand right now.
    private Step WorkOutStack()
    {
        var held = chef.HeldFood;
        var plates = FindObjectsByType<Plate>(FindObjectsSortMode.None).Where(p => !p.HasHamburger).ToArray();
        // Keep building the burger that is already started; otherwise the plate nearest to him.
        var plate = plates.FirstOrDefault(p => p.LayerCount > 0)
            ?? plates.OrderBy(p => Vector3.Distance(p.transform.position, chef.transform.position)).FirstOrDefault();

        // 1. Something in his hands: where does it go next?
        if (held != null)
        {
            if (held.Preparation == Prep.Burnt) return new Step { Target = Nearest<Bin>(), Line = "Burnt! Into the <b>bin</b> it goes." };
            if (held.Preparation == Prep.Good) return new Step { Target = plate, Line = "Ready! Stack it on a <b>plate</b>." };
            if (held is Bun || held is Burger || held is CuttableBase) return StationStep(held);
            return new Step { Target = Nearest<Bin>(), Line = "Not that one -\ninto the <b>bin</b>." };
        }

        // 2. Hands empty: whatever has finished is ready to take, and a burnt thing has to go.
        var ready = Ready();
        if (ready != null) return new Step { Target = ready, Line = "Take it out - it's ready!" };
        var burnt = BurntOnTheStove();
        if (burnt != null) return new Step { Target = burnt, Line = "Something burnt!\nTake it out first." };

        // 3. Nothing ready: the next ingredient that is not already on a plate, cooking or being chopped,
        // and whose station is free (lettuce needs none).
        var have = new System.Collections.Generic.List<string>();
        foreach (var p in FindObjectsByType<Plate>(FindObjectsSortMode.None)) have.AddRange(p.LayerNames);
        foreach (var oven in FindObjectsByType<Oven>(FindObjectsSortMode.None)) if (oven.Food != null) have.Add(oven.Food.Name);
        foreach (var pan in FindObjectsByType<Pan>(FindObjectsSortMode.None)) if (pan.Food != null) have.Add(pan.Food.Name);
        foreach (var board in FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None)) if (board.Food != null) have.Add(board.Food.Name);
        foreach (var name in Classic)
        {
            if (have.Contains(name) || !StationFree(name)) continue;
            return new Step
            {
                Target = Source(name),
                Line = $"Next up: <b>{Label(name)}</b>!\nOne of each is the classic -\nany six layers work.",
            };
        }

        // 4. Everything that can be started is cooking: wait for the first one, pointing at it.
        return new Step { Target = Cooking(), Line = "Cooking...\nTake each one out when it's ready." };
    }

    private static bool StationFree(string ingredient) => ingredient switch
    {
        "bun" => Any<Oven>(o => !o.IsHaveFood()),
        "burger" => Any<Pan>(p => !p.IsHaveFood()),
        "lettuce" => true,
        _ => Any<ChoppingBoard>(b => !b.IsHaveFood()),   // tomato, cheese, onion
    };

    // --- the serve step ------------------------------------------------------------------------

    // The burger first, the customer second.
    private Step WorkOutServe()
    {
        var held = chef.HeldFood;
        if (held is Hamburger) return new Step { Target = Waiting(), Line = "Now carry it to a\n<b>hungry customer</b>." };
        if (held != null) return new Step { Target = Nearest<Bin>(), Line = "Hands full!\nBin that first." };
        var finished = FindObjectsByType<Plate>(FindObjectsSortMode.None).Select(p => p.FinishedBurger).FirstOrDefault(b => b != null);
        return new Step { Target = finished, Line = "A burger! <b>Pick it up</b>\nfrom the plate." };
    }

    // --- the snack step ------------------------------------------------------------------------

    // Holding something a customer will take as a snack: cooked, baked or sliced (or lettuce, which is
    // ready as it is), and not a whole burger.
    private bool HoldingSnack => chef.HeldFood is { } food && food is not Hamburger && food.Preparation == Prep.Good;

    private Step WorkOutSnack()
    {
        var held = chef.HeldFood;
        if (held is Hamburger) return new Step { Target = Waiting(), Line = "Serve that burger first!" };
        if (HoldingSnack) return new Step { Target = WaitingToSnack(), Line = "Now <b>toss</b> it at a waiting\ncustomer - it earns a tip!" };

        if (held != null)
        {
            if (held.Preparation == Prep.Burnt) return new Step { Target = Nearest<Bin>(), Line = "Oops, that burnt!\nBin it and try again." };
            if (held is Bun || held is Burger || held is CuttableBase) return StationStep(held, "Then it's a <b>snack</b>!");
            return new Step { Target = Nearest<Bin>(), Line = "Not that one -\ninto the <b>bin</b>." };
        }

        // Hands empty. Whatever is ready comes first; then clearing a burnt thing, so a fresh one can go in;
        // then, if something is already on its way, WAIT for it - starting a second one while the first is
        // still baking is the confusion this used to cause (the arrow kept sending him back to the buns
        // and then to an oven that was already full).
        const string intro = "Customers love a snack!\n";
        var ready = Ready();
        if (ready != null) return new Step { Target = ready, Line = intro + "Take it out - it's ready to toss." };
        var burnt = BurntOnTheStove();
        if (burnt != null) return new Step { Target = burnt, Line = "Something burnt!\nTake it out first." };
        var cooking = Cooking();
        if (cooking != null) return new Step { Target = cooking, Line = intro + "Take it out when it's ready,\nthen toss it!" };

        // Nothing under way: lettuce needs no cooking at all, which makes it the quickest snack there is.
        return new Step { Target = Source("lettuce"), Line = intro + "Lettuce needs no cooking -\n<b>grab some</b> to toss them." };
    }
    #endregion

    // The customer who has been waiting longest, so the arrow doesn't flick between tables.
    private Component Waiting()
    {
        foreach (var npc in NpcFsm.Active)
        {
            var state = npc.executingNpcState;
            if (state == ExecutingNpcState.ORDER || state == ExecutingNpcState.WAIT) return npc;
        }
        return null;
    }

    // Only a customer who is already waiting for their order, and has not been thrown anything yet, takes a
    // snack (NpcFsm.CatchThrown): the second thing thrown at anyone is just "I want a BURGER!".
    private Component WaitingToSnack() => NpcFsm.Active.FirstOrDefault(npc => npc.CanTakeSnack);

    private static bool Any<T>(Func<T, bool> test) where T : Component =>
        FindObjectsByType<T>(FindObjectsSortMode.None).Any(test);
    #endregion
}
