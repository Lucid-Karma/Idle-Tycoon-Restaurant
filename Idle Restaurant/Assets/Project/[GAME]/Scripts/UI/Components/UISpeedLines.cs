using UnityEngine;
using UnityEngine.UI;

// Manga speed lines for the lesson's opening titles (LessonIntro): Radial - streaks rushing in from the screen's
// edges towards a point (a zoom, a hit); Sweep - long streaks across the screen (a whip pan). A new random set every
// few frames, so they flicker the way drawn speed lines do. A mesh: no texture, nothing added to the download.
[RequireComponent(typeof(CanvasRenderer))]
public class UISpeedLines : MaskableGraphic
{
    public enum Mode { Radial, Sweep }

    public Mode mode = Mode.Radial;
    public Vector2 focus;          // Radial: where the lines point, in pixels from the screen's centre
    public float strength;         // 0 = none, 1 = full
    public float sweepDirection = 1f;

    private int seed;
    private float nextFlicker;

    private void Update()
    {
        if (Time.unscaledTime >= nextFlicker) { nextFlicker = Time.unscaledTime + 0.06f; seed++; }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (strength <= 0.01f) return;
        var rnd = new System.Random(seed);
        float w = Screen.width, h = Screen.height;
        float R() => (float)rnd.NextDouble();
        var tint = color;

        if (mode == Mode.Radial)
        {
            float far = new Vector2(w, h).magnitude * 0.75f;
            for (int i = 0; i < 64; i++)
            {
                float a = R() * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                var side = new Vector2(-dir.y, dir.x);
                float inner = h * (0.22f + 0.28f * R()) / Mathf.Max(0.35f, strength);
                float width = h * (0.004f + 0.012f * R());
                var c = tint; c.a = tint.a * strength * (0.45f + 0.55f * R());
                int v = vh.currentVertCount;
                vh.AddVert(focus + dir * inner, c, Vector4.zero);
                vh.AddVert(focus + dir * far + side * width, c, Vector4.zero);
                vh.AddVert(focus + dir * far - side * width, c, Vector4.zero);
                vh.AddTriangle(v, v + 1, v + 2);
            }
        }
        else
        {
            for (int i = 0; i < 46; i++)
            {
                float y = (R() - 0.5f) * h;
                float length = w * (0.25f + 0.6f * R()) * strength;
                float x0 = (R() - 0.5f) * w * 1.4f - length * 0.5f;
                float thick = h * (0.002f + 0.009f * R());
                var c = tint; c.a = tint.a * strength * (0.4f + 0.6f * R());
                var a = new Vector2(x0 * sweepDirection, y);
                var b = new Vector2((x0 + length) * sweepDirection, y);
                var mid = (a + b) * 0.5f;
                int v = vh.currentVertCount;
                vh.AddVert(a, c, Vector4.zero);
                vh.AddVert(mid + Vector2.up * thick, c, Vector4.zero);
                vh.AddVert(b, c, Vector4.zero);
                vh.AddVert(mid - Vector2.up * thick, c, Vector4.zero);
                vh.AddTriangle(v, v + 1, v + 2); vh.AddTriangle(v, v + 2, v + 3);
            }
        }
    }
}
