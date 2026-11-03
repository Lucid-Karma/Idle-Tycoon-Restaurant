using UnityEngine;

// Simple shapes the UI draws at runtime instead of shipping textures for them (nothing added to the download).
public static class UIShapes
{
    private static Sprite disc, pill, notebook, ring;

    // A round, soft-edged white disc (tint it with the Image's colour).
    public static Sprite Disc()
    {
        if (disc != null) return disc;
        const int n = 128;
        var pixels = new Color32[n * n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float dx = (i + 0.5f) / n * 2f - 1f, dy = (j + 0.5f) / n * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[j * n + i] = new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * n * 0.5f));
            }
        return disc = Make(pixels, n, n, Vector4.zero);
    }

    // A thin white ring, soft-edged (the tap queue's marker round a thing: UITapQueue).
    public static Sprite Ring()
    {
        if (ring != null) return ring;
        const int n = 128;
        var pixels = new Color32[n * n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float dx = (i + 0.5f) / n * 2f - 1f, dy = (j + 0.5f) / n * 2f - 1f;
                float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - 0.86f) - 0.1f;   // 0.76..0.96 of the radius
                pixels[j * n + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(-d * n * 0.5f + 0.5f));
            }
        return ring = Make(pixels, n, n, Vector4.zero);
    }

    // A white pill with round ends, sliced (Image.Type.Sliced) so it stretches to any width.
    public static Sprite Pill()
    {
        if (pill != null) return pill;
        const int w = 64, h = 32;
        float r = h * 0.5f;
        var pixels = new Color32[w * h];
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                float x = i + 0.5f, y = j + 0.5f;
                float cx = Mathf.Clamp(x, r, w - r);
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - r) * (y - r));
                pixels[j * w + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f));
            }
        return pill = Make(pixels, w, h, new Vector4(r, r, r, r));
    }

    // A notebook: a page with lines and binding rings down its left side, white (the Image colours it).
    public static Sprite Notebook()
    {
        if (notebook != null) return notebook;
        const int n = 128;
        float px = 2f / n;
        var pixels = new Color32[n * n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                var p = new Vector2((i + 0.5f) / n * 2f - 1f, (j + 0.5f) / n * 2f - 1f);
                // the page's outline (a rounded box), three lines on it, rings along the left edge
                var q = new Vector2(Mathf.Abs(p.x - 0.06f), Mathf.Abs(p.y)) - new Vector2(0.62f, 0.8f) + Vector2.one * 0.16f;
                float box = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - 0.16f;
                float outline = Mathf.Abs(box) - 0.07f;
                float lines = 1f;
                foreach (float y in new[] { 0.32f, 0.02f, -0.28f })
                    lines = Mathf.Min(lines, Mathf.Max(Mathf.Abs(p.y - y) - 0.05f, Mathf.Abs(p.x - 0.14f) - 0.36f));
                float rings = 1f;
                foreach (float y in new[] { 0.5f, 0.17f, -0.17f, -0.5f })
                    rings = Mathf.Min(rings, new Vector2(p.x + 0.56f, p.y - y).magnitude - 0.1f);
                float shape = Mathf.Min(Mathf.Min(outline, lines), rings);
                pixels[j * n + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - shape / px));
            }
        return notebook = Make(pixels, n, n, Vector4.zero);
    }

    private static Sprite Make(Color32[] pixels, int w, int h, Vector4 border)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}
