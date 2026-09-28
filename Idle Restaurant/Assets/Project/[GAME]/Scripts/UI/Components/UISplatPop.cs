using UnityEngine;

// "SPLAT!" pops over the chef's head when a customer's tomato hits him (FoodFight.OnChefSplatted):
// a tomato splat shape with the word on it, punching in, holding, then fading. HUD canvas (overlay).
[RequireComponent(typeof(CanvasGroup))]
public class UISplatPop : MonoBehaviour
{
    [SerializeField] private float hold = 0.7f;

    private CanvasGroup group;
    private RectTransform rect, parent;
    private Vector3 worldAnchor;
    private float age = float.MaxValue;
    private float pop = 1f, popVelocity;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        rect = (RectTransform)transform;
        parent = (RectTransform)rect.parent;
        group.alpha = 0f;
    }

    private void OnEnable() => FoodFight.OnChefSplatted.AddListener(Show);
    private void OnDisable() => FoodFight.OnChefSplatted.RemoveListener(Show);

    private void Show(Vector3 world)
    {
        worldAnchor = world;
        age = 0f;
        pop = 0.3f;
        popVelocity = 0f;
        rect.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));
    }

    private void LateUpdate()
    {
        if (age > hold + UITokens.Motion.Slow) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        age += dt;

        // Follow the chef's head on screen.
        Vector2 screen = Camera.main.WorldToScreenPoint(worldAnchor);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local))
            rect.anchoredPosition = local + Vector2.up * 70f;

        UITokens.Spring(ref pop, ref popVelocity, 1f, dt);
        rect.localScale = Vector3.one * pop;
        group.alpha = 1f - Mathf.Clamp01((age - hold) / UITokens.Motion.Slow);
    }
}
