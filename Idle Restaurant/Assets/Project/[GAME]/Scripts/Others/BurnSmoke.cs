using System.Collections.Generic;
using UnityEngine;

// Smoke when a bun or a patty burns: a grey puff the moment it happens, then a thin wisp rising from it for as
// long as the burnt thing is still on the stove or in the chef's hands (it stops once it is binned, stacked or
// served), so a burnt patty on a pan across the kitchen is visible at a glance, not only on its timer.
// Lives on <<<Controllers>>>/BurnSmoke (step 7 makes its URP particle material); gameplay calls
// BurnSmoke.On(food). The particle systems are built here in code and reused. The smoke starts a little in
// front of the food (toward the camera) and drifts that way as it rises: started on the food it was inside
// the range hood over the pan and inside the oven's box, and nobody saw it.
public class BurnSmoke : MonoBehaviour
{
    [SerializeField] private Material particleMaterial;   // URP particle material with a soft round blob (step 7)

    private static BurnSmoke instance;

    private ParticleSystem puff;
    private readonly List<(ParticleSystem wisp, EdibleBase food)> wisps = new();

    // Charcoal where it comes off the food, lightening to a pale grey as it thins: all dark, it read as a
    // stain on the floor rather than smoke in the air.
    private static readonly Color Smoke = new(0.36f, 0.33f, 0.35f, 1f);
    private static readonly Color SmokeFaded = new(0.86f, 0.84f, 0.86f, 1f);
    private const float InFront = 0.55f, InOven = 1.1f, Above = 0.2f;
    private Vector3 towardViewer;

    public static void On(EdibleBase food)
    {
        if (instance != null && food != null) instance.Burnt(food);
    }

    private void Awake()
    {
        instance = this;
        var cam = Camera.main;
        towardViewer = cam != null ? Vector3.ProjectOnPlane(-cam.transform.forward, Vector3.up).normalized : Vector3.back;
        puff = Build("Puff", burst: true);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Burnt(EdibleBase food)
    {
        puff.transform.position = Origin(food);
        puff.Emit(20);

        var (wisp, _) = wisps.Find(w => w.food == null);
        if (wisp == null)
        {
            wisp = Build("Wisp", burst: false);
            wisps.Add((wisp, food));
        }
        else
        {
            wisps[wisps.FindIndex(w => w.wisp == wisp)] = (wisp, food);
        }
        wisp.transform.position = Origin(food);
        wisp.Play();
    }

    private void LateUpdate()
    {
        for (int i = 0; i < wisps.Count; i++)
        {
            var (wisp, food) = wisps[i];
            if (food == null) continue;
            if (StillSmoking(food))
            {
                wisp.transform.position = Origin(food);
                continue;
            }
            wisp.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            wisps[i] = (wisp, null);
        }
    }

    // Still burnt and loose: on the stove or carried. Not once it is part of a burger or has been binned.
    private static bool StillSmoking(EdibleBase food) =>
        food.gameObject.activeInHierarchy && food.Preparation == Prep.Burnt
        && food.GetComponentInParent<Plate>() == null && food.GetComponentInParent<Hamburger>() == null
        && food.GetComponentInParent<NpcFsm>() == null;

    // A bun in the oven is deep inside the oven's box: its smoke comes out of the open door, further forward.
    private Vector3 Origin(EdibleBase food) =>
        TopOf(food) + towardViewer * (food.GetComponentInParent<Oven>() != null ? InOven : InFront) + Vector3.up * Above;

    private static Vector3 TopOf(EdibleBase food)
    {
        var renderers = food.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return food.transform.position + Vector3.up * 0.2f;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
    }

    private ParticleSystem Build(string name, bool burst)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = !burst;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = burst ? new ParticleSystem.MinMaxCurve(0.9f, 1.4f) : new ParticleSystem.MinMaxCurve(1.4f, 2f);
        main.startSpeed = burst ? new ParticleSystem.MinMaxCurve(0.5f, 1.0f) : new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        main.startSize = burst ? new ParticleSystem.MinMaxCurve(0.7f, 1.1f) : new ParticleSystem.MinMaxCurve(0.42f, 0.62f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;   // the colour comes from colorOverLifetime
        main.gravityModifier = burst ? -0.05f : -0.02f;
        main.maxParticles = burst ? 40 : 30;

        var emission = ps.emission;
        emission.enabled = !burst;
        emission.rateOverTime = 6.5f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = burst ? 50f : 10f;
        shape.radius = burst ? 0.18f : 0.08f;
        shape.rotation = new Vector3(-90f, 0f, 0f);   // the cone points up

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, burst ? 2.2f : 2.6f));

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Smoke, 0f), new GradientColorKey(SmokeFaded, 0.7f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(burst ? 0.95f : 0.9f, 0.1f), new GradientAlphaKey(burst ? 0.6f : 0.55f, 0.55f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var velocity = ps.velocityOverLifetime;   // a slow drift, so it curls instead of rising like a column
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        var drift = towardViewer * (burst ? 0.7f : 0.45f);
        velocity.x = new ParticleSystem.MinMaxCurve(drift.x - 0.15f, drift.x + 0.15f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(drift.z - 0.15f, drift.z + 0.15f);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }
}
