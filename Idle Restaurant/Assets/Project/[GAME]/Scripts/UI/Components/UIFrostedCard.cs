using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A HUD card that steps back when nothing is happening (2026-10-15, user: the order ticket and the Grill House card
// "a little more see-through"): only its background goes see-through - frosted, the cafe showing through - while its
// words and pictures stay sharp. Fading the whole card had washed the text out. It comes back to solid while the
// pointer is over it and for a moment after Flash() (something changed: a layer on the plate, money in the wallet),
// which is how it reads on touch screens. Used by UIOrderCard and UIGrandGoal; the builder (step 15) wires it.
public class UIFrostedCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image surface, shadow;
    [SerializeField, Range(0f, 1f)] private float idleSurface = 0.6f;
    [SerializeField] private float flashSeconds = 2f;

    private Color surfaceColor, shadowColor;
    private float solid, flashUntil;
    private bool hovered;

    private void Awake()
    {
        if (surface != null) surfaceColor = surface.color;
        if (shadow != null) shadowColor = shadow.color;
    }

    public void OnPointerEnter(PointerEventData e) => hovered = true;
    public void OnPointerExit(PointerEventData e) => hovered = false;

    public void Flash() => flashUntil = Time.unscaledTime + flashSeconds;

    private void LateUpdate()
    {
        float target = hovered || Time.unscaledTime < flashUntil ? 1f : 0f;
        solid = Mathf.MoveTowards(solid, target, Mathf.Min(Time.unscaledDeltaTime, 1f / 30f) / UITokens.Motion.Slow);
        float a = Mathf.Lerp(idleSurface, 1f, solid);
        if (surface != null) surface.color = new Color(surfaceColor.r, surfaceColor.g, surfaceColor.b, surfaceColor.a * a);
        if (shadow != null) shadow.color = new Color(shadowColor.r, shadowColor.g, shadowColor.b, shadowColor.a * a * a);
    }
}
