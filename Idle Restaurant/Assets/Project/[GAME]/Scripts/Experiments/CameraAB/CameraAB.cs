using UnityEngine;
using UnityEngine.SceneManagement;

// A/B test of the camera (user, 2026-10-27: "use the camera more: a shake at some moments; follow the player a little
// closer, so the whole kitchen is only seen at certain moments, with UI notices for customers off the screen -
// closer things are easier to tap, especially on phones, and it may feel more like a simulation. Let's A/B test it,
// and if we don't like it, remove all of it in one go").
//   A  the camera as it always was: the whole cafe, fixed.
//   B  after the first lesson, a closer camera (Closer: a little over half the size) that follows the chef and never
//      shows anything outside A's picture. It only moves once he walks out of a box round the middle of the screen
//      (user: "if he only moves a little, don't follow, or very slowly"), then glides after him. It widens to keep
//      two things in the picture: his burger's customer while he carries one, a customer throwing something at him
//      (FoodFight.Thrower - so it is seen coming), and the customer whose badge was tapped (Look); it opens out to the
//      whole cafe at the start of a shift, between shifts, on a level-up and while the snack tip shows; and it shakes
//      a little on a burger finished, a tomato or an arrow landing, a slip, a level-up. Customers it cannot see are
//      shown at the screen's edge by their order badges (UIOrderTags, which does that in both variants).
// No A/B split after all (user, same day: "let's not A/B test - keep only B for now and try it; if we don't like
// it we go back"): everyone gets B. ?cam=a in the address shows the old camera, to compare.
// The whole cafe on request (user, 2026-10-28: "a way to see the whole scene, even if only briefly"): a round eye
// button under the goal card, Tab, or the mouse wheel - the camera opens out for OverviewSeconds, or until asked again.
// TO GO BACK: delete this folder (Scripts/Experiments/CameraAB). Nothing else refers to it; the camera is then A.
public class CameraAB : MonoBehaviour
{
    private const float Closer = 0.6f;       // B's camera size, as a share of the whole-cafe one
    private const float Smooth = 0.5f;       // seconds the camera takes to catch up
    private const float DeadX = 0.3f, DeadY = 0.22f;   // the box he can move about in unfollowed, as a share of the view
    private const float WideAfterShiftStart = 3f, WideOnLevelUp = 2.5f, LookSeconds = 2.5f, OverviewSeconds = 5f;

