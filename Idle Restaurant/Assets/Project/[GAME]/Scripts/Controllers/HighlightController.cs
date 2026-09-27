using System.Collections.Generic;
using UnityEngine;

// Shows the player what can be tapped, with a mouse and on touch screens alike:
// - Hover (mouse only): the object under the cursor turns yellow.
// - Target: the object the chef was just sent to stays yellow until he gets there (tap feedback on phones,
//   where there is no hover).
// - Hints: a soft yellow pulse on what makes sense next: where the held food can go (pan, oven, chopping
//   board, a plate, a waiting customer's table), food that is ready to take, or the ingredient crates when
//   the chef's hands are empty and nothing is ready.
// Materials are swapped on the renderers and restored exactly (no material instances are created per object).
public class HighlightController : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;   // solid yellow: hover and tap target
    [SerializeField] private Material hintMaterial;        // Chibi/HintOverlay: drawn on top, pulsing
    [SerializeField] private float targetMinTime = 0.35f;  // a tap stays lit at least this long
    [SerializeField] private Vector2 hintAlpha = new Vector2(0.12f, 0.55f);
    [SerializeField] private float hintPulsesPerSecond = 1.1f;

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
    private Material hintOverlay;
    private Color hintColor;
    private bool usesTouch;

    private void Start()
    {
        _camera = Camera.main;
        player = FindFirstObjectByType<PlayerFSM>();
        pans = FindObjectsByType<Pan>(FindObjectsSortMode.None);
        ovens = FindObjectsByType<Oven>(FindObjectsSortMode.None);
        boards = FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None);
        plates = FindObjectsByType<Plate>(FindObjectsSortMode.None);
        sources = FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None);

        // Animate a copy, so the pulse never dirties the material asset.
        hintOverlay = new Material(hintMaterial);
        hintColor = hintMaterial.color;
    }

    // Hints only make sense while a shift is running (not on the title, help or result screens).
    private bool inShift;

    private void OnEnable()
    {
        EventManager.OnLevelStart.AddListener(OnShiftStart);
        EventManager.OnLevelFinish.AddListener(OnShiftEnd);
    }

    private void OnShiftStart() => inShift = true;
    private void OnShiftEnd() => inShift = false;

    private void OnDisable()
    {
        EventManager.OnLevelStart.RemoveListener(OnShiftStart);
        EventManager.OnLevelFinish.RemoveListener(OnShiftEnd);
        foreach (var pair in applied) Restore(pair.Key);
        applied.Clear();
        originals.Clear();
    }

    private void OnDestroy()
    {
        if (hintOverlay != null) Destroy(hintOverlay);
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

        // What the chef is on his way to (or was tapped a moment ago) glows solid; nothing else is hinted
        // meanwhile, so the destination is unambiguous.
        var tapped = player.LastTapped;
        bool showTap = tapped != null && (player.IsHeadingTo(tapped) || Time.unscaledTime - player.LastTappedAt < targetMinTime);
        if (showTap)
            Mark(tapped, Look.Full);
        else
        {
            CollectHints(hints);
            foreach (var c in hints) Mark(c, Look.Hint);
        }

        // Hover needs a mouse: on touch screens the pointer stays where the last finger lifted.
        if (Input.touchCount > 0) usesTouch = true;
        if (!usesTouch && !PointerUtility.IsOverUI() && Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hit))
        {
            var selectable = hit.collider.GetComponent<ISelectable>() as Component;
            if (selectable != null) Mark(selectable, Look.Full);
        }

        Apply();

        float pulse = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * hintPulsesPerSecond * 2f * Mathf.PI);
        var color = hintColor;
        color.a = Mathf.Lerp(hintAlpha.x, hintAlpha.y, pulse);
        hintOverlay.color = color;
    }

    #region Hints
    private void CollectHints(List<Component> into)
    {
        into.Clear();
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
        foreach (var oven in ovens) if (oven.Food is Bun bun && bun.Preparation != Prep.Raw) into.Add(bun);
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
                materials = new Material[original.Length + 1];
                original.CopyTo(materials, 0);
                materials[original.Length] = hintOverlay;
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
