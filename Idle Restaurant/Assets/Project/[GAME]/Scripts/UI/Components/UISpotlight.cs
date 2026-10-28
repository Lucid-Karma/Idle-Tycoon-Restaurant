using UnityEngine;
using UnityEngine.UI;

// The first lesson's spotlight (2026-10-20, user: "only the relevant things in colour, the rest slightly darkened";
// then "too chaotic - darken the UI that isn't needed too, remove the yellow arrow"): everything dims - the kitchen
// and the HUD - except an oval around the one thing to tap next, and it glides from one target to the next. A tap
// that misses it makes the oval pulse (Punch). Drawn as a mesh (no texture, no shader: nothing added to the
// download), on its own overlay canvas above the HUD; Mochi's card is raised above it (Tutorial). Driven by Tutorial.
// A cartoon hand taps the lit thing over and over, a ring spreading from its fingertip (user, 2026-10-28: the lesson
// should teach tapping - without words; keys still work, but everyone should see that a tap does it). Hand and ring
// are drawn here too (a Texture2D made once each), so still nothing is added to the download.
[RequireComponent(typeof(CanvasRenderer))]
public class UISpotlight : MaskableGraphic
{
    private const int RingSteps = 64;

    private Vector2 center, radius, targetCenter, targetRadius;
    private float shown, punch;
    private bool on, placed;

    public static UISpotlight Create()
    {
        var go = new GameObject("Lesson Spotlight", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var spot = go.AddComponent<UISpotlight>();
        spot.raycastTarget = false;
        spot.MakeHand();
        return spot;
    }

    // ---- the tapping hand -----------------------------------------------------------------------------
    private const float TapCycle = 1.25f;
    private RectTransform hand, ripple;
    private Image rippleImage;
    private bool handWanted;
    private float handShown;

    public void ShowHand(bool show) => handWanted = show;

    private void MakeHand()
    {
        ripple = Child("Ripple", RingSprite(), out rippleImage);
        hand = Child("Hand", HandSprite(), out _);
        hand.pivot = new Vector2(0.5f, 0.975f);   // the fingertip
    }

    private RectTransform Child(string name, Sprite sprite, out Image image)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        go.SetActive(false);
        return rect;
    }

    private void MoveHand(float dt)
    {
        if (hand == null) return;
        handShown = Mathf.MoveTowards(handShown, handWanted && on ? 1f : 0f, dt / 0.25f);
        bool visible = handShown > 0f && shown > 0f;
        if (hand.gameObject.activeSelf != visible) { hand.gameObject.SetActive(visible); ripple.gameObject.SetActive(visible); }
        if (!visible) return;
        float h = Screen.height * 0.12f;
        hand.sizeDelta = new Vector2(h * 0.8f, h);
        // The fingertip a little below the middle of the lit thing, the hand coming in from below right: it lifts,
        // comes down, presses (a little squash) and lifts again; the ring spreads from where it pressed.
        float t = Time.unscaledTime % TapCycle / TapCycle;
        float lift = t < 0.35f ? 1f - Smooth(t / 0.35f) : t < 0.5f ? 0f : Smooth((t - 0.5f) / 0.5f);
        float press = t >= 0.35f && t < 0.5f ? Mathf.Sin((t - 0.35f) / 0.15f * Mathf.PI) : 0f;
        var tip = center + new Vector2(0.15f, -0.25f) * Mathf.Min(radius.x, radius.y);
        hand.anchoredPosition = tip + new Vector2(0.35f, -0.6f) * h * 0.35f * lift;
        hand.localRotation = Quaternion.Euler(0f, 0f, 22f);
        hand.localScale = new Vector3(1f + 0.05f * press, 1f - 0.08f * press, 1f) * handShown;
        float spread = t >= 0.4f ? (t - 0.4f) / 0.6f : -1f;
        ripple.anchoredPosition = tip;
        ripple.sizeDelta = Vector2.one * h * (spread < 0f ? 0f : 0.25f + 0.65f * spread);
        var c = UITokens.Colors.WarmWhite;
        c.a = spread < 0f ? 0f : (1f - spread) * 0.9f * handShown;
        rippleImage.color = c;
    }

    private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

    private static Sprite handSprite, ringSprite;

