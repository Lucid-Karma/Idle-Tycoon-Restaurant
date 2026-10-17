using TMPro;
using UnityEngine;

// "SPLAT!" pops over the chef's head when a customer's tomato hits him (FoodFight.OnChefSplatted),
// "WHOOPS!" when he slips on it, "BULLSEYE!" (on a yellow burst) when a Ranger's arrow gets him: a splat
// shape with the word on it, punching in, holding, then fading. HUD canvas (overlay).
[RequireComponent(typeof(CanvasGroup))]
public class UISplatPop : MonoBehaviour
{
    [SerializeField] private float hold = 0.7f;
    private float size = 1f, holdNow = 0.7f;

    private CanvasGroup group;
    private TMP_Text label;
    private UnityEngine.UI.Image shape;
    private Color tomatoColor, labelColor;
    private RectTransform rect, parent;
    private Vector3 worldAnchor;
    private float age = float.MaxValue;
    private float pop = 1f, popVelocity;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        label = GetComponentInChildren<TMP_Text>(true);
        shape = GetComponentInChildren<UnityEngine.UI.Image>(true);
        if (shape != null) tomatoColor = shape.color;
        if (label != null) labelColor = label.color;
        rect = (RectTransform)transform;
        parent = (RectTransform)rect.parent;
        group.alpha = 0f;
    }

    private void OnEnable()
    {
        FoodFight.OnChefSplatted.AddListener(Show);
        FoodFight.OnMochiSplatted.AddListener(ShowMochi);
    }

    private void OnDisable()
    {
        FoodFight.OnChefSplatted.RemoveListener(Show);
        FoodFight.OnMochiSplatted.RemoveListener(ShowMochi);
    }

    // (A perfect burger and a rush are celebrated by UICheerPop: the user wanted them unlike a splat.)

    // Mochi, hit by a tomato: an outraged "MRRAOW!" over his head.
    private void ShowMochi(Vector3 world)
    {
        Show(world, FoodFight.Hit.Splat);
        if (label != null) label.text = "MRRAOW!";
    }

    private void Show(Vector3 world, FoodFight.Hit hit)
    {
        worldAnchor = world;
        if (label != null) label.text = hit == FoodFight.Hit.Arrow ? "BULLSEYE!" : hit == FoodFight.Hit.Slip ? "WHOOPS!" : "SPLAT!";
        if (shape != null) shape.color = hit == FoodFight.Hit.Arrow ? UITokens.Colors.Yellow : tomatoColor;
        if (label != null) label.color = hit == FoodFight.Hit.Arrow ? UITokens.Colors.Ink : labelColor;   // white on yellow didn't read
        age = 0f;
        pop = 0.3f;
        size = 1f;
        holdNow = hold;
        popVelocity = 0f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));
    }

    private void LateUpdate()
    {
        if (age > holdNow + UITokens.Motion.Slow) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        age += dt;

        // Follow the chef's head on screen.
        Vector2 screen = Camera.main.WorldToScreenPoint(worldAnchor);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local))
            rect.anchoredPosition = local + Vector2.up * 70f;

        UITokens.Spring(ref pop, ref popVelocity, size, dt);
        rect.localScale = Vector3.one * pop;
        group.alpha = 1f - Mathf.Clamp01((age - holdNow) / UITokens.Motion.Slow);
    }
}
