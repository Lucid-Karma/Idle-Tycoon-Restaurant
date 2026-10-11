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
// Second version (CrazyGames, average play time 4.5 min - players were leaving inside the lesson): players do
// not read, so every line is two to four words with the one word that matters in bold; nothing waits for a
// tap (no hello card; the goodbye goes away by itself); the burger shows its progress (3/6); baking and frying
// run twice as fast while it lasts (CookSpeed), so nobody stands waiting for a bun; and the snack toss is no
// longer part of the lesson - it is a tip shown once, the first time a customer gets impatient
// (BeginSnackTip), when it is actually useful. Every beat is reported to Google Analytics (tutorial_step).
public class Tutorial : MonoBehaviour
{
    public static Tutorial Instance { get; private set; }
    // The lesson itself (patience frozen, nothing burns, faster cooking); the snack tip is not "Running".
    public static bool Running => Instance != null && Instance.step >= 0 && !Instance.tip;
    public static float CookSpeed => Running ? 2f : 1f;

    // What the arrow is pointing at right now, so the hints can agree with it (HighlightController).
    public static Component Focus { get; private set; }

    [SerializeField] private UICoachCard card;
    [SerializeField] private Transform arrow;
    [SerializeField] private float arrowBob = 0.22f;
    [SerializeField] private float arrowGap = 0.12f;      // clear air between the arrow's TIP and the top of the thing
    [SerializeField] private float aboveCustomer = 1.05f; // a customer's order bubble floats over their head: clear it

    private PlayerFSM chef;
    private int step = -1;
    private bool rated, snacked, tip;
    private float stepStarted;
    private bool skipOffered;
    private string shownLine;
    private Component pointedAt;
    private Vector3 above;
    // The arrow hangs from its own origin, which is the top of the arrowhead: its tip is this far below it.
    private float tipDepth = 0.62f;

    private sealed class Beat
    {
        public string Name;                              // for analytics (tutorial_step)
        public string Line;
        public Func<Tutorial, string> LineNow;           // the line can follow what the player is doing
        public Func<Tutorial, Component> Point;          // what to point at (null: nothing)
        public Func<Tutorial, bool> Done;                // null: wait for a tap
        public float SkipAfter;                          // seconds before "tap to skip" is offered; 0 = never
    }

    // Mochi's face on the card says hello; the first line is already the first thing to do.
    private static readonly Beat[] Beats =
    {
        new()
        {
            Name = "bun", Line = "Tap the <b>buns</b>!",
            Point = t => t.Source("bun"), Done = t => t.chef.HeldFood is Bun,
        },
        new()
        {
            Name = "oven", Line = "Into the <b>oven</b>!",
            Point = t => t.Nearest<Oven>(), Done = t => Any<Oven>(o => o.IsHaveFood()),
        },
        new()
        {
            Name = "patty", Line = "Grab a <b>patty</b>!",
            Point = t => t.Source("burger"), Done = t => t.chef.HeldFood is Burger,
        },
        new()
        {
            Name = "pan", Line = "Onto the <b>pan</b>!",
            Point = t => t.Nearest<Pan>(), Done = t => Any<Pan>(p => p.IsHaveFood()),
        },
        // Veggies are the third station: a whole tomato is a flaw ("A WHOLE tomato?!"), so it is chopped.
        new()
        {
            Name = "tomato", Line = "Grab a <b>tomato</b>!",
            Point = t => t.Source("tomato"), Done = t => t.chef.HeldFood is Tomato,
        },
        new()
        {
            Name = "board", Line = "<b>Chop</b> it!",
            Point = t => t.Nearest<ChoppingBoard>(), Done = t => Any<ChoppingBoard>(b => b.IsHaveFood()),
        },
        // The rest of the burger, one ingredient at a time. The arrow always points at the single next tap
        // (see WorkOutStack), and the line says what it is and how far the burger has got.
        new()
        {
            Name = "stack", Line = "Stack <b>6 layers</b>!",
            LineNow = t => t.StackStep().Line,
            Point = t => t.StackStep().Target, Done = t => Any<Plate>(p => p.FinishedBurger != null),
        },
        // Picking the burger up comes first: the customer is only the target once it is in his hands.
        new()
        {
            Name = "serve", Line = "Grab the <b>burger</b>!",
            LineNow = t => t.ServeStep().Line,
            Point = t => t.ServeStep().Target, Done = t => t.rated,
        },
        // The goodbye goes by itself: nothing in the lesson waits for a tap.
        new() { Name = "done", Line = "You're a chef!\nStars grow your cafe.", Done = t => Time.time - t.stepStarted > 3f },
    };

