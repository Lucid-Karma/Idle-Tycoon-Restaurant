using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Order numbers (2026-10-27, user: "it is unclear which customer the recipe card is for and which card the burger
// being made follows - nobody should be even a little confused, as clear as for a five-year-old"). Every seated
// customer has a number (NpcFsm.Ticket), and the same round badge - the number on its own colour - is worn by:
//   their order bubble          who ordered it - on its corner, where it covers a little of the bubble's patience
//                               ring, so the badge carries the patience too: a ring of its own that fills the
//                               same way, in the same colours (out of the picture: at the screen's edge, pointing
//                               at them)
//   the plate being stacked     who this burger is for (Plate.ForCustomer: kept from the first layer on)
//   the finished burger         on the plate, in the chef's or Mochi's hands, until it is on their table
//   the order card              made by UIOrderCard with MakeBadge()
// so "which burger goes where" is matching two numbers. At the screen's edge a badge is bigger, never lies on
// another, and a tap on it brings that customer into the picture (EdgeBadgeTapped: the follow camera). A customer
// about to throw something at the chef shows a red "!" instead, over them or at the edge (Warn), so it is seen coming. Drawn on a screen overlay above the world (constant size,
// crisp at any zoom) and under the HUD's cards; a badge pops when its object first wears it. Created by UIOrderCard,
// which lends it the UI's font.
public class UIOrderTags : MonoBehaviour
{
    private static UIOrderTags instance;
    private static Sprite circle, ring;

    private TMP_FontAsset font;
    private RectTransform root;
    private readonly List<Badge> pool = new();
    private readonly Dictionary<Object, Badge> shown = new();
    private readonly Dictionary<Object, (int ticket, Vector3 screen, Vector2 toward, float waited)> want = new();
    private readonly List<Object> gone = new();
    private readonly Vector3[] corners = new Vector3[4];
    private readonly Dictionary<NpcFsm, float> warnings = new();
    private readonly List<Object> edge = new();
    private Camera cam;

    public const int Warning = -2;   // the "!" badge's ticket

    // A badge at the screen's edge was tapped: whose it is.
    public static event System.Action<NpcFsm> EdgeBadgeTapped;

    // A red "!" over this customer (or at the screen's edge, towards them) for `seconds`.
    public static void Warn(NpcFsm npc, float seconds)
    {
        if (instance != null && npc != null) instance.warnings[npc] = Time.time + seconds;
    }

    public static void Create(TMP_FontAsset font)
    {
        if (instance != null) return;
        var go = new GameObject("Order Tags", typeof(RectTransform));
        instance = go.AddComponent<UIOrderTags>();
        instance.font = font;
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -1;   // over the world (and its bubbles), under the HUD (0) and the lesson's spotlight
        go.AddComponent<GraphicRaycaster>();   // (only the edge badges are tappable)
        instance.root = (RectTransform)go.transform;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // A badge made for a UI parent (the order card's portrait): the caller places it and calls Set.
    public static Badge MakeBadge(Transform parent, TMP_FontAsset font, float size) => Badge.Make(parent, font, size);

    private float edgeSize;
    private float wideSize;   // the camera's widest view so far (the whole cafe): badges are sized for it


    private void LateUpdate()
    {
        cam = Camera.main;
        if (cam == null) return;
        // On a bubble the badge stays well under the burger icon's size (user: at 0.05 the numbers were nearly as big
        // as the burger), and keeps that share of the bubble when the camera comes closer (the bubbles grow on screen
        // with it; a badge of a fixed screen size looked smaller and smaller - user). Alone at the screen's edge it is
        // bigger and fixed: it has nothing to sit on.
        wideSize = Mathf.Max(wideSize, cam.orthographic ? cam.orthographicSize : 1f);
        float zoom = cam.orthographic ? wideSize / Mathf.Max(0.01f, cam.orthographicSize) : 1f;
        float size = Screen.height * 0.034f * zoom;
        edgeSize = Screen.height * 0.065f;
        want.Clear();

        // (Nothing during the opening titles: the camera is somewhere else.)
        if (!LessonIntro.Playing) Collect(size);
        Spread(edgeSize);

        // Hand the badges out (a pooled badge per object), hide the rest.
        gone.Clear();
        foreach (var pair in shown) if (pair.Key == null || !want.ContainsKey(pair.Key)) gone.Add(pair.Key);
        foreach (var key in gone) { shown[key].Hide(); pool.Add(shown[key]); shown.Remove(key); }
        foreach (var pair in want)
        {
            bool fresh = !shown.TryGetValue(pair.Key, out var badge);
            if (fresh)
            {
                badge = pool.Count > 0 ? pool[pool.Count - 1] : Badge.Make(root, font, size);
                if (pool.Count > 0) pool.RemoveAt(pool.Count - 1);
                shown[pair.Key] = badge;
            }
            if (fresh || badge.Ticket != pair.Value.ticket) badge.Pop();
            badge.Set(pair.Value.ticket);
            bool atEdge = pair.Value.toward != Vector2.zero;
            badge.Size(atEdge ? edgeSize : size);   // (at the edge a little bigger: it stands alone there)
            badge.Tappable(atEdge && pair.Key is NpcFsm, pair.Key as NpcFsm);
            badge.Point(pair.Value.toward);
            badge.Patience(pair.Value.waited);
            badge.Place(pair.Value.screen, root);
        }
    }

    private void Collect(float size)
    {
        // A customer about to throw something: a red "!" over their head (or at the edge, towards them).
        foreach (var pair in warnings)
        {
            var npc = pair.Key;
            if (npc == null || !npc.isActiveAndEnabled || Time.time > pair.Value) continue;
            var at = cam.WorldToScreenPoint(npc.transform.position + Vector3.up * 2.6f);
            var (inside, toward) = AtEdge(at, edgeSize);
            want[npc] = (Warning, inside, toward, -1f);
        }
        // Customers waiting for their burger: on their order bubble's top-left corner.
        foreach (var npc in NpcFsm.Active)
        {
            if (npc == null || npc.Ticket <= 0 || want.ContainsKey(npc)) continue;
            var state = npc.executingNpcState;
            if (state != ExecutingNpcState.ORDER && state != ExecutingNpcState.WAIT) continue;
            var bubble = Bubble(npc);
            if (bubble == null || !bubble.gameObject.activeInHierarchy) continue;
            bubble.GetWorldCorners(corners);   // bottom-left, top-left, top-right, bottom-right
            var at = cam.WorldToScreenPoint(corners[1]) + new Vector3(size * 0.2f, -size * 0.2f, 0f);
            // Out of the picture (a closer camera, a seat at the edge): the badge waits at the screen's edge and
            // points at them, so a customer nobody can see still says "I'm waiting, number 3".
            var (inside, toward) = AtEdge(at, edgeSize);
            float waited = npc.executingNpcState == ExecutingNpcState.WAIT ? Mathf.Clamp01(npc.WaitedSeconds / Mathf.Max(1f, npc.Patience)) : -1f;
            want[npc] = (npc.Ticket, inside, toward, waited);
        }

        // Plates being stacked for someone, and finished burgers that are not on a table yet.
        foreach (var plate in Orders.AllPlates)
        {
            if (plate == null || !plate.isActiveAndEnabled || plate.HasHamburger || plate.LayerCount == 0) continue;
            var who = plate.ForCustomer;
            if (who == null || who.Ticket <= 0) continue;
            want[plate] = (who.Ticket, cam.WorldToScreenPoint(Top(plate.transform)) + Vector3.up * size * 0.7f, Vector2.zero, -1f);
        }
        foreach (var burger in Orders.Burgers)
        {
            if (burger == null || !burger.isActiveAndEnabled) continue;
            var who = burger.ForCustomer;
            if (who == null || who.Ticket <= 0 || !Orders.Waiting(who)) continue;
            want[burger] = (who.Ticket, cam.WorldToScreenPoint(Top(burger.transform)) + Vector3.up * size * 0.7f, Vector2.zero, -1f);
        }
    }

    // A screen point kept inside the screen (with room for a badge of `size` and its patience ring, and clear of
    // the HUD's top row), and which way it was pushed in - zero when it was on the screen already.
    private static (Vector3 inside, Vector2 toward) AtEdge(Vector3 at, float size)
    {
        float m = size * 0.95f;
        if (at.z < 0f) at = new Vector3(Screen.width - at.x, Screen.height - at.y, 1f);
        var inside = new Vector3(Mathf.Clamp(at.x, m, Screen.width - m), Mathf.Clamp(at.y, m, Screen.height - m), 1f);
        var toward = (Vector2)(at - inside);
        return (inside, toward.sqrMagnitude > 1f ? toward.normalized : Vector2.zero);
    }

    // Badges waiting at the screen's edge: each edge's badges lined up along it in their order, a badge apart, in the
    // stretch of that edge the HUD leaves free - never under a card (the goal card and the last order card hid them),
    // never on top of one another, and on the left and right edges never up beside the top row of the HUD (user,
    // 2026-10-28, twice). The cards are measured by what they draw (their own rects can be smaller than that).
    private readonly List<Object> line = new();

    private void Spread(float size)
    {
        edge.Clear();
        foreach (var pair in want) if (pair.Value.toward != Vector2.zero) edge.Add(pair.Key);
        if (edge.Count == 0) return;
        CollectHud();
        float gap = size * 1.2f, pad = size * 0.7f;
        for (int side = 0; side < 4; side++)   // left, right, bottom, top
        {
            line.Clear();
            foreach (var key in edge) if (Side(want[key].toward) == side) line.Add(key);
            if (line.Count == 0) continue;
            bool vertical = side < 2;
            float length = vertical ? Screen.height : Screen.width;
            float lo = pad, hi = length - pad;
            if (vertical) hi = Mathf.Min(hi, Screen.height * 0.7f);
            foreach (var r in hudRects)
            {
                bool touches = side switch
                {
                    0 => r.xMin < size * 2.5f,
                    1 => r.xMax > Screen.width - size * 2.5f,
                    2 => r.yMin < size * 2.5f,
                    _ => r.yMax > Screen.height - size * 2.5f,
                };
                if (!touches) continue;
                float a = vertical ? r.yMin : r.xMin, b = vertical ? r.yMax : r.xMax;
                if ((a + b) * 0.5f > length * 0.5f) hi = Mathf.Min(hi, a - pad); else lo = Mathf.Max(lo, b + pad);
            }
            int n = line.Count;
            float need = gap * (n - 1);
            if (hi - lo < need) { float c = (lo + hi) * 0.5f; lo = c - need * 0.5f; hi = c + need * 0.5f; }
            line.Sort((x, y) => Along(want[x].screen, vertical).CompareTo(Along(want[y].screen, vertical)));
            var at = new float[n];
            for (int k = 0; k < n; k++) at[k] = Mathf.Clamp(Along(want[line[k]].screen, vertical), lo, hi);
            for (int k = 1; k < n; k++) at[k] = Mathf.Max(at[k], at[k - 1] + gap);
            if (at[n - 1] > hi)
            {
                at[n - 1] = hi;
                for (int k = n - 2; k >= 0; k--) at[k] = Mathf.Min(at[k], at[k + 1] - gap);
            }
            for (int k = 0; k < n; k++)
            {
                var w = want[line[k]];
                if (vertical) w.screen.y = at[k]; else w.screen.x = at[k];
                want[line[k]] = w;
            }
        }
    }

    private static int Side(Vector2 toward) =>
        Mathf.Abs(toward.x) >= Mathf.Abs(toward.y) ? (toward.x < 0f ? 0 : 1) : (toward.y < 0f ? 2 : 3);

    private static float Along(Vector3 p, bool vertical) => vertical ? p.y : p.x;

    // The HUD's cards and buttons, on screen.
    private static readonly string[] HudCovers = { "CustomersCard", "GoalCard", "AudioControls", "HelpBTN", "Wallet", "GrandGoal", "ChefCard", "OrderCard", "OverviewBTN" };
    private RectTransform[] hudCovers;
    private float hudFound;
    private readonly List<Rect> hudRects = new();
    private readonly List<Graphic> drawn = new();

    private void CollectHud()
    {
        if (hudCovers == null || Time.unscaledTime > hudFound + 2f)   // (looked up again now and then: the overview button comes later)
        {
            hudFound = Time.unscaledTime;
            var hud = GameObject.Find("<<<UI>>>/StaticCanvas/InGamePanel/HUD");
            hudCovers = hud == null ? new RectTransform[0]
                : System.Array.ConvertAll(HudCovers, n => hud.transform.Find(n) as RectTransform);
        }
        hudRects.Clear();
        foreach (var cover in hudCovers)
        {
            if (cover == null || !cover.gameObject.activeInHierarchy) continue;
            if (cover.TryGetComponent<CanvasGroup>(out var group) && group.alpha < 0.05f) continue;
            var canvas = cover.GetComponentInParent<Canvas>();
            var eye = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            cover.GetComponentsInChildren(false, drawn);
            foreach (var g in drawn)
            {
                if (!g.enabled || g.color.a < 0.02f) continue;
                g.rectTransform.GetWorldCorners(corners);
                foreach (var c in new[] { corners[0], corners[2] })
                {
                    Vector2 s = RectTransformUtility.WorldToScreenPoint(eye, c);
                    minX = Mathf.Min(minX, s.x); maxX = Mathf.Max(maxX, s.x); minY = Mathf.Min(minY, s.y); maxY = Mathf.Max(maxY, s.y);
                }
            }
            if (minX < maxX) hudRects.Add(Rect.MinMaxRect(minX, minY, maxX, maxY));
        }
    }

    private static RectTransform Bubble(NpcFsm npc)
    {
        var ticket = npc.GetComponentInChildren<UIOrderTicket>(false);
        return ticket != null ? ticket.transform.parent as RectTransform : null;
    }

    // The top of what is drawn (the stack on a plate, the burger), in the world.
    private static Vector3 Top(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>(false);
        bool any = false;
        var bounds = new Bounds(t.position, Vector3.zero);
        foreach (var r in renderers)
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
        }
        return any ? new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) : t.position + Vector3.up * 0.5f;
    }

    // A ring (the badge's patience), as a sprite an Image can fill radially.
    private static Sprite Ring()
    {
        if (ring != null) return ring;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[n * n];
        float outer = n * 0.5f, inner = outer * 0.78f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(outer, outer));
                float a = Mathf.Clamp01(outer - 0.5f - d + 1f) * Mathf.Clamp01(d - inner + 0.5f);
                pixels[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        ring = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return ring;
    }

    private static Sprite Circle()
    {
        if (circle != null) return circle;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[n * n];
        float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                byte a = (byte)(Mathf.Clamp01(r - 0.5f - d + 1f) * 255f);
                pixels[y * n + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return circle;
    }

    // One badge: a soft shadow, a warm-white rim, the number's colour, the number; and a pointer, when what it
    // stands for is off the screen.
    public class Badge
    {
        public int Ticket { get; private set; } = -1;
        public RectTransform Rect => rt;
        private RectTransform rt, pointer;
        private Image face, tip, track, patience;
        private Tap tap;
        private TextMeshProUGUI number;
        private float popUntil;

        public static Badge Make(Transform parent, TMP_FontAsset font, float size)
        {
            var b = new Badge();
            var go = new GameObject("Order Badge", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            b.rt = (RectTransform)go.transform;
            b.rt.anchorMin = b.rt.anchorMax = new Vector2(0.5f, 0.5f);
            // The pointer: a diamond under the rim, pushed out toward what the badge stands for.
            b.tip = new GameObject("Pointer", typeof(RectTransform)).AddComponent<Image>();
            b.tip.transform.SetParent(go.transform, false);
            b.tip.raycastTarget = false;
            b.tip.color = UITokens.Colors.WarmWhite;
            b.pointer = b.tip.rectTransform;
            b.pointer.anchorMin = b.pointer.anchorMax = new Vector2(0.5f, 0.5f);
            b.tip.gameObject.SetActive(false);
            Disc(go.transform, "Shadow", UITokens.Colors.Shadow, 1f, new Vector2(0f, -0.08f));
            Disc(go.transform, "Rim", UITokens.Colors.WarmWhite, 1f, Vector2.zero);
            // The patience on the badge's own white rim (user: a ring round it made the badge too big): the part
            // already waited fills it clockwise from the top in the bubble's colours, under the face.
            b.track = Disc(go.transform, "Patience track", UITokens.WithAlpha(UITokens.Colors.Ink, 0.12f), 1f, Vector2.zero);
            b.track.sprite = Ring();
            b.patience = Disc(go.transform, "Patience", UITokens.Colors.Teal, 1f, Vector2.zero);
            b.patience.sprite = Ring();
            b.patience.type = Image.Type.Filled;
            b.patience.fillMethod = Image.FillMethod.Radial360;
            b.patience.fillOrigin = (int)Image.Origin360.Top;
            b.patience.fillClockwise = true;
            b.track.gameObject.SetActive(false);
            b.patience.gameObject.SetActive(false);
            b.face = Disc(go.transform, "Face", Color.white, 0.8f, Vector2.zero);
            b.number = new GameObject("Number", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            b.number.transform.SetParent(go.transform, false);
            if (font != null) b.number.font = font;
            b.number.alignment = TextAlignmentOptions.Center;
            b.number.fontStyle = FontStyles.Bold;
            b.number.color = Color.white;
            b.number.raycastTarget = false;
            b.number.textWrappingMode = TextWrappingModes.NoWrap;
            var nrt = b.number.rectTransform;
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            b.Size(size);
            return b;
        }

        private static Image Disc(Transform parent, string name, Color color, float scale, Vector2 offset)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = Circle();
            image.color = color;
            image.raycastTarget = false;
            var r = image.rectTransform;
            r.anchorMin = new Vector2(0.5f - scale * 0.5f, 0.5f - scale * 0.5f) + offset;
            r.anchorMax = new Vector2(0.5f + scale * 0.5f, 0.5f + scale * 0.5f) + offset;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return image;
        }

        public void Set(int ticket)
        {
            rt.gameObject.SetActive(true);
            if (ticket == Ticket) return;
            Ticket = ticket;
            face.color = ticket == Warning ? UITokens.Colors.Tomato : UITokens.TicketColor(ticket);
            number.text = ticket == Warning ? "!" : ticket.ToString();
        }

        // At the screen's edge: a tap on it says whose it is (the face catches the tap; nothing else does).
        public void Tappable(bool on, NpcFsm who)
        {
            face.raycastTarget = on;
            if (!on) return;
            tap ??= face.gameObject.AddComponent<Tap>();
            tap.who = who;
        }

        public void Size(float size)
        {
            rt.sizeDelta = new Vector2(size, size);
            number.fontSize = size * 0.62f;
            float t = popUntil - Time.unscaledTime;   // a quick pop: bigger, then settle
            float k = t > 0f ? Mathf.Sin((1f - t / 0.3f) * Mathf.PI) * 0.35f : 0f;
            if (Ticket == Warning) k = Mathf.Max(k, 0.12f + 0.12f * Mathf.Sin(Time.unscaledTime * 18f));   // it throbs
            rt.localScale = Vector3.one * (1f + k);
        }

        public void Pop() => popUntil = Time.unscaledTime + 0.3f;

        // How much of their patience is used up (0..1), or below zero for no ring (plates, burgers, the card).
        public void Patience(float waited)
        {
            bool on = waited >= 0f;
            if (patience.gameObject.activeSelf != on) { patience.gameObject.SetActive(on); track.gameObject.SetActive(on); }
            if (!on) return;
            patience.fillAmount = waited;
            patience.color = UITokens.UrgencyColor(1f - waited);
        }

        // Toward what it stands for, when that is off the screen (zero: no pointer).
        public void Point(Vector2 toward)
        {
            bool on = toward != Vector2.zero;
            if (tip.gameObject.activeSelf != on) tip.gameObject.SetActive(on);
            if (!on) return;
            float size = rt.sizeDelta.x;
            pointer.sizeDelta = Vector2.one * size * 0.5f;
            pointer.anchoredPosition = toward * size * 0.55f;
            pointer.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg + 45f);
        }

        // At a screen point (pixels), in a screen-overlay canvas.
        public void Place(Vector3 screen, RectTransform canvas)
        {
            if (screen.z < 0f) { rt.gameObject.SetActive(false); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out var local);
            rt.anchoredPosition = local;
        }

        public void Hide()
        {
            rt.gameObject.SetActive(false);
            Ticket = -1;
            face.raycastTarget = false;
        }
    }

    private sealed class Tap : MonoBehaviour, IPointerClickHandler
    {
        public NpcFsm who;
        public void OnPointerClick(PointerEventData _) { if (who != null) EdgeBadgeTapped?.Invoke(who); }
    }
}
