using System.Collections.Generic;
using UnityEngine;

// Every tap answers (user, 2026-10-30: "I want feedback for every interaction - the ingredient you tap grows and
// shrinks, or hops in place; someone who ignores every rule and just pokes at random things should still enjoy it;
// every element of the game should feel good"):
//   - a soft ring opens where the finger lands, always (UITapRipple);
//   - a customer hops; the chef hops;
//   - the thing under the finger squashes and stretches (TapJuice.Boing) - a crate, a station, a plate, food;
//   - anything else with a shape - the giant burger, a pumpkin, a chair - wobbles with a "boing".
// Called first thing on every tap in the kitchen (PlayerFSM.HandleScreenTap), whatever the tap then does - in the
// lesson too, where a tap on the wrong thing makes that thing bounce instead of sending the chef anywhere.
public static class TapFeedback
{
    public static void On(Vector3 screen, PlayerFSM chef)
    {
        UITapRipple.Show(screen);
        var cam = Camera.main;
        if (cam == null) return;

        // A customer (they have no colliders): the one nearest the finger, around their middle.
        NpcFsm npc = null;
        float nearest = Screen.height * 0.06f;
        foreach (var n in NpcFsm.Active)
        {
            if (n == null) continue;
            var p = cam.WorldToScreenPoint(n.transform.position + Vector3.up * 0.9f);
            float d = Vector2.Distance(p, screen);
            if (p.z > 0f && d < nearest) { nearest = d; npc = n; }
        }
        if (npc != null)
        {
            var model = npc.transform.Find("Model");
            if (model != null) TapJuice.Hop(model, 0.28f);
            GameSfx.Play(GameSfx.Cue.Seated);
            return;
        }
        if (chef != null)
        {
            var p = cam.WorldToScreenPoint(chef.transform.position + Vector3.up * 0.9f);
            if (p.z > 0f && Vector2.Distance(p, screen) < Screen.height * 0.05f)
            {
                var body = chef.GetComponentInChildren<Animator>();
                if (body != null && body.transform != chef.transform) TapJuice.Hop(body.transform, 0.3f);
                else TapJuice.Boing(chef.transform, 0.12f);
                GameSfx.Play(GameSfx.Cue.Seated);
                return;
            }
        }

        if (Physics.Raycast(cam.ScreenPointToRay(screen), out var hit, 200f))
        {
            var thing = Shape(hit.collider, out bool useful);
            if (thing != null)
            {
                // It hops in place and squashes as it lands - small things more (a crate, food), furniture less.
                TapJuice.Boing(thing, useful ? 0.28f : 0.32f);
                TapJuice.Hop(thing, useful ? 0.14f : 0.1f);
                if (!useful) GameSfx.Play(GameSfx.Cue.Seated);
                return;
            }
        }
        var decor = DecorAt(screen, cam);
        if (decor != null)
        {
            TapJuice.Boing(decor, 0.3f);
            TapJuice.Hop(decor, 0.12f);
            GameSfx.Play(GameSfx.Cue.Seated);
        }
    }

    // What to bounce for a collider: the thing it is (a crate, a station, a plate, food, the bin) - "useful" - or the
    // nearest shape of a reasonable size above it (a chair, a table); never the floor or a wall.
    private static Transform Shape(Collider col, out bool useful)
    {
        useful = false;
        var proxy = col.GetComponent<TapProxy>();
        if (proxy != null) { useful = true; return proxy.transform; }
        if (col.GetComponent<EdibleBase>() != null || col.GetComponent<ISpawnable>() != null || col.GetComponent<PlaceableBase>() != null
            || col.GetComponent<Bin>() != null)
        {
            useful = true;
            return col.transform;
        }
        if (col.bounds.size.magnitude > 4f) return null;   // the floor, a wall, a counter run
        var t = col.transform;
        while (t != null && t.GetComponent<Renderer>() == null) t = t.parent;
        return t != null && t.GetComponent<Renderer>().bounds.size.magnitude < 4f ? t : null;
    }

    // Decoration with no collider (the giant burger and tomato, the carrots, the season's pumpkins and ghost...): the
    // smallest one whose shape on screen holds the finger.
    private static readonly List<Renderer> decor = new();
    private static float decorFound = -999f;

    private static Transform DecorAt(Vector2 screen, Camera cam)
    {
        if (Time.unscaledTime - decorFound > 5f)
        {
            decorFound = Time.unscaledTime;
            decor.Clear();
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.isPartOfStaticBatch) continue;
                var root = r.transform;
                bool isDecor = false;
                while (root != null)
                {
                    if (root.name == "Decorations" || root.name.StartsWith("Seasonal Decor")) { isDecor = true; break; }
                    root = root.parent;
                }
                if (isDecor) decor.Add(r);
            }
        }
        Transform best = null;
        float smallest = float.MaxValue;
        foreach (var r in decor)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            var b = r.bounds;
            if (b.size.magnitude > 8f) continue;
            var lo = cam.WorldToScreenPoint(b.min);
            var hi = cam.WorldToScreenPoint(b.max);
            var rect = Rect.MinMaxRect(Mathf.Min(lo.x, hi.x), Mathf.Min(lo.y, hi.y), Mathf.Max(lo.x, hi.x), Mathf.Max(lo.y, hi.y));
            // (the corners of a box seen at an angle: widen it a little)
            rect.xMin -= rect.width * 0.1f; rect.xMax += rect.width * 0.1f;
            if (!rect.Contains(screen)) continue;
            float area = rect.width * rect.height;
            if (area < smallest)
            {
                smallest = area;
                // A season decoration bounces as a whole (its pieces share a parent); a prop by itself.
                best = r.transform.parent != null && r.transform.parent.name != "Decorations" && !r.transform.parent.name.StartsWith("Seasonal Decor")
                    ? r.transform.parent : r.transform;
            }
        }
        return best;
    }
}
