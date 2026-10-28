using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The opening titles of a new player's first game, in a "stylized action" look (user, 2026-10-20: "like an opening
// title sequence with cinematic shots - the camera suddenly closing in and pulling out, speed lines passing behind").
// About four seconds, before the lesson's first step: cinematic bars slide in, and the camera snaps from close-up to
// close-up - the chef, the bun baked in the oven, the patty on the pan, the first customer walking in - each a whip
// pan with sweeping speed lines, a hit with lines rushing in, a slammed label and a shake, then it pulls back out to
// the kitchen ("LET'S COOK!") and hands over to the lesson. The words name the loop (bake, fry, serve) and are set the
// way anime titles are (user's pick, 2026-10-21, after Permanent Marker and then Fredoka on pills): Bangers - the
// manga sound-effect face (TitleFontBuilder) - straight on the picture, no label, thick ink outline and an offset
// shadow in the shot's colour. A tap anywhere skips to the end. Everything is drawn at
// runtime (UISpeedLines, plain Images, the game's own fonts): nothing added to the download. The camera, the HUD and
// the time are put back exactly as they were.
public class LessonIntro : MonoBehaviour
{
    public static bool Playing { get; private set; }
    private static LessonIntro current;

    private Action done;
    private bool skip;
    private Camera cam;
    private Vector3 homePos;
    private Quaternion homeRot;
    private float homeSize, lookDistance;
    private CanvasGroup hud;
    private float hudAlpha;

    private RectTransform topBar, bottomBar;
    private Image flash;
    private UISpeedLines lines;
    private TextMeshProUGUI caption;
    private RectTransform label;       // the caption's frame (moved, turned, scaled as one)
    private CanvasGroup labelGroup;
    private Material captionMaterial;  // its own copy: the shadow's colour changes per shot
    private TMP_FontAsset titleFont;
    // Its own sounds, made here (user: "the bings and boings during the titles should go - speed sounds instead"):
    // a whoosh for each whip pan, a soft thump on each hit, a long whoosh as it pulls back out. The game's own cues
    // are silent while it plays (GameSfx).
    private AudioSource sfx;
    private AudioClip whoosh, whooshLong, thump;

    public static void Play(TMP_FontAsset font, Action whenDone)
    {
        var go = new GameObject("Lesson Intro", typeof(RectTransform));
        current = go.AddComponent<LessonIntro>();
        current.done = whenDone;
        current.titleFont = font;
        Playing = true;
    }

    public static void Skip()
    {
        if (current != null) current.skip = true;
    }

    private void Start()
    {
        cam = Camera.main;
        if (cam == null) { Finish(); return; }
        homePos = cam.transform.position;
        homeRot = cam.transform.rotation;
        homeSize = cam.orthographicSize;
        var f = cam.transform.forward;
        lookDistance = Mathf.Abs(f.y) > 0.01f ? (homePos.y - 1.2f) / -f.y : 20f;   // to the floor along the view
        var panel = GameObject.Find("<<<UI>>>/StaticCanvas/InGamePanel");
        hud = panel != null ? panel.GetComponent<CanvasGroup>() : null;
        if (hud != null) hudAlpha = hud.alpha;
        BuildOverlay();
        StartCoroutine(Run());
    }

    private void Sound(AudioClip clip, float volume)
    {
        if (clip != null && sfx != null && Audio.IsMusicOn) sfx.PlayOneShot(clip, volume);
    }

    private const int Rate = 22050;

