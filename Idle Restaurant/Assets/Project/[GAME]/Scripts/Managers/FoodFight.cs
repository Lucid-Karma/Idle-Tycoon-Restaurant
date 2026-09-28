using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Food flying across the cafe. The chef can throw a single ingredient at a waiting customer
// (PlayerFSM.TryCustomerAt → ChefThrows; the customer decides what it means in NpcFsm.CatchThrown), and
// customers throw a tomato back at the chef when bonked, pestered or left waiting too long
// (CustomerThrows): it always hits, splats, and freezes him for a moment.
public class FoodFight : MonoBehaviour
{
    public static FoodFight Instance { get; private set; }
    // World position of the chef's head when a tomato hits him (UISplatPop).
    public static readonly UnityEvent<Vector3> OnChefSplatted = new();

    [SerializeField] private GameObject tomatoModel;          // the whole-tomato visual the customers throw
    [SerializeField] private Material splatMaterial;          // round particle for the splash
    [SerializeField] private float chefThrowSeconds = 0.55f;
    [SerializeField] private float customerThrowSeconds = 0.7f;
    [SerializeField] private float arcHeight = 2.4f;
    [SerializeField] private float throwBackDelay = 0.45f;
    [SerializeField] private float stunSeconds = 1f;

    private PlayerFSM chef;
    private ParticleSystem splash;

    private void Awake()
    {
        Instance = this;
        chef = FindFirstObjectByType<PlayerFSM>();
        splash = BuildSplash();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ChefThrows(EdibleBase food, NpcFsm npc)
    {
        var item = new BurgerLayer(food.Name, food.Preparation); // read before the pooled object resets
        var body = food.transform;
        body.SetParent(null, true);
        var col = food.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(Fly(body, () => npc != null ? npc.transform.position + Vector3.up * 1.6f : body.position, chefThrowSeconds, () =>
        {
            Splash(body.position, ColorOf(item.Name), 14);
            if (col != null) col.enabled = true;
            food.gameObject.SetActive(false); // back to its pool
            if (npc != null) npc.CatchThrown(item);
        }));
    }

    public void CustomerThrows(NpcFsm npc) => StartCoroutine(ThrowBack(npc));

    private IEnumerator ThrowBack(NpcFsm npc)
    {
        yield return new WaitForSeconds(throwBackDelay);
        var start = (npc != null ? npc.transform.position : chef.transform.position + Vector3.forward * 6f) + Vector3.up * 1.6f;
        var tomato = Instantiate(tomatoModel, start, UnityEngine.Random.rotation);
        foreach (var c in tomato.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var b in tomato.GetComponentsInChildren<MonoBehaviour>()) Destroy(b);

        yield return Fly(tomato.transform, ChefHead, customerThrowSeconds, () =>
        {
            Splash(tomato.transform.position, ColorOf("tomato"), 28);
            Destroy(tomato);
            chef.Splat(stunSeconds);
            OnChefSplatted.Invoke(ChefHead());
        });
    }

    private Vector3 ChefHead() => chef.transform.position + Vector3.up * 1.9f;

    // Arc from where the object is to a (possibly moving) target, spinning; homing, so it always lands.
    private IEnumerator Fly(Transform body, Func<Vector3> target, float seconds, Action onArrive)
    {
        Vector3 from = body.position;
        Vector3 spin = UnityEngine.Random.onUnitSphere;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / seconds);
            body.position = Vector3.Lerp(from, target(), t) + Vector3.up * (arcHeight * 4f * t * (1f - t));
            body.Rotate(spin, 720f * Time.deltaTime, Space.World);
            yield return null;
        }
        onArrive();
    }

    private void Splash(Vector3 at, Color color, int count)
    {
        splash.transform.position = at;
        var emit = new ParticleSystem.EmitParams { startColor = color, applyShapeToPosition = true };
        splash.Emit(emit, count);
    }

    private static Color ColorOf(string ingredient) => ingredient switch
    {
        "tomato" => UITokens.Colors.Tomato,
        "cheese" => UITokens.Colors.Yellow,
        "lettuce" => new Color(0.55f, 0.75f, 0.3f),
        "onion" => UITokens.Colors.Cream,
        "burger" => new Color(0.45f, 0.25f, 0.2f),
        _ => new Color(0.9f, 0.7f, 0.45f),
    };

    private ParticleSystem BuildSplash()
    {
        var go = new GameObject("FoodSplash");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
        main.gravityModifier = 1.8f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 128;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = splatMaterial;
        return ps;
    }
}
