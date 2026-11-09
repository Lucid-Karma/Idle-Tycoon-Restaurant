using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The little chef in the bottom-left card has two kinds of face (user, 2026-11-04):
//   - in his card, the old animations (happy, angry...) - only for how a burger was rated (ChefAnimationController);
//   - everything else is a reaction to the player, and never in the card: his portrait jumps out to a random spot near
//     the edge of the screen, grows three times its size, plays a face of its own (Resources/ChefFaces: laugh, smirk,
//     shock, dizzy, love - drawn in the card's line style) and goes back. No words: the face says it (user, 2026-11-04).
// He laughs when a tomato gets the kitchen chef or he slips, smirks when a customer leaves hungry, is shocked by a
// Chaos Burger, dizzy at a flurry of taps that do nothing, and in love with the player's first burger. Not more often
// than every 10 seconds, except the new player's opening gag (Tutorial.TomatoOpening) and the first burger.
// In the lesson nothing of him sits on the floor any more (user, 2026-11-07: "he sits there tiny and still in the middle
// of the floor - react like the other reactions"); his corner card stays up, and he jumps out of it at the player's lesson taps - in love with a right one, shocked at one the lesson turned down (Tutorial.LessonTap) - no
// more often than the other reactions.
// Attached at runtime by GameManager.
public class UIChefLaugh : MonoBehaviour
{
    public enum Face { Laugh, Smirk, Shock, Dizzy, Love }

    private const float Grow = 0.32f, Hold = 1.4f, Shrink = 0.3f, Size = 3f, Gap = 10f, FrameSeconds = 0.16f;
    private const string PortraitPath = "<<<UI>>>/StaticCanvas/InGamePanel/HUD/ChefCard/Portrait";

    private static UIChefLaugh instance;
    private RectTransform portrait;
    private Canvas ownCanvas;
    private CanvasGroup group;
    private Animator animator;
    private Image chibi;
    private readonly Dictionary<Face, Sprite[]> faces = new();
    private float lastReaction = -100f;
    private bool reacting;

    public static void Attach()
    {
        var found = GameObject.Find(PortraitPath);
        if (found == null || found.GetComponent<UIChefLaugh>() != null) return;
        found.AddComponent<UIChefLaugh>();
    }

    // A reaction from anywhere in the game. `always`: even right after another one.
    public static void React(Face face, bool always = false)
    {
        if (instance != null) instance.Play(face, always);
    }

    private void Awake()
    {
        instance = this;
        portrait = (RectTransform)transform;
        ownCanvas = GetComponent<Canvas>();
        animator = GetComponentInChildren<Animator>(true);
        foreach (var img in GetComponentsInChildren<Image>(true)) if (img.name == "ChibiIMG") chibi = img;
        if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();   // (not ??: Unity's missing component is not null to it)
        var all = Resources.LoadAll<Sprite>("ChefFaces");
        foreach (Face f in System.Enum.GetValues(typeof(Face)))
        {
            var key = "Chef-" + f.ToString().ToLowerInvariant() + "-";
            var list = new List<Sprite>();
            foreach (var s in all) if (s.name.StartsWith(key)) list.Add(s);
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            faces[f] = list.ToArray();
        }
    }

    private void OnEnable()
    {
        FoodFight.OnChefSplatted.AddListener(OnSplatted);
        EventManager.OnCustomerProtest.AddListener(OnWalkout);
        NpcFsm.OnRated.AddListener(OnRated);
        Tutorial.LessonTap += OnLessonTap;
    }

    private void OnDisable()
    {
        FoodFight.OnChefSplatted.RemoveListener(OnSplatted);
        EventManager.OnCustomerProtest.RemoveListener(OnWalkout);
        NpcFsm.OnRated.RemoveListener(OnRated);
        Tutorial.LessonTap -= OnLessonTap;
    }

    private void OnLessonTap(bool right) => Play(right ? Face.Love : Face.Shock);

    private void OnSplatted(Vector3 at, FoodFight.Hit hit) => Play(Face.Laugh, Tutorial.Opening);

    private void OnWalkout() => Play(Face.Smirk);

    private bool firstBurger = true;
    private void OnRated(NpcFsm npc, OrderResult order)
    {
        if (firstBurger) { firstBurger = false; Play(Face.Love, true); return; }
        if (order.Review.IsChaos) Play(Face.Shock);
    }

