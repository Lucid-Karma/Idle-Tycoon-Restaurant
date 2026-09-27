using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using V = UnityEngine.Vector2;

// Bakes the Chibi Burger Cafe UI shapes and icons from signed-distance functions, so every sprite in
// Graphics/Sprites/UI shares one geometry and stroke language (see .claude/skills/chibi-burger-cafe-ui).
// All sprites are white; tint them with Image.color from UITokens. Add new icons here, 128 px canvas,
// ~14 px strokes, rounded ends.
public static class UISpriteBaker
{
    const string Dir = "Assets/Project/[GAME]/Graphics/Sprites/UI";

    [MenuItem("Tools/Chibi UI/Rebake UI Sprites")]
    public static void BakeAll()
    {
        Directory.CreateDirectory(Dir);

        // Surfaces (9-slice where a border is given)
        Bake("ui_rounded", 128, p => RBox(p, new V(64, 64), new V(64, 64), 40), 44, false);
        Bake("ui_shadow", 160, p => RBox(p, new V(80, 80), new V(56, 56), 40), 78, false, soft: 20);
        Bake("ui_shadow_round", 128, p => Circle(p, new V(64, 64), 40), 0, false, soft: 16);
        Bake("ui_circle", 256, p => Circle(p, new V(128, 128), 127), 0, true);
        Bake("ui_pill", 128, p => Circle(p, new V(64, 64), 64), 63, false);
        Bake("ui_ring", 128, p => Mathf.Abs(Circle(p, new V(64, 64), 44)) - 6, 0, true);
        Bake("ui_ring_bubble", 128, p => Mathf.Abs(Circle(p, new V(64, 64), 53)) - 8, 0, true);
        Bake("ui_tail", 64, p => Poly(p, new[] { new V(8, 62), new V(56, 62), new V(32, 14) }) - 4, 0, true);

        // Icons
        Bake("icon_close", 128, p => Mathf.Min(Seg(p, new V(38, 38), new V(90, 90), 9), Seg(p, new V(38, 90), new V(90, 38), 9)), 0, true);
        Bake("icon_check", 128, p => Mathf.Min(Seg(p, new V(28, 66), new V(52, 42), 10), Seg(p, new V(52, 42), new V(100, 90), 10)), 0, true);
        Bake("icon_star", 128, p => Poly(p, StarPoints(new V(64, 60), 56, 25)) - 5, 0, true);
        Bake("icon_person", 128, p => Mathf.Min(Circle(p, new V(64, 90), 24), Mathf.Max(RBox(p, new V(64, 28), new V(40, 32), 30), 8 - p.y)), 0, true);
        Bake("icon_cart", 128, p =>
        {
            float basket = Poly(p, new[] { new V(30, 94), new V(112, 94), new V(100, 56), new V(42, 56) }) - 3;
            float handle = Mathf.Min(Seg(p, new V(8, 106), new V(24, 106), 7), Seg(p, new V(24, 106), new V(42, 42), 7));
            float rail = Seg(p, new V(42, 42), new V(102, 42), 7);
            float wheels = Mathf.Min(Circle(p, new V(52, 20), 11), Circle(p, new V(94, 20), 11));
            return Mathf.Min(Mathf.Min(basket, handle), Mathf.Min(rail, wheels));
        }, 0, true);
        Func<V, float> speaker = p => Mathf.Min(RBox(p, new V(28, 64), new V(13, 17), 5), Poly(p, new[] { new V(34, 80), new V(64, 106), new V(64, 22), new V(34, 48) }) - 3);
        Bake("icon_sound_on", 128, p => Mathf.Min(speaker(p), Mathf.Min(Arc(p, new V(66, 64), 24, 7, 52), Arc(p, new V(66, 64), 46, 7, 52))), 0, true);
        Bake("icon_sound_off", 128, p => Mathf.Min(speaker(p), Mathf.Min(Seg(p, new V(80, 46), new V(112, 82), 7), Seg(p, new V(80, 82), new V(112, 46), 7))), 0, true);
        Bake("icon_burger", 128, p =>
        {
            float topBun = Mathf.Max(RBox(p, new V(64, 79), new V(48, 27), 27), 66 - p.y);
            float patty = RBox(p, new V(64, 54), new V(50, 7), 7);
            float bottomBun = RBox(p, new V(64, 32), new V(45, 10), 10);
            return Mathf.Min(topBun, Mathf.Min(patty, bottomBun));
        }, 0, true);
        Bake("icon_cutlery", 128, p =>
        {
            float fork = Mathf.Min(Seg(p, new V(44, 16), new V(44, 66), 6.5f), RBox(p, new V(44, 72), new V(16, 8), 7));
            fork = Mathf.Min(fork, Mathf.Min(Seg(p, new V(33, 76), new V(33, 110), 5), Mathf.Min(Seg(p, new V(44, 76), new V(44, 110), 5), Seg(p, new V(55, 76), new V(55, 110), 5))));
            float knife = Mathf.Min(Seg(p, new V(86, 16), new V(86, 60), 7), Mathf.Max(RBox(p, new V(88, 84), new V(10, 28), 10), 78 - p.x));
            return Mathf.Min(fork, knife);
        }, 0, true);
        Bake("icon_flame", 128, p =>
        {
            // Round base, a tall tongue leaning right and a short one on the left, inner flame cut out.
            float body = Circle(p, new V(63, 44), 32);
            float tall = Poly(p, new[] { new V(42, 56), new V(86, 56), new V(74, 116) }) - 5;
            float side = Poly(p, new[] { new V(36, 50), new V(54, 64), new V(38, 92) }) - 5;
            float outer = SMin(SMin(body, tall, 12), side, 10);
            float inner = SMin(Circle(p, new V(63, 36), 12), Poly(p, new[] { new V(55, 44), new V(71, 44), new V(64, 70) }) - 2, 8);
            return Mathf.Max(outer, -inner);
        }, 0, true);

        // Customer moods: a filled face with the features cut out (reads at ~28 px), tinted by UITokens.MoodColor.
        Func<V, float> head = p => Circle(p, new V(64, 64), 58);
        Bake("icon_face_love", 128, p =>
        {
            // Happy closed eyes (∩ ∩) and a wide smile.
            float eyes = Mathf.Min(ArcDir(p, new V(44, 72), 11, 5.5f, 90, 75), ArcDir(p, new V(84, 72), 11, 5.5f, 90, 75));
            float smile = ArcDir(p, new V(64, 56), 26, 6, -90, 62);
            return Mathf.Max(head(p), -Mathf.Min(eyes, smile));
        }, 0, true);
        Bake("icon_face_laugh", 128, p =>
        {
            // Squeezed eyes (> <) and a big open laugh.
            float left = Mathf.Min(Seg(p, new V(34, 84), new V(50, 76), 5), Seg(p, new V(50, 76), new V(34, 68), 5));
            float right = Mathf.Min(Seg(p, new V(94, 84), new V(78, 76), 5), Seg(p, new V(78, 76), new V(94, 68), 5));
            float mouth = Mathf.Max(Circle(p, new V(64, 50), 25), p.y - 50);
            return Mathf.Max(head(p), -Mathf.Min(Mathf.Min(left, right), mouth));
        }, 0, true);
        Bake("icon_face_shock", 128, p =>
        {
            // Wide eyes and a round "O" mouth.
            float eyes = Mathf.Min(Circle(p, new V(44, 76), 9), Circle(p, new V(84, 76), 9));
            float mouth = Mathf.Abs(Circle(p, new V(64, 40), 13)) - 5.5f;
            return Mathf.Max(head(p), -Mathf.Min(eyes, mouth));
        }, 0, true);
        Bake("icon_bolt", 128, p => Poly(p, new[] { new V(74, 120), new V(30, 58), new V(60, 58), new V(50, 8), new V(98, 74), new V(68, 74) }) - 5, 0, true);

        // Title / poster effects: radiating burst behind the hero, speed streaks, sweat drops.
        // Rays fade out towards the rim, so the burst never shows an edge however it is sized.
        Bake("fx_sunburst", 512, p =>
        {
            V q = p - new V(256, 256);
            float r = q.magnitude;
            return -Mathf.Sin(16f * Mathf.Atan2(q.y, q.x)) * r / 16f;
        }, 0, true, fade: p => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(90f, 250f, (p - new V(256, 256)).magnitude)));
        Bake("fx_speedline", 256, p =>
        {
            // Thick at the leading (right) end, tapering to a point behind.
            V a = new V(14, 128), b = new V(236, 128), pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(V.Dot(pa, ba) / V.Dot(ba, ba));
            return (pa - ba * h).magnitude - Mathf.Lerp(2f, 16f, h);
        }, 0, true);
        Bake("fx_drop", 128, p => SMin(Circle(p, new V(64, 46), 30), Poly(p, new[] { new V(42, 60), new V(86, 60), new V(64, 118) }) - 2, 10), 0, true);

