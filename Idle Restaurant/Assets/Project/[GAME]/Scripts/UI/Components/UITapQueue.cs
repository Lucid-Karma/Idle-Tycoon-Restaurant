using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The chef's waiting taps (PlayerFSM.Queued): a thin teal ring round each thing (or customer) a tap is waiting for, flat
// like a mark on the counter, with a small "x2" when the same thing waits more than once (the plate: everything onto
// it, then "done!"). No numbers (user, 2026-10-30: "the queue's numbers and the customers' numbers look almost the
// same - numbers scattered all over the scene; make it a professional design"): the customers' numbered badges
// (UIOrderTags) are the only numbers in the kitchen, and the order of the taps is plain from the chef doing them.
// On an overlay canvas, under the HUD. Made at runtime by the chef (PlayerFSM.Start).
public class UITapQueue : MonoBehaviour
{
    private PlayerFSM chef;
    private Camera cam;

    private sealed class Mark
    {
        public RectTransform Root;
        public Image Edge, Line;
        public RectTransform Chip;
        public TMP_Text Count;
        public Object Key;
        public float Born;
    }

    private readonly List<Mark> marks = new();
    private readonly List<(Object key, Rect box, int count)> wanted = new();

    public static void Create(PlayerFSM chef)
    {
        var go = new GameObject("Tap Queue", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -2;   // over the world, under the order badges and the HUD
        var queue = go.AddComponent<UITapQueue>();
        queue.chef = chef;
    }

    private Mark Make()
    {
        var m = new Mark();
        var go = new GameObject("Queued", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        m.Root = (RectTransform)go.transform;
        m.Edge = Ring(m.Root, "Edge", UITokens.WithAlpha(UITokens.Colors.WarmWhite, 0.9f), 1.12f);
        m.Line = Ring(m.Root, "Line", UITokens.Colors.Teal, 1f);
        var chip = new GameObject("Count", typeof(RectTransform));
        chip.transform.SetParent(m.Root, false);
        m.Chip = (RectTransform)chip.transform;
        var face = chip.AddComponent<Image>();
        face.sprite = UIShapes.Pill();
        face.type = Image.Type.Sliced;
        face.color = UITokens.Colors.Teal;
        face.raycastTarget = false;
        var words = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        words.transform.SetParent(chip.transform, false);
        words.alignment = TextAlignmentOptions.Center;
        words.fontStyle = FontStyles.Bold;
        words.color = UITokens.Colors.WarmWhite;
        words.raycastTarget = false;
        var wr = words.rectTransform;
        wr.anchorMin = Vector2.zero;
        wr.anchorMax = Vector2.one;
        wr.offsetMin = wr.offsetMax = Vector2.zero;
        m.Count = words;
        go.SetActive(false);
        marks.Add(m);
        return m;
    }

    private static Image Ring(Transform parent, string name, Color color, float scale)
    {
        var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(parent, false);
        image.sprite = UIShapes.Ring();
        image.color = color;
        image.raycastTarget = false;
        var r = image.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.localScale = new Vector3(scale, scale, 1f);
        return image;
    }

    private void LateUpdate()
    {
        if (chef == null) { Destroy(gameObject); return; }
        cam = Camera.main;
        wanted.Clear();
        if (cam != null)
        {
            var queued = chef.Queued;
            for (int i = 0; i < queued.Count; i++)
            {
                Object key = queued[i].Customer != null ? queued[i].Customer : queued[i].Thing;
                int at = -1;
                for (int w = 0; w < wanted.Count; w++) if (wanted[w].key == key) { at = w; break; }
                if (at >= 0) { wanted[at] = (key, wanted[at].box, wanted[at].count + 1); continue; }
                if (Box(queued[i], out var box)) wanted.Add((key, box, 1));
            }
        }

        while (marks.Count < wanted.Count) Make();
        float minSize = Screen.height * 0.07f, maxSize = Screen.height * 0.16f;
        for (int i = 0; i < marks.Count; i++)
        {
            var m = marks[i];
            bool on = i < wanted.Count;
            if (m.Root.gameObject.activeSelf != on) m.Root.gameObject.SetActive(on);
            if (!on) { m.Key = null; continue; }
            var (key, box, count) = wanted[i];
            if (m.Key != key) { m.Key = key; m.Born = Time.unscaledTime; }
            // Flat, as if lying round the thing: as wide as it is on screen, a little more than half as tall.
            float width = Mathf.Clamp(box.width * 1.15f, minSize, maxSize);
            var size = new Vector2(width, width * 0.58f);
            m.Root.position = box.center;
            m.Edge.rectTransform.sizeDelta = m.Line.rectTransform.sizeDelta = size;
            // It settles in from a little bigger as it appears, then breathes very gently.
            float t = Mathf.Clamp01((Time.unscaledTime - m.Born) / 0.22f);
            float s = Mathf.Lerp(1.25f, 1f, 1f - (1f - t) * (1f - t)) * (1f + 0.03f * Mathf.Sin((Time.unscaledTime - m.Born) * 4f));
            m.Root.localScale = new Vector3(s, s, 1f);
            var line = m.Line.color;
            line.a = t;
            m.Line.color = line;
            m.Chip.gameObject.SetActive(count > 1);
            if (count > 1)
            {
                float h = Screen.height * 0.03f;
                m.Count.text = "x" + count;
                m.Count.fontSize = h * 0.72f;
                m.Chip.sizeDelta = new Vector2(h * 1.7f, h);
                m.Chip.anchoredPosition = new Vector2(size.x * 0.42f, size.y * 0.42f);
            }
        }
    }

    // Its renderers, looked up once a second at most (not every frame: the lookup makes garbage).
    private readonly Dictionary<Object, (Renderer[] shapes, float at)> shapesOf = new();

    private Renderer[] Shapes(Collider thing)
    {
        if (shapesOf.TryGetValue(thing, out var known) && Time.unscaledTime - known.at < 1f) return known.shapes;
        var shapes = thing.GetComponentsInChildren<Renderer>();
        shapesOf[thing] = (shapes, Time.unscaledTime);
        if (shapesOf.Count > 64) shapesOf.Clear();
        return shapes;
    }

    // The thing on screen (its renderers' box), or the customer's body.
    private bool Box(PlayerFSM.QueuedTap tap, out Rect box)
    {
        box = default;
        Bounds b;
        if (tap.Customer != null)
        {
            if (!tap.Customer.isActiveAndEnabled) return false;
            b = new Bounds(tap.Customer.transform.position + Vector3.up * 0.15f, new Vector3(0.9f, 0.3f, 0.9f));
        }
        else if (tap.Thing != null)
        {
            var shapes = Shapes(tap.Thing);
            if (shapes.Length > 0)
            {
                b = shapes[0].bounds;
                foreach (var r in shapes) if (r != null && r is not ParticleSystemRenderer) b.Encapsulate(r.bounds);
            }
            else b = tap.Thing.bounds;
            if (b.size.sqrMagnitude < 1e-6f || b.size.magnitude > 6f) b = new Bounds(tap.Point, Vector3.one * 0.5f);
        }
        else return false;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var p = cam.WorldToScreenPoint(new Vector3(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i < 4 ? b.min.z : b.max.z));
            if (p.z < 0f) return false;
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
        box = Rect.MinMaxRect(minX, minY, maxX, maxY);
        return true;
    }
}