    // Shown once, after the lesson, the first time a waiting customer is over half way through their patience:
    // a snack (anything ready) buys them time and tips (NpcFsm.CatchThrown).
    private static readonly Beat SnackTip = new()
    {
        Name = "snack_tip", Line = "Toss a <b>snack</b>!",
        LineNow = t => t.SnackStep().Line,
        Point = t => t.SnackStep().Target, Done = t => t.snacked || Time.time - t.stepStarted > SnackTipSeconds,
    };
    private const float SnackTipSeconds = 20f;

    private Beat Current => tip ? SnackTip : Beats[step];

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
        rated = snacked = tip = false;
        step = 0;
        GoogleAnalytics.Event("tutorial_begin", "replay", CafeProgress.Taught);
        Show();
    }

    public void Stop()
    {
        step = -1;
        tip = false;
        Focus = null;
        if (card != null) card.Hide();
        if (arrow != null) arrow.gameObject.SetActive(false);
    }

    // The snack tip: the same card and arrow, but the game is not slowed or frozen for it.
    private void BeginSnackTip()
    {
        CafeProgress.SnackTipShown = true;
        snacked = false;
        tip = true;
        step = 0;
        Show();
    }

    private void Show()
    {
        var beat = Current;
        GoogleAnalytics.Event("tutorial_step", "step", tip ? -1 : step, "step_name", beat.Name);
        stepStarted = Time.time;
        skipOffered = false;
        Focus = beat.Point?.Invoke(this);
        shownLine = beat.LineNow != null ? beat.LineNow(this) : beat.Line;
        card.Show(shownLine, beat.Done == null ? Next : (Action)null);
    }

    private void Next()
    {
        if (step < 0) return;
        if (tip)
        {
            GoogleAnalytics.Event("snack_tip", "outcome", snacked ? "tossed" : "timeout");
            Stop();
            return;
        }
        if (++step < Beats.Length) { Show(); return; }
        CafeProgress.Taught = true;
        GoogleAnalytics.Event("tutorial_complete");
        Stop();
    }

    private void Update()
    {
        if (step < 0)
        {
            if (SnackTipDue()) BeginSnackTip();
            return;
        }
        var beat = Current;
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

    // After the lesson, during a shift, the first time someone who could take a snack has used up half their
    // patience. Checked twice a second.
    private float nextTipCheck;
    private bool SnackTipDue()
    {
        if (Time.time < nextTipCheck) return false;
        nextTipCheck = Time.time + 0.5f;
        if (!CafeProgress.Taught || CafeProgress.SnackTipShown || Time.timeScale == 0f) return false;
        return NpcFsm.Active.Any(npc => npc.CanTakeSnack && npc.WaitedSeconds > npc.Patience * 0.5f);
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
        var (free, name, doIt) = held is Bun ? (Pick<Oven>().free, "Oven", "<b>Bake</b> it first!")
            : held is Burger ? (Pick<Pan>().free, "Pan", "<b>Fry</b> it first!")
            : (Pick<ChoppingBoard>().free, "Board", "<b>Chop</b> it first!");
        if (free != null) return new Step { Target = free, Line = then == null ? doIt : doIt + "\n" + then };
        return new Step { Target = Nearest<Bin>(), Line = $"{name} busy - <b>bin</b> it." };
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
            if (held.Preparation == Prep.Burnt) return new Step { Target = Nearest<Bin>(), Line = "Burnt! <b>Bin</b> it." };
            if (held.Preparation == Prep.Good) return new Step { Target = plate, Line = $"Onto a <b>plate</b>! {Layers(plate)}" };
            if (held is Bun || held is Burger || held is CuttableBase) return StationStep(held);
            return new Step { Target = Nearest<Bin>(), Line = "Not that - <b>bin</b> it." };
        }

        // 2. Hands empty: whatever has finished is ready to take, and a burnt thing has to go.
        var ready = Ready();
        if (ready != null) return new Step { Target = ready, Line = $"Ready! <b>Take</b> it! {Layers(plate)}" };
        var burnt = BurntOnTheStove();
        if (burnt != null) return new Step { Target = burnt, Line = "Burnt! <b>Take</b> it out." };

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
                Line = $"Next: <b>{Label(name)}</b>! {Layers(plate)}",
            };
        }

        // 4. Everything that can be started is cooking: wait for the first one, pointing at it.
        return new Step { Target = Cooking(), Line = $"Cooking... {Layers(plate)}" };
    }

    // How far the burger has got, as the player counts it: "(3/6)".
    private static string Layers(Plate plate) => $"<color=#B96D8B>({(plate != null ? plate.LayerCount : 0)}/6)</color>";

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
        if (held is Hamburger) return new Step { Target = Waiting(), Line = "<b>Serve</b> a customer!" };
        if (held != null) return new Step { Target = Nearest<Bin>(), Line = "Hands full - <b>bin</b> it." };
        var finished = FindObjectsByType<Plate>(FindObjectsSortMode.None).Select(p => p.FinishedBurger).FirstOrDefault(b => b != null);
        // Handed over and being eaten: the card used to flick back to "pick it up" until they paid.
        if (finished == null) return new Step { Target = null, Line = "Served! Let's see..." };
        return new Step { Target = finished, Line = "Grab the <b>burger</b>!" };
    }

    // --- the snack step ------------------------------------------------------------------------

    // Holding something a customer will take as a snack: cooked, baked or sliced (or lettuce, which is
    // ready as it is), and not a whole burger.
    private bool HoldingSnack => chef.HeldFood is { } food && food is not Hamburger && food.Preparation == Prep.Good;

    private Step WorkOutSnack()
    {
        var held = chef.HeldFood;
        if (held is Hamburger) return new Step { Target = Waiting(), Line = "<b>Serve</b> that first!" };
        if (HoldingSnack) return new Step { Target = WaitingToSnack(), Line = "<b>Tap</b> a hungry customer!" };

        if (held != null)
        {
            if (held.Preparation == Prep.Burnt) return new Step { Target = Nearest<Bin>(), Line = "Burnt! <b>Bin</b> it." };
            if (held is Bun || held is Burger || held is CuttableBase) return StationStep(held);
            return new Step { Target = Nearest<Bin>(), Line = "Not that - <b>bin</b> it." };
        }

        // Hands empty. Whatever is ready comes first; then clearing a burnt thing, so a fresh one can go in;
        // then, if something is already on its way, WAIT for it - starting a second one while the first is
        // still baking is the confusion this used to cause (the arrow kept sending him back to the buns
        // and then to an oven that was already full).
        var ready = Ready();
        if (ready != null) return new Step { Target = ready, Line = "Hungry customer!\n<b>Take</b> it, toss it!" };
        var burnt = BurntOnTheStove();
        if (burnt != null) return new Step { Target = burnt, Line = "Burnt! <b>Take</b> it out." };
        var cooking = Cooking();
        if (cooking != null) return new Step { Target = cooking, Line = "Hungry customer!\nToss them a <b>snack</b>!" };

        // Nothing under way: lettuce needs no cooking at all, which makes it the quickest snack there is.
        return new Step { Target = Source("lettuce"), Line = "Hungry customer!\nToss them <b>lettuce</b>!" };
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
