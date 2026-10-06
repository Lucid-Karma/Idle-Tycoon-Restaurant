using UnityEngine;

// Central design tokens for Chibi Burger Cafe UI (.claude/skills/chibi-burger-cafe-ui).
// Every UI color, radius, spacing and motion value should come from here.
public static class UITokens
{
    public static class Colors
    {
        public static readonly Color Ink       = Hex(0x10263A);
        public static readonly Color InkSoft   = WithAlpha(Ink, 0.72f);
        public static readonly Color InkMuted  = WithAlpha(Ink, 0.55f);
        public static readonly Color WarmWhite = Hex(0xFFF9F4);
        public static readonly Color Cream     = Hex(0xF5E7D0);
        public static readonly Color Pink      = Hex(0xF6AFC2);
        public static readonly Color LightPink = Hex(0xF9D6E0);
        public static readonly Color Berry     = Hex(0xB96D8B);
        public static readonly Color Teal      = Hex(0x239FA4);
        public static readonly Color DarkTeal  = Hex(0x176C73);
        public static readonly Color Yellow    = Hex(0xF4C95D);
        public static readonly Color DeepYellow = Hex(0xD9A441);
        public static readonly Color Tomato    = Hex(0xE96A5F);
        public static readonly Color Raspberry = Hex(0xE0457B);       // the pulse on what to tap next: pink, but loud enough to find

        // Derived surfaces
        public static readonly Color SecondaryEdge = Hex(0xE6CAD5);   // edge of warm-white buttons over the 3D world
        public static readonly Color SubtleFace = Hex(0xFBE4EB);      // secondary button face on a warm-white card
        public static readonly Color SubtleEdge = Hex(0xE8BCCB);
        public static readonly Color StarEmpty = Hex(0xF8C7D5);
        public static readonly Color CreamSoft = Hex(0xFAF0E2);       // shop tile already owned: done, recedes
        public static readonly Color Scrim  = WithAlpha(Ink, 0.38f);
        public static readonly Color Shadow = new Color(0.35f, 0.16f, 0.26f, 0.22f);
    }

    public static class Space
    {
        public const float XS = 8f, S = 12f, M = 16f, L = 24f, XL = 32f;
        public const float ScreenMargin = 32f;
    }

    public static class Radius
    {
        public const float Small = 12f, Medium = 20f, Large = 28f, XL = 40f;
    }

    public static class Motion
    {
        public const float Fast = 0.10f, Normal = 0.18f, Slow = 0.30f;
        // Spring used for press / bounce feedback: slight overshoot, settles in ~200 ms.
        public const float SpringStiffness = 520f, SpringDamping = 20f;
        public const float PressScale = 0.95f, HoverScale = 1.03f, PunchScale = 1.12f;
    }

    public static class Typography
    {
        public const float Micro = 22f, Body = 26f, Button = 34f, Numeric = 46f, Heading = 56f, Hero = 96f;
    }

    // "Time left" colour for countdown rings (customer patience, food about to burn): teal above
    // half, yellow below half, tomato in the last quarter, with only a short blend at each edge so the
    // ring never lingers on a muddy in-between colour.
    public static Color UrgencyColor(float remaining)
    {
        float toYellow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 0.48f, remaining));
        float toTomato = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.27f, 0.23f, remaining));
        var color = Color.Lerp(Colors.Teal, Colors.DeepYellow, toYellow);
        return Color.Lerp(color, Colors.Tomato, toTomato);
    }

    // Customer mood faces (reaction bubble, order toast): teal = loved it, berry = laughing at the mess,
    // tomato = shocked.
    public static Color MoodColor(Mood mood) => mood switch
    {
        Mood.Delighted => Colors.Teal,
        Mood.Amused => Colors.Berry,
        _ => Colors.Tomato,
    };

    // Simple damped spring step (unscaled time friendly).
    public static void Spring(ref float value, ref float velocity, float target, float dt)
    {
        velocity += (target - value) * Motion.SpringStiffness * dt;
        velocity *= Mathf.Exp(-Motion.SpringDamping * dt);
        value += velocity * dt;
    }

    public static Color Hex(int rgb) =>
        new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

    public static Color WithAlpha(Color c, float a) { c.a = a; return c; }
}
