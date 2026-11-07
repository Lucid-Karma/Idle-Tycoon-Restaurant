using System;
using System.Collections;
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
// Third version (user: "it feels even worse than before - make it as short as possible"; tested as a player): the
// first six steps were a fixed sequence (buns, oven, patty, pan, tomato, board), and a player who did anything out of
// order hit a dead end - a raw bun put straight on the plate left "Into the oven!" pointing at an empty oven forever.
// Now the whole lesson is the one step that always works out the next tap from where things stand (WorkOutStack),
// the order is the shortest real burger (a Cheeseburger: bun, patty, cheese - Orders.LessonRecipe), cooking runs
// three times as fast, and it ends the moment the burger is on the table: a short "Your first burger!" with
// confetti, no watching. Gone again: Mochi walking about as a guide, the card sliding from side to side (it covered
// customers), the tomato joke at the end (a punishment right after the first success) and throwing food at
// customers during the lesson (a new player "giving the tomato to the customer" got a tomato in the face).
// Fourth version (2026-10-20; CrazyGames: 66% of players leave inside the first minute, and the bot that plays the
// lesson like a careful new player needed 13 taps and 37 s to its first coins): the first order is just a bun and a
// patty, and the kitchen has them ready when the game opens - a baked bun in the oven, a fried patty on the pan - so
// the first burger is six taps and the first coins come in about 15 s. A short cheer, and the second customer's
// Cheeseburger is the one made from scratch (oven, pan, board) with the same arrow. Since 2026-10-21 that one is a
// plain Hamburger too, made from scratch (oven and pan): a new player who served it without the cheese - or gave the
// first one to the wrong customer - was told "Where's my cheese?!" right in the lesson (user: "better if they don't
// want cheese"). The board is shown by the hints in the first real orders.
// While it runs everything dims - the kitchen and the HUD, all but Mochi's card - except the thing to tap
// (UISpotlight), and a tap anywhere else only makes the light pulse (AcceptsTap): the one thing to do is the only
// thing to do. No arrow any more (user: "too chaotic"). A new player's first lesson opens with short "stylized action"
// titles (LessonIntro: camera close-ups, speed lines, slammed captions), then the first step.
// Fifth version (2026-10-30; user: "tapping ahead isn't taught at all - the player must be able to make the burger they
// want in one session of taps, and understand very well that they can"; then "a stack: while we tap he runs round
// collecting the ingredients"): the chef carries a stack (PlayerFSM) and both burgers are PLANNED (ComboFirst, Combo):
// the light and the hand jump to the next thing the moment one is tapped, while he is still walking to the first, and
// he works through them in order - the bun, the patty, the plate, the plate again, the customer; then from the crates:
// buns, patties, oven, pan, oven, pan, plate, plate, customer. What is tapped and waiting gets a teal ring
// (UITapQueue); the card says two or three words ("Tap - don't wait!", "Keep tapping!", "Watch him go!"; user: "less
// text, and not numbers all over the scene"). If anything leaves the plan (steering by hand...) the rest is shown one
// tap at a time, as before.
// New things after the lesson are taught where they happen, with the same card and pointing hand but nothing frozen
// (2026-10-28, from the report: the genre's best put the player into the first order at once and spread the teaching
// through the game): the snack toss the first time someone gets impatient, each new station's first go the first time
// one of its orders comes in (BeginStationTip: bacon at level 3, fries at level 5), and tapping ahead (the tap queue)
// when the chef sets off on a long walk and the player is not tapping ahead yet - up to three times, until they do
// (BeginQueueTip).
public class Tutorial : MonoBehaviour
{
    public static Tutorial Instance { get; private set; }
    // The lesson itself (patience frozen, nothing burns, faster cooking); the snack tip is not "Running".
    public static bool Running => Instance != null && Instance.step >= 0 && !Instance.tip;
    public static float CookSpeed => Running ? 3f : 1f;
    // The lesson's burger planned in one go: its taps queue up (PlayerFSM) and say exactly what they were for.
    public static bool Planning => Running && (Instance.Current == Combo || Instance.Current == ComboFirst) && Instance.plan != null && !Instance.planBroken;

    // What the arrow is pointing at right now, so the hints can agree with it (HighlightController).
    public static Component Focus { get; private set; }

    [SerializeField] private UICoachCard card;
    [SerializeField] private TMPro.TMP_FontAsset titleFont;   // the opening titles' face (Bangers SDF, TitleFontBuilder)
    [SerializeField] private Transform arrow;
    [SerializeField] private float arrowBob = 0.22f;
    [SerializeField] private float arrowGap = 0.12f;      // clear air between the arrow's TIP and the top of the thing
    [SerializeField] private float aboveCustomer = 1.05f; // a customer's order bubble floats over their head: clear it

