using System.Collections.Generic;
using UnityEngine;

// Shows the player what can be tapped, with a mouse and on touch screens alike:
// - Hover (mouse only): the object under the cursor turns yellow.
// - Target: the object the chef was just sent to stays yellow until he gets there (tap feedback on phones,
//   where there is no hover).
// - Hints: what makes sense next pulses between its own look and raspberry (solid colour like the
//   highlight, no glow): where the held food can go (pan, oven, chopping board, a plate, a waiting customer
//   and their table), food that is ready to take, or the ingredient crates when hands are empty and nothing
//   is ready.
// Materials are swapped on the renderers and restored exactly. A hint is shown the way the yellow is: its own
// materials for half the beat, a solid pink material for the other half (user, 2026-10-27: "can't the materials
// just change colour, like the yellow?"). It used to blend towards the pink in a copy of each material with a
// shader of its own (Chibi/HintLit), and on the web the copy lost the scene's baked light: hinted things went
// darker than themselves. A swap keeps the material count, so it also works on statically batched renderers.
public class HighlightController : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;   // solid yellow: hover and tap target
    [SerializeField] private Material hintMaterial;        // a solid Lit material: copied in pink for the hint
    [SerializeField] private float targetMinTime = 0.35f;  // a tap stays lit at least this long
    [SerializeField] private float hintPulseSeconds = 1f;  // one breath: own look → raspberry → own look


    private enum Look { None, Hint, Full, HintOff, HintOn }   // (HintOff/HintOn: a hint, at this moment of its beat)
    private Material pink;
    private bool pinkNow;

    private readonly Dictionary<Renderer, Material[]> originals = new();
    private readonly Dictionary<Renderer, Look> applied = new();
    private readonly Dictionary<Renderer, Look> wanted = new();
    private readonly List<Renderer> stale = new();
    private readonly List<Component> hints = new();

    private Camera _camera;
    private PlayerFSM player;
    private Pan[] pans;
    private HeatStation[] heat;
    private Oven[] ovens;
    private ChoppingBoard[] boards;
    private Plate[] plates;
    private IngredientsSource[] sources;
    private bool usesTouch;

    private void Start()
    {
        _camera = Camera.main;
        player = FindFirstObjectByType<PlayerFSM>();
        FindUtensils();
        // A light, clear pink (user, 2026-10-27: "from its own colour to pink is enough" - the deep raspberry it
        // used went darker than the thing itself on the way, in shade and at night).
        pink = new Material(hintMaterial) { name = "Hint pink" };
        pink.SetColor("_BaseColor", UITokens.Hex(0xFF7BB0));
        if (pink.HasProperty("_Color")) pink.SetColor("_Color", UITokens.Hex(0xFF7BB0));
    }

    // Again after a purchase: a second stove brings a pan of its own.
    private void FindUtensils()
    {
        pans = FindObjectsByType<Pan>(FindObjectsSortMode.None);
        heat = FindObjectsByType<HeatStation>(FindObjectsSortMode.None);
        ovens = FindObjectsByType<Oven>(FindObjectsSortMode.None);
        boards = FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None);
        plates = FindObjectsByType<Plate>(FindObjectsSortMode.None);
        sources = FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None);
    }

    private void OnCafeChanged(Upgrade upgrade) => FindUtensils();
    private void OnStationsChanged() => FindUtensils();

    private void OnDestroy()
    {
        if (pink != null) Destroy(pink);
    }

    // Hints only make sense once service has started (not on the title screen). Shifts follow each other
    // without a break now (ShiftFlow), so they stay on between them; the help card and the shop pause the game.
    private bool inShift;

    private void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(OnShiftStart);
        CafeShop.Changed += OnCafeChanged;
        KitchenStations.Changed += OnStationsChanged;
    }

    private void OnShiftStart() => inShift = true;

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(OnShiftStart);
        CafeShop.Changed -= OnCafeChanged;
        KitchenStations.Changed -= OnStationsChanged;
        foreach (var pair in applied) Restore(pair.Key);
        applied.Clear();
        originals.Clear();
    }

    private void LateUpdate()
    {
        if (player == null) return;
        wanted.Clear();
        if (!inShift || Time.timeScale == 0f)
        {
            Apply();
            return;
        }

        // What the chef is on his way to (or was tapped a moment ago) turns yellow; nothing else is hinted
        // meanwhile, so the destination is unambiguous.
        var tapped = player.LastTapped;
        bool showTap = tapped != null && (player.IsHeadingTo(tapped) || Time.unscaledTime - player.LastTappedAt < targetMinTime);
        if (showTap)
            hints.Clear();
        else
            CollectHints(hints);
        UpdatePulse();
        if (showTap)
            Mark(tapped, Look.Full);
        else
            foreach (var c in hints) Mark(c, Look.Hint);

        // Hover needs a mouse: on touch screens the pointer stays where the last finger lifted.
        if (Input.touchCount > 0) usesTouch = true;
        if (!usesTouch && !PointerUtility.IsOverUI() && Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hit))
        {
            var proxy = hit.collider.GetComponent<TapProxy>();
            var col = proxy != null ? player.ResolveTap(hit.collider) : hit.collider;
            var selectable = col != null ? col.GetComponent<ISelectable>() as Component : null;
            if (selectable != null) Mark(selectable, Look.Full);
            else if (proxy != null) Mark(proxy.Target, Look.Full); // full oven: still show it's the oven
        }

        Apply();
    }

    #region Hints
    private int hintSignature;
    private float hintSince;

    // The pulse restarts whenever the hinted set changes, so newly hinted things ease in from their own
    // look instead of popping in at full colour.
    private void UpdatePulse()
    {
        int signature = hints.Count;
        foreach (var c in hints) signature = signature * 31 + (c != null ? c.GetInstanceID() : 0);
        if (signature != hintSignature)
        {
            hintSignature = signature;
            hintSince = Time.unscaledTime;
        }
        // Own look first (newly hinted things don't pop straight to pink), pink for the second half of each beat.
        float phase = (Time.unscaledTime - hintSince) / Mathf.Max(0.1f, hintPulseSeconds);
        pinkNow = Mathf.Repeat(phase, 1f) >= 0.5f;
    }

    private void CollectHints(List<Component> into)
    {
        into.Clear();
        // While the first shift is being taught, only the one thing the lesson is about pulses: two
        // pulsing things at once is exactly the confusion the tutorial exists to prevent.
        if (Tutorial.Running)
        {
            if (Tutorial.Focus != null) into.Add(Tutorial.Focus);
            return;
        }
        var held = player.HeldFood;

        if (held is Hamburger burger)
        {
            // The customer it was made for, if they are still waiting; otherwise everyone still waiting, and
            // their table spot (tapping either serves them).
            if (Orders.Waiting(burger.ForCustomer) && burger.ForCustomer.chair.GetTableService() is ServiceBase own && own.IsSuitable(burger))
            {
                into.Add(own);
                into.Add(burger.ForCustomer);
                return;
            }
            foreach (var npc in NpcFsm.Active)
            {
                var state = npc.executingNpcState;
                if (state != ExecutingNpcState.COME && state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT) continue;
                if (npc.chair?.GetTableService() is ServiceBase service && service.IsSuitable(burger))
                {
                    into.Add(service);
                    into.Add(npc);
                }
            }
            return;
        }

        // His stack (PlayerFSM.Carried): where each thing in it goes next - raw ones to their cooker, whole ones to the
        // board, fried fries to whoever ordered them (tap them to toss), the rest to the plate; and while there is room,
        // whatever is ready to take on top.
        var carried = player.Carried;
        if (carried.Count > 0)
        {
            EdibleBase forPlate = null;
            foreach (var food in carried)
            {
                if (food == null) continue;
                if (food is Fries)
                {
                    if (food.Preparation == Prep.Raw)
                        foreach (var station in heat) if (station.Accepts(food)) into.Add(station);
                    if (food.Preparation == Prep.Good)
                        foreach (var npc in NpcFsm.Active) if (npc.FriesDue) into.Add(npc);
                    continue;
                }
                forPlate ??= food;
                if (food.Preparation == Prep.Raw)
                    foreach (var station in heat) if (station.Accepts(food)) into.Add(station);
                if (food is Burger && food.Preparation == Prep.Raw)
                    foreach (var pan in pans) if (!pan.IsHaveFood()) into.Add(pan);
                if (food is Bun && food.Preparation == Prep.Raw)
                    foreach (var oven in ovens) if (!oven.IsHaveFood()) into.Add(oven);
                if (food is CuttableBase cuttable && !cuttable.isSliced)
                    foreach (var board in boards) if (!board.IsHaveFood()) into.Add(board);
            }
            // A burger in his hands (two things or more) that has everything someone ordered: them and their table
            // (no plate any more - user, 2026-10-30).
            if (forPlate != null && player.CarriesBurger)
            {
                names.Clear();
                foreach (var food in carried) if (food != null && food is not Fries && food is not Hamburger) names.Add(food.Name);
                foreach (var npc in NpcFsm.Active)
                {
                    if (!Orders.Waiting(npc) || npc.Ordered == null || Orders.Missing(npc.Ordered, names).Count > 0) continue;
                    into.Add(npc);
                    if (npc.chair?.GetTableService() is ServiceBase table) into.Add(table);
                }
            }
            if (player.CanTakeMore)
            {
                AddReady(into);
                AddOrderCrates(into);
            }
            return;
        }

        // Empty hands: whatever is ready to take (burnt counts too: it's blocking the pan).
        AddReady(into);
        foreach (var plate in plates) if (plate.FinishedBurger != null) into.Add(plate.FinishedBurger);
        // Everything an order needs is on it: tap it to finish the burger (adding more is the player's choice).
        foreach (var plate in plates) if (plate.CoversAnOrder) into.Add(plate);

        AddOrderCrates(into);
    }

    // The crates of everything the waiting customers' burgers are made of, all the while there is room in his hands
    // (user, 2026-11-07: "all the ingredients of the burgers the customers want should keep flashing"; and only those:
    // "if no recipe has a tomato yet, no need to point at it"). With nobody waiting, what every burger needs.
    private void AddOrderCrates(List<Component> into)
    {
        names.Clear();
        foreach (var npc in NpcFsm.Active)
        {
            if (!Orders.Waiting(npc) || npc.Ordered == null) continue;
            foreach (var layer in npc.Ordered.Layers) names.Add(layer);
            if (npc.FriesDue) names.Add("fries");
        }
        if (names.Count == 0) { names.Add("bun"); names.Add("burger"); }
        foreach (var source in sources) if (names.Contains(source.Ingredient) && !into.Contains(source)) into.Add(source);
    }

    private readonly List<string> names = new();

    // Cooked, baked, chopped - or burnt (it's blocking the pan) - and waiting to be taken.
    private void AddReady(List<Component> into)
    {
        foreach (var pan in pans) if (pan.Food is Burger patty && patty.Preparation != Prep.Raw) into.Add(patty);
        foreach (var station in heat) if (station.Food is CookedBase cooked && cooked.Preparation != Prep.Raw) into.Add(cooked);
        foreach (var oven in ovens) if (oven.Food is Bun bun && bun.Preparation != Prep.Raw) { into.Add(bun); into.Add(oven); }
        foreach (var board in boards) if (board.Food is CuttableBase sliced && sliced.isSliced) into.Add(sliced);
    }
    #endregion

    #region Materials
    private void Mark(Component target, Look look)
    {
        if (target == null) return;
        // A finished burger is lit as a whole (its layers are selectables of their own).
        Collect(target.transform, target.transform, look, target is Hamburger);
        // A utensil tappable through bigger furniture (the oven tray through the whole oven) lights it too.
        foreach (var proxy in TapProxy.All)
            if (proxy.Target == target) Collect(proxy.transform, proxy.transform, look, false);
    }

    private void Collect(Transform node, Transform root, Look look, bool includeNested)
    {
        if (!node.gameObject.activeInHierarchy) return;
        if (node != root && !includeNested && node.GetComponent<ISelectable>() != null) return;

        var renderer = node.GetComponent<Renderer>();
        if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && !SeasonalGhost.KeepsOwnLook(node))
        {
            if (!wanted.TryGetValue(renderer, out var current) || current < look) wanted[renderer] = look;
        }
        for (int i = 0; i < node.childCount; i++) Collect(node.GetChild(i), root, look, includeNested);
    }

    private void Apply()
    {
        stale.Clear();
        foreach (var pair in applied)
            if (pair.Key == null || !wanted.ContainsKey(pair.Key)) stale.Add(pair.Key);
        foreach (var renderer in stale)
        {
            Restore(renderer);
            applied.Remove(renderer);
            originals.Remove(renderer);
        }

        foreach (var pair in wanted)
        {
            var renderer = pair.Key;
            var want = pair.Value == Look.Hint ? (pinkNow ? Look.HintOn : Look.HintOff) : pair.Value;
            if (applied.TryGetValue(renderer, out var look) && look == want) continue;
            if (!originals.TryGetValue(renderer, out var original))
                originals[renderer] = original = renderer.sharedMaterials;

            if (want == Look.HintOff)
            {
                renderer.sharedMaterials = original;
                applied[renderer] = want;
                continue;
            }
            var solid = want == Look.Full ? highlightMaterial : pink;
            renderer.sharedMaterials = Filled(solid, original.Length);
            applied[renderer] = want;
        }
    }

    // One array per material and length, reused (Renderer.sharedMaterials copies it): the pulse used to make new ones
    // twice a second for every hinted renderer - most of this script's garbage (measured 2026-10-30).
    private readonly Dictionary<(Material, int), Material[]> filled = new();

    private Material[] Filled(Material material, int count)
    {
        if (filled.TryGetValue((material, count), out var array)) return array;
        array = new Material[count];
        for (int i = 0; i < count; i++) array[i] = material;
        filled[(material, count)] = array;
        return array;
    }

    private void Restore(Renderer renderer)
    {
        if (renderer != null && originals.TryGetValue(renderer, out var original))
            renderer.sharedMaterials = original;
    }
    #endregion
}