    public static string Variant { get; private set; }
    private static CameraAB instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        Variant = Assign();
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Attach();
    }

    private static void OnLoaded(Scene scene, LoadSceneMode mode) => Attach();

    private static string Assign() => (Application.absoluteURL ?? "").ToLowerInvariant().Contains("cam=a") ? "A" : "B";

    private static void Attach()
    {
        if (GameObject.Find("Level") == null) return;
        if (Variant != "B") return;
        var cam = Camera.main;
        if (cam == null || cam.GetComponent<CameraAB>() != null || GameObject.Find("Level") == null) return;
        cam.gameObject.AddComponent<CameraAB>();
    }

    // Brings a customer into the picture for a moment (their order badge at the screen's edge was tapped).
    public static void Look(NpcFsm npc)
    {
        if (instance == null || npc == null) return;
        instance.lookAt = npc;
        instance.lookUntil = Time.time + LookSeconds;
    }

    private Camera cam;
    private PlayerFSM chef;
    private Vector3 homePos, homeFocus;
    private float homeSize;
    private float x, y, size, vx, vy, vs;   // the follow, in the camera's own right/up axes from the home picture
    private float followX, followY;         // where the dead box is centred
    private float wideUntil, shake, lookUntil, overviewUntil;
    private GameObject overviewButton;
    private UnityEngine.UI.Image overviewEye;
    private NpcFsm lookAt;
    private bool started;

    private void Start()
    {
        instance = this;
        cam = GetComponent<Camera>();
        homePos = transform.position;
        homeSize = cam.orthographicSize;
        size = homeSize;
        var floor = new Plane(Vector3.up, new Vector3(0f, 1.2f, 0f));
        var ray = new Ray(homePos, transform.forward);
        homeFocus = floor.Raycast(ray, out float d) ? ray.GetPoint(d) : homePos;
        chef = FindFirstObjectByType<PlayerFSM>();
        wideUntil = Time.time + WideAfterShiftStart;
        GameSfx.Played += OnCue;
        UIOrderTags.EdgeBadgeTapped += Look;
        EventManager.OnLevelStart.AddListener(OnShiftStart);
        MakeOverviewButton();
        started = true;
    }

    private void Overview() => overviewUntil = Time.unscaledTime < overviewUntil ? 0f : Time.unscaledTime + OverviewSeconds;

    // A copy of the help button (same look and press feel) with an eye on it, under the goal card on the right.
    private void MakeOverviewButton()
    {
        var help = GameObject.Find("<<<UI>>>/StaticCanvas/InGamePanel/HUD/HelpBTN");
        if (help == null) return;
        overviewButton = Instantiate(help, help.transform.parent);
        overviewButton.name = "OverviewBTN";
        foreach (var helper in overviewButton.GetComponents<HelpButton>()) Destroy(helper);
        var button = overviewButton.GetComponent<UnityEngine.UI.Button>();
        button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        button.onClick.AddListener(() => { EventManager.OnClick.Invoke(); Overview(); });
        foreach (var text in overviewButton.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.gameObject.SetActive(false);
        var rect = (RectTransform)overviewButton.transform;
        rect.anchoredPosition = new Vector2(-32f, -248f);
        var face = overviewButton.transform.Find("Face");
        var icon = new GameObject("Eye", typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
        icon.transform.SetParent(face != null ? face : overviewButton.transform, false);
        var iconRect = (RectTransform)icon.transform;
        iconRect.anchorMin = new Vector2(0.18f, 0.18f);
        iconRect.anchorMax = new Vector2(0.82f, 0.82f);
        iconRect.offsetMin = iconRect.offsetMax = Vector2.zero;
        icon.sprite = EyeSprite();
        icon.raycastTarget = false;
        overviewEye = icon;
    }

    // An eye drawn here (no texture added to the download): an almond outline and a pupil with a glint (white: the
    // Image colours it - ink, teal while the whole cafe is showing).
    private static Sprite eye;
    private static Sprite EyeSprite()
    {
        if (eye != null) return eye;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var ink = Color.white;
        var pixels = new Color32[n * n];
        float px = 2f / n;
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                var p = new Vector2((i + 0.5f) / n * 2f - 1f, (j + 0.5f) / n * 2f - 1f);
                // the almond: inside two big circles at once (above and below)
                float lens = Mathf.Max(Vector2.Distance(p, new Vector2(0f, -0.62f)) - 1.05f, Vector2.Distance(p, new Vector2(0f, 0.62f)) - 1.05f);
                float outline = Mathf.Abs(lens) - 0.075f;
                float pupil = p.magnitude - 0.27f;
                float glint = Vector2.Distance(p, new Vector2(0.09f, 0.1f)) - 0.08f;
                float shape = Mathf.Min(outline, pupil);
                float a = Mathf.Clamp01(0.5f - shape / px);
                if (glint < 0f) a = Mathf.Clamp01(glint / px + 0.5f) * a;
                var c = ink; c.a = a;
                pixels[j * n + i] = c;
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        eye = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return eye;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        GameSfx.Played -= OnCue;
        UIOrderTags.EdgeBadgeTapped -= Look;
        EventManager.OnLevelStart.RemoveListener(OnShiftStart);
    }

    private void OnShiftStart() => wideUntil = Time.time + WideAfterShiftStart;

    private void OnCue(GameSfx.Cue cue)
    {
        switch (cue)
        {
            case GameSfx.Cue.BurgerDone: Kick(0.1f); break;
            case GameSfx.Cue.Splat: case GameSfx.Cue.ArrowHit: Kick(0.2f); break;
            case GameSfx.Cue.Slip: Kick(0.28f); break;
            case GameSfx.Cue.LevelUp: Kick(0.14f); wideUntil = Time.time + WideOnLevelUp; break;
        }
    }

    private void Kick(float amount) => shake = Mathf.Max(shake, amount);

    private void LateUpdate()
    {
        if (overviewButton != null && overviewButton.activeSelf == (Tutorial.Running || LessonIntro.Playing))
            overviewButton.SetActive(!(Tutorial.Running || LessonIntro.Playing));   // (the lesson shows the whole cafe anyway)
        if (overviewEye != null) overviewEye.color = Time.unscaledTime < overviewUntil ? UITokens.Colors.Teal : UITokens.Colors.Ink;
        if (!started || LessonIntro.Playing) return;   // the opening titles fly the camera themselves
        if (!Tutorial.Running && Time.timeScale > 0f && (Input.GetKeyDown(KeyCode.Tab) || Input.mouseScrollDelta.y < -0.1f && Time.unscaledTime >= overviewUntil
                                                         || Input.mouseScrollDelta.y > 0.1f && Time.unscaledTime < overviewUntil))
            Overview();
        if (chef == null) chef = FindFirstObjectByType<PlayerFSM>();
        float dt = Time.unscaledDeltaTime;
        var right = transform.right;
        var up = transform.up;
        float aspect = Mathf.Max(0.1f, cam.aspect);

        // Where to look and how much to show.
        bool wide = Tutorial.Running || Tutorial.TipShowing || Time.time < wideUntil || Time.unscaledTime < overviewUntil || chef == null
                    || (UIBreakCard.Instance != null && UIBreakCard.Instance.Showing);
        float wantX = 0f, wantY = 0f, wantSize = homeSize;
        if (!wide)
        {
            var him = chef.transform.position;
            wantSize = homeSize * Closer;
            // Someone else who has to be in the picture with him: a customer throwing something at him, the one whose
            // badge was tapped, his burger's customer.
            NpcFsm other = FoodFight.Thrower;
            if (other == null && lookAt != null && Time.time < lookUntil && lookAt.isActiveAndEnabled) other = lookAt;
            if (other == null && chef.HeldFood is Hamburger burger && Orders.Waiting(burger.ForCustomer)) other = burger.ForCustomer;
            float hx = Vector3.Dot(him - homeFocus, right), hy = Vector3.Dot(him - homeFocus, up);
            if (other != null)
            {
                var them = other.transform.position + Vector3.up * 1.5f;
                float ox = Vector3.Dot(them - homeFocus, right), oy = Vector3.Dot(them - homeFocus, up);
                wantSize = Mathf.Clamp(Mathf.Max(Mathf.Abs(oy - hy) * 0.5f + 2.2f, (Mathf.Abs(ox - hx) * 0.5f + 2.2f) / aspect), wantSize, homeSize);
                followX = (hx + ox) * 0.5f;
                followY = (hy + oy) * 0.5f;
            }
            else
            {
                // Only once he leaves the box round the middle does the picture follow him (to the box's edge).
                float boxX = size * aspect * DeadX, boxY = size * DeadY;
                if (hx > followX + boxX) followX = hx - boxX; else if (hx < followX - boxX) followX = hx + boxX;
                if (hy > followY + boxY) followY = hy - boxY; else if (hy < followY - boxY) followY = hy + boxY;
            }
            wantX = followX;
            wantY = followY;
        }
        else { followX = x; followY = y; }

        size = Mathf.SmoothDamp(size, wantSize, ref vs, Smooth, Mathf.Infinity, dt);
        // Never past the edges of the whole-cafe picture.
        float roomY = homeSize - size, roomX = roomY * aspect;
        wantX = Mathf.Clamp(wantX, -roomX, roomX);
        wantY = Mathf.Clamp(wantY, -roomY, roomY);
        followX = Mathf.Clamp(followX, -roomX, roomX);
        followY = Mathf.Clamp(followY, -roomY, roomY);
        x = Mathf.SmoothDamp(x, wantX, ref vx, Smooth, Mathf.Infinity, dt);
        y = Mathf.SmoothDamp(y, wantY, ref vy, Smooth, Mathf.Infinity, dt);
        x = Mathf.Clamp(x, -roomX, roomX);
        y = Mathf.Clamp(y, -roomY, roomY);

        var offset = Vector3.zero;
        if (shake > 0.001f)
        {
            offset = (right * (Random.value - 0.5f) + up * (Random.value - 0.5f)) * 2f * shake * (size / homeSize);
            shake = Mathf.MoveTowards(shake, 0f, dt);
        }
        transform.position = homePos + right * x + up * y + offset;
        cam.orthographicSize = size;
    }
}