        AssetDatabase.Refresh();
        Debug.Log("[UISpriteBaker] UI sprites baked to " + Dir);
    }

    static void Bake(string name, int size, Func<V, float> sdf, int border, bool mips, float soft = 0, Func<V, float> fade = null)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = sdf(new V(x + .5f, y + .5f));
                float a;
                if (soft > 0) { float t = Mathf.Clamp01(.5f - d / (2 * soft)); a = t * t * (3 - 2 * t); }
                else a = Mathf.Clamp01(.5f - d);
                if (fade != null) a *= fade(new V(x + .5f, y + .5f));
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255));
            }
        tex.SetPixels32(px);
        string path = Dir + "/" + name + ".png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100;
        imp.spriteBorder = new Vector4(border, border, border, border);
        imp.mipmapEnabled = mips;
        imp.alphaIsTransparency = true;
        imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.filterMode = FilterMode.Bilinear;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.SaveAndReimport();
    }

    static float Circle(V p, V c, float r) => (p - c).magnitude - r;

    // Smooth union (organic joins, e.g. the flame's body and tip).
    static float SMin(float a, float b, float k)
    {
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(b, a, h) - k * h * (1 - h);
    }

    static float Seg(V p, V a, V b, float r)
    {
        V pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(V.Dot(pa, ba) / V.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    }

    static float RBox(V p, V c, V half, float r)
    {
        V q = new V(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + new V(r, r);
        return new V(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
    }

    static float Poly(V p, V[] v)
    {
        float d = V.Dot(p - v[0], p - v[0]);
        float s = 1;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
        {
            V e = v[j] - v[i], w = p - v[i];
            V b = w - e * Mathf.Clamp01(V.Dot(w, e) / V.Dot(e, e));
            d = Mathf.Min(d, V.Dot(b, b));
            bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
        }
        return s * Mathf.Sqrt(d);
    }

    // Arc of radius r and stroke half-width w, opening towards +x, spanning ±halfAngle degrees.
    static float Arc(V p, V c, float r, float w, float halfAngle)
    {
        V q = p - c;
        float a0 = halfAngle * Mathf.Deg2Rad;
        if (Mathf.Abs(Mathf.Atan2(q.y, q.x)) <= a0) return Mathf.Abs(q.magnitude - r) - w;
        V end = new V(Mathf.Cos(a0), Mathf.Sin(a0) * (q.y >= 0 ? 1 : -1)) * r;
        return (q - end).magnitude - w;
    }

    // Arc like Arc() but centred on direction dirDeg (0 = +x, 90 = up).
    static float ArcDir(V p, V c, float r, float w, float dirDeg, float halfAngle)
    {
        float a = -dirDeg * Mathf.Deg2Rad;
        V q = p - c;
        V rotated = new V(q.x * Mathf.Cos(a) - q.y * Mathf.Sin(a), q.x * Mathf.Sin(a) + q.y * Mathf.Cos(a));
        return Arc(rotated + c, c, r, w, halfAngle);
    }

    static V[] StarPoints(V c, float outer, float inner)
    {
        var pts = new V[10];
        for (int k = 0; k < 10; k++)
        {
            float ang = (90f + k * 36f) * Mathf.Deg2Rad;
            pts[k] = c + new V(Mathf.Cos(ang), Mathf.Sin(ang)) * (k % 2 == 0 ? outer : inner);
        }
        return pts;
    }
}
