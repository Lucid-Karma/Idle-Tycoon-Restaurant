using UnityEngine;
using UnityEngine.UI;

// Keeps the title poster alive: the chef bobs with his stride, speed streaks rush past behind him, the
// sweat flies off and the sun-burst turns slowly. Unscaled time (the title shows while the game is idle).
// Battery: it only animates while the title is on screen (the panel is hidden with its CanvasGroup, so this
// used to keep rebuilding the HUD's canvas all game long), and the opaque poster switches off the world
// camera under it: no point rendering the kitchen nobody can see.
public class UITitleMotion : MonoBehaviour
{
    [SerializeField] private bool hideWorldBehind = true;

    [SerializeField] private RectTransform chef;
    [SerializeField] private RectTransform burst;
    [SerializeField] private RectTransform[] streaks;
    [SerializeField] private RectTransform[] drops;
    [SerializeField] private RectTransform callout;
    [SerializeField] private float strideHz = 2.6f, bobHeight = 14f;
    [SerializeField] private float streakSpeed = 900f, streakTravel = 420f;
    [SerializeField] private float burstDegreesPerSecond = 6f;

    private Vector2 chefRest, calloutRest;
    private Vector2[] streakRest, dropRest;
    private Graphic[] streakGraphics, dropGraphics;
    private CanvasGroup panel;
    private Camera world;

    private void Awake()
    {
        chefRest = chef.anchoredPosition;
        if (callout != null) calloutRest = callout.anchoredPosition;
        streakRest = new Vector2[streaks.Length];
        streakGraphics = new Graphic[streaks.Length];
        for (int i = 0; i < streaks.Length; i++)
        {
            streakRest[i] = streaks[i].anchoredPosition;
            streakGraphics[i] = streaks[i].GetComponent<Graphic>();
        }
        dropRest = new Vector2[drops.Length];
        dropGraphics = new Graphic[drops.Length];
        for (int i = 0; i < drops.Length; i++)
        {
            dropRest[i] = drops[i].anchoredPosition;
            dropGraphics[i] = drops[i].GetComponent<Graphic>();
        }
    }

    // After every Start of the first frame (they look up Camera.main, which is null while it's disabled).
    private void Start()
    {
        panel = GetComponentInParent<CanvasGroup>();
        world = Camera.main;
    }

    private void Update()
    {
        bool visible = panel == null || panel.alpha > 0.01f;
        if (hideWorldBehind && world != null)
        {
            bool covered = panel != null && panel.alpha > 0.99f;
            if (world.enabled == covered) world.enabled = !covered;
        }
        if (!visible) return;

        float t = Time.unscaledTime;

        // Two bounces per stride, sharp at the bottom like footfalls.
        float stride = Mathf.Abs(Mathf.Sin(t * strideHz * Mathf.PI));
        chef.anchoredPosition = chefRest + Vector2.up * (bobHeight * stride);
        chef.localRotation = Quaternion.Euler(0f, 0f, 1.5f * Mathf.Sin(t * strideHz * Mathf.PI * 2f));

        if (burst != null) burst.localRotation = Quaternion.Euler(0f, 0f, -t * burstDegreesPerSecond);

        // Streaks slide backwards (to the left) and fade, each on its own phase.
        for (int i = 0; i < streaks.Length; i++)
        {
            float phase = Mathf.Repeat(t * streakSpeed / streakTravel + i * 0.37f, 1f);
            streaks[i].anchoredPosition = streakRest[i] + Vector2.left * (streakTravel * phase);
            SetAlpha(streakGraphics[i], Mathf.Sin(phase * Mathf.PI) * 0.9f);
        }

        // Sweat drops fly up-and-back from his head and fade.
        for (int i = 0; i < drops.Length; i++)
        {
            float phase = Mathf.Repeat(t * 1.3f + i * 0.5f, 1f);
            drops[i].anchoredPosition = dropRest[i] + new Vector2(-60f, 40f - 70f * phase) * phase;
            SetAlpha(dropGraphics[i], 1f - phase);
        }

        if (callout != null)
            callout.anchoredPosition = calloutRest + Vector2.up * (4f * Mathf.Sin(t * 3.1f));
    }

    private static void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
