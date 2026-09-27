using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Toy-like button feedback: the face sinks onto its base while pressed, then springs back
// with a small overshoot. Handles hover (desktop) and a clearly dimmed disabled state.
// Runs on unscaled time so it also works while the game is paused (shop, help, score).
[DisallowMultipleComponent]
public class UIPressable : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform face;
    [SerializeField] private float depth = 6f;
    [SerializeField] private float disabledAlpha = 0.45f;

    private Selectable selectable;
    private CanvasGroup group;
    private Vector2 faceRest;
    private float scale = 1f, scaleVel;
    private float sink, sinkVel;
    private bool hovered, pressed, lastInteractable = true;

    public void Setup(RectTransform faceRect, float faceDepth)
    {
        face = faceRect;
        depth = faceDepth;
    }

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        group = GetComponent<CanvasGroup>();
        if (face != null) faceRest = face.anchoredPosition;
    }

    private void OnDisable()
    {
        hovered = pressed = false;
        scale = 1f; scaleVel = 0f; sink = 0f; sinkVel = 0f;
        Apply();
    }

    private bool Interactable => selectable == null || selectable.IsInteractable();

    public void OnPointerDown(PointerEventData e) { if (Interactable) pressed = true; }
    public void OnPointerUp(PointerEventData e)
    {
        if (pressed) scaleVel += 2.5f; // release kick → tiny bounce
        pressed = false;
    }
    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }

    private void Update()
    {
        bool interactable = Interactable;
        if (interactable != lastInteractable)
        {
            lastInteractable = interactable;
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }
        if (group != null) group.alpha = interactable ? 1f : disabledAlpha;

        float targetScale = !interactable ? 1f
            : pressed ? UITokens.Motion.PressScale
            : hovered ? UITokens.Motion.HoverScale : 1f;
        float targetSink = pressed && interactable ? 1f : 0f;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        UITokens.Spring(ref scale, ref scaleVel, targetScale, dt);
        UITokens.Spring(ref sink, ref sinkVel, targetSink, dt);
        Apply();
    }

    private void Apply()
    {
        transform.localScale = new Vector3(scale, scale, 1f);
        if (face != null) face.anchoredPosition = faceRest + Vector2.down * (depth * Mathf.Clamp01(sink));
    }
}