    // A tap that did nothing, three times in a short while: he gets dizzy watching.
    private readonly Queue<float> nopes = new();
    public static void Noped()
    {
        if (instance == null) return;
        var q = instance.nopes;
        q.Enqueue(Time.unscaledTime);
        while (q.Count > 0 && Time.unscaledTime - q.Peek() > 5f) q.Dequeue();
        if (q.Count >= 3) { q.Clear(); instance.Play(Face.Dizzy); }
    }

    private void Play(Face face, bool always = false)
    {
        if (reacting || (!always && Time.unscaledTime - lastReaction < Gap)) return;
        StartCoroutine(Reaction(face));
    }

    private void LateUpdate()
    {
        // (His card stays up through the lesson now - user, 2026-11-07: "the last order card can show in the onboarding".)
        group.ignoreParentGroups = reacting;
    }

    private IEnumerator Reaction(Face face)
    {
        reacting = true;
        lastReaction = Time.unscaledTime;
        var corner = portrait.position;
        var homeScale = portrait.localScale;
        var homePos = corner;
        float rest = 1f;
        // The opening gag's laugh is shorter (user, 2026-11-07: "shorten his laugh in the opening").
        float hold = Tutorial.Opening || LessonIntro.Playing ? Hold * 0.55f : Hold;
        bool sorted = ownCanvas != null && ownCanvas.overrideSorting;
        int order = ownCanvas != null ? ownCanvas.sortingOrder : 0;
        if (ownCanvas != null) { ownCanvas.overrideSorting = true; ownCanvas.sortingOrder = 50; }
        var frames = faces.TryGetValue(face, out var f) ? f : null;
        var restSprite = chibi != null ? chibi.sprite : null;
        bool animated = animator != null && animator.enabled;
        if (frames != null && frames.Length > 0 && animator != null) animator.enabled = false;   // (it would set the sprite back)
        GameSfx.Play(face == Face.Laugh ? GameSfx.Cue.Tip : GameSfx.Cue.Seated);

        // Somewhere near the edge of the screen, never the same corner twice in a row.
        var spot = Spot();
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Grow)
        {
            float k = Back(Mathf.Clamp01(t));
            portrait.position = Vector3.LerpUnclamped(homePos, spot, k);
            portrait.localScale = homeScale * Mathf.LerpUnclamped(rest, Size, k);
            ShowFrame(frames, t * Grow);
            yield return null;
        }
        for (float t = 0f; t < hold; t += Time.unscaledDeltaTime)
        {
            // He shakes with it.
            float shake = Mathf.Sin(t * (face == Face.Laugh ? 38f : 14f));
            portrait.localRotation = Quaternion.Euler(0f, 0f, shake * (face == Face.Laugh ? 6f : 3f));
            portrait.localScale = homeScale * Size * (1f + Mathf.Abs(shake) * 0.04f);
            ShowFrame(frames, Grow + t);
            yield return null;
        }
        portrait.localRotation = Quaternion.identity;
        var from = portrait.position;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Shrink)
        {
            float k = t * t * (3f - 2f * t);
            portrait.position = Vector3.Lerp(from, homePos, k);
            portrait.localScale = homeScale * Mathf.Lerp(Size, rest, k);
            yield return null;
        }
        portrait.position = corner;
        portrait.localScale = homeScale;
        if (chibi != null && restSprite != null) chibi.sprite = restSprite;
        if (animator != null) animator.enabled = animated;
        if (ownCanvas != null) { ownCanvas.overrideSorting = sorted; ownCanvas.sortingOrder = order; }
        reacting = false;
    }

    private void ShowFrame(Sprite[] frames, float t)
    {
        if (chibi == null || frames == null || frames.Length == 0) return;
        chibi.sprite = frames[(int)(t / FrameSeconds) % frames.Length];
    }

    private int lastSpot = -1;
    private Vector3 Spot()
    {
        // Left, right, top-left, top-right, middle-top (in the root canvas, as fractions of its size).
        var spots = new[] { new Vector2(0.18f, 0.5f), new Vector2(0.82f, 0.5f), new Vector2(0.22f, 0.7f), new Vector2(0.78f, 0.7f), new Vector2(0.5f, 0.62f) };
        int i;
        do i = Random.Range(0, spots.Length); while (i == lastSpot);
        lastSpot = i;
        // (The HUD is a screen-space overlay: its world positions are screen pixels.)
        return new Vector3(Screen.width * spots[i].x, Screen.height * spots[i].y, 0f);
    }

    // Ease out with an overshoot.
    private static float Back(float t)
    {
        const float s = 1.70158f;
        float x = t - 1f;
        return 1f + (s + 1f) * x * x * x + s * x * x;
    }
}