    // A white cartoon glove pointing up: the index finger, three folded fingers, a thumb, a teal cuff, an ink outline.
    private static Sprite HandSprite()
    {
        if (handSprite != null) return handSprite;
        const int w = 128, h = 160;
        var pixels = new Color32[w * h];
        var ink = UITokens.Colors.Ink;
        var white = UITokens.Colors.WarmWhite;
        var cuffColor = UITokens.Colors.Teal;
        float px = 2f / h;
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                // y from -1 (bottom) to 1 (top); x in the same units, centred
                var p = new Vector2(((i + 0.5f) - w * 0.5f) / (h * 0.5f) + 0.06f, (j + 0.5f) / h * 2f - 1f);
                float finger = Capsule(p, new Vector2(0f, 0.12f), new Vector2(0f, 0.78f), 0.17f);
                float k1 = Capsule(p, new Vector2(0.2f, 0.0f), new Vector2(0.2f, 0.1f), 0.14f);
                float k2 = Capsule(p, new Vector2(0.39f, -0.06f), new Vector2(0.39f, 0.03f), 0.13f);
                float k3 = Capsule(p, new Vector2(0.55f, -0.15f), new Vector2(0.55f, -0.08f), 0.11f);
                float thumb = Capsule(p, new Vector2(-0.2f, -0.32f), new Vector2(-0.4f, -0.06f), 0.13f);
                float palm = RoundBox(p - new Vector2(0.13f, -0.3f), new Vector2(0.36f, 0.32f), 0.2f);
                float glove = Mathf.Min(Mathf.Min(Mathf.Min(finger, k1), Mathf.Min(k2, k3)), Mathf.Min(thumb, palm));
                float cuff = RoundBox(p - new Vector2(0.13f, -0.78f), new Vector2(0.33f, 0.12f), 0.06f);
                float all = Mathf.Min(glove, cuff);
                Color c = cuff < 0f && glove > -0.01f ? cuffColor : white;
                // the outline, and thin lines between the folded fingers
                float line = Mathf.Abs(all) - 0.035f;
                float seams = Mathf.Min(Mathf.Min(Mathf.Abs(k1), Mathf.Abs(k2)), Mathf.Abs(k3)) - 0.012f;
                if (glove < -0.03f && p.y > -0.2f) line = Mathf.Min(line, seams);
                c = Color.Lerp(c, ink, Mathf.Clamp01(0.5f - line / px));
                c.a = Mathf.Clamp01(0.5f - (all - 0.035f) / px);
                pixels[j * w + i] = c;
            }
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        return handSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.975f), 100f);
    }

    private static Sprite RingSprite()
    {
        if (ringSprite != null) return ringSprite;
        const int n = 128;
        var pixels = new Color32[n * n];
        float px = 2f / n;
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                var p = new Vector2((i + 0.5f) / n * 2f - 1f, (j + 0.5f) / n * 2f - 1f);
                float d = Mathf.Abs(p.magnitude - 0.85f) - 0.08f;
                pixels[j * n + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d / px));
            }
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        return ringSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
    }

    private static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        var pa = p - a;
        var ba = b - a;
        float k = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * k).magnitude - r;
    }

    private static float RoundBox(Vector2 p, Vector2 half, float r)
    {
        var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + Vector2.one * r;
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
    }

    // The thing to look at, as a rectangle in screen pixels: lit as an oval a little bigger than it.
    public void Show(Rect screenRect)
    {
        on = true;
        targetCenter = screenRect.center - new Vector2(Screen.width, Screen.height) * 0.5f;
        float pad = Screen.height * 0.025f;
        targetRadius = new Vector2(screenRect.width * 0.56f + pad, screenRect.height * 0.56f + pad);
        if (!placed || shown <= 0f) { center = targetCenter; radius = targetRadius * 1.6f; placed = true; }
    }

    public void Hide() => on = false;

    public void Punch() => punch = 1f;

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 20f);
        shown = Mathf.MoveTowards(shown, on ? 1f : 0f, dt / 0.3f);
        float k = 1f - Mathf.Exp(-10f * dt);
        center = Vector2.Lerp(center, targetCenter, k);
        radius = Vector2.Lerp(radius, targetRadius * (1f + 0.18f * Mathf.Sin(punch * Mathf.PI)), k);
        punch = Mathf.MoveTowards(punch, 0f, dt / 0.35f);
        if (shown > 0f || on) SetVerticesDirty();
        MoveHand(dt);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (shown <= 0f) return;
        var dim = UITokens.Colors.Ink;
        dim.a = 0.6f * shown;
        var clear = dim; clear.a = 0f;
        float soft = Screen.height * 0.05f;
        float far = new Vector2(Screen.width, Screen.height).magnitude * 1.2f;

        // The dim: an oval hole with a soft edge, then solid out past the screen's corners.
        for (int i = 0; i < RingSteps; i++)
        {
            float a = i * Mathf.PI * 2f / RingSteps;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var edge = center + Vector2.Scale(dir, radius);
            vh.AddVert(edge, clear, Vector4.zero);
            vh.AddVert(edge + dir * soft, dim, Vector4.zero);
            vh.AddVert(center + dir * far, dim, Vector4.zero);
        }
        for (int i = 0; i < RingSteps; i++)
        {
            int a = i * 3, b = (i + 1) % RingSteps * 3;
            vh.AddTriangle(a, b, a + 1); vh.AddTriangle(b, b + 1, a + 1);
            vh.AddTriangle(a + 1, b + 1, a + 2); vh.AddTriangle(b + 1, b + 2, a + 2);
        }

    }
}
