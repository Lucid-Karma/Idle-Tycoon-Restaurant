using UnityEngine;

// Kitchen moments that deserve a pop, each with a look of its own (user, 2026-10-21: the first version was all soft
// smoke blobs - "ugly, everything is steam or fog; the splash suits its moment, the others should be unique too"):
//   Chop       each time the knife comes down: a few small bits of the ingredient hop off the board (the splash's
//              family; it was one big burst after the chopping - user: "smaller, or while it is being chopped")
//   BurgerDone a plate becomes a burger: a thick yellow ring bursts out and sparkles fly from it - "ta-da"
// (A sparkle when a bun or patty is ready was tried and taken out again - user: the ding and the colour say it.)
// Customers, sitting down and the bin get nothing: their reaction bubbles, coins and sounds already say it, and smoke
// stays the one effect for burning. Hard-edged and alpha blended (the food fight splash's material, Circles_AB, with
// the Hyper Casual FX textures), built in code on BurnSmoke's object: nothing new in the download.
public class KitchenFx : MonoBehaviour
{
    private static KitchenFx instance;

    private Material crisp;
    private Texture2D sparkle, disc, rings;
    private ParticleSystem flares, chunks, thickRing;

    public static void Create(GameObject host, Material crisp, Texture2D sparkle, Texture2D disc, Texture2D rings)
    {
        if (crisp == null || sparkle == null || disc == null || rings == null) return;
        var fx = host.AddComponent<KitchenFx>();
        (fx.crisp, fx.sparkle, fx.disc, fx.rings) = (crisp, sparkle, disc, rings);
        fx.Build();
        instance = fx;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Chop(EdibleBase food)
    {
        if (instance == null || food == null) return;
        instance.Emit(instance.chunks, BurnSmoke.InFrontOf(food), 3, ColorOf(food.Name));
    }

    public static void BurgerDone(Vector3 at)
    {
        if (instance == null) return;
        instance.Emit(instance.thickRing, at, 1, UITokens.Colors.Yellow);
        instance.Emit(instance.flares, at, 4, UITokens.Colors.Yellow);
        instance.Emit(instance.flares, at, 2, UITokens.Colors.WarmWhite);
        instance.Emit(instance.flares, at, 2, UITokens.Colors.Raspberry);
    }

    private static Color ColorOf(string ingredient) => ingredient switch
    {
        "tomato" => UITokens.Colors.Tomato,
        "cheese" => UITokens.Colors.Yellow,
        "lettuce" => new Color(0.55f, 0.75f, 0.3f),
        "onion" => new Color(0.86f, 0.72f, 0.9f),
        _ => UITokens.Colors.Cream,
    };

    private void Emit(ParticleSystem ps, Vector3 at, int count, Color color)
    {
        ps.transform.position = at;
        ps.Emit(new ParticleSystem.EmitParams { startColor = color, applyShapeToPosition = true }, count);
    }

    private void Build()
    {
        // Flares: sparkles thrown out of the burst, slowing down as they shrink.
        flares = System("Flares", sparkle, lifetime: (0.6f, 0.8f), speed: (3f, 4.5f), size: (0.7f, 1f), gravity: 0f,
            Curve((0f, 0.6f), (0.15f, 1f), (1f, 0f)));
        var shape = flares.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;
        var drag = flares.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.limit = 0.5f;
        drag.dampen = 0.12f;
        Spin(flares, 200f);

        // Chunks: small solid bits that hop off the board and fall, like the splash.
        chunks = System("Chunks", disc, lifetime: (0.4f, 0.55f), speed: (1.8f, 2.6f), size: (0.16f, 0.24f), gravity: 1.6f,
            Curve((0f, 1f), (0.7f, 1f), (1f, 0.4f)));
        shape = chunks.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.2f;
        shape.rotation = new Vector3(-90f, 0f, 0f);   // the cone points up

        // A ring that grows out to its full size and fades (a tile of the 2x2 ring sheet).
        thickRing = Ring("ThickRing", tile: 1, size: 3.2f);
    }

    private ParticleSystem Ring(string name, int tile, float size)
    {
        var ps = System(name, rings, lifetime: (0.38f, 0.38f), speed: (0f, 0f), size: (size, size), gravity: 0f,
            Curve((0f, 0.15f), (1f, 1f)), fadeFrom: 0.2f);
        var shape = ps.shape;
        shape.enabled = false;
        var sheet = ps.textureSheetAnimation;
        sheet.enabled = true;
        sheet.numTilesX = 2;
        sheet.numTilesY = 2;
        sheet.frameOverTime = new ParticleSystem.MinMaxCurve((tile + 0.5f) / 4f);   // (normalised over the 4 tiles)
        var main = ps.main;
        main.startRotation = 0f;
        return ps;
    }

    private static void Spin(ParticleSystem ps, float degreesPerSecond)
    {
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-degreesPerSecond * Mathf.Deg2Rad, degreesPerSecond * Mathf.Deg2Rad);
    }

    private static AnimationCurve Curve(params (float t, float v)[] keys)
    {
        var curve = new AnimationCurve();
        foreach (var (t, v) in keys) curve.AddKey(t, v);
        return curve;
    }

    // fadeFrom: where in its life a particle starts fading out (until then it is fully opaque).
    private ParticleSystem System(string name, Texture2D texture, (float, float) lifetime, (float, float) speed,
        (float, float) size, float gravity, AnimationCurve sizeOverLife, float fadeFrom = 0.6f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.Item1, lifetime.Item2);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.Item1, speed.Item2);
        main.startSize = new ParticleSystem.MinMaxCurve(size.Item1, size.Item2);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
        main.gravityModifier = gravity;
        main.maxParticles = 48;

        var emission = ps.emission;
        emission.enabled = false;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeOverLife);

        var color = ps.colorOverLifetime;   // opaque, then a quick fade: the colour is each emit's
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, fadeFrom), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = new Material(crisp) { mainTexture = texture, name = name };
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }
}