    // Air rushing past: noise through a band that sweeps up and back down, swelling and fading.
    private static AudioClip Whoosh(float seconds, int seed)
    {
        int n = Mathf.CeilToInt(seconds * Rate);
        var d = new float[n];
        var rng = new System.Random(seed);
        float low = 0f, lower = 0f, peak = 0f;
        for (int i = 0; i < n; i++)
        {
            float u = i / (float)n;
            float sweep = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.7f));            // 0 -> 1 -> 0
            // A low, soft band riding up a little and back (a bright hiss grated - user, 2026-10-27).
            float k1 = Mathf.Lerp(0.008f, 0.07f, sweep), k2 = k1 * 0.3f;
            float white = (float)rng.NextDouble() * 2f - 1f;
            low += (white - low) * k1;
            lower += (low - lower) * k2;
            float env = Mathf.Sin(Mathf.PI * Mathf.Pow(u, 0.55f));
            d[i] = (low - lower) * env * env;
            peak = Mathf.Max(peak, Mathf.Abs(d[i]));
        }
        for (int i = 0; i < n; i++) d[i] *= 0.7f / Mathf.Max(peak, 1e-4f);
        var clip = AudioClip.Create("Whoosh", n, 1, Rate, false);
        clip.SetData(d, 0);
        return clip;
    }

    // A soft, low impact: a sine dropping in pitch with a puff of noise on top.
    private static AudioClip Thump()
    {
        int n = Mathf.CeilToInt(0.22f * Rate);
        var d = new float[n];
        var rng = new System.Random(11);
        float phase = 0f, low = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            phase += 2f * Mathf.PI * Mathf.Lerp(110f, 45f, t / 0.22f) / Rate;
            low += (((float)rng.NextDouble() * 2f - 1f) - low) * 0.2f;
            d[i] = (Mathf.Sin(phase) * 0.8f + low * 0.5f * Mathf.Exp(-t / 0.03f)) * Mathf.Exp(-t / 0.07f);
        }
        var clip = AudioClip.Create("Thump", n, 1, Rate, false);
        clip.SetData(d, 0);
        return clip;
    }

    private void BuildOverlay()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.spatialBlend = 0f;
        var gameSfx = FindFirstObjectByType<GameSfx>();
        var mixer = gameSfx != null ? gameSfx.GetComponent<AudioSource>() : null;
        if (mixer != null) { sfx.outputAudioMixerGroup = mixer.outputAudioMixerGroup; sfx.volume = mixer.volume; }
        whoosh = Whoosh(0.32f, 3);
        whooshLong = Whoosh(0.6f, 5);
        thump = Thump();
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        lines = Child<UISpeedLines>("Lines");
        lines.color = UITokens.Colors.WarmWhite;
        lines.raycastTarget = false;
        Stretch(lines.rectTransform);

        topBar = Bar("Top", 1f);
        bottomBar = Bar("Bottom", 0f);

        flash = Child<Image>("Flash");
        flash.color = new Color(1f, 0.98f, 0.95f, 0f);
        flash.raycastTarget = false;
        Stretch(flash.rectTransform);

        // The caption, in a frame the size of the screen's middle band (moved, turned and scaled as one).
        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(transform, false);
        label = (RectTransform)labelGo.transform;
        label.anchorMin = label.anchorMax = new Vector2(0.5f, 0.5f);
        label.sizeDelta = new Vector2(Screen.width, Screen.height * 0.3f);
        labelGroup = labelGo.AddComponent<CanvasGroup>();
        labelGroup.alpha = 0f;
        labelGroup.blocksRaycasts = false;
        caption = new GameObject("Caption", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        caption.transform.SetParent(label, false);
        if (titleFont != null) caption.font = titleFont;
        caption.fontSize = Screen.height * 0.15f;   // was 0.21: too big (user)
        caption.characterSpacing = 3f;
        caption.alignment = TextAlignmentOptions.Center;
        caption.raycastTarget = false;
        caption.textWrappingMode = TextWrappingModes.NoWrap;
        captionMaterial = new Material(caption.fontSharedMaterial);   // keeps the asset's OUTLINE_ON / UNDERLAY_ON
        captionMaterial.SetColor("_OutlineColor", UITokens.Colors.Ink);
        captionMaterial.SetFloat("_OutlineWidth", 0f);   // no ink frame round the letters (user, 2026-10-27): the shadow alone
        captionMaterial.SetFloat("_UnderlayOffsetX", 0.6f);
        captionMaterial.SetFloat("_UnderlayOffsetY", -0.6f);
        captionMaterial.SetFloat("_UnderlayDilate", 0.3f);   // the shadow as wide as the outlined word: a clean block
        captionMaterial.SetFloat("_UnderlaySoftness", 0f);
        ShaderUtilities.UpdateShaderRatios(captionMaterial);  // or the outline is clipped to the old range: ragged edges
        caption.fontSharedMaterial = captionMaterial;
        var rt = caption.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    // The word, its colour, and the colour of the shadow offset behind it (under the ink outline).
    private void SetLabel(string word, Color face, Color shadow)
    {
        caption.text = word;
        caption.color = face;
        captionMaterial.SetColor("_UnderlayColor", shadow);
    }

    private T Child<T>(string name) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        return go.AddComponent<T>();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private RectTransform Bar(string name, float y)
    {
        var img = Child<Image>(name);
        img.color = UITokens.Colors.Ink;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0f, y); rt.anchorMax = new Vector2(1f, y);
        rt.pivot = new Vector2(0.5f, y);
        rt.sizeDelta = new Vector2(0f, 0f);
        return rt;
    }

    // ---- the sequence ----
    private struct Shot { public Func<Vector3?> At; public float Size, Roll; public string Line; public Color Face, Shadow; }

    private IEnumerator Run()
    {
        var shots = new[]
        {
            new Shot { At = () => Chef(), Size = 3.0f, Roll = -5f, Line = "HI, CHEF!", Face = UITokens.Colors.WarmWhite, Shadow = UITokens.Colors.Raspberry },
            new Shot { At = () => Middle(FindFirstObjectByType<Oven>()), Size = 2.4f, Roll = 6f, Line = "BAKE!", Face = UITokens.Colors.WarmWhite, Shadow = UITokens.Colors.DeepYellow },
            new Shot { At = () => Middle(FindObjectsByType<Pan>(FindObjectsSortMode.InstanceID).FirstOrDefault(p => p.IsHaveFood())), Size = 2.4f, Roll = -6f, Line = "FRY!", Face = UITokens.Colors.WarmWhite, Shadow = UITokens.Colors.Tomato },
            new Shot { At = () => Customer(), Size = 3.2f, Roll = 5f, Line = "SERVE!", Face = UITokens.Colors.WarmWhite, Shadow = UITokens.Colors.Teal },
        };

        // Bars in, the HUD out.
        yield return Over(0.22f, t => { Bars(t); Hud(1f - t); });

        Vector3 from = Look(homePos);
        float fromSize = homeSize;
        float fromRoll = 0f;
        for (int i = 0; i < shots.Length && !skip; i++)
        {
            var shot = shots[i];
            var at = shot.At();
            if (at == null) continue;
            var target = at.Value;
            // Whip: a fast move with streaks across the screen.
            lines.mode = UISpeedLines.Mode.Sweep;
            lines.sweepDirection = i % 2 == 0 ? 1f : -1f;
            float wipe = i == 0 ? 0.28f : 0.16f;
            var start = from; float startSize = fromSize, startRoll = fromRoll;
            Sound(i == 0 ? whooshLong : whoosh, 0.4f);
            yield return Over(wipe, t =>
            {
                float e = t * t * (3f - 2f * t);
                Frame(Vector3.Lerp(start, target, e), Mathf.Lerp(startSize, shot.Size * 1.35f, e), Mathf.Lerp(startRoll, shot.Roll, e));
                lines.strength = Mathf.Sin(t * Mathf.PI);
                labelGroup.alpha = Mathf.Max(0f, labelGroup.alpha - Time.unscaledDeltaTime * 8f);
            });
            if (skip) break;
            // Hit: snap in, lines rush in at it, the caption slams, a shake.
            lines.mode = UISpeedLines.Mode.Radial;
            lines.focus = Vector2.zero;
            SetLabel(shot.Line, shot.Face, shot.Shadow);
            label.localRotation = Quaternion.Euler(0f, 0f, 6f - shot.Roll * 0.4f);
            label.anchoredPosition = new Vector2(0f, -Screen.height * 0.24f);
            Sound(thump, 0.5f);
            yield return Over(0.2f, t =>
            {
                float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                Frame(target, Mathf.Lerp(shot.Size * 1.35f, shot.Size, Ease(t)) * (2f - overshoot), shot.Roll, Shake(1f - t));
                lines.strength = 1f;
                flash.color = new Color(1f, 0.98f, 0.95f, (1f - t) * 0.35f);
                labelGroup.alpha = Mathf.Clamp01(t * 3f);
                label.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, Ease(t));
            });
            if (skip) break;
            // Hold: a slow push in, the lines settle to a flicker.
            yield return Over(0.55f, t =>
            {
                Frame(target, shot.Size * (1f - 0.05f * t), shot.Roll);
                lines.strength = Mathf.Lerp(1f, 0.35f, t);
            });
            from = target; fromSize = shot.Size * 0.95f; fromRoll = shot.Roll;
        }

        // Pull back out to the kitchen.
        lines.mode = UISpeedLines.Mode.Radial;
        SetLabel("LET'S COOK!", UITokens.Colors.Yellow, UITokens.Colors.Raspberry);
        label.localRotation = Quaternion.Euler(0f, 0f, 5f);
        label.anchoredPosition = Vector2.zero;
        if (!skip) Sound(whooshLong, 0.45f);
        var pullFrom = cam.transform.position; float pullSize = cam.orthographicSize, pullRoll = fromRoll;
        yield return Over(skip ? 0.2f : 0.45f, t =>
        {
            float e = Ease(t);
            cam.transform.position = Vector3.Lerp(pullFrom, homePos, e);
            cam.orthographicSize = Mathf.Lerp(pullSize, homeSize, e);
            cam.transform.rotation = homeRot * Quaternion.Euler(0f, 0f, Mathf.Lerp(pullRoll, 0f, e));
            lines.strength = (1f - t) * (skip ? 0.3f : 1f);
            labelGroup.alpha = skip ? 1f - t : Mathf.Clamp01(t * 4f);
            label.localScale = Vector3.one * Mathf.Lerp(1.5f, 1f, e);
        }, ignoreSkip: true);
        if (!skip) yield return Over(0.35f, t => { labelGroup.alpha = 1f - t * t; }, ignoreSkip: true);
        // Bars out, the HUD back.
        yield return Over(0.22f, t => { Bars(1f - t); Hud(t); labelGroup.alpha = Mathf.Min(labelGroup.alpha, 1f - t); }, ignoreSkip: true);
        Finish();
    }

    private void Finish()
    {
        if (cam != null)
        {
            cam.transform.position = homePos;
            cam.transform.rotation = homeRot;
            cam.orthographicSize = homeSize;
        }
        if (hud != null) hud.alpha = hudAlpha;
        Playing = false;
        if (current == this) current = null;
        if (captionMaterial != null) Destroy(captionMaterial);
        var whenDone = done;
        done = null;
        Destroy(gameObject);
        whenDone?.Invoke();
    }

    private void OnDestroy()
    {
        // Left early (a scene reload): never leave the camera somewhere else.
        if (Playing && current == this) { Playing = false; current = null; if (cam != null) { cam.transform.SetPositionAndRotation(homePos, homeRot); cam.orthographicSize = homeSize; } }
        if (whoosh != null) Destroy(whoosh);
        if (whooshLong != null) Destroy(whooshLong);
        if (thump != null) Destroy(thump);
    }

    // ---- helpers ----
    private IEnumerator Over(float seconds, Action<float> step, bool ignoreSkip = false)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            if (skip && !ignoreSkip) yield break;
            step(t / seconds);
            yield return null;
        }
        step(1f);
    }

    private static float Ease(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

    private Vector3 Shake(float amount) => amount <= 0f ? Vector3.zero
        : (cam.transform.right * UnityEngine.Random.Range(-1f, 1f) + cam.transform.up * UnityEngine.Random.Range(-1f, 1f)) * 0.12f * amount;

    // The camera looking at `look` (a point it should have in the middle), orthographic size, roll.
    private void Frame(Vector3 look, float size, float roll, Vector3 shake = default)
    {
        cam.transform.rotation = homeRot * Quaternion.Euler(0f, 0f, roll);
        cam.transform.position = look - homeRot * Vector3.forward * lookDistance + shake;
        cam.orthographicSize = size;
    }

    // The point the camera has in the middle when it is at `position` (looking along its home direction).
    private Vector3 Look(Vector3 position) => position + homeRot * Vector3.forward * lookDistance;

    private void Bars(float t)
    {
        float h = Screen.height * 0.12f * Ease(Mathf.Clamp01(t));
        topBar.sizeDelta = new Vector2(0f, h);
        bottomBar.sizeDelta = new Vector2(0f, h);
    }

    private void Hud(float t)
    {
        if (hud != null) hud.alpha = hudAlpha * Mathf.Clamp01(t);
    }

    private static Vector3? Chef()
    {
        var chef = FindFirstObjectByType<PlayerFSM>();
        return chef != null ? chef.transform.position + Vector3.up * 0.9f : null;
    }

    private static Vector3? Middle(Component c)
    {
        if (c == null) return null;
        var renderers = c.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r is not ParticleSystemRenderer).ToList();
        if (renderers.Count == 0) return c.transform.position;
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b.size.magnitude > 6f ? c.transform.position : b.center;
    }

    private static Vector3? Customer()
    {
        var npc = NpcFsm.Active.FirstOrDefault(n => n != null && n.isActiveAndEnabled);
        return npc != null ? npc.transform.position + Vector3.up * 0.8f : new Vector3(19.5f, 2f, 10.5f);
    }
}
