using System;
using System.Collections.Generic;
using UnityEngine;

// How the chef's stack moves (PlayerFSM, "Carrying a stack"), the way the arcade cooking games do it (user, 2026-10-30:
// "use the kind of animation those games have for the stack"):
//   - what he takes flies to the top of his stack in a little arc and lands with a squash;
//   - the stack trails behind him as he runs - the higher, the more - and springs back when he stops;
//   - what he puts down (on a station, on the plate) pops as it lands;
//   - fries ride in his other hand when he carries anything else (a second column, PlayerFSM.Restack);
//   - when he slips, everything he carries is thrown up in the air, tumbling, and comes down into his hands as he gets
//     up - or onto Mochi's tray if the cat runs over in time, who then hands it back (Scatter; user, 2026-11-03).
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
    // Thrown up when he slips: how high above his head, and how far apart they spread on the way.
    private const float ScatterHeight = 1.9f, ScatterSpread = 0.55f;

    private Transform hand;
    private PlayerFSM chef;

    private sealed class Layer
    {
        public Vector3 Pos, Vel;        // in its parent's space (the hand, or the side hand)
        public Transform Parent;        // the parent Pos is measured in
        public Vector3 Applied = Vector3.one;   // the squash on its scale right now (undone by dividing it out: it may
                                                // have been moved to another parent meanwhile)
        public Vector3 From;            // its parent's space, where its flight started
        public float FlyStart = -1f, SquashStart = -1f;
    }

    // One pile: the stack in his hand, or the fries in his other one.
    private sealed class Column
    {
        public Transform Anchor;
        public readonly List<Transform> Order = new();     // bottom first
        public readonly List<float> Heights = new();
        public readonly List<float> Bottoms = new();     // how far its look reaches below its position
    }

    private readonly Dictionary<Transform, Layer> layers = new();
    private readonly Column main = new(), side = new();
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
        main.Anchor = hand;
        lidPrefab = topBun;
    }

    // `food` has just been taken from `fromWorld` (a crate, a station): it flies from there.
    public void Arrive(EdibleBase food, Vector3 fromWorld)
    {
        if (food == null || hand == null) return;
        var t = food.transform;
        var layer = Track(t);
        layer.From = t.parent.InverseTransformPoint(fromWorld);
        layer.Pos = layer.From;
        layer.Vel = Vector3.zero;
        layer.FlyStart = Time.time;
        layer.SquashStart = -1f;
        t.localPosition = layer.From;
    }

    // What he carries now, bottom first; `topBun`: it is a burger with a bun - its top goes on top. `aside`: what
    // rides in his other hand (its parent is that hand).
    public void Layout(IReadOnlyList<EdibleBase> carried, bool topBun = false, IReadOnlyList<EdibleBase> aside = null)
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
        Fill(main, carried);
        side.Anchor = aside != null && aside.Count > 0 && aside[0] != null ? aside[0].transform.parent : null;
        Fill(side, aside);
        // Gone from both piles: back to its own scale.
        done.Clear();
        foreach (var pair in layers) if (!main.Order.Contains(pair.Key) && !side.Order.Contains(pair.Key)) done.Add(pair.Key);
        foreach (var t in done)
        {
            if (t != null) Apply(t, layers[t].Applied, Vector3.one);
            layers.Remove(t);
            scatter.Remove(t);
        }
    }

    private void Fill(Column column, IReadOnlyList<EdibleBase> foods)
    {
        column.Order.Clear();
        column.Heights.Clear();
        column.Bottoms.Clear();
        if (foods == null) return;
        foreach (var food in foods)
        {
            if (food == null) continue;
            var t = food.transform;
            var layer = Track(t);
            column.Order.Add(t);
            // What its look takes up (EdibleBase.LookSpan), the way a plate stacks it: its bottom on the top of the
            // one below. (It was its collider's height, capped at 0.6 m: a whole onion is taller than that, and the
            // layer above sat inside it - user, 2026-11-04.) Measured upright, its squash taken out.
            // In the hand's own space (EdibleBase.UprightSpan): the same whatever pose the hand is in.
            t.localRotation = Quaternion.identity;
            var (bottom, top) = food.UprightSpan(layer.Applied.y);
            float scale = Mathf.Max(0.01f, Mathf.Abs(t.parent.lossyScale.y));
            column.Heights.Add(Mathf.Clamp((top - bottom) * scale, 0.04f, 2f) / scale);
            column.Bottoms.Add(bottom);
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
        if (layers.TryGetValue(t, out var layer))
        {
            // Moved to his other hand (or back): carry on from where it is, in the new hand's space.
            if (layer.Parent != t.parent)
            {
                if (layer.FlyStart >= 0f && layer.Parent != null) layer.From = t.parent.InverseTransformPoint(layer.Parent.TransformPoint(layer.From));
                layer.Pos = t.localPosition;
                layer.Vel = Vector3.zero;
                layer.Parent = t.parent;
            }
            return layer;
        }
        layer = new Layer { Pos = t.localPosition, Parent = t.parent };
        if (pops.TryGetValue(t, out var p)) { Apply(t, p.applied, Vector3.one); pops.Remove(t); }
        layers[t] = layer;
        return layer;
    }

    #region Thrown up when he slips
    // Each thing in the air: where it set off, which way it drifts, how it tumbles, and its place in the pile it
    // lands in (counted bottom first over both of his hands, the lid last).
    private sealed class Flight
    {
        public Vector3 Start, Drift, Axis;
        public int Index;
    }

    private readonly Dictionary<Transform, Flight> scatter = new();
    private float scatterStart = -1f, scatterAir, scatterHold, scatterHeight = ScatterHeight;
    private Func<int, Vector3> catchAt;     // where the i-th thing comes down (world); null: in his own hands

    public bool Scattering => scatterStart >= 0f;

    // Everything he carries goes up in the air for `airSeconds` and comes down where `landAt` says (Mochi's tray) -
    // and stays there, riding along, until `holdUntil` (Time.time), when it flies back into his hands - or, with no
    // `landAt`, straight into his hands as he gets up.
    public void Scatter(float airSeconds, Func<int, Vector3> landAt = null, float holdUntil = -1f, float height = ScatterHeight,
                        float spread = ScatterSpread)
    {
        scatterHeight = height;
        scatter.Clear();
        int index = 0;
        void Add(Transform t)
        {
            if (t == null) return;
            var drift = UnityEngine.Random.insideUnitSphere;
            drift.y = 0f;
            scatter[t] = new Flight
            {
                Start = t.position,
                Drift = drift.normalized * UnityEngine.Random.Range(0.4f, 1f) * spread,
                Axis = UnityEngine.Random.onUnitSphere,
                Index = index++,
            };
        }
        foreach (var t in main.Order) Add(t);
        foreach (var t in side.Order) Add(t);
        if (showLid && lid != null) Add(lid.transform);
        if (scatter.Count == 0) return;
        scatterStart = Time.time;
        scatterAir = airSeconds;
        scatterHold = holdUntil;
        catchAt = landAt;
    }

    // Still in one of his hands (or the burger's top): the stack only ever moves what he holds - it once dragged a
    // plate's cheese off to where it had been in his hands (chaos bot, 2026-11-04).
    private bool Ours(Transform t) => t != null && (t.parent == hand || (side.Anchor != null && t.parent == side.Anchor)
                                                     || (lid != null && t == lid.transform));

    // Where the i-th thing comes down when he catches it himself: over his hands, a little apart.
    private Vector3 OwnCatch(int i) => hand.position + Vector3.up * (0.12f * i);

    // Places everything that is in the air; true while it still is. Ends the throw by sending each thing home.
    private bool Fly(float now)
    {
        if (scatterStart < 0f) return false;
        float k = (now - scatterStart) / Mathf.Max(0.01f, scatterAir);
        bool holding = k >= 1f && catchAt != null && now < scatterHold;
        if (k < 1f || holding)
        {
            foreach (var (t, flight) in scatter)
            {
                if (!Ours(t)) continue;
                var land = catchAt != null ? catchAt(flight.Index) : OwnCatch(flight.Index);
                if (holding)
                {
                    t.position = land;
                    t.rotation = Quaternion.identity;
                    continue;
                }
                // Up high and back down, drifting apart and coming together again; the stack's top goes higher.
                float up = scatterHeight * (0.8f + 0.1f * flight.Index);
                t.position = Vector3.Lerp(flight.Start, land, k) + Vector3.up * (up * 4f * k * (1f - k))
                             + flight.Drift * Mathf.Sin(k * Mathf.PI);
                t.rotation = Quaternion.AngleAxis(540f * k, flight.Axis);
            }
            return true;
        }
        // Down: each one flies the last bit into its place in his hands (from the tray, or from just over them).
        foreach (var (t, _) in scatter)
        {
            if (t == null || !layers.TryGetValue(t, out var layer)) continue;
            layer.Parent = t.parent;
            layer.From = t.parent.InverseTransformPoint(t.position);
            layer.Pos = layer.From;
            layer.Vel = Vector3.zero;
            layer.FlyStart = now;
            layer.SquashStart = -1f;
        }
        scatter.Clear();
        scatterStart = -1f;
        catchAt = null;
        return false;
    }
    #endregion

    private void LateUpdate()
    {
        if (hand == null) return;
        float now = Time.time;
        bool flying = Fly(now);

        if (!flying)
        {
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            var velocity = chef != null && chef.Agent != null ? chef.Agent.velocity : Vector3.zero;
            velocity.y = 0f;
            float speed = velocity.magnitude;
            float run01 = Mathf.Clamp01(speed / 4.6f);
            var back = speed > 0.01f ? -velocity / speed : Vector3.zero;

            float top = Move(main, dt, now, run01, back);
            if (side.Anchor != null) Move(side, dt, now, run01, back);

            // The top bun on a burger in his hands, riding the stack like another layer.
            if (showLid && lid != null)
            {
                float lidLean = Mathf.Min(LeanPerLevel * main.Order.Count, MaxLean) * run01;
                float lidHop = Mathf.Abs(Mathf.Sin(now * StepsPerSecond * Mathf.PI - main.Order.Count * 0.5f)) * StepHop * run01;
                lid.transform.localPosition = new Vector3(0f, top, 0f) + hand.InverseTransformVector(back * lidLean + Vector3.up * lidHop);
                lid.transform.localRotation = Quaternion.identity;
            }
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

    // One pile towards its places; returns the height of its top (the anchor's space).
    private float Move(Column column, float dt, float now, float run01, Vector3 back)
    {
        var anchor = column.Anchor;
        float y = 0f;
        for (int i = 0; i < column.Order.Count; i++)
        {
            var t = column.Order[i];
            if (t == null || t.parent != anchor || !layers.TryGetValue(t, out var layer)) continue;   // (taken by something else)
            float lean = Mathf.Min(LeanPerLevel * i, MaxLean) * run01;
            float hop = Mathf.Abs(Mathf.Sin(now * StepsPerSecond * Mathf.PI - i * 0.5f)) * StepHop * run01;
            var target = new Vector3(0f, y - column.Bottoms[i], 0f) + anchor.InverseTransformVector(back * lean + Vector3.up * hop);
            y += column.Heights[i];
            if (layer.FlyStart >= 0f)
            {
                float k = Mathf.Clamp01((now - layer.FlyStart) / FlySeconds);
                float ease = 1f - (1f - k) * (1f - k);
                layer.Pos = Vector3.Lerp(layer.From, target, ease) + Vector3.up * (ArcHeight * 4f * k * (1f - k) / Mathf.Max(0.01f, anchor.lossyScale.y));
                if (k >= 1f)
                {
                    layer.FlyStart = -1f;
                    layer.Vel = Vector3.zero;
                    // It lands: it squashes, and the whole pile under it gives a little.
                    layer.SquashStart = now;
                    for (int j = 0; j < i; j++)
                        if (column.Order[j] != null && layers.TryGetValue(column.Order[j], out var below)) below.Vel += Vector3.down * 0.6f;
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
        return y;
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
