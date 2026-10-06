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
// Materials are swapped on the renderers and restored exactly. Hinted Lit materials are swapped for copies
// using Chibi/HintLit (made once per original material), whose colour follows a global pulse. A swap keeps
// the material count, so it also works on statically batched renderers.
public class HighlightController : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;   // solid yellow: hover and tap target
    [SerializeField] private Material hintTemplate;        // Chibi/HintLit material (keeps the shader in builds)
    [SerializeField] private Material hintMaterial;        // solid raspberry, for the rare non-Lit material
    [SerializeField] private float targetMinTime = 0.35f;  // a tap stays lit at least this long
    [SerializeField] private float hintPulseSeconds = 1f;  // one breath: own look → raspberry → own look
    [SerializeField, Range(0f, 1f)] private float hintPeak = 0.9f;

    private static readonly int HintAmountId = Shader.PropertyToID("_HintAmount");
    private static readonly int HintColorId = Shader.PropertyToID("_HintColor");
    private readonly Dictionary<Material, Material> hintVariants = new();

    private enum Look { None, Hint, Full }

    private readonly Dictionary<Renderer, Material[]> originals = new();
    private readonly Dictionary<Renderer, Look> applied = new();
    private readonly Dictionary<Renderer, Look> wanted = new();
    private readonly List<Renderer> stale = new();
    private readonly List<Component> hints = new();

    private Camera _camera;
    private PlayerFSM player;
    private Pan[] pans;
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
        Shader.SetGlobalColor(HintColorId, UITokens.Colors.Raspberry);
        Shader.SetGlobalFloat(HintAmountId, 0f);
    }

    // Again after a purchase: a second stove brings a pan of its own.
    private void FindUtensils()
    {
        pans = FindObjectsByType<Pan>(FindObjectsSortMode.None);
        ovens = FindObjectsByType<Oven>(FindObjectsSortMode.None);
        boards = FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None);
        plates = FindObjectsByType<Plate>(FindObjectsSortMode.None);
        sources = FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None);
    }

    private void OnCafeChanged(Upgrade upgrade) => FindUtensils();

    private void OnDestroy()
    {
        foreach (var variant in hintVariants.Values) if (variant != null && variant != hintMaterial) Destroy(variant);
        hintVariants.Clear();
    }

    // The original with its colour free to drift towards the hint colour; one copy per original material.
    private Material HintVariant(Material original)
    {
        if (original == null || original.shader == null || original.shader.name != "Universal Render Pipeline/Lit")
        {
#if UNITY_EDITOR
            if (original != null && hintVariants.TryAdd(original, hintMaterial))
                Debug.LogWarning($"[Highlight] '{original.name}' ({original.shader?.name}) isn't URP Lit: hinted with the plain raspberry material");
#endif
            return hintMaterial;
        }
        if (!hintVariants.TryGetValue(original, out var variant))
        {
            variant = new Material(original) { name = original.name + " (hint)", shader = hintTemplate.shader };
            hintVariants[original] = variant;
        }
        return variant;
    }

    // Hints only make sense while a shift is running (not on the title, help or result screens).
    private bool inShift;

    private void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(OnShiftStart);
        EventManager.OnLevelFinish.AddListener(OnShiftEnd);
        CafeShop.Changed += OnCafeChanged;
    }

    private void OnShiftStart() => inShift = true;
    private void OnShiftEnd() => inShift = false;

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(OnShiftStart);
        EventManager.OnLevelFinish.RemoveListener(OnShiftEnd);
        CafeShop.Changed -= OnCafeChanged;
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
        float phase = (Time.unscaledTime - hintSince) / Mathf.Max(0.1f, hintPulseSeconds);
        Shader.SetGlobalFloat(HintAmountId, hintPeak * (0.5f - 0.5f * Mathf.Cos(phase * 2f * Mathf.PI)));
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
            // Customers still waiting for their order, and their table spot (tapping either serves them).
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

        if (held != null)
        {
            if (held is Burger)
                foreach (var pan in pans) if (!pan.IsHaveFood()) into.Add(pan);
            if (held is Bun)
                foreach (var oven in ovens) if (!oven.IsHaveFood()) into.Add(oven);
            if (held is CuttableBase cuttable && !cuttable.isSliced)
                foreach (var board in boards) if (!board.IsHaveFood()) into.Add(board);

            // Keep building the burger in progress; otherwise any free plate.
            int before = into.Count;
            foreach (var plate in plates) if (plate.LayerCount > 0 && plate.Accepts(held)) into.Add(plate);
            if (into.Count == before)
                foreach (var plate in plates) if (plate.Accepts(held)) into.Add(plate);
            return;
        }

        // Empty hands: whatever is ready to take (burnt counts too: it's blocking the pan).
        foreach (var pan in pans) if (pan.Food is Burger patty && patty.Preparation != Prep.Raw) into.Add(patty);
        foreach (var oven in ovens) if (oven.Food is Bun bun && bun.Preparation != Prep.Raw) { into.Add(bun); into.Add(oven); }
        foreach (var board in boards) if (board.Food is CuttableBase sliced && sliced.isSliced) into.Add(sliced);
        foreach (var plate in plates) if (plate.FinishedBurger != null) into.Add(plate.FinishedBurger);

        if (into.Count == 0)
            foreach (var source in sources) into.Add(source);
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
        if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
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
            if (applied.TryGetValue(renderer, out var look) && look == pair.Value) continue;
            if (!originals.TryGetValue(renderer, out var original))
                originals[renderer] = original = renderer.sharedMaterials;

            Material[] materials;
            if (pair.Value == Look.Full)
            {
                materials = new Material[original.Length];
                for (int i = 0; i < materials.Length; i++) materials[i] = highlightMaterial;
            }
            else
            {
                materials = new Material[original.Length];
                for (int i = 0; i < materials.Length; i++) materials[i] = HintVariant(original[i]);
            }
            renderer.sharedMaterials = materials;
            applied[renderer] = pair.Value;
        }
    }

    private void Restore(Renderer renderer)
    {
        if (renderer != null && originals.TryGetValue(renderer, out var original))
            renderer.sharedMaterials = original;
    }
    #endregion
}
