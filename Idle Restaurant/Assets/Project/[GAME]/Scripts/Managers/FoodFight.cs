using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Food flying across the cafe. The chef can throw a single ingredient at a waiting customer
// (PlayerFSM.TryCustomerAt → ChefThrows; the customer decides what it means in NpcFsm.CatchThrown), and
// customers throw a tomato back at the chef when bonked, pestered or left waiting too long
// (CustomerThrows); a Ranger shoots an arrow instead. It always hits. One rule for what it costs him:
// a tomato caught while running always puts him on his back (he keeps whatever he was carrying), and
// standing still it only splats — or freezes him a moment if his hands were empty.
public class FoodFight : MonoBehaviour
{
    public static FoodFight Instance { get; private set; }

    // A customer took a prepared ingredient as a snack (the first thing thrown at them, NpcFsm.CatchThrown).
    // The first-shift lesson waits for this. Static: subscribe with named methods only.
    public static readonly UnityEvent OnSnackAccepted = new();

    public enum Hit { Splat, Slip, Arrow }
    // World position of the chef's head when something hits him, and what it was (UISplatPop).
    public static readonly UnityEvent<Vector3, Hit> OnChefSplatted = new();

    [SerializeField] private GameObject tomatoModel;          // the whole-tomato visual the customers throw
    [SerializeField] private GameObject arrowModel;           // what a Ranger shoots (long axis +Z, tip forward)
    [SerializeField] private float arrowSeconds = 0.4f;
    [SerializeField] private float arrowArc = 0.5f;
    [SerializeField] private float arrowStuckSeconds = 1.6f;  // it stays stuck in his head (or hat) a moment
    [SerializeField] private float arrowScale = 1.6f;
    [SerializeField] private Material splatMaterial;          // round particle for the splash
    [SerializeField] private float chefThrowSeconds = 0.55f;
    [SerializeField] private float customerThrowSeconds = 0.7f;
    [SerializeField] private float arcHeight = 2.4f;
    [SerializeField] private float throwBackDelay = 0.45f;
    [SerializeField] private float stunSeconds = 1f;
    [SerializeField] private float slipSeconds = 2.1f;   // the fall + getting-up animation

    private PlayerFSM chef;
    private Transform chefHead;
    private ParticleSystem splash;

    private void Awake()
    {
        Instance = this;
        chef = FindFirstObjectByType<PlayerFSM>();
        chefHead = chef.transform.Find("Model/Rig/root/hips/spine/chest/head");
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
        GameSfx.Play(GameSfx.Cue.ChefThrow);

        StartCoroutine(Fly(body, () => npc != null ? npc.transform.position + Vector3.up * 1.6f : body.position, chefThrowSeconds, () =>
        {
            Splash(body.position, ColorOf(item.Name), 14);
            if (item.Prep != Prep.Good) GameSfx.Play(GameSfx.Cue.Bonk);
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
        if (npc != null && npc.Kind == CustomerKind.Ranger && arrowModel != null)
        {
            yield return ShootArrow(start);
            yield break;
        }
        var tomato = Instantiate(tomatoModel, start, UnityEngine.Random.rotation);
        foreach (var c in tomato.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var b in tomato.GetComponentsInChildren<MonoBehaviour>()) Destroy(b);
        GameSfx.Play(GameSfx.Cue.CustomerThrow);

        yield return Fly(tomato.transform, ChefHead, customerThrowSeconds, () =>
        {
            Splash(tomato.transform.position, ColorOf("tomato"), 28);
            Destroy(tomato);
            GameSfx.Play(GameSfx.Cue.Splat);
            // Running into a tomato, he goes down every time: one rule the player can read off the screen.
            bool slip = chef.IsRunning;
            if (slip)
            {
                chef.Slip(slipSeconds);
                GameSfx.Play(GameSfx.Cue.Slip);
            }
            else if (chef.HeldFood == null) chef.Splat(stunSeconds);
            OnChefSplatted.Invoke(ChefHead(), slip ? Hit.Slip : Hit.Splat);
        });
    }

    // A Ranger's answer: a quick, flat shot that sticks in the chef's head (or hat) for a moment.
    private IEnumerator ShootArrow(Vector3 start)
    {
        var arrow = Instantiate(arrowModel, start, Quaternion.LookRotation(ChefHead() - start));
        arrow.transform.localScale *= arrowScale;   // the model is a thin prop: bigger reads from the game camera
        foreach (var c in arrow.GetComponentsInChildren<Collider>()) Destroy(c);
        GameSfx.Play(GameSfx.Cue.ArrowShot);

        yield return Fly(arrow.transform, ArrowTarget, arrowSeconds, () =>
        {
            GameSfx.Play(GameSfx.Cue.ArrowHit);
            if (chef.HeldFood == null) chef.Splat(stunSeconds);
            OnChefSplatted.Invoke(ChefHead(), Hit.Arrow);
            // Only the tip goes in: pull it back along its flight so the shaft and feathers stick out.
            arrow.transform.position -= arrow.transform.forward * (0.45f * arrowScale);
            if (chefHead != null) arrow.transform.SetParent(chefHead, true);
        }, arrowArc, false);

        yield return new WaitForSeconds(arrowStuckSeconds);
        if (arrow == null) yield break;
        var scale = arrow.transform.localScale;
        for (float t = 0f; t < 1f && arrow != null; t += Time.deltaTime / 0.2f)
        {
            arrow.transform.localScale = scale * (1f - t);
            yield return null;
        }
        if (arrow != null) Destroy(arrow);
    }

    private Vector3 ChefHead() => chef.transform.position + Vector3.up * 1.9f;
    private Vector3 ArrowTarget() => chef.transform.position + Vector3.up * 2.1f;   // the top of his head

    // Arc from where the object is to a (possibly moving) target; homing, so it always lands. Tumbling
    // food spins; an arrow points along its flight.
    private IEnumerator Fly(Transform body, Func<Vector3> target, float seconds, Action onArrive, float arc = -1f, bool spin = true)
    {
        Vector3 from = body.position;
        Vector3 axis = UnityEngine.Random.onUnitSphere;
        float height = arc >= 0f ? arc : arcHeight;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / seconds);
            var next = Vector3.Lerp(from, target(), t) + Vector3.up * (height * 4f * t * (1f - t));
            if (spin) body.Rotate(axis, 720f * Time.deltaTime, Space.World);
            else if ((next - body.position).sqrMagnitude > 1e-6f) body.rotation = Quaternion.LookRotation(next - body.position);
            body.position = next;
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