    private PlayerFSM chef;
    private UISpotlight spot;
    private Rect focusRect;      // the thing to tap, on screen, this frame
    private bool focusOnScreen;
    private int step = -1;
    private bool snacked, tip;
    private Beat tipBeat;                // the tip on show (snack, a station's first go, tapping ahead)
    private bool tipAside;               // hidden while a card is open over the kitchen
    private Stations.Kind tipStation;    // for a station's first go
    private bool friesTossed;
    private int ratedCount;            // orders paid during the lesson
    private NpcFsm firstServed;        // the first customer, who may still be eating when the second order starts
    private bool intro;                // the opening titles are playing: the first step waits
    private static bool introShown;    // once a session
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetIntro() => introShown = false;   // (domain reload is off in the editor)
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
        public Action<Tutorial> Enter;                   // something that happens as the beat starts
        public Func<Tutorial, bool> Lost;                // the step can no longer be done: back to the one before
    }

    // (Before Beats, which holds them: static fields are set in the order they are written.)
    // Both burgers of the lesson are planned in one go: every tap numbered on the kitchen at once (The planned burger).
    private static readonly Beat ComboFirst = new()
    {
        Name = "combo1", Line = "Tap - <b>don't wait</b>!",
        Enter = t => t.StartPlan(true),
        LineNow = t => t.PlanLine(),
        Point = t => t.PlanTarget(),
        Done = t => t.ratedCount >= 1 || Served(null) != null,
    };

    private static readonly Beat Combo = new()
    {
        Name = "combo", Line = "Now <b>from scratch</b>!",
        Enter = t => t.StartPlan(false),
        LineNow = t => t.PlanLine(),
        Point = t => t.PlanTarget(),
        Done = t => t.ratedCount >= 2 || Served(t.firstServed) != null,
    };

    // Mochi's face on the card says hello; the first line is already the first thing to do.
    private static readonly Beat[] Beats =
    {
        // The first burger: bun and patty are ready on the oven and the pan (ReadyKitchen) - tap the bun, the patty, the
        // plate, the plate again, the customer, all in a row: he grabs both and carries them (PlayerFSM's stack).
        ComboFirst,
        // The first coins: a cheer, and straight on to the next one.
        new()
        {
            Name = "first", Line = "<b>First</b> burger!",
            Enter = t =>
            {
                t.Celebrate();
                t.firstServed = Served(null);
            },
            Done = t => Time.time - t.stepStarted > 1.8f,
        },
        // The second customer's Hamburger, from the crates (oven and pan), planned in one go.
        Combo,
        // A short cheer, and the shift is the player's.
        new()
        {
            Name = "done", Line = "<b>One</b> go, <b>one</b> burger!",
            Enter = t => t.Celebrate(), Done = t => Time.time - t.stepStarted > 3f,
        },
    };

    // A customer (other than `except`) who has their burger: eating it, or it is on their table while they are still
    // walking to it (they can be served on their way in, and only start eating once they sit).
    private static NpcFsm Served(NpcFsm except) => NpcFsm.Active.FirstOrDefault(n => n != null && n != except
        && (n.executingNpcState == ExecutingNpcState.EAT
            || n.chair != null && n.chair.GetTableService() is ServiceBase table && table.IsHaveFood()));

    private static Beat Make(string name) => new()
    {
        Name = name, Line = "Grab a <b>bun</b>!",
        LineNow = t => t.StackStep().Line,
        Point = t => t.StackStep().Target,
        Done = t => t.chef.HeldFood is Hamburger || Any<Plate>(p => p.FinishedBurger != null || p.CoversAnOrder),
    };

    // Picking the burger up comes first: the customer is only the target once it is in his hands. Done as soon as
    // it is on their table - no waiting for them to finish eating.
    private static Beat Serve(string name, Func<Tutorial, bool> served) => new()
    {
        Name = name, Line = "Grab the <b>burger</b>!",
        LineNow = t => t.ServeStep().Line,
        Point = t => t.ServeStep().Target,
        Done = served,
        // The burger went in the bin (or anywhere it is no longer a burger to serve): make another.
        Lost = t => t.chef.HeldFood is not Hamburger && !Any<Plate>(p => p.FinishedBurger != null || p.CoversAnOrder) && !served(t),
    };

    // Shown once, after the lesson, the first time a waiting customer is over half way through their patience:
    // a snack (anything ready) buys them time and tips (NpcFsm.CatchThrown).
    private static readonly Beat SnackTip = new()
    {
        Name = "snack_tip", Line = "Toss a <b>snack</b>!",
        LineNow = t => t.SnackStep().Line,
        Point = t => t.SnackStep().Target,
        // Over when it is thrown; given up after a while - but not while the snack is in his hands and there is
        // someone to throw it to (fetching it could take most of the time).
        Done = t => t.snacked || Time.time - t.stepStarted > SnackTipSeconds && !(t.HoldingSnack && t.WaitingToSnack() != null),
    };
    private const float SnackTipSeconds = 20f;

    // A new station's first go (Stations): its food from the crate, onto its cooker, and on to where it goes - the
    // bacon onto the burger's plate, the fries tossed to the customer with the little carton by their order. Over when
    // that is done; given up (and offered again next time) once nobody is waiting for it any more.
    private static readonly Beat BaconTip = new()
    {
        Name = "bacon_tip", Line = "New: <b>bacon</b>!",
        LineNow = t => t.BaconStep().Line,
        Point = t => t.BaconStep().Target,
        Done = t => t.BaconOnABurger() || !t.BaconStillWanted(),
        SkipAfter = 25f,
    };

    private static readonly Beat FriesTip = new()
    {
        Name = "fries_tip", Line = "New: <b>fries</b>!",
        LineNow = t => t.FriesStep().Line,
        Point = t => t.FriesStep().Target,
        Done = t => t.friesTossed || !t.FriesStillWanted(),
        SkipAfter = 25f,
    };

    // Everything else the player needs is taught the first time it matters, one thing at a time (user, 2026-10-30:
    // "chopping isn't shown in the onboarding - teach everything the player needs to know slowly, each when its time
    // comes; they'll learn every feature, but not all at once"). The lesson only teaches tapping in a row, the oven, the
    // pan, the plate and serving; then:
    //   the board - the first order that needs veg or cheese (ChopTip);
    //   the bin   - the first time something burns (BinTip);
    //   a snack   - the first time a customer gets impatient (SnackTip);
    //   the shop  - the first time something in it can be bought (ShopTip);
    //   bacon, fries - their stations' first orders (BaconTip, FriesTip); tapping ahead - a long walk (QueueTip).
    // Each is two or three words on Mochi's card with the light and the hand on what to tap, and nothing frozen. One
    // left half done comes back the next time (except the shop's, said once).
    private static readonly Beat ChopTip = new()
    {
        Name = "chop_tip", Line = "<b>Chop</b> it!",
        LineNow = t => t.ChopStep().Line,
        Point = t => t.ChopStep().Target,
        Done = t => t.SlicedOnAPlate() || !t.ChopStillWanted(),
        SkipAfter = 25f,
    };

    private static readonly Beat BinTip = new()
    {
        Name = "bin_tip", Line = "Burnt! <b>Bin</b> it.",
        LineNow = t => t.BinStep().Line,
        Point = t => t.BinStep().Target,
        Done = t => t.BinStep().Target == null,
        SkipAfter = 20f,
    };

    // Mochi, once he is in the cafe: the first time the chef carries a burger, "put it on a plate - he serves it".
    private static readonly Beat MochiTip = new()
    {
        Name = "mochi_tip", Line = "<b>Plate</b> it - Mochi serves!",
        Point = t => t.FreePlate(),
        Done = t => Any<Plate>(p => p.FinishedBurger != null) || !t.chef.CarriesBurger,
        SkipAfter = 12f,
    };

    private static readonly Beat ShopTip = new()
    {
        Name = "shop_tip", Line = "<b>Upgrades</b>!",
        Point = t => t.ShopButton(),
        Done = t => ShopOpen() || Time.time - t.stepStarted > 7f,
    };

    // Tapping ahead (PlayerFSM's tap queue): said while he is on a long walk; over as soon as the player tries it.
    private static readonly Beat QueueTip = new()
    {
        Name = "queue_tip", Line = "<b>Don't wait</b> - keep tapping!",
        Done = t => t.chef.Queued.Count >= 2 || Time.time - t.stepStarted > 6f,
    };

    private Beat Current => tip ? tipBeat : Beats[step];

    private void Awake()
    {
        Instance = this;
        // The whole-cafe picture, before the opening titles or the follow camera move anything (InClearView).
        var cam = Camera.main;
        if (cam != null) (homePos, homeRot, homeSize) = (cam.transform.position, cam.transform.rotation, cam.orthographicSize);
        chef = FindFirstObjectByType<PlayerFSM>();
        found.Clear();   // (a scene loaded again: the last one's things are gone)
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
        NpcFsm.FriesServed += OnFriesServed;
    }

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(OnShiftStart);
        EventManager.OnLevelFinish.RemoveListener(Stop);
        EventManager.OnOrderRated.RemoveListener(OnRated);
        FoodFight.OnSnackAccepted.RemoveListener(OnSnack);
        NpcFsm.FriesServed -= OnFriesServed;
        Stop();
    }

    private void OnRated() => ratedCount++;
    private void OnSnack() => snacked = true;
    private void OnFriesServed(NpcFsm npc) => friesTossed = true;
    private void OnShiftStart()
    {
        if (!CafeProgress.Taught) Begin();
    }

    public void Begin()
    {
        snacked = tip = false;
        ratedCount = 0;
        firstServed = null;
        step = 0;
        StartCoroutine(ReadyKitchen());
        GoogleAnalytics.Event("tutorial_begin", "replay", CafeProgress.Taught);
        if (!introShown)
        {
            introShown = true;
            intro = true;
            card.Hide();
            // The opening is a joke, not titles (user, 2026-11-04; CrazyGames: half the players left in the first
            // minute): a tomato out of nowhere hits the chef, the chef in the corner card laughs at him, big
            // (UIChefLaugh), and the lesson starts - he is its teacher.
            StartCoroutine(TomatoOpening(() => { intro = false; if (step >= 0) Show(); }));
            return;
        }
        Show();
    }

    // What a customer sitting down during the lesson orders: a Hamburger (bun and patty), the first from what the
    // kitchen has ready, the second from scratch.
    public static Recipe LessonOrder() => Orders.FirstRecipe;

    // A baked bun in the oven and a fried patty on the pan, waiting (nothing burns while the lesson runs): the first
    // burger needs no cooking. Only onto empty stations (the Help screen can start the lesson again mid-shift).
    private IEnumerator ReadyKitchen()
    {
        var oven = FindFirstObjectByType<Oven>();
        var pan = FindObjectsByType<Pan>(FindObjectsSortMode.InstanceID).FirstOrDefault(x => x.isActiveAndEnabled && !x.IsHaveFood());
        EdibleBase bun = null, patty = null;
        if (oven != null && !oven.IsHaveFood() && Source("bun") is IngredientsSource buns) bun = buns.SpawnInto(oven);
        if (pan != null && Source("burger") is IngredientsSource patties) patty = patties.SpawnInto(pan);
        yield return null;   // their Start sets up their looks first
        yield return null;
        if (bun is Bun b && b.Preparation == Prep.Raw) b.SetCookedBun();
        if (patty is Burger f && f.Preparation == Prep.Raw) f.SetCooked();
    }

    // While the lesson runs (titles included) the HUD's buttons - help, sound, shop, bonus - take no taps (user: "help
    // and the shop can still be clicked during onboarding"); a tap there falls through to the kitchen and only makes
    // the spotlight pulse. Only buttons on the HUD's own canvas: Mochi's card and the break card have canvases of their
    // own. Each gets a CanvasGroup so its look is untouched (a non-interactable Button would grey out).
    private CanvasGroup[] hudButtons;
    private bool hudLocked;

    private void LockHud(bool locked)
    {
        if (locked == hudLocked) return;
        hudLocked = locked;
        if (hudButtons == null)
        {
            var panel = GameObject.Find("<<<UI>>>/StaticCanvas/InGamePanel");
            hudButtons = panel == null ? new CanvasGroup[0] : panel.GetComponentsInChildren<UnityEngine.UI.Selectable>(true)
                .Where(b => b.GetComponentInParent<Canvas>(true) is Canvas c && c.isRootCanvas)
                .Select(b => b.TryGetComponent<CanvasGroup>(out var g) ? g : b.gameObject.AddComponent<CanvasGroup>())
                .ToArray();
        }
        foreach (var group in hudButtons)
            if (group != null) group.blocksRaycasts = !locked;
    }

    // What to tap swells and settles once a beat (TapPulse; user: "a player who doesn't read should still see the
    // burger has to be tapped to finish it"). Always for the last tap of a burger - the plate to close ("Done!") and
    // the finished burger to take; anything else only once the player has not found it for a while (IdleNudge), so
    // the kitchen isn't bouncing all the time. Never a customer (their order bubble already says who).
    private const float IdleNudge = 5f;
    private TapPulse pulse;
    private Component pulsing, focusSeen;
    private float focusSince;

    private void Nudge(Beat beat)
    {
        if (Focus != focusSeen) { focusSeen = Focus; focusSince = Time.time; }
        Component target = null;
        bool finish = false, patty = false;
        if (Running && Focus != null && focusOnScreen && Focus is not NpcFsm)
        {
            string line = beat.LineNow != null ? beat.LineNow(this) : beat.Line;
            finish = Focus is Hamburger || (line != null && line.StartsWith("Done!"));
            // A fried patty is brown on a dark pan, hard to pick out (user, 2026-10-28): it swells at once, and more.
            patty = Focus is Burger;
            if (finish || patty || Time.time - focusSince > IdleNudge) target = Focus;
        }
        if (target == pulsing && (target == null || pulse != null)) return;
        if (pulse != null) pulse.Stop();
        pulse = target != null ? TapPulse.On(target.transform, finish ? 0.16f : patty ? 0.22f : 0.09f, finish || patty ? 0.9f : 1.5f) : null;
        pulsing = target;
    }

    public void Stop()
    {
        plan = null;
        plannedTap = null;
        tipAside = false;
        if (pulse != null) pulse.Stop();
        pulse = null;
        pulsing = null;
        step = -1;
        tip = false;
        Focus = null;
        if (card != null) card.Hide();
        if (arrow != null) arrow.gameObject.SetActive(false);
        if (spot != null) spot.Hide();
        focusOnScreen = false;
        sayUntil = -1f;
    }

    private void Celebrate()
    {
        GameSfx.Play(GameSfx.Cue.LevelUp);
        if (CafeShop.Instance != null) CafeShop.Instance.Confetti(chef.transform.position + Vector3.up * 1.6f);
    }

    // A line said for a moment over the step's own (the player did something the lesson is not about).
    private string sayLine;
    private float sayUntil = -1f;

    public static void Say(string line, float seconds = 2.2f)
    {
        if (Instance == null || Instance.step < 0) return;
        Instance.sayLine = line;
        Instance.sayUntil = Time.time + seconds;
    }

    // The snack tip: the same card and arrow, but the game is not slowed or frozen for it.
    private void BeginSnackTip()
    {
        CafeProgress.SnackTipShown = true;
        snacked = false;
        tipBeat = SnackTip;
        tip = true;
        step = 0;
        Show();
    }

    // A new station's first go: marked as given now, and given back if it is walked away from (Next).
    private void BeginStationTip(Stations.Kind station)
    {
        Stations.MarkTaught(station, true);
        tipStation = station;
        tipBeat = station == Stations.Kind.Bacon ? BaconTip : FriesTip;
        friesTossed = false;
        tip = true;
        step = 0;
        Show();
    }

    // How many times the tip has been shown; QueueTipLearnt once the player taps ahead by themselves.
    private const string QueueTipKey = "Cafe.QueueTip";
    private const int QueueTipTimes = 3, QueueTipLearnt = 9;
    private float queueTipShownAt = -999f;

    private void BeginQueueTip()
    {
        GamePlatform.SetInt(QueueTipKey, GamePlatform.GetInt(QueueTipKey, 0) + 1);
        GamePlatform.Flush();
        queueTipShownAt = Time.time;
        tipBeat = QueueTip;
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
        beat.Enter?.Invoke(this);
        shownLine = beat.LineNow != null ? beat.LineNow(this) : beat.Line;
        card.Show(shownLine, beat.Done == null ? Next : (Action)null);
    }

    private void Next()
    {
        if (step < 0) return;
        if (tip)
        {
            if (tipBeat == SnackTip) GoogleAnalytics.Event("snack_tip", "outcome", snacked ? "tossed" : "timeout");
            else if (tipBeat == ChopTip || tipBeat == BinTip || tipBeat == MochiTip)
            {
                // Done: never again. Walked away from: again the next time it matters.
                bool done = tipBeat == ChopTip ? SlicedOnAPlate() : tipBeat == BinTip ? BinStep().Target == null
                          : Any<Plate>(p => p.FinishedBurger != null);
                if (done) { Learnt(tipBeat == ChopTip ? ChopKey : tipBeat == BinTip ? BinKey : MochiKey); Celebrate(); }
                GoogleAnalytics.Event("tip", "name", tipBeat.Name, "outcome", done ? "done" : "left");
            }
            else if (tipBeat == BaconTip || tipBeat == FriesTip)
            {
                bool done = tipBeat == BaconTip ? BaconOnABurger() : friesTossed;
                // Walked away from (or skipped): offered again the next time one of its orders comes in.
                if (!done) Stations.MarkTaught(tipStation, false);
                GoogleAnalytics.Event("station_tip", "station", tipStation.ToString(), "outcome", done ? "done" : "left");
                if (done) Celebrate();
            }
            Stop();
            return;
        }
        if (++step < Beats.Length) { Show(); return; }
        CafeProgress.Taught = true;
        GoogleAnalytics.Event("tutorial_complete");
        Stop();
        // Now the day can begin properly: its goal, held back while the lesson ran (UIToast).
        var toast = FindFirstObjectByType<UIToast>();
        if (toast != null) toast.ShowObjective();
    }

    private void Update()
    {
        LockHud(Running);
        // Tapping ahead by themselves (two or more waiting taps, outside the lesson): the tip has done its job.
        if (!Running && chef != null && chef.Queued.Count >= 2 && GamePlatform.GetInt(QueueTipKey, 0) < QueueTipLearnt)
        {
            GamePlatform.SetInt(QueueTipKey, QueueTipLearnt);
            GamePlatform.Flush();
        }
        if (step < 0)
        {
            // (The board's, the bin's and the shop's are looked at twice a second: they search the kitchen.)
            bool look = Time.time >= nextLearnCheck;
            if (look) nextLearnCheck = Time.time + 0.5f;
            if (StationTipDue(out var station)) BeginStationTip(station);
            else if (look && TipDue(ChopKey, ChopDue)) BeginTip(ChopTip);
            else if (look && TipDue(BinKey, BinDue)) BeginTip(BinTip);
            else if (look && TipDue(MochiKey, MochiDue)) BeginTip(MochiTip);
            else if (SnackTipDue()) BeginSnackTip();
            else if (look && TipDue(ShopKey, ShopDue)) { Learnt(ShopKey); BeginTip(ShopTip); }
            else if (QueueTipDue()) BeginQueueTip();
            return;
        }
        if (intro) return;
        // A tip steps aside while a card is open over the kitchen (the book, the week's tasks): its line lay over the
        // card's button. It comes back as the card closes.
        if (tip && UIBreakCard.Instance != null && UIBreakCard.Instance.Showing)
        {
            if (!tipAside)
            {
                tipAside = true;
                card.Hide();
                if (spot != null) { spot.Hide(); spot.ShowHand(false); }
            }
            return;
        }
        if (tipAside)
        {
            tipAside = false;
            card.Show(shownLine, null);
        }
        // (Not while he holds the snack: the tip is then half done, and its customer is the one to show - user,
        // 2026-10-28: with the close camera the tip vanished once the lettuce was picked up.)
        if (tip && tipBeat == SnackTip && !snacked && SnackableInView() == null && !(chef.HeldFood is Hamburger) && !HoldingSnack)
        {
            CafeProgress.SnackTipShown = false;   // not taught after all: again, when someone in view is hungry
            Stop();
            return;
        }
        var beat = Current;
        if (beat == Combo || beat == ComboFirst) FollowPlan();
        // The thing to point at can appear late (a customer still walking in) or move on by itself.
        Focus = beat.Point?.Invoke(this);
        PointArrow();
        Spotlight();
        Nudge(beat);

        if (!tip && beat.Lost != null && step > 0 && beat.Lost(this))
        {
            step--;
            Show();
            return;
        }

        var now = Time.time < sayUntil ? sayLine : beat.LineNow != null ? beat.LineNow(this) : beat.Line;
        if (now != shownLine) { shownLine = now; card.SetLine(now); }
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

    // The spotlight on what to tap: the lesson's steps and the snack tip (user: "no dimming in the lettuce lesson");
    // the cheers between steps dim nothing. Only the lesson blocks other taps (AcceptsTap): the tip is optional.
    private void Spotlight()
    {
        if (spot == null)
        {
            spot = UISpotlight.Create();
            // Mochi's card stays above the dim (it is what to read); every other screen stays under it.
            // (Its own nested canvas: raising the root one lifted the whole HUD over the dim.)
            var cardCanvas = card != null ? card.GetComponent<Canvas>() : null;
            if (cardCanvas != null) { cardCanvas.overrideSorting = true; cardCanvas.sortingOrder = 10; }
        }
        focusOnScreen = (Running || tip) && Focus != null && TryScreenRect(Focus, out focusRect);
        if (focusOnScreen) spot.Show(focusRect); else spot.Hide();
        spot.ShowHand(focusOnScreen);
    }

    // During the lesson a tap only counts on (or near) the thing the arrow points at; it is then aimed at that thing,
    // so a fingertip that lands on its edge still works. Anywhere else: nothing happens but the spotlight's punch.
    // The snack tip is on screen (the follow camera shows the whole cafe meanwhile).
    public static bool TipShowing => Instance != null && Instance.step >= 0 && Instance.tip;

    // A tap on what the lesson or the tip is pointing at counts even where a HUD card lies over it (the snack tip's
    // lettuce could be under the order card and could not be tapped - user, 2026-10-27); a button there keeps it.
    public static bool TapsThrough(Vector2 screen)
    {
        var t = Instance;
        if (t == null || Focus == null || !t.focusOnScreen || t.step < 0) return false;
        var r = t.focusRect;
        float slop = Screen.height * 0.04f;
        if (screen.x < r.xMin - slop || screen.x > r.xMax + slop || screen.y < r.yMin - slop || screen.y > r.yMax + slop) return false;
        var events = UnityEngine.EventSystems.EventSystem.current;
        if (events == null) return true;
        var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position = screen }, hits);
        foreach (var hit in hits)
            if (hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null) return false;
        return true;
    }

    // The opening gag (Begin): about two and a half seconds; taps wait for it.
    public static bool Opening { get; private set; }

    private IEnumerator TomatoOpening(System.Action done)
    {
        Opening = true;
        // Not while a phone is still held upright (the portal asks to turn it): the gag waits for the picture, then
        // a moment more (user, 2026-11-04: "it was over before I had turned the phone").
        while (Screen.height > Screen.width) yield return null;
        float openedAt = Time.realtimeSinceStartup;
        // Short (CrazyGames 2026-11-05: 44% of players gone inside the first minute): a beat, the throw, and the
        // lesson starts while the corner chef is still laughing.
        yield return new WaitForSeconds(0.6f);
        var chef = FindFirstObjectByType<PlayerFSM>();
        if (chef != null && FoodFight.Instance != null)
        {
            // From outside the picture (user, 2026-11-04): it comes in over the top-right edge of the screen, flies
            // across, slowly enough to follow, and lands on his head.
            var cam = Camera.main;
            float depth = cam != null ? Vector3.Dot(chef.transform.position - cam.transform.position, cam.transform.forward) : 10f;
            var from = cam != null ? cam.ViewportToWorldPoint(new Vector3(1.1f, 0.95f, depth)) : chef.transform.position + Vector3.up * 6f;
            const float flight = 1.4f;
            FoodFight.Instance.SurpriseTomato(from, flight);
            yield return new WaitForSeconds(flight + 0.4f);   // the flight and the splat; the laugh goes on by itself
        }
        Opening = false;
        GoogleAnalytics.Event("opening_done", "seconds", Time.realtimeSinceStartup - openedAt);
        done();
    }

    // Taps the lesson turned down in a row: the third goes through - the lesson guides, it does not hold the player
    // back (2026-11-04: CrazyGames, half the players gone within the first minute).
    private int refused;

    private static bool Refuse(Tutorial t)
    {
        if (++t.refused < 3) { if (t.spot != null) t.spot.Punch(); return false; }
        t.refused = 0;
        return true;
    }

    public static bool AcceptsTap(Vector3 screen, out Vector3 aim)
    {
        aim = screen;
        if (LessonIntro.Playing) { LessonIntro.Skip(); return false; }   // a tap during the titles skips them
        if (Opening) return false;
        var t = Instance;
        if (t == null || !Running) return true;
        // Planning: every step tapped (or the customer not in yet) - taps wait, so nothing knocks the plan over.
        if (Planning && (Focus == null || !t.focusOnScreen)) return false;
        if (Focus == null || !t.focusOnScreen) return true;
        // A tap on something else the chef could use (the crate beside the oven...) is that thing, not the lit one: it
        // bounces (TapFeedback) and the light nudges; the chef goes nowhere (user, 2026-10-30: "I tapped something
        // else and he went to the oven anyway - the lesson doesn't care about my input").
        if (TappedSomethingElse(screen)) return Refuse(t);
        var r = t.focusRect;
        float slop = Screen.height * 0.035f;
        float dx = Mathf.Max(r.xMin - screen.x, 0f, screen.x - r.xMax), dy = Mathf.Max(r.yMin - screen.y, 0f, screen.y - r.yMax);
        if (dx * dx + dy * dy > slop * slop) return Refuse(t);
        t.refused = 0;
        aim = AimAt(Focus);
        if (Planning) t.TapPlanned();
        return true;
    }

    // The planned tap just made, for the chef (PlayerFSM.HandleScreenTap): exactly the thing its number is on.
    public static bool TakePlannedTap(out PlayerFSM.QueuedTap tap)
    {
        tap = default;
        var t = Instance;
        if (t == null || t.plannedTap == null) return false;
        tap = t.plannedTap.Value;
        t.plannedTap = null;
        return true;
    }

    private static bool TappedSomethingElse(Vector3 screen)
    {
        var cam = Camera.main;
        if (cam == null || Focus == null || Focus is NpcFsm) return false;
        if (!Physics.Raycast(cam.ScreenPointToRay(screen), out var hit, 200f)) return false;
        var col = hit.collider;
        var proxy = col.GetComponent<TapProxy>();
        bool usable = proxy != null || col.GetComponent<EdibleBase>() != null || col.GetComponent<ISpawnable>() != null
                      || col.GetComponent<PlaceableBase>() != null || col.GetComponent<Bin>() != null;
        if (!usable) return false;
        // The lit thing itself, what is on it, what it sits on, or the furniture it is tapped through.
        var focus = Focus.transform;
        if (col.transform.IsChildOf(focus) || focus.IsChildOf(col.transform)) return false;
        if (proxy != null && proxy.Target == Focus) return false;
        if (Focus is NonStackBase station && station.Food != null && col.transform.IsChildOf(station.Food.transform)) return false;
        if (col.GetComponent<EdibleBase>() is EdibleBase food && (Component)food.Placeable == Focus) return false;
        if (Focus is EdibleBase lit && lit.Placeable != null && col.GetComponent<PlaceableBase>() == lit.Placeable) return false;
        foreach (var p in TapProxy.All) if (p != null && p.Target == Focus && col.transform.IsChildOf(p.transform)) return false;
        return true;
    }

    // Where a tap on it lands: a customer's head (that is how serving taps work), the food inside a station whose
    // own collider is off, otherwise the middle of its collider.
    private static Vector3 AimAt(Component c)
    {
        var cam = Camera.main;
        if (c is NpcFsm npc) return cam.WorldToScreenPoint(npc.transform.position + Vector3.up);
        if (c is NonStackBase station && station.Food != null && station.GetComponent<Collider>() is Collider own && !own.enabled) c = station.Food;
        var col = c.GetComponentsInChildren<Collider>().FirstOrDefault(x => x.enabled) ?? c.GetComponentsInChildren<Collider>().FirstOrDefault();
        return cam.WorldToScreenPoint(col != null ? col.bounds.center : c.transform.position);
    }

    // The thing on screen: its renderers (and the furniture tapped for it, and a customer's bubble) plus the arrow.
    private bool TryScreenRect(Component target, out Rect rect)
    {
        rect = default;
        // A HUD button (the shop's): its own rectangle on the screen.
        if (target.transform is RectTransform ui)
        {
            var canvas = ui.GetComponentInParent<Canvas>();
            var uiCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4];
            ui.GetWorldCorners(corners);
            var lo = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]);
            var hi = RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]);
            rect = Rect.MinMaxRect(Mathf.Min(lo.x, hi.x), Mathf.Min(lo.y, hi.y), Mathf.Max(lo.x, hi.x), Mathf.Max(lo.y, hi.y));
            return true;
        }
        var cam = Camera.main;
        if (cam == null) return false;
        var renderers = target.GetComponentsInChildren<Renderer>()
            .Where(x => x.enabled && x.gameObject.activeInHierarchy && x is not ParticleSystemRenderer).ToList();
        foreach (var proxy in TapProxy.All)
            if (proxy != null && proxy.Target == target)
                renderers.AddRange(proxy.GetComponentsInChildren<Renderer>().Where(x => x.enabled && x is not ParticleSystemRenderer));
        Bounds b;
        if (renderers.Count == 0) b = new Bounds(target.transform.position + Vector3.up * 0.5f, Vector3.one);
        else
        {
            b = renderers[0].bounds;
            foreach (var x in renderers) b.Encapsulate(x.bounds);
            if (b.size.magnitude > 6f) b = new Bounds(target.transform.position + Vector3.up * 0.5f, Vector3.one);
        }
        if (target is NpcFsm) b.Encapsulate(b.max + Vector3.up * aboveCustomer);
        if (arrow != null && arrow.gameObject.activeSelf) b.Encapsulate(arrow.position);
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var p = cam.WorldToScreenPoint(new Vector3(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i < 4 ? b.min.z : b.max.z));
            if (p.z < 0f) return false;
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
        rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }

    private void PointArrow()
    {
        // The arrow is gone: the spotlight shows what to tap (user, 2026-10-20: "remove the yellow arrow").
        if (arrow != null && arrow.gameObject.activeSelf) arrow.gameObject.SetActive(false);
        return;
#pragma warning disable CS0162
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
#pragma warning restore CS0162
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
        return NpcFsm.Active.Any(npc => npc.CanTakeSnack && npc.WaitedSeconds > npc.Patience * 0.5f && CustomerInClearView(npc));
    }

    // A station's first go is due when one of its orders is waiting and his hands are free for it (or already hold
    // its food). Checked twice a second, after the lesson, never over another tip.
    private float nextStationCheck;
    private bool StationTipDue(out Stations.Kind station)
    {
        station = default;
        if (Time.time < nextStationCheck || !CafeProgress.Taught || Time.timeScale == 0f) return false;
        nextStationCheck = Time.time + 0.5f;
        if (Stations.TipPending(Stations.Kind.Bacon) && (chef.CanTakeMore || chef.Carries(f => f is Bacon)) && BaconStillWanted())
        {
            station = Stations.Kind.Bacon;
            return true;
        }
        if (Stations.TipPending(Stations.Kind.Fries) && (chef.CanTakeMore || chef.Carries(f => f is Fries)) && FriesStillWanted())
        {
            station = Stations.Kind.Fries;
            return true;
        }
        return false;
    }

    // Tapping ahead is said when he sets off on a long walk to something and nothing is tapped after it: up to three
    // times, a minute apart at least, until the player does it by themselves.
    private float walkingSince = -1f;
    private bool QueueTipDue()
    {
        if (!CafeProgress.Taught || Time.timeScale == 0f || GamePlatform.GetInt(QueueTipKey, 0) >= QueueTipTimes
            || Time.time - queueTipShownAt < 60f || chef.Queued.Count > 0)
            return false;
        if (!chef.OnItsWay) { walkingSince = -1f; return false; }
        if (walkingSince < 0f) walkingSince = Time.time;
        return Time.time - walkingSince > 0.6f && chef.WalkLeft > 4f;
    }

    // --- the board, the bin, the shop (each the first time it matters) ---------------------------

    private const string ChopKey = "Cafe.ChopTip", BinKey = "Cafe.BinTip", ShopKey = "Cafe.ShopTip", MochiKey = "Cafe.MochiTip";

    // Mochi is in the cafe, he is free, and the chef has a burger in his hands.
    private bool MochiDue() => chef.CarriesBurger && Waiter.Free && FreePlate() != null;

    private Component FreePlate() => All<Plate>().Where(p => p.isActiveAndEnabled && !p.HasHamburger && p.LayerCount == 0 && p.name != "Burger Assembler")
        .OrderBy(p => Vector3.Distance(p.transform.position, chef.transform.position)).FirstOrDefault();
    private static readonly string[] Choppable = { "tomato", "cheese", "onion" };
    private float nextLearnCheck;

    // Not learnt yet, after the lesson, the game running.
    private static bool TipDue(string key, Func<bool> due) =>
        CafeProgress.Taught && Time.timeScale > 0f && GamePlatform.GetInt(key, 0) != 1 && due();

    private static void Learnt(string key)
    {
        GamePlatform.SetInt(key, 1);
        GamePlatform.Flush();
    }

    private void BeginTip(Beat beat)
    {
        tipBeat = beat;
        tip = true;
        step = 0;
        Show();
    }

    // Someone waiting wants veg or cheese, and his hands have room for it.
    private bool ChopDue() => chef.CanTakeMore && ChopStillWanted();

    private bool ChopStillWanted() =>
        NpcFsm.Active.Any(n => Orders.Waiting(n) && n.Ordered != null && n.Ordered.Layers.Any(l => Choppable.Contains(l)))
        || chef.Carries(f => f is CuttableBase);

    private bool SlicedOnAPlate() => chef.Carries(f => f is CuttableBase { isSliced: true }) ||
        Any<Plate>(p => p.LayerPreps.Any(l => Choppable.Contains(l.name) && l.prep == Prep.Good)
                        || p.FinishedBurger != null && p.FinishedBurger.Layers.Any(l => Choppable.Contains(l.Name) && l.Prep == Prep.Good))
        || chef.HeldFood is Hamburger burger && burger.Layers.Any(l => Choppable.Contains(l.Name) && l.Prep == Prep.Good);

    private Step chopStep;
    private int chopFrame = -1;
    private Step ChopStep() => Cached(ref chopFrame, ref chopStep, WorkOutChop);

    // Grab it, the board (he chops everything whole he carries), the plate.
    private Step WorkOutChop()
    {
        if (chef.Carries(f => f is CuttableBase { isSliced: false }))
            return new Step { Target = Pick<ChoppingBoard>().free ?? Pick<ChoppingBoard>().any, Line = "<b>Chop</b> it!" };
        if (chef.Carries(f => f is CuttableBase { isSliced: true }))
        {
            var sliced = chef.Carried.First(f => f is CuttableBase { isSliced: true });
            var plate = All<Plate>().Where(p => p.Accepts(sliced))
                .OrderByDescending(p => p.LayerCount).FirstOrDefault();
            return new Step { Target = plate, Line = "Onto the <b>plate</b>!" };
        }
        foreach (var board in All<ChoppingBoard>())
            if (board.Food is CuttableBase { isSliced: true } ready) return new Step { Target = ready, Line = "<b>Take</b> it!" };
        // On the board and being chopped: wait for it (it said "Grab the tomato" again - user, 2026-11-04).
        foreach (var board in All<ChoppingBoard>())
            if (board.Food is CuttableBase { isSliced: false }) return new Step { Target = board, Line = "<b>Chop</b> it!" };
        var wanted = NpcFsm.Active.Where(n => Orders.Waiting(n) && n.Ordered != null)
            .SelectMany(n => n.Ordered.Layers).FirstOrDefault(l => Choppable.Contains(l));
        return new Step { Target = wanted != null ? Source(wanted) : null, Line = $"Grab the <b>{wanted ?? "veg"}</b>!" };
    }

    // Something burnt in his hands or on a cooker.
    private bool BinDue() => BinStep().Target != null;

    private Step binStep;
    private int binFrame = -1;
    private Step BinStep() => Cached(ref binFrame, ref binStep, WorkOutBin);

    private Step WorkOutBin()
    {
        if (chef.Carries(f => f.Preparation == Prep.Burnt)) return new Step { Target = Nearest<Bin>(), Line = "Burnt! <b>Bin</b> it." };
        foreach (var station in All<CookingBase>())
            if (station.Food != null && station.Food.Preparation == Prep.Burnt)
                return new Step { Target = station.Food, Line = "Burnt! <b>Take</b> it." };
        return new Step { Target = null, Line = "" };
    }

    // The first time the shop has something the wallet can pay for - from the second level, and only in a calm moment
    // (nobody waiting for an order, nothing in his hands): straight after the lesson it piled one more thing on.
    // Not as the game opens (user, 2026-11-05: the shop's pictures may not be there yet) nor over the level's end cards.
    private const float ShopTipNotBefore = 45f;
    private bool ShopDue() => CafeProgress.LevelNumber >= 2 && Time.timeSinceLevelLoad > ShopTipNotBefore && ShopIcons.Ready
                              && !(UIBreakCard.Instance != null && UIBreakCard.Instance.Showing)
                              && CafeShop.Instance != null && CafeShop.Instance.AnythingToBuy()
                              && ShopButton() != null && chef.HeldFood == null && !NpcFsm.Active.Any(Orders.Waiting);

    private Component ShopButton()
    {
        var badge = FindFirstObjectByType<UIShopBadge>();
        return badge != null && badge.isActiveAndEnabled ? badge : null;
    }

    private static bool ShopOpen()
    {
        var shop = FindFirstObjectByType<UIShop>(FindObjectsInactive.Include);
        return shop != null && shop.gameObject.activeInHierarchy;
    }

    // --- a station's first go ------------------------------------------------------------------

    private bool BaconStillWanted() =>
        NpcFsm.Active.Any(n => Orders.Waiting(n) && n.Ordered.NeedsBacon) || chef.HeldFood is Bacon || Any<Griddle>(g => g.Food != null);

    private bool FriesStillWanted() => NpcFsm.Active.Any(n => n.FriesDue);

    // Crisp bacon on a plate (or in the burger made from it): the bacon part is done; the burger is the player's.
    private bool BaconOnABurger() => chef.Carries(f => f is Bacon && f.Preparation == Prep.Good) ||
        Any<Plate>(p => p.LayerPreps.Any(l => l.name == "bacon" && l.prep == Prep.Good)
                        || p.FinishedBurger != null && p.FinishedBurger.Layers.Any(l => l.Name == "bacon" && l.Prep == Prep.Good))
        || chef.HeldFood is Hamburger burger && burger.Layers.Any(l => l.Name == "bacon" && l.Prep == Prep.Good);

    private Step baconStep, friesStep;
    private int baconFrame = -1, friesFrame = -1;
    private Step BaconStep() => Cached(ref baconFrame, ref baconStep, WorkOutBacon);
    private Step FriesStep() => Cached(ref friesFrame, ref friesStep, WorkOutFries);

    private Step WorkOutBacon()
    {
        // (The bacon anywhere in his stack.)
        var held = chef.Carried.FirstOrDefault(f => f is Bacon) ?? chef.HeldFood;
        if (held is Bacon bacon)
        {
            if (bacon.Preparation == Prep.Burnt) return new Step { Target = Nearest<Bin>(), Line = "Burnt! <b>Bin</b> it." };
            if (bacon.Preparation == Prep.Raw) return StationStep(held);
            var plate = All<Plate>()
                .Where(p => p.Accepts(held))
                .OrderByDescending(p => p.Order != null && p.Order.NeedsBacon)
                .ThenByDescending(p => p.LayerCount)
                .FirstOrDefault();
            return new Step { Target = plate, Line = "Crispy! Onto the <b>plate</b>!" };
        }
        if (!chef.CanTakeMore) return new Step { Target = null, Line = "Hands full - then <b>bacon</b>!" };
        foreach (var griddle in All<Griddle>())
        {
            if (griddle.Food is not Bacon on) continue;
            // While it crisps nothing is lit (the rest of the kitchen stays bright): the burger's other layers meanwhile.
            if (on.Preparation == Prep.Raw) return new Step { Target = null, Line = "Sizzling! Make the <b>rest</b>\nwhile it crisps." };
            return new Step { Target = on, Line = on.Preparation == Prep.Good ? "Crispy! <b>Take</b> it!" : "Burnt! <b>Take</b> it out." };
        }
        return new Step { Target = Source("bacon"), Line = "New: <b>bacon</b>!\nGrab some!" };
    }

    private Step WorkOutFries()
    {
        var held = chef.Carried.FirstOrDefault(f => f is Fries) ?? chef.HeldFood;
        if (held is Fries fries)
        {
            if (fries.Preparation == Prep.Burnt) return new Step { Target = Nearest<Bin>(), Line = "Burnt! <b>Bin</b> it." };
            if (fries.Preparation == Prep.Raw) return StationStep(held);
            var who = NpcFsm.Active.Where(n => n.FriesDue).OrderByDescending(n => n.WaitedSeconds / Mathf.Max(1f, n.Patience)).FirstOrDefault();
            return new Step { Target = who, Line = "Golden! <b>Tap</b> the customer\nwith the fries - toss!" };
        }
        if (!chef.CanTakeMore) return new Step { Target = null, Line = "Hands full - then <b>fries</b>!" };
        foreach (var fryer in All<Fryer>())
        {
            if (fryer.Food is not Fries on) continue;
            if (on.Preparation == Prep.Raw) return new Step { Target = null, Line = "Frying! Make the <b>burger</b>\nwhile it fries." };
            return new Step { Target = on, Line = on.Preparation == Prep.Good ? "Golden! <b>Take</b> them!" : "Burnt! <b>Take</b> them out." };
        }
        return new Step { Target = Source("potato"), Line = "New: <b>fries</b>!\nGrab a potato!" };
    }

    #region The planned burger
    // Both burgers of the lesson, planned in one go (ComboFirst, Combo; user, 2026-10-30: "while we tap one thing after
    // another he runs round collecting them - that is how it should be taught"). Each step: what to tap, whether he has
    // done it - read off the kitchen (his stack, the oven, the pan, the plate), so a step done another way counts - and
    // what he must carry for it (when he stops with taps missing he goes back to the first step not done; if his stack
    // doesn't fit it, the plan is off and the rest is shown one tap at a time).
    private sealed class PlanStep
    {
        public Component Target;                 // null: the customer waiting for it (whoever that is by then)
        public string What;
        public Func<Tutorial, bool> Done;
        public Func<Tutorial, bool> Hands;
    }

    private PlanStep[] plan;
    private int planTapped, planDone;
    private bool planBroken, planFirst;
    private float planAsked;
    private PlayerFSM.QueuedTap? plannedTap;
    private Oven planOven;
    private Pan planPan;
    private Plate planPlate;

    private bool PlateHas(string layer) => planPlate != null && planPlate.LayerNames.Contains(layer);
    private bool Served1 => planFirst ? ratedCount >= 1 || Served(null) != null : ratedCount >= 2 || Served(firstServed) != null;
    private bool BurgerMade => chef.CarriesBurger || Served1;
    private bool Has<T>() where T : EdibleBase => chef.Carries(f => f is T);
    private bool Has<T>(Prep prep) where T : EdibleBase => chef.Carries(f => f is T && f.Preparation == prep);

    private void StartPlan(bool first)
    {
        planFirst = first;
        plan = null;
        planBroken = false;
        planTapped = planDone = 0;
        plannedTap = null;
        planAsked = Time.time;
        TryMakePlan();
    }

    // The first plan needs the kitchen made ready first (ReadyKitchen puts the bun and the patty in a frame or two).
    private void TryMakePlan()
    {
        var ovens = All<Oven>();
        var pans = All<Pan>();
        planOven = planFirst ? ovens.FirstOrDefault(o => o.Food is Bun b && b.Preparation == Prep.Good) : Pick<Oven>().free as Oven;
        planPan = planFirst ? pans.FirstOrDefault(p => p.Food is Burger b && b.Preparation == Prep.Good) : Pick<Pan>().free as Pan;
        planPlate = null;   // (no plate: the burger is made in his hands)
        var buns = Source("bun");
        var patties = Source("burger");
        if (planOven == null || planPan == null || buns == null || patties == null || chef.HeldFood != null)
        {
            if (Time.time - planAsked > 3f) planBroken = true;   // one tap at a time, then
            return;
        }
        static PlanStep S(Component target, string what, Func<Tutorial, bool> done, Func<Tutorial, bool> hands) =>
            new() { Target = target, What = what, Done = done, Hands = hands };
        Func<Tutorial, bool> room = t => t.chef.CanTakeMore;
        Func<Tutorial, bool> some = t => t.chef.HeldFood != null && t.chef.HeldFood is not Hamburger;
        Func<Tutorial, bool> empty = t => t.chef.HeldFood == null;
        Func<Tutorial, bool> burger = t => t.chef.CarriesBurger;
        if (planFirst)
            plan = new[]
            {
                S(planOven, "the baked <b>bun</b>", t => t.Has<Bun>(Prep.Good) || t.Served1, room),
                S(planPan, "the fried <b>patty</b>", t => t.Has<Burger>(Prep.Good) || t.Served1, room),
                S(null, "the <b>customer</b>", t => t.Served1, burger),
            };
        else
            plan = new[]
            {
                S(buns, "the <b>buns</b>", t => t.Has<Bun>() || t.planOven.Food is Bun || t.Served1, room),
                S(patties, "the <b>patties</b>", t => t.Has<Burger>() || t.planPan.Food is Burger || t.Served1, room),
                S(planOven, "the <b>oven</b>: bun in", t => t.planOven.Food is Bun || t.Has<Bun>(Prep.Good) || t.Served1, t => t.Has<Bun>(Prep.Raw)),
                S(planPan, "the <b>pan</b>: patty on", t => t.planPan.Food is Burger || t.Has<Burger>(Prep.Good) || t.Served1, t => t.Has<Burger>(Prep.Raw)),
                S(planOven, "the <b>oven</b>: baked bun out", t => t.Has<Bun>(Prep.Good) || t.Served1, room),
                S(planPan, "the <b>pan</b>: patty out", t => t.Has<Burger>(Prep.Good) || t.Served1, room),
                S(null, "the <b>customer</b>", t => t.Served1, burger),
            };
        GoogleAnalytics.Event("combo_plan", "steps", plan.Length, "first", planFirst);
    }

    // Where the plan stands: how much he has done; and if he has stopped while taps are missing from what he did (one
    // came to nothing), back to the first step not done - unless his stack does not fit it: then the plan is off.
    private void FollowPlan()
    {
        if (plan == null && !planBroken) TryMakePlan();
        if (plan == null || planBroken) return;
        int done = 0;
        while (done < plan.Length && plan[done].Done(this)) done++;
        planDone = done;
        if (planTapped < done) planTapped = done;
        if (chef.Idle && planTapped > done && plannedTap == null)
        {
            planTapped = done;
            if (done < plan.Length && !plan[done].Hands(this))
            {
                planBroken = true;
                GoogleAnalytics.Event("combo_plan", "broken_at", done + 1);
                return;
            }
        }
    }

    private Component PlanTarget()
    {
        if (plan == null && !planBroken) return null;   // the kitchen is still being made ready
        if (plan == null || planBroken)
            return chef.CarriesBurger || Any<Plate>(p => p.FinishedBurger != null) ? ServeStep().Target : StackStep().Target;
        if (planTapped >= plan.Length) return null;
        return plan[planTapped].Target != null ? plan[planTapped].Target : Waiting();
    }

    // Two or three words (user, 2026-10-30: "use less text in the onboarding"): the light and the hand show what to tap.
    private string PlanLine()
    {
        if (plan == null || planBroken)
        {
            if (plan == null && !planBroken) return planFirst ? "Tap - <b>don't wait</b>!" : "Now <b>from scratch</b>!";
            return chef.CarriesBurger || Any<Plate>(p => p.FinishedBurger != null) ? ServeStep().Line : StackStep().Line;
        }
        if (planTapped >= plan.Length) return "<b>Watch</b> him go!";
        if (planTapped == 0) return planFirst ? "Tap - <b>don't wait</b>!" : "Now <b>from scratch</b>!";
        return "Keep <b>tapping</b>!";
    }

    private void TapPlanned()
    {
        if (plan == null || planTapped >= plan.Length) return;
        var target = Focus;
        if (target is NpcFsm npc) plannedTap = new PlayerFSM.QueuedTap { Customer = npc, Point = npc.transform.position };
        else
        {
            var col = target.GetComponent<Collider>();
            if (col == null) col = target.GetComponentInChildren<Collider>(true);
            plannedTap = new PlayerFSM.QueuedTap { Thing = col, Point = col != null ? col.bounds.center : target.transform.position };
        }
        planTapped++;
    }
    #endregion

    #region Finding what to point at
    private Component Source(string food)
    {
        foreach (var source in All<IngredientsSource>())
            if (source.name.ToLower().Contains(food)) return source;
        return null;
    }

    private Component Nearest<T>() where T : Component
    {
        var all = All<T>();
        return all.Length == 0 ? null
            : all.OrderBy(x => Vector3.Distance(x.transform.position, chef.transform.position)).First();
    }

    #region Working out the next tap
    // The lesson walks through the layers of the order being made (Orders.Missing) - the order of the layers does
    // not matter to BurgerReview, only what is on the plate; the top bun is added by the plate itself.
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
        foreach (var oven in All<Oven>())
            if (oven.Food is Bun bun && bun.Preparation == Prep.Good) return oven;
        foreach (var pan in All<Pan>())
            if (pan.Food is Burger patty && patty.Preparation == Prep.Good) return patty;
        foreach (var board in All<ChoppingBoard>())
            if (board.Food is CuttableBase sliced && sliced.isSliced) return sliced;
        foreach (var heat in All<HeatStation>())
            if (heat.Food is CookedBase cooked && cooked.Preparation == Prep.Good) return cooked;
        return null;
    }

    // Something on its way to ready: baking, frying or being chopped. The station is what to point at.
    private static Component Cooking()
    {
        foreach (var oven in All<Oven>())
            if (oven.Food is Bun bun && bun.Preparation == Prep.Raw) return oven;
        foreach (var pan in All<Pan>())
            if (pan.Food is Burger patty && patty.Preparation == Prep.Raw) return pan;
        foreach (var board in All<ChoppingBoard>())
            if (board.Food is CuttableBase whole && !whole.isSliced) return board;
        return null;
    }

    // A burnt thing blocks the oven or the pan until it is taken out.
    private static Component BurntOnTheStove()
    {
        foreach (var oven in All<Oven>())
            if (oven.Food is Bun bun && bun.Preparation == Prep.Burnt) return oven;
        foreach (var pan in All<Pan>())
            if (pan.Food is Burger patty && patty.Preparation == Prep.Burnt) return patty;
        return null;
    }

    private static (Component free, Component any) Pick<T>() where T : NonStackBase
    {
        var all = All<T>();
        return (all.FirstOrDefault(station => !station.IsHaveFood()), all.FirstOrDefault());
    }

    // Food in his hands that is not ready yet needs a station: the oven (bun), the pan (patty), the chopping
    // board (veg and cheese). If that station is already busy, "put it in the oven" while the oven is full
    // is exactly the confusing thing - and with his hands full he cannot take the finished one out either -
    // so the honest next tap is the bin. `then` is added after the instruction when there is one.
    private Step StationStep(EdibleBase held, string then = null)
    {
        if (held is CookedBase cooked)
        {
            var heat = All<HeatStation>().Where(s => s.Takes(cooked)).ToList();
            var open = heat.FirstOrDefault(s => s.Accepts(held));
            string where = cooked is Bacon ? "Onto the <b>griddle</b>!" : "Into the <b>fryer</b>!";
            if (open != null) return new Step { Target = open, Line = then == null ? where : where + "\n" + then };
            return new Step { Target = Nearest<Bin>(), Line = $"{(cooked is Bacon ? "Griddle" : "Fryer")} busy - <b>bin</b> it." };
        }
        var (free, name, doIt) = held is Bun ? (Pick<Oven>().free, "Oven", "Into the <b>oven</b>!")
            : held is Burger ? (Pick<Pan>().free, "Pan", "Onto the <b>pan</b>!")
            : (Pick<ChoppingBoard>().free, "Board", "<b>Chop</b> it!");
        if (free != null) return new Step { Target = free, Line = then == null ? doIt : doIt + "\n" + then };
        return new Step { Target = Nearest<Bin>(), Line = $"{name} busy - <b>bin</b> it." };
    }

    // --- the stack step ------------------------------------------------------------------------

    // The one tap that moves the burger along, from where things stand right now.
    private Step WorkOutStack()
    {
        var carried = chef.Carried;
        var parts = carried.Where(f => f != null && f is not Fries && f is not Hamburger).Select(f => f.Name).ToList();
        // The order his stack is for: the waiting one it is closest to.
        var order = NpcFsm.Active.Where(Orders.Waiting).Select(n => n.Ordered)
                        .OrderBy(r => Orders.Missing(r, parts).Count).FirstOrDefault() ?? Orders.FirstRecipe;

        // 1. Something burnt in his hands: the bin. Something raw or whole: its station.
        if (chef.Carries(f => f.Preparation == Prep.Burnt)) return new Step { Target = Nearest<Bin>(), Line = "Burnt! <b>Bin</b> it." };
        foreach (var f in carried)
            if (f is Bun { Preparation: Prep.Raw } || f is Burger { Preparation: Prep.Raw } || f is CuttableBase { isSliced: false })
            {
                var station = StationStep(f);
                if (station.Target != null && station.Target is not Bin) return station;
            }

        // 2. Everything the order needs is in his hands: serve it.
        if (parts.Count >= 2 && Orders.Missing(order, parts).Count == 0) return new Step { Target = Waiting(), Line = "<b>Serve</b> it!" };

        // 3. What the order still needs and is not in his hands, cooking or being chopped: from its crate.
        var have = new System.Collections.Generic.List<string>(parts);
        foreach (var oven in All<Oven>()) if (oven.Food != null) have.Add(oven.Food.Name);
        foreach (var pan in All<Pan>()) if (pan.Food != null) have.Add(pan.Food.Name);
        foreach (var board in All<ChoppingBoard>()) if (board.Food != null) have.Add(board.Food.Name);
        if (chef.CanTakeMore)
            foreach (var name in Orders.Missing(order, have))
            {
                if (!StationFree(name)) continue;
                return new Step { Target = Source(name), Line = $"Grab a <b>{Label(name)}</b>!" };
            }

        // 4. Whatever has finished: take it. A burnt thing has to come out. Otherwise wait for the first one.
        var ready = Ready();
        if (ready != null && chef.CanTakeMore) return new Step { Target = ready, Line = "Ready! <b>Take</b> it!" };
        var burnt = BurntOnTheStove();
        if (burnt != null) return new Step { Target = burnt, Line = "Burnt! <b>Take</b> it out." };
        return new Step { Target = Cooking(), Line = "Cooking..." };
    }

    // How far the burger has got, as the player counts it: "(2/4)".
    private static string Layers(Plate plate, Recipe order) =>
        $"<color=#B96D8B>({(plate != null ? plate.LayerCount : 0)}/{order.Count})</color>";

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
        if (chef.CarriesBurger) return new Step { Target = Waiting(), Line = "<b>Serve</b> it!" };
        var finished = All<Plate>().Select(p => p.FinishedBurger).FirstOrDefault(b => b != null);
        if (finished != null) return new Step { Target = finished, Line = "Grab the <b>burger</b>!" };
        return new Step { Target = null, Line = "Served!" };
    }

    // --- the snack step ------------------------------------------------------------------------

    // Holding something a customer will take as a snack: cooked, baked or sliced (or lettuce, which is
    // ready as it is), and not a whole burger.
    private bool HoldingSnack => chef.HeldFood is { } food && food is not Hamburger && food.Preparation == Prep.Good;

    private Step WorkOutSnack()
    {
        var held = chef.HeldFood;
        if (held is Hamburger) return new Step { Target = Waiting(), Line = "<b>Serve</b> that first!" };
        if (HoldingSnack) return new Step { Target = WaitingToSnack(), Line = "<b>Toss</b> it - tap a hungry customer!" };

        if (held != null)
        {
            if (held.Preparation == Prep.Burnt) return new Step { Target = Nearest<Bin>(), Line = "Burnt! <b>Bin</b> it." };
            // Not ready yet (a raw patty is no snack): it goes where it gets ready, and then it is the snack - named,
            // since "Chop it! Then toss lettuce!" with a cheese in his hands left players unsure which to throw.
            if (held is Bun || held is Burger || held is CuttableBase || held is CookedBase)
                return StationStep(held, $"Then toss the <b>{(held is Fries ? "fries" : Label(held.Name))}</b>!");
            return new Step { Target = Nearest<Bin>(), Line = "Not that - <b>bin</b> it." };
        }

        // Hands empty. Whatever is ready comes first; otherwise lettuce, which needs no cooking at all and is the
        // quickest snack there is. Never something still cooking: it used to point at a patty on the pan, a tap
        // took it out raw, the arrow sent it back onto the pan and then pointed at it again (user report).
        var ready = Ready();
        if (ready != null) return new Step { Target = ready, Line = "Hungry customer!\n<b>Take</b> it, toss it!" };
        return new Step { Target = Source("lettuce"), Line = "Hungry customer!\nToss them <b>lettuce</b>!" };
    }
    #endregion

    // The customer the burger in his hands was made for; otherwise the first one waiting, so the arrow doesn't
    // flick between tables.
    // Never one the player cannot see (sitting under the chef card at the bottom of the screen: user report).
    private Component Waiting()
    {
        // Always the customer whose number the burger wears, seen or not (a burger numbered 1 was sent to number 3,
        // who sat where 1 could not be seen - user, 2026-10-27). The others only for a burger made for nobody.
        if (chef.HeldFood is Hamburger burger && Orders.Waiting(burger.ForCustomer)) return burger.ForCustomer;
        // Someone seated and waiting first; a customer still walking in only if nobody is (the first burger can be
        // ready before its customer sits - but a second customer walking in must not get the first one's burger).
        NpcFsm hidden = null, walking = null;
        foreach (var npc in NpcFsm.Active)
        {
            var state = npc.executingNpcState;
            if (state == ExecutingNpcState.COME) { walking ??= npc; continue; }
            if (state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT) continue;
            if (CustomerInClearView(npc)) return npc;
            hidden ??= npc;
        }
        return hidden ?? walking;
    }

    #region In clear view
    // On screen and not under a HUD card: the customers and goal cards and the buttons at the top, the chef card at
    // the bottom left and the coach's line at the bottom. The lesson seats its customers where they can be seen
    // (NpcSpawnController) and points only at them.
    private static readonly string[] HudCovers = { "CustomersCard", "GoalCard", "AudioControls", "HelpBTN", "Wallet", "BonusBTN", "ChefCard", "Coach" };
    private static RectTransform[] covers;

    // Judged against the whole-cafe picture, wherever the camera is right now: the lesson's first customer arrives
    // while the opening titles have the camera on a close-up, and a seat was taken for one in plain view that sat
    // under the customers card (user, 2026-10-27).
    private static Vector3 homePos;
    private static Quaternion homeRot;
    private static float homeSize;

    private static Vector3 HomeScreenPoint(Camera cam, Vector3 world)
    {
        if (homeSize <= 0f) return cam.WorldToScreenPoint(world);
        var local = Quaternion.Inverse(homeRot) * (world - homePos);
        float aspect = cam.pixelWidth / (float)Mathf.Max(1, cam.pixelHeight);
        return new Vector3((local.x / (homeSize * aspect) * 0.5f + 0.5f) * cam.pixelWidth,
                           (local.y / homeSize * 0.5f + 0.5f) * cam.pixelHeight, local.z);
    }

    public static bool InClearView(Vector3 world)
    {
        var cam = Camera.main;
        if (cam == null) return true;
        var screen = HomeScreenPoint(cam, world);
        if (screen.z < 0f) return false;
        float margin = 0.04f * Screen.height;
        if (screen.x < margin || screen.x > Screen.width - margin || screen.y < margin || screen.y > Screen.height - margin) return false;
        if (covers == null || covers.Length == 0 || covers[0] == null)
        {
            var hud = GameObject.Find("<<<UI>>>/StaticCanvas/InGamePanel/HUD");
            covers = hud == null ? new RectTransform[0]
                : HudCovers.Select(n => hud.transform.Find(n) as RectTransform).Where(t => t != null).ToArray();
        }
        foreach (var cover in covers)
            if (cover != null && cover.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(cover, screen, null))
                return false;
        return true;
    }

    // A customer sitting at `seat`: their body and the order bubble over them, with a little room either side.
    public static bool SeatInClearView(Vector3 seat)
    {
        var cam = Camera.main;
        if (cam == null) return true;
        // While the lesson runs, never in the bottom band of the screen, where Mochi's card, the chef card and the
        // order card are (the card is not up yet when the first customer picks a seat, so it was not counted, and
        // the lesson's first customer sat under it - user, twice, 2026-10-27).
        if (Running && HomeScreenPoint(cam, seat).y < Screen.height * 0.4f) return false;
        var side = cam.transform.right * 0.45f;
        foreach (var up in new[] { 0.7f, 2.4f })
        {
            var at = seat + Vector3.up * up;
            if (!InClearView(at) || !InClearView(at + side) || !InClearView(at - side)) return false;
        }
        return true;
    }

    private static bool CustomerInClearView(NpcFsm npc) =>
        npc.chair != null ? SeatInClearView(npc.chair.CalculateSitPos()) : InClearView(npc.transform.position + Vector3.up);
    #endregion

    // Only a customer who is already waiting for their order, and has not been thrown anything yet, takes a
    // snack (NpcFsm.CatchThrown): the second thing thrown at anyone is just "I want a BURGER!". And only one the
    // player can see and tap - never one sitting under the coach card or the chef card (user report): the most
    // hungry of those.
    private static NpcFsm SnackableInView() => NpcFsm.Active
        .Where(npc => npc.CanTakeSnack && CustomerInClearView(npc))
        .OrderByDescending(npc => npc.WaitedSeconds / Mathf.Max(1f, npc.Patience))
        .FirstOrDefault();

    // The hungry customer in clear view; else, with the snack already in his hands, any who would take one (the
    // camera shows the whole cafe for the tip).
    private Component WaitingToSnack() => (Component)SnackableInView() ?? (HoldingSnack ? NpcFsm.Active
        .Where(npc => npc != null && npc.CanTakeSnack)
        .OrderByDescending(npc => npc.WaitedSeconds / Mathf.Max(1f, npc.Patience))
        .FirstOrDefault() : null);

    private static bool Any<T>(Func<T, bool> test) where T : Component =>
        All<T>().Any(test);

    // The kitchen's stations, plates, crates..., looked up once a second at most: the lesson and the tips ask for them
    // several times a frame, and a search of the whole scene each time cost ~2 ms a frame while a tip showed
    // (measured 2026-10-30). None of them is destroyed during a shift; new ones (a bought table, a station that opens)
    // turn up within a second.
    private static readonly System.Collections.Generic.Dictionary<Type, (UnityEngine.Object[] items, float at)> found = new();

    private static T[] All<T>() where T : UnityEngine.Object
    {
        if (found.TryGetValue(typeof(T), out var known) && Time.unscaledTime - known.at < 1f && known.at <= Time.unscaledTime)
            return (T[])known.items;
        var items = FindObjectsByType<T>(FindObjectsSortMode.None);
        found[typeof(T)] = (items, Time.unscaledTime);
        return items;
    }
    #endregion
}
