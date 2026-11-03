using System.Collections.Generic;
using UnityEngine;

// How the chef's stack moves (PlayerFSM, "Carrying a stack"), the way the arcade cooking games do it (user, 2026-10-30:
// "use the kind of animation those games have for the stack"):
//   - what he takes flies to the top of his stack in a little arc and lands with a squash;
//   - the stack trails behind him as he runs - the higher, the more - and springs back when he stops;
//   - what he puts down (on a station, on the plate) pops as it lands.
// Purely visual: only the carried things' local positions and, for a moment, their scale; the hand's own sway is
// CarryWobble. Added by the chef at runtime.
public class CarryStack : MonoBehaviour
{
    private const float FlySeconds = 0.26f, ArcHeight = 0.55f, SquashSeconds = 0.18f, Squash = 0.22f;
    // A small, cute bounce (user, 2026-10-30: "the top one trails nearly a man's height behind - it doesn't suit the
    // game; no need to copy those games, a sweet little hop and sway is enough"): each layer leans back a centimetre
    // more than the one under it as he runs (at most a few), and the stack hops with his steps, the hop rippling up.
    // In metres, in the world (the hand's own space is scaled: offsets measured there came out far too big).
    private const float LeanPerLevel = 0.012f, MaxLean = 0.05f;
    private const float StepHop = 0.02f, StepsPerSecond = 2.6f;
    private const float Stiffness = 320f, Damping = 22f;

    private Transform hand;
    private PlayerFSM chef;

    private sealed class Layer
    {
        public Vector3 Pos, Vel;        // hand space
        public Vector3 Applied = Vector3.one;   // the squash on its scale right now (undone by dividing it out: it may
                                                // have been moved to another parent meanwhile)
        public Vector3 From;            // hand space, where its flight started
        public float FlyStart = -1f, SquashStart = -1f;
    }

    private readonly Dictionary<Transform, Layer> layers = new();
    private readonly List<Transform> order = new();       // bottom first
    private readonly List<float> heights = new();
    private readonly Dictionary<Transform, (Vector3 applied, float start)> pops = new();
    private readonly List<Transform> done = new();
    private readonly List<(Transform t, Vector3 scale, float start)> popping = new();

    // Swaps the factor on its scale: `was` out, `now` in.
    private static void Apply(Transform t, Vector3 was, Vector3 now)
    {
        var s = t.localScale;
        t.localScale = new Vector3(s.x / was.x * now.x, s.y / was.y * now.y, s.z / was.z * now.z);
    }

    private GameObject lidPrefab, lid;
    private bool showLid;

    public void Init(PlayerFSM owner, Transform holdParent, GameObject topBun)
    {
        chef = owner;
        hand = holdParent;
        lidPrefab = topBun;
    }

    // `food` has just been taken from `fromWorld` (a crate, a station): it flies from there.
    public void Arrive(EdibleBase food, Vector3 fromWorld)
    {
        if (food == null || hand == null) return;
        var t = food.transform;
        var layer = Track(t);
        layer.From = hand.InverseTransformPoint(fromWorld);
        layer.Pos = layer.From;
        layer.Vel = Vector3.zero;
        layer.FlyStart = Time.time;
        layer.SquashStart = -1f;
        t.localPosition = layer.From;
    }

    // What he carries now, bottom first; `topBun`: it is a burger with a bun - its top goes on top.
    public void Layout(IReadOnlyList<EdibleBase> carried, bool topBun = false)
    {
        showLid = topBun && lidPrefab != null;
        if (showLid && lid == null)
        {
            lid = Instantiate(lidPrefab, hand);
            lid.name = "Burger Top (in hand)";
            foreach (var c in lid.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var b in lid.GetComponentsInChildren<MonoBehaviour>()) b.enabled = false;
        }
        if (lid != null && lid.activeSelf != showLid) lid.SetActive(showLid);
        order.Clear();
        heights.Clear();
        foreach (var food in carried)
        {
            if (food == null) continue;
            var t = food.transform;
            Track(t);
            order.Add(t);
            // Its height the way a plate stacks it (its collider's): the shapes' box came out too tall for some things
            // after a plate had changed their look - a gap opened between the lettuce and the onion (user, 2026-10-30).
            float height = 0.12f;
            if (food.collider != null) height = Mathf.Abs(food.collider.size.y * food.transform.lossyScale.y);
            height = Mathf.Clamp(height, 0.04f, 0.6f);
            heights.Add(height / Mathf.Max(0.01f, Mathf.Abs(hand.lossyScale.y)));
            t.localRotation = Quaternion.identity;
        }
        // Gone from the stack: back to its own scale.
        done.Clear();
        foreach (var pair in layers) if (!order.Contains(pair.Key)) done.Add(pair.Key);
        foreach (var t in done)
        {
            if (t != null) Apply(t, layers[t].Applied, Vector3.one);
            layers.Remove(t);
        }
    }

