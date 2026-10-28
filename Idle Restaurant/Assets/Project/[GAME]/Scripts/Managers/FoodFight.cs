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
    // Mochi (the cat waiter) took a tomato meant for the kitchen: where his head is (UISplatPop: "MRRAOW!").
    public static readonly UnityEvent<Vector3> OnMochiSplatted = new();

    // How often a customer's tomato goes at Mochi instead of the chef when he is about: rarely - the chef is the
    // target, as he always was (user, 2026-10-15: it had become 30-60% Mochi). A little more often when Mochi is the
    // nearer of the two. It costs nothing; now and then he takes it personally and throws back (Waiter.Struck).
    private const float MochiChance = 0.08f, MochiNearerChance = 0.15f;
    private static readonly string[] HitByMochiLines = { "Bad kitty!", "OW! Kitty!", "Sorry, kitty!", "Not the cat!" };

    [SerializeField] private GameObject tomatoModel;          // the whole-tomato visual the customers throw
    [SerializeField] private GameObject arrowModel;           // what a Ranger shoots (long axis +Z, tip forward)
    [SerializeField] private GameObject bowModel;             // what they shoot it with (KayKit bow_withString)
    private const float BowHeight = 1.35f;                    // the bow from tip to tip, in metres
    private const float DrawSeconds = 0.45f;                  // the bow appearing and the string pulled back
    [SerializeField] private float arrowSeconds = 0.4f;
    [SerializeField] private float arrowArc = 0.5f;
    [SerializeField] private float arrowStuckSeconds = 1.6f;  // it stays stuck in his head (or hat) a moment
    [SerializeField] private float arrowScale = 1.6f;
    [SerializeField] private Material splatMaterial;          // round particle for the splash
    public Material SplatMaterial => splatMaterial;          // (also the unlit glow of the seasonal decorations)
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

    public void CustomerThrows(NpcFsm npc) => StartCoroutine(Warned(npc));

    // Who is throwing something at the chef right now (the follow camera keeps them in the picture with him).
    public static NpcFsm Thrower { get; private set; }
    private const float WarnSeconds = 0.9f;

    // A throw is seen coming (user, 2026-10-27: with the closer camera "we can't tell they threw something until it
    // hits us"): a red "!" over the thrower - or at the screen's edge, pointing at them - for a moment before the
    // throw and while it flies, and the camera takes them in.
    private IEnumerator Warned(NpcFsm npc)
    {
        if (npc != null)
        {
            Thrower = npc;
            UIOrderTags.Warn(npc, WarnSeconds + customerThrowSeconds);
        }
        yield return ThrowBack(npc);
        if (Thrower == npc) Thrower = null;
    }

    private IEnumerator ThrowBack(NpcFsm npc)
    {
        yield return new WaitForSeconds(Mathf.Max(throwBackDelay, npc != null ? WarnSeconds : 0f));
        var start = (npc != null ? npc.transform.position : chef.transform.position + Vector3.forward * 6f) + Vector3.up * 1.6f;
        if (npc != null && npc.Kind == CustomerKind.Ranger && arrowModel != null)
        {
            yield return ShootArrow(npc, start);
            yield break;
        }
        var mochi = MochiInRange(npc);
        if (mochi == null)
        {
            yield return TomatoAtChef(start);
            yield break;
        }
        var tomato = Tomato(start);
        GameSfx.Play(GameSfx.Cue.CustomerThrow);

        yield return Fly(tomato.transform, () => mochi != null ? mochi.Head : tomato.transform.position, customerThrowSeconds, () =>
        {
            Splash(tomato.transform.position, ColorOf("tomato"), 28);
            Destroy(tomato);
            GameSfx.Play(GameSfx.Cue.Splat);
            if (mochi == null) return;
            OnMochiSplatted.Invoke(mochi.Head);
            mochi.Struck(npc);
        });
    }

    // A customer's tomato at the chef: running into it puts him on his back; standing, it splats.
    private IEnumerator TomatoAtChef(Vector3 start)
    {
        var tomato = Tomato(start);
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
                // From behind (flying the way he runs): onto his face; otherwise onto his back.
                var flight = chef.transform.position - start;
                flight.y = 0f;
                chef.Slip(slipSeconds, Vector3.Dot(flight.normalized, chef.transform.forward) > 0.3f);
                GameSfx.Play(GameSfx.Cue.Slip);
            }
            else if (chef.HeldFood == null) chef.Splat(stunSeconds);
            OnChefSplatted.Invoke(ChefHead(), slip ? Hit.Slip : Hit.Splat);
        });
    }

    private GameObject Tomato(Vector3 at)
    {
        var tomato = Instantiate(tomatoModel, at, UnityEngine.Random.rotation);
        foreach (var c in tomato.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var b in tomato.GetComponentsInChildren<MonoBehaviour>()) Destroy(b);
        return tomato;
    }

    // Mochi, if he is in the cafe and the tomato goes his way this time.
    private Waiter MochiInRange(NpcFsm npc)
    {
        if (npc == null || Tutorial.Running) return null;
        var mochi = FindFirstObjectByType<Waiter>();
        if (mochi == null || !mochi.isActiveAndEnabled || mochi.Angry) return null;
        var from = npc.transform.position;
        bool nearer = (mochi.transform.position - from).sqrMagnitude < (chef.transform.position - from).sqrMagnitude;
        return UnityEngine.Random.value < (nearer ? MochiNearerChance : MochiChance) ? mochi : null;
    }

    // His answer: three tomatoes in quick succession at whoever threw it, and a line from them.
    public void MochiThrowsBack(Waiter mochi, NpcFsm npc) => StartCoroutine(Retaliate(mochi, npc));

    private IEnumerator Retaliate(Waiter mochi, NpcFsm npc)
    {
        yield return new WaitForSeconds(0.45f);
        if (mochi == null || npc == null) yield break;
        var target = npc.transform.position + Vector3.up * 1.6f;
        for (int i = 0; i < 3; i++)
        {
            if (mochi == null) yield break;
            var tomato = Tomato(mochi.Head);
            tomato.transform.localScale *= 0.75f;
            bool last = i == 2;
            if (i == 0) GameSfx.Play(GameSfx.Cue.ChefThrow);   // one sound for the volley: three in a row was noise
            StartCoroutine(Fly(tomato.transform, () => npc != null && npc.isActiveAndEnabled ? npc.transform.position + Vector3.up * 1.6f : target,
                customerThrowSeconds * 0.7f, () =>
                {
                    Splash(tomato.transform.position, ColorOf("tomato"), 16);
                    Destroy(tomato);
                    if (last) GameSfx.Play(GameSfx.Cue.Splat);
                    if (last && npc != null && npc.isActiveAndEnabled)
                        npc.OnNpcSays.Invoke(HitByMochiLines[UnityEngine.Random.Range(0, HitByMochiLines.Length)], Mood.Shocked);
                }, arcHeight * 0.6f));
            yield return new WaitForSeconds(0.16f);
        }
    }

    // A Ranger's answer: a bow appears in front of them and draws, the arrow leaves it in a quick, flat shot
    // and sticks in the chef's head (or hat) for a moment; the bow lingers, then goes. No animation clip: the
    // customers share the skeleton rig, which has no archery, so the bow and arrow move on their own.
    private IEnumerator ShootArrow(NpcFsm npc, Vector3 start)
    {
        var aim = ChefHead() - start;
        var flat = new Vector3(aim.x, 0f, aim.z).normalized;
        // The chef is usually *behind* a seated customer as the game camera sees it (the kitchen is further
        // back than the tables), so a bow held towards him hid inside the customer: it is held on their
        // camera side instead, a little forward of them.
        var toViewer = Camera.main != null ? -Camera.main.transform.forward : Vector3.zero;
        toViewer.y = 0f;
        // The bow's curve only shows from the side: held facing the chef, it was edge-on to the camera (the chef is
        // up the screen from the tables), a thin line nobody saw. It faces his way *on the screen* instead - the
        // aim with the camera's depth taken out - so the bow and its string are always seen side-on.
        var onScreen = Camera.main != null ? Vector3.ProjectOnPlane(aim, Camera.main.transform.forward) : flat;
        if (onScreen.sqrMagnitude < 0.0001f) onScreen = flat;
        // Its limbs run across that direction on the screen; the model's curve sits towards its pivot's back, so
        // the pivot looks away from the chef for the curve (and not the string) to point at him.
        var limbs = Camera.main != null ? Vector3.Cross(Camera.main.transform.forward, onScreen).normalized : Vector3.up;
        // Held out beside them, towards the chef as the screen shows it: in front of them, it overlapped their
        // head and body on screen and could hardly be seen.
        var at = npc.transform.position + Vector3.up * 1.45f + onScreen.normalized * 0.95f + toViewer.normalized * 0.3f;
        var bow = SpawnBow(at, -onScreen.normalized, limbs);
        var nock = bow != null ? bow.position : start;
        var arrow = Instantiate(arrowModel, nock, Quaternion.LookRotation(ChefHead() - nock));
        arrow.transform.localScale *= arrowScale;   // the model is a thin prop: bigger reads from the game camera
        foreach (var c in arrow.GetComponentsInChildren<Collider>()) Destroy(c);

        if (bow != null)
        {
            // Draw: the bow pops in while the arrow is pulled back along the string.
            var full = Vector3.one;          // (SpawnBow leaves it at zero, to pop in)
            var pulled = nock - arrow.transform.forward * 0.35f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / DrawSeconds)
            {
                float pop = t < 0.4f ? Mathf.Sin(t / 0.4f * Mathf.PI * 0.5f) * 1.12f : Mathf.Lerp(1.12f, 1f, (t - 0.4f) / 0.6f);
                bow.localScale = full * pop;
                arrow.transform.position = Vector3.Lerp(nock, pulled, Mathf.SmoothStep(0f, 1f, t));
                arrow.transform.rotation = Quaternion.LookRotation(ChefHead() - arrow.transform.position);
                yield return null;
            }
            bow.localScale = full;
            StartCoroutine(PutAway(bow, full));
        }
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

    // The bow, tip to tip `BowHeight` tall, its limbs along `up`; its curve bulges towards -`aim`.
    private Transform SpawnBow(Vector3 at, Vector3 aim, Vector3 up)
    {
        if (bowModel == null) return null;
        var pivot = new GameObject("Bow").transform;
        pivot.SetPositionAndRotation(at, Quaternion.LookRotation(aim, up));
        var model = Instantiate(bowModel, pivot);
        model.transform.localPosition = Vector3.zero;
        // KayKit's Unity-axis bow: limbs along Z, the arc bulging towards +X with the string behind it, flat
        // across Y. Limbs up, arc towards the target, string towards the archer.
        model.transform.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.right);
        foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            if (b.size.y > 0.01f) model.transform.localScale *= BowHeight / b.size.y;
            // centre the grip on the pivot
            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            model.transform.position += at - b.center;
        }
        pivot.localScale = Vector3.zero;
        return pivot;
    }

    // After the shot the bow kicks back a little, stays a moment, then shrinks away.
    private IEnumerator PutAway(Transform bow, Vector3 full)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.18f)
        {
            if (bow == null) yield break;
            bow.localScale = full * (1f + Mathf.Sin(t * Mathf.PI) * 0.12f);
            yield return null;
        }
        yield return new WaitForSeconds(0.55f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.2f)
        {
            if (bow == null) yield break;
            bow.localScale = full * (1f - t);
            yield return null;
        }
        if (bow != null) Destroy(bow.gameObject);
    }

    private Vector3 ChefHead() => chef.transform.position + Vector3.up * 1.9f;
    // Into the upper half of his head, from the head bone (his head spans about 1.05-2.15 m: a fixed 2.1 above his
    // feet only grazed the top of it, so the arrow seemed to fly over his head).
    private Vector3 ArrowTarget() => chefHead != null ? chefHead.position + Vector3.up * 0.55f : chef.transform.position + Vector3.up * 1.75f;

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