    // `food` has just been put down somewhere: a little pop where it lands.
    public void Landed(EdibleBase food)
    {
        if (food == null) return;
        var t = food.transform;
        pops[t] = (pops.TryGetValue(t, out var running) ? running.applied : Vector3.one, Time.time);
    }

    private Layer Track(Transform t)
    {
        if (layers.TryGetValue(t, out var layer)) return layer;
        layer = new Layer { Pos = t.localPosition };
        if (pops.TryGetValue(t, out var p)) { Apply(t, p.applied, Vector3.one); pops.Remove(t); }
        layers[t] = layer;
        return layer;
    }

    private void LateUpdate()
    {
        if (hand == null) return;
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        float now = Time.time;
        var velocity = chef != null && chef.Agent != null ? chef.Agent.velocity : Vector3.zero;
        velocity.y = 0f;
        float speed = velocity.magnitude;
        float run01 = Mathf.Clamp01(speed / 4.6f);
        var back = speed > 0.01f ? -velocity / speed : Vector3.zero;

        float y = 0f;
        for (int i = 0; i < order.Count; i++)
        {
            var t = order[i];
            if (t == null || !layers.TryGetValue(t, out var layer)) continue;
            float lean = Mathf.Min(LeanPerLevel * i, MaxLean) * run01;
            float hop = Mathf.Abs(Mathf.Sin(now * StepsPerSecond * Mathf.PI - i * 0.5f)) * StepHop * run01;
            var target = new Vector3(0f, y, 0f) + hand.InverseTransformVector(back * lean + Vector3.up * hop);
            y += heights[i];
            if (layer.FlyStart >= 0f)
            {
                float k = Mathf.Clamp01((now - layer.FlyStart) / FlySeconds);
                float ease = 1f - (1f - k) * (1f - k);
                layer.Pos = Vector3.Lerp(layer.From, target, ease) + Vector3.up * (ArcHeight * 4f * k * (1f - k) / Mathf.Max(0.01f, hand.lossyScale.y));
                if (k >= 1f)
                {
                    layer.FlyStart = -1f;
                    layer.Vel = Vector3.zero;
                    // It lands: it squashes, and the whole stack under it gives a little.
                    layer.SquashStart = now;
                    for (int j = 0; j < i; j++)
                        if (order[j] != null && layers.TryGetValue(order[j], out var below)) below.Vel += Vector3.down * 0.6f;
                }
            }
            else
            {
                // A spring towards its place: it lags and settles, more the higher it is.
                layer.Vel += (target - layer.Pos) * (Stiffness * dt);
                layer.Vel *= Mathf.Exp(-Damping * dt);
                layer.Pos += layer.Vel * dt;
            }
            t.localPosition = layer.Pos;
            t.localRotation = Quaternion.identity;
            var squash = Squashed(layer.SquashStart, now);
            Apply(t, layer.Applied, squash);
            layer.Applied = squash;
            if (layer.SquashStart >= 0f && now - layer.SquashStart > SquashSeconds) layer.SquashStart = -1f;
        }

        // The top bun on a burger in his hands, riding the stack like another layer.
        if (showLid && lid != null)
        {
            float lidLean = Mathf.Min(LeanPerLevel * order.Count, MaxLean) * run01;
            float lidHop = Mathf.Abs(Mathf.Sin(now * StepsPerSecond * Mathf.PI - order.Count * 0.5f)) * StepHop * run01;
            lid.transform.localPosition = new Vector3(0f, y, 0f) + hand.InverseTransformVector(back * lidLean + Vector3.up * lidHop);
            lid.transform.localRotation = Quaternion.identity;
        }

        // What was put down pops once where it lands.
        done.Clear();
        foreach (var pair in pops)
        {
            var t = pair.Key;
            if (t == null || !t.gameObject.activeInHierarchy || now - pair.Value.start > SquashSeconds)
            {
                if (t != null) Apply(t, pair.Value.applied, Vector3.one);
                done.Add(t);
                continue;
            }
            float k = (now - pair.Value.start) / SquashSeconds;
            popping.Add((t, Vector3.one * (1f + 0.22f * Mathf.Sin(k * Mathf.PI)), pair.Value.start));
        }
        foreach (var t in done) pops.Remove(t);
        foreach (var (t, scale, start) in popping)
        {
            Apply(t, pops[t].applied, scale);
            pops[t] = (scale, start);
        }
        popping.Clear();
    }

    // Wide and flat as it lands, then back with a little overshoot.
    private static Vector3 Squashed(float start, float now)
    {
        if (start < 0f) return Vector3.one;
        float k = Mathf.Clamp01((now - start) / SquashSeconds);
        float s = Squash * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.4f);
        return new Vector3(1f + s, 1f - s, 1f + s);
    }
}
