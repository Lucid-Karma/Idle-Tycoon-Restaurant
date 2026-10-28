using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// The cafe dressed for the season (user, 2026-10-27: "on special days, models fitting the theme - in the places
// the cafe's own decorations stand and places where a decoration causes no trouble; bigger differences between
// themes, and it must look professional"). Each theme re-dresses the three giant props (the burger on its stand,
// the tomato, the standing carrot) and changes the mood of the whole picture:
//   Halloween  15 Oct - 3 Nov   spooky, not gloomy (user: the first night was far too dark): a twilight-purple sky
//                               full of twinkling stars and a crescent moon, little orange and violet sparks; the tomato is a giant
//                               jack-o'-lantern, little pumpkins sit on the burger's stand, a ghost peeks out from
//                               behind the burger's top; some customers come as ghosts. (A scared face on the giant
//                               burger was tried and taken off; the carrots stay - see below.)
//   Winter      1 Dec - 7 Jan   a cool, bright grade and snow drifting down the screen; the burger is a giant
//                               Christmas tree on its stand - star, baubles and little burgers for ornaments,
//                               presents round it - the tomato a snowman; customers stroll a little slower
//   Valentine   7 - 15 Feb      a warm pink grade and hearts rising; the tomato is a giant heart, the burger
//                               holds three heart balloons
// The props were static when the scene's lighting was baked: their shadows are painted into the floor's lightmap
// and stay when a prop is hidden. So a prop is only hidden where what replaces it is at least as big on screen and
// covers that shadow (the tomato's jack-o'-lantern, snowman and heart, the burger's tree on its stand); the carrots
// are never hidden (a ghost there left their shadows lying about - user, 2026-10-27).
// They cast no real shadows (their flat-shaded shadows looked odd, and every caster is another draw in the shadow
// map): each stands on a soft blob shadow instead - one quad. Each theme's music is the cafe's own loop
// made over for it (Audio: music_loop_<theme>.m4a, made by Tools/Music/make_theme_music.py).
// Everything is built here in code - low-poly and flat-shaded like the KayKit props, in coloured copies of the
// cafe's own lit material, glowing bits in the splash particle's unlit one - so nothing is added to the download.
// Only where the props already stand (and on the burger's stand, where nobody walks), never on a floor anyone
// walks: the navmesh is untouched. Try a theme with ?season=halloween / winter / valentine / none.
public static class SeasonalDecor
{
    public enum Season { None, Halloween, Winter, Valentine }

    public static Season? Force;   // editor / tests

    public static Season Current
    {
        get
        {
            if (Force.HasValue) return Force.Value;
            var url = (Application.absoluteURL ?? "").ToLowerInvariant();
            if (url.Contains("season=halloween")) return Season.Halloween;
            if (url.Contains("season=winter")) return Season.Winter;
            if (url.Contains("season=valentine")) return Season.Valentine;
            if (url.Contains("season=none")) return Season.None;
            var d = System.DateTime.Now;
            if ((d.Month == 10 && d.Day >= 15) || (d.Month == 11 && d.Day <= 3)) return Season.Halloween;
            if (d.Month == 12 || (d.Month == 1 && d.Day <= 7)) return Season.Winter;
            if (d.Month == 2 && d.Day >= 7 && d.Day <= 15) return Season.Valentine;
            return Season.None;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Rebuild();
    }

    private static void OnLoaded(Scene scene, LoadSceneMode mode) => Rebuild();

    // --- the cafe --------------------------------------------------------------------------------

    private static GameObject root, weather;
    private static Volume grade;
    private static readonly List<Renderer> hiddenProps = new();
    private static Color? cameraBackground;
    private static Season applied;

    public static Season Applied => applied;   // the theme on show

    public static void Rebuild()
    {
        Undo();
        if (Audio.Instance != null) Audio.Instance.MusicFor(Current);   // the theme's own music (Audio)
        var level = GameObject.Find("Level");
        var cam = Camera.main;
        if (level == null || cam == null) return;
        var season = Current;
        Screens(season);
        Solid(level, "food_burger", 0.95f);
        Solid(level, "food_ingredient_tomato", 0.9f);
        if (season == Season.None) return;
        var lit = FindLitMaterial(level);
        if (lit == null) return;
        applied = season;
        var kit = new Kit(lit, cam);
        root = new GameObject("Seasonal Decor (" + season + ")");
        var burger = Prop(level, "food_burger");
        var tomato = Prop(level, "food_ingredient_tomato");
        var carrot = Prop(level, "food_ingredient_carrot");
        cameraBackground = cam.backgroundColor;

        switch (season)
        {
            case Season.Halloween:
                Grade(new Color(0.94f, 0.87f, 0.99f), -0.12f, -10f, 6f);
                cam.backgroundColor = new Color(0.35f, 0.25f, 0.44f);
                if (tomato != null)
                {
                    Place(kit.Pumpkin(Radius(tomato) * 0.95f, face: true), Foot(tomato, 1.05f), 25f);
                    Place(kit.Candy(Radius(tomato), ShadowOffset(tomato)), Foot(tomato, 1.2f), 0f, blob: false);
                    Hide(tomato);
                }
                if (burger != null)
                {
                    Place(kit.Pumpkin(0.45f, face: true), StandSpot(burger, -35f), 30f);
                    Place(kit.Pumpkin(0.32f, face: false), StandSpot(burger, -60f), -20f);
                    Place(kit.Pumpkin(0.38f, face: false), StandSpot(burger, 30f), 70f);
                }
                if (burger != null)
                {
                    // Peeking out from behind the burger's top, on the kitchen's side, floating (no shadow).
                    var b = burger.bounds;
                    var side = Vector3.Cross(Vector3.up, ToViewer());   // the camera's right, on the ground
                    float r = Mathf.Min(b.size.x, b.size.z) * 0.5f;
                    var at = new Vector3(b.center.x, b.max.y - 2.1f, b.center.z) - side * r * 0.72f - ToViewer() * r * 0.5f;
                    Place(kit.BigGhost(2.4f), at, 0f, blob: false);
                }
                weather = kit.Sparks(cam);
                var sky = kit.NightSky(cam);
                if (weather != null) sky.transform.SetParent(weather.transform, true); else weather = sky;
                break;

            case Season.Winter:
                Grade(new Color(0.92f, 0.97f, 1.06f), 0.08f, -12f, 4f);
                cam.backgroundColor = new Color(0.62f, 0.68f, 0.86f);
                if (burger != null)
                {
                    var b = burger.bounds;
                    float r = Mathf.Min(b.size.x, b.size.z) * 0.48f;
                    var foot = new Vector3(b.center.x, 1.2f, b.center.z);
                    Place(kit.Stand(r, 0.77f), foot, 0f);
                    Place(kit.Tree(b.size.y * 1.05f, r * 0.8f), foot + Vector3.up * 0.77f, 10f);
                    Place(kit.Present(0.9f, UITokens.Colors.Raspberry, UITokens.Colors.Yellow), StandSpot(burger, -30f, 0.85f), 20f);
                    Place(kit.Present(0.7f, UITokens.Colors.Teal, UITokens.Colors.WarmWhite), StandSpot(burger, -5f, 0.88f), -15f);
                    Place(kit.Present(0.6f, UITokens.Colors.Yellow, UITokens.Colors.Raspberry), StandSpot(burger, 20f, 0.86f), 40f);
                    Hide(burger);
                }
                if (tomato != null)
                {
                    Place(kit.Snowman(Radius(tomato)), Foot(tomato, 1.2f), 0f, blob: false);   // (the snow is his shadow's ground)
                    Hide(tomato);
                }
                // A dusting of snow on the giant carrots (user, 2026-10-27).
                foreach (var name in new[] { "food_ingredient_carrot", "food_ingredient_carrot_chopped" })
                {
                    var prop = Prop(level, name);
                    if (prop != null) kit.SnowOn(prop, root.transform);
                }
                weather = kit.Snow(cam);
                break;

            case Season.Valentine:
                Grade(new Color(1.03f, 0.96f, 0.98f), 0.04f, 0f, 0f);
                if (tomato != null)
                {
                    Place(kit.BigHeart(Radius(tomato) * 1.05f, Radius(tomato) * 1.05f), Foot(tomato, 1.2f), 0f);
                    Hide(tomato);
                }
                if (burger != null)
                {
                    var b = burger.bounds;
                    for (int i = 0; i < 3; i++)
                        Place(kit.Balloon(b.max.y - 1.97f + 0.3f + i * 0.45f, i), StandSpot(burger, -40f + i * 35f, 0.9f), 0f, blob: false);
                }
                weather = kit.Hearts(cam);
                break;
        }
        // The customers already in the cafe (the rest are dressed as they come in).
        foreach (var npc in NpcFsm.Active) Dress(npc);
    }

    private static void Undo()
    {
        if (root != null) Object.Destroy(root);
        if (weather != null) Object.Destroy(weather);
        if (grade != null) { Object.Destroy(grade.profile); Object.Destroy(grade.gameObject); }
        foreach (var r in hiddenProps) if (r != null) r.enabled = true;
        hiddenProps.Clear();
        var cam = Camera.main;
        if (cam != null && cameraBackground.HasValue) cam.backgroundColor = cameraBackground.Value;
        cameraBackground = null;
        var was = applied;
        applied = Season.None;
        if (was != Season.None) foreach (var npc in NpcFsm.Active) Dress(npc);
    }

    // The whole picture's colour, over the scene's own grade (a separate volume of higher priority).
    private static void Grade(Color filter, float exposure, float saturation, float contrast)
    {
        var go = new GameObject("Seasonal Grade");
        grade = go.AddComponent<Volume>();
        grade.isGlobal = true;
        grade.priority = 50f;
        grade.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var adjust = grade.profile.Add<ColorAdjustments>(true);
        adjust.colorFilter.value = filter;
        adjust.postExposure.value = exposure;
        adjust.saturation.value = saturation;
        adjust.contrast.value = contrast;
    }

    // --- customers -------------------------------------------------------------------------------

    private static readonly Dictionary<NpcFsm, float> baseSpeed = new();

    // A customer coming in (NpcFsm.OnEnable): a ghost now and then at Halloween, a slower stroll in winter.
    public static void Dress(NpcFsm npc)
    {
        if (npc == null) return;
        var ghost = npc.GetComponent<SeasonalGhost>();
        if (ghost != null) ghost.Undress();
        if (applied == Season.Halloween && Random.value < 0.4f) Ghostify(npc);

        var agent = npc.Agent;
        if (agent == null) return;
        if (!baseSpeed.TryGetValue(npc, out float speed)) baseSpeed[npc] = speed = agent.speed;
        agent.speed = speed * (applied == Season.Winter ? 0.8f : 1f);
    }

    private static void Ghostify(NpcFsm npc)
    {
        var model = npc.transform.Find("Model");
        var cam = Camera.main;
        var level = GameObject.Find("Level");
        var lit = level != null ? FindLitMaterial(level) : null;
        if (model == null || lit == null || cam == null) return;
        var kit = new Kit(lit, cam);
        var g = npc.gameObject.AddComponent<SeasonalGhost>();
        g.Hide(model.GetComponentsInChildren<Renderer>(true));
        // Not the decoration's ghost: a face painted for the night (one of six) on a sheet of its own colour, and
        // a sheet that flutters as they glide.
        var (body, face) = kit.GhostParts(1.7f, Random.Range(1, Mesher.GhostFaces), Random.Range(0, 4));
        g.flutter = true;
        body.SetParent(model, false);
        body.localPosition = Vector3.zero;
        face.SetParent(model, false);   // (not keeping its world scale: the model is still at scale 0 as it pops in)
        g.body = body;
        g.face = face;
        var shadow = Kit.Blob(0.55f);
        if (shadow != null)
        {
            shadow.transform.SetParent(model, false);
            shadow.transform.localPosition = Vector3.up * 0.02f;
        }
    }

    // --- where -----------------------------------------------------------------------------------

    // The cafe's two little screens (the kitchen window, the frame on the right wall: Custom/UnlitScrollingTexture)
    // scroll a quiet strip of pastel text (Tools/Screens/make_screens.py): the frame always "beyzosh.com" (the plain
    // strip on the screens' material), the window a theme's own greeting or hearts (the material's _Halloween /
    // _Winter / _Valentine) - never the two in one strip (user). The strip is one band across the middle of each screen, the same
    // height in the world on both (the panel colour above and below it: the texture is clamped in V), and both scroll
    // at the same pace.
    private const float StripOfSmaller = 0.6f;  // the band's height, of the smaller screen's (its letters about half that)
    private const float StripPace = 0.35f;      // the band moves this many of its own heights a second
    private static void Screens(Season season)
    {
        var screens = new List<MeshRenderer>();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var m = r.sharedMaterial;
            if (m != null && m.shader != null && m.shader.name == "Custom/UnlitScrollingTexture") screens.Add(r);
        }
        // The window is the taller one (a big quad behind its opening).
        screens.Sort((a, b) => Mathf.Abs(b.transform.lossyScale.y).CompareTo(Mathf.Abs(a.transform.lossyScale.y)));
        if (screens.Count == 0) return;
        float stripHeight = StripOfSmaller * Mathf.Abs(screens[screens.Count - 1].transform.lossyScale.y);
        var block = new MaterialPropertyBlock();
        for (int i = 0; i < screens.Count; i++)
        {
            var r = screens[i];
            var m = r.sharedMaterial;
            string theme = "_" + season;
            var tex = i == 0 && season != Season.None && m.HasProperty(theme) ? m.GetTexture(theme) : m.GetTexture("_BaseMap");
            if (tex == null) continue;
            var scale = r.transform.lossyScale;                                  // (a built-in quad: 1 x 1 in its x/y)
            float width = Mathf.Abs(scale.x), height = Mathf.Max(0.01f, Mathf.Abs(scale.y));
            float stripWidth = stripHeight * tex.width / tex.height;
            float rows = height / stripHeight;
            r.GetPropertyBlock(block);
            block.SetTexture("_BaseMap", tex);
            block.SetVector("_BaseMap_ST", new Vector4(width / stripWidth, rows, 0f, 0.5f - rows * 0.5f));
            block.SetFloat("_ScrollSpeed", StripPace * stripHeight / stripWidth);
            r.SetPropertyBlock(block);
        }
    }

    // The giant burger (on its stand) and the giant tomato stand on walkable floor: the navmesh was baked without
    // them, so steering with the keys or the stick walked the chef right into them (user, 2026-10-28; a tap never
    // aims there). Each gets a carving obstacle as wide as its foot - whatever a theme puts in its place is as
    // wide. (The navmesh itself is never rebaked: see CafeGrowthBuilder.)
    private static void Solid(GameObject level, string name, float share)
    {
        var t = level.transform.Find("Decorations/" + name);
        var r = t != null ? t.GetComponentInChildren<Renderer>() : null;
        if (r == null || level.transform.Find("Solid " + name) != null) return;
        var b = r.bounds;
        var go = new GameObject("Solid " + name);
        go.transform.SetParent(level.transform, false);
        go.transform.SetPositionAndRotation(b.center, Quaternion.identity);
        go.transform.localScale = Vector3.one;
        var obstacle = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule;
        obstacle.radius = Mathf.Min(b.size.x, b.size.z) * 0.5f * share / Mathf.Max(0.01f, go.transform.lossyScale.x);
        obstacle.height = b.size.y / Mathf.Max(0.01f, go.transform.lossyScale.y);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
    }

    private static Renderer Prop(GameObject level, string name)
    {
        var t = level.transform.Find("Decorations/" + name);
        var r = t != null ? t.GetComponentInChildren<Renderer>() : null;
        return r != null && r.enabled && r.gameObject.activeInHierarchy ? r : null;
    }

    private static float Radius(Renderer prop) => Mathf.Min(prop.bounds.size.x, prop.bounds.size.z) * 0.5f;

    private static Vector3 Foot(Renderer prop, float y) => new(prop.bounds.center.x, y, prop.bounds.center.z);

    // Where a prop's shadow lies on the floor (y 1.2) relative to its foot: under its middle, along the sun.
    private static Vector3 ShadowOffset(Renderer prop)
    {
        Light sun = null;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.isActiveAndEnabled) { sun = l; break; }
        if (sun == null) return Vector3.zero;
        var d = sun.transform.forward;
        if (d.y > -0.05f) return Vector3.zero;
        var flat = new Vector3(d.x, 0f, d.z);
        return flat * ((prop.bounds.center.y - 1.2f) / -d.y);
    }

    // A spot on the giant burger's round stand (y 1.97, nobody walks up there), `degrees` round from the side that
    // faces the camera, `rim` of the way out to the stand's edge.
    private static Vector3 StandSpot(Renderer burger, float degrees, float rim = 0.9f)
    {
        var b = burger.bounds;
        var dir = Quaternion.Euler(0f, degrees, 0f) * ToViewer();
        float r = Mathf.Min(b.size.x, b.size.z) * 0.5f * rim;
        return new Vector3(b.center.x, 1.97f, b.center.z) + dir * r;
    }

    private static Vector3 ToViewer() => Vector3.ProjectOnPlane(-Camera.main.transform.forward, Vector3.up).normalized;

    private static void Hide(Renderer prop)
    {
        foreach (var r in prop.transform.GetComponentsInChildren<Renderer>())
            if (r.enabled) { r.enabled = false; hiddenProps.Add(r); }
    }

    private static void Place(GameObject go, Vector3 at, float yaw, bool blob = true)
    {
        go.transform.SetParent(root.transform, false);
        go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
        if (blob)
        {
            // A soft shadow on the ground under it, as wide as it is.
            var renderers = go.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var x in renderers) b.Encapsulate(x.bounds);
                var shadow = Kit.Blob(Mathf.Max(b.extents.x, b.extents.z) * 1.1f);
                if (shadow != null)
                {
                    shadow.transform.SetParent(go.transform, true);
                    shadow.transform.position = new Vector3(b.center.x, at.y + 0.02f, b.center.z);
                }
            }
        }
        // Flat glowing pieces (faces, stars) look at the camera, whichever way the rest is turned.
        var turn = Quaternion.LookRotation(-ToViewer());
        foreach (var part in go.GetComponentsInChildren<Transform>())
            if (part.name == "Face" || part.name == "Star") part.rotation = turn;
    }

    private static Material FindLitMaterial(GameObject level)
    {
        foreach (var r in level.GetComponentsInChildren<MeshRenderer>(true))
        {
            var m = r.sharedMaterial;
            if (m != null && m.HasProperty("_BaseColor") && m.HasProperty("_BaseMap")) return m;
        }
        return null;
    }

    // --- what ------------------------------------------------------------------------------------

    private sealed class Kit
    {
        private static readonly Dictionary<Color, Material> tints = new();
        private static Material lit, glow, particles, blob;
        private static Texture2D flake, heart, soft;

        // A soft, round, flat shadow of radius r lying on the ground (y up), or null before the materials exist.
        public static GameObject Blob(float r)
        {
            if (glow == null) return null;
            if (blob == null)
            {
                soft ??= Paint(32, (x, y) => Mathf.SmoothStep(1f, 0f, Mathf.Sqrt(x * x + y * y)));
                blob = new Material(glow) { name = "Decor Blob Shadow", mainTexture = soft };
            }
            var go = new GameObject("Blob shadow");
            var m = new Mesh { name = "Blob" };
            m.SetVertices(new[] { new Vector3(-r, 0f, -r), new Vector3(-r, 0f, r), new Vector3(r, 0f, r), new Vector3(r, 0f, -r) });
            m.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
            var shade = new Color(0.16f, 0.08f, 0.2f, 0.42f);
            m.SetColors(new[] { shade, shade, shade, shade });
            m.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            m.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = blob;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }

        // Now and then a star flares: a four-point glint swells and fades in a second (a few a minute).
        private void Glints(Transform sky, float w, float h)
        {
            var go = new GameObject("Glints");
            go.transform.SetParent(sky, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.2f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            main.startColor = new Color(1f, 0.96f, 0.85f, 1f);
            main.maxParticles = 6;
            var emission = ps.emission;
            emission.rateOverTime = 0.35f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(w * 2f, h * 2f, 0.01f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = 0.6f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = new Material(particles) { mainTexture = Glint(), name = "Glints" };
            r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
        }

        private static Texture2D glint;
        // A four-point star: two thin crossed rays and a small soft core.
        private static Texture2D Glint() => glint != null ? glint : glint = Paint(64, (x, y) =>
        {
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
            float rays = Mathf.Max(Mathf.Clamp01(1f - ax * 9f) * (1f - ay), Mathf.Clamp01(1f - ay * 9f) * (1f - ax));
            float core = Mathf.Clamp01(1f - Mathf.Sqrt(x * x + y * y) * 3.5f);
            return Mathf.Clamp01(rays * rays + core);
        });

        // The Halloween sky: twinkling stars and a crescent moon, far behind everything (only the sky shows them),
        // fixed to the camera like a backdrop.
        public GameObject NightSky(Camera camera)
        {
            var go = new GameObject("Night sky");
            go.transform.SetParent(camera.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, camera.farClipPlane * 0.92f);
            float h = camera.orthographicSize, w = h * Mathf.Max(1f, camera.aspect);
            if (particles != null)
            {
                var ps = go.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = true;
                main.prewarm = true;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 7f);
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f), new Color(1f, 0.92f, 0.7f));
                main.maxParticles = 220;
                var emission = ps.emission;
                emission.rateOverTime = 40f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(w * 2.2f, h * 2.2f, 0.01f);
                var twinkle = ps.colorOverLifetime;
                twinkle.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0.5f, 0.55f),
                                  new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
                twinkle.color = g;
                var r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = new Material(particles) { mainTexture = Flake(), name = "Stars" };
                r.shadowCastingMode = ShadowCastingMode.Off;
                ps.Play();
                Glints(go.transform, w, h);
            }
            if (glow != null)
            {
                var moon = new GameObject("Moon");
                moon.transform.SetParent(go.transform, false);
                moon.transform.localPosition = new Vector3(w * 0.82f, h * 0.55f, -0.5f);   // (top right: open sky in the whole-cafe picture)
                moon.AddComponent<MeshFilter>().sharedMesh = Mesher.Crescent(h * 0.09f, new Color(1f, 0.95f, 0.75f));
                var mr = moon.AddComponent<MeshRenderer>();
                mr.sharedMaterial = glow;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
            return go;
        }
        private readonly Camera cam;
        private readonly Vector3 toViewer;

        public Kit(Material litMaterial, Camera camera)
        {
            if (lit != litMaterial) { lit = litMaterial; tints.Clear(); }
            cam = camera;
            toViewer = Vector3.ProjectOnPlane(-cam.transform.forward, Vector3.up).normalized;
            if (glow == null)
            {
                var ff = Object.FindFirstObjectByType<FoodFight>();
                var splat = ff != null ? ff.SplatMaterial : null;
                if (splat != null)
                {
                    glow = new Material(splat) { name = "Decor Glow", mainTexture = Texture2D.whiteTexture };
                    if (glow.HasProperty("_TintColor")) glow.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    particles = new Material(glow) { name = "Decor Particles" };
                }
            }
        }

        private static Material Tint(Color c)
        {
            if (tints.TryGetValue(c, out var m) && m != null) return m;
            m = new Material(lit) { name = "Decor " + ColorUtility.ToHtmlStringRGB(c) };
            m.SetTexture("_BaseMap", null);
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return tints[c] = m;
        }

        private static readonly Color Orange = new(0.96f, 0.5f, 0.15f), Stem = new(0.36f, 0.42f, 0.17f),
            Pine = new(0.16f, 0.5f, 0.34f), PineLight = new(0.24f, 0.62f, 0.4f), Bark = new(0.45f, 0.29f, 0.18f),
            Flame = new(1f, 0.8f, 0.3f), SnowWhite = new(0.96f, 0.97f, 1f), Coal = new(0.13f, 0.13f, 0.17f),
            Sheet = new(0.97f, 0.96f, 1f), Ink = UITokens.Colors.Ink, Cream = UITokens.Colors.WarmWhite,
            BunBrown = new(0.86f, 0.55f, 0.27f), Patty = new(0.42f, 0.24f, 0.16f), Leaf = new(0.45f, 0.72f, 0.28f),
            Cheese = UITokens.Colors.Yellow, Gold = new(0.98f, 0.78f, 0.3f), StandWhite = new(0.95f, 0.93f, 0.98f);

        private static GameObject Part(GameObject parent, string name, Mesh mesh, Material material, Vector3 at = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = at;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = ShadowCastingMode.Off;   // (a blob shadow under the whole thing instead: Place)
            return go;
        }

        // ---- Halloween ----

        public GameObject Pumpkin(float radius, bool face)
        {
            var go = new GameObject(face ? "Jack-o'-lantern" : "Pumpkin");
            Part(go, "Body", Mesher.Pumpkin(radius), Tint(Orange));
            var stem = Part(go, "Stem", Mesher.Cylinder(radius * 0.12f, radius * 0.08f, radius * 0.4f, 6), Tint(Stem), new Vector3(0f, radius * 1.32f, 0f));
            stem.transform.localRotation = Quaternion.Euler(12f, 0f, 8f);
            if (face && glow != null) Part(go, "Face", Mesher.LanternFace(radius, Flame), glow, Vector3.up * radius * 0.92f);
            return go;
        }

        private static readonly Color[] Sheets =
            { new(0.97f, 0.96f, 1f), new(0.88f, 0.84f, 1f), new(0.84f, 0.97f, 0.92f), new(1f, 0.9f, 0.84f) };

        // A sheet ghost `height` tall; `face` 0 is the classic one (the big ghost), 1.. the customers' painted ones.
        // Sweets spilt at the big jack-o'-lantern's foot, big and small, covering the floor where the tomato's shadow is
        // baked (`shadow`: from the foot to the middle of that shadow) and round the lantern's front. Each one a sweet
        // anyone knows at a glance (user: "a heap of lumps - make them read as sweets, like the first ones"): wrapped
        // bonbons with their twisted ends, swirled lollipops, peppermints, candy corn. Merged into one mesh per colour.
        public GameObject Candy(float footprint, Vector3 shadow)
        {
            var go = new GameObject("Candy");
            var rng = new System.Random(3);
            float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var colors = new[] { new Color(0.62f, 0.32f, 0.9f), new Color(0.4f, 0.78f, 0.35f), UITokens.Colors.Yellow, Orange,
                                 UITokens.Colors.Raspberry, new Color(0.35f, 0.72f, 0.95f) };
            var along = shadow.sqrMagnitude > 1e-4f ? shadow.normalized : toViewer;
            var across = Vector3.Cross(Vector3.up, along);
            float a = footprint * 1.25f + shadow.magnitude * 0.35f, b = footprint * 1.12f;   // the shadow's ellipse
            bool Covered(Vector3 q)
            {
                var rel = q - shadow;
                float u = Vector3.Dot(rel, along) / a, v = Vector3.Dot(rel, across) / b;
                float fromFoot = q.magnitude;
                bool inFront = fromFoot < footprint * 1.3f && Vector3.Dot(q, toViewer) > -footprint * 0.2f;
                return (u * u + v * v <= 1f || inFront) && fromFoot > footprint * 0.6f;
            }
            const float step = 0.34f;
            float reach = a + shadow.magnitude + footprint * 1.4f;
            int n = 0;
            for (float x = -reach; x <= reach; x += step)
                for (float z = -reach; z <= reach; z += step * 0.87f)
                {
                    var q = new Vector3(x + (Mathf.Round(z / (step * 0.87f)) % 2 == 0 ? 0f : step * 0.5f) + Rand(-0.1f, 0.1f), 0f, z + Rand(-0.1f, 0.1f));
                    if (!Covered(q)) continue;
                    float size = Rand(0.75f, 1.55f), yaw = Rand(0f, 360f);
                    var c = colors[rng.Next(colors.Length)];
                    var piece = new GameObject("Sweet");
                    piece.transform.SetParent(go.transform, false);
                    piece.transform.localPosition = q;
                    piece.transform.localRotation = Quaternion.Euler(Rand(-8f, 8f), yaw, Rand(-8f, 8f));
                    piece.transform.localScale = Vector3.one * size;
                    switch (n++ % 6)
                    {
                        case 0: case 3: Bonbon(piece, c); break;
                        case 1: Lollipop(piece, c); break;
                        case 2: case 5: Peppermint(piece, n % 2 == 0 ? UITokens.Colors.Tomato : new Color(0.35f, 0.75f, 0.45f)); break;
                        default: CandyCorn(piece); break;
                    }
                }
            Merge(go);
            return go;
        }

        // A wrapped sweet lying on its side: a round body and its wrapper twisted into a fan at each end.
        private static void Bonbon(GameObject at, Color color)
        {
            var body = Part(at, "Body", Mesher.Ball(0.13f, 10, 6), Tint(color), Vector3.up * 0.1f);
            body.transform.localScale = new Vector3(1.35f, 0.8f, 0.95f);
            var wrapper = Tint(Color.Lerp(color, Color.white, 0.45f));
            foreach (float s in new[] { -1f, 1f })
            {
                var fan = Part(at, "Wrapper", Mesher.Fan(0.2f, 0.11f), wrapper, new Vector3(s * 0.15f, 0.1f, 0f));
                fan.transform.localRotation = Quaternion.Euler(0f, s > 0 ? 0f : 180f, 0f);
            }
        }

        // A round lollipop lying flat: rings of colour and white (a swirl, from above) and its stick.
        private static void Lollipop(GameObject at, Color color)
        {
            float r = 0.2f;
            Part(at, "Candy", Mesher.Cylinder(r, r, 0.06f, 16), Tint(color));
            Part(at, "Ring", Mesher.Cylinder(r * 0.74f, r * 0.74f, 0.064f, 16), Tint(UITokens.Colors.WarmWhite));
            Part(at, "Ring", Mesher.Cylinder(r * 0.5f, r * 0.5f, 0.068f, 16), Tint(color));
            Part(at, "Ring", Mesher.Cylinder(r * 0.22f, r * 0.22f, 0.072f, 12), Tint(UITokens.Colors.WarmWhite));
            var stick = Part(at, "Stick", Mesher.Cylinder(0.022f, 0.022f, 0.34f, 6), Tint(UITokens.Colors.WarmWhite), new Vector3(r * 0.95f, 0.03f, 0f));
            stick.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        }

        // A round peppermint: a flat disc in alternate wedges of colour and white.
        private static void Peppermint(GameObject at, Color color)
        {
            Part(at, "White", Mesher.Sectors(0.16f, 0.07f, 8, 0), Tint(UITokens.Colors.WarmWhite));
            Part(at, "Stripes", Mesher.Sectors(0.16f, 0.07f, 8, 1), Tint(color));
        }

        private static void CandyCorn(GameObject at)
        {
            var corn = new GameObject("Candy corn");
            corn.transform.SetParent(at.transform, false);
            corn.transform.localPosition = Vector3.up * 0.09f;
            corn.transform.localRotation = Quaternion.Euler(80f, 0f, 0f);
            corn.transform.localScale = Vector3.one * 1.6f;
            var bands = new[] { (0f, 0.42f, new Color(1f, 0.82f, 0.25f)), (0.42f, 0.78f, new Color(1f, 0.52f, 0.14f)), (0.78f, 1f, new Color(1f, 0.98f, 0.94f)) };
            foreach (var (from, to, color) in bands) Part(corn, "Band", Mesher.CornBand(0.2f, from, to), Tint(color));
        }

        // Everything under `go` drawn with the same material becomes one mesh (fewer draws).
        private static void Merge(GameObject go)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var parts = go.GetComponentsInChildren<MeshFilter>();
            var toLocal = go.transform.worldToLocalMatrix;
            foreach (var mf in parts)
            {
                var mat = mf.GetComponent<MeshRenderer>().sharedMaterial;
                if (!groups.TryGetValue(mat, out var list)) groups[mat] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mf.sharedMesh, transform = toLocal * mf.transform.localToWorldMatrix });
            }
            foreach (Transform child in go.transform) Object.Destroy(child.gameObject);
            foreach (var pair in groups)
            {
                var mesh = new Mesh { name = "Candy " + pair.Key.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(pair.Value.ToArray(), true, true);
                var part = new GameObject("Candy (" + pair.Key.name + ")");
                part.transform.SetParent(go.transform, false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = part.AddComponent<MeshRenderer>();
                mr.sharedMaterial = pair.Key;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }
            foreach (var mf in parts) Object.Destroy(mf.sharedMesh);   // (each piece had a mesh of its own)
        }

        public (Transform body, Transform face) GhostParts(float height, int face = 0, int sheet = 0)
        {
            var body = new GameObject("Ghost").transform;
            body.gameObject.AddComponent<MeshFilter>().sharedMesh = Mesher.Ghost();
            var bodyRenderer = body.gameObject.AddComponent<MeshRenderer>();
            bodyRenderer.sharedMaterial = Tint(Sheets[sheet % Sheets.Length]);
            bodyRenderer.shadowCastingMode = ShadowCastingMode.Off;
            body.localScale = Vector3.one * (height / 1.4f);
            var faceT = new GameObject("Ghost face").transform;
            if (glow != null)
            {
                faceT.gameObject.AddComponent<MeshFilter>().sharedMesh = Mesher.GhostFace((face == 0 ? 0.5f : 0.62f) * height / 1.4f, face);   // (the customers' faces a little bigger: they are smaller ghosts)
                var fr = faceT.gameObject.AddComponent<MeshRenderer>();
                fr.sharedMaterial = glow;
                fr.shadowCastingMode = ShadowCastingMode.Off;
            }
            return (body, faceT);
        }

        // The big ghost floating by the burger: a little see-through, like a real one (its sheet shaded by hand -
        // the see-through material has no lighting - and drawn without depth, so it glows over what is behind it).
        public GameObject BigGhost(float height)
        {
            var go = new GameObject("Big ghost");
            var g = go.AddComponent<SeasonalGhost>();
            var (body, face) = GhostParts(height);
            if (glow != null)
            {
                var mf = body.GetComponent<MeshFilter>();
                mf.sharedMesh = Mesher.Shaded(mf.sharedMesh, new Color(0.96f, 0.95f, 1f), 0.72f);
                body.GetComponent<MeshRenderer>().sharedMaterial = glow;
            }
            body.SetParent(go.transform, false);
            body.localPosition = Vector3.up * 0.35f;
            face.SetParent(go.transform, false);
            g.body = body;
            g.face = face;
            g.bob = 0.18f;
            g.speed = 1.3f;
            return go;
        }

        // ---- Winter ----

        // Snow lying on a prop: little flattened lumps wherever its surface faces up (found by dropping rays on it).
        // A build can't drop rays on a statically batched prop (its combined mesh isn't readable there), so the
        // editor saves what it made to Resources/Seasonal (a few KB each, in world space) and the game loads that.
        public void SnowOn(Renderer prop, Transform parent)
        {
            var baked = Resources.Load<Mesh>("Seasonal/snow_" + prop.name);
            if (baked != null)
            {
                var lying = Part(new GameObject("Snow on " + prop.name), "Snow", baked, Tint(SnowWhite));
                lying.transform.parent.SetParent(parent, false);
                return;
            }
            var mf = prop.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || !Application.isEditor) return;
            // A statically batched prop draws a mesh combined with its neighbours, already in world space: the
            // surface to drop rays on then sits at the origin (and only hits inside the prop's bounds count).
            bool batched = prop.isPartOfStaticBatch;
            var probe = batched ? new GameObject("Snow probe") : prop.gameObject;
            var mc = probe.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
            Physics.SyncTransforms();
            var b = prop.bounds;
            var go = new GameObject("Snow on " + prop.name);
            go.transform.SetParent(parent, false);
            var rng = new System.Random(prop.name.Length * 31);
            const float step = 0.16f;
            for (float x = b.min.x; x <= b.max.x; x += step)
                for (float z = b.min.z; z <= b.max.z; z += step)
                {
                    var from = new Vector3(x + (float)rng.NextDouble() * 0.1f, b.max.y + 1f, z + (float)rng.NextDouble() * 0.1f);
                    if (!mc.Raycast(new Ray(from, Vector3.down), out var hit, b.size.y + 2f)) continue;
                    // a dusting: thin caps only where the prop faces nearly straight up (dots spread over its sides read
                    // as polka dots, not snow), never on the floor or a counter beside it
                    if (hit.normal.y < 0.86f || hit.point.y < b.min.y + b.size.y * 0.3f) continue;
                    if (!b.Contains(hit.point)) continue;                                  // (a neighbour in the same batch)
                    float size = 0.08f + (float)rng.NextDouble() * 0.04f;
                    var lump = Part(go, "Snow", Mesher.Ball(size, 6, 3), Tint(SnowWhite));   // (tiny: few faces)
                    lump.transform.position = hit.point + hit.normal * size * 0.15f;
                    lump.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    lump.transform.localScale = new Vector3(1.3f, 0.35f, 1.3f);
                }
            if (batched) Object.Destroy(probe); else Object.Destroy(mc);
            Merge(go);
#if UNITY_EDITOR
            MeshFilter made = null;   // (the merged one: the lumps are only destroyed at the end of the frame)
            foreach (var f in go.GetComponentsInChildren<MeshFilter>()) if (f.name.StartsWith("Candy")) made = f;
            if (made != null)
            {
                System.IO.Directory.CreateDirectory("Assets/Resources/Seasonal");
                var keep = Object.Instantiate(made.sharedMesh);
                UnityEditor.MeshUtility.SetMeshCompression(keep, UnityEditor.ModelImporterMeshCompression.High);
                UnityEditor.AssetDatabase.CreateAsset(keep, "Assets/Resources/Seasonal/snow_" + prop.name + ".asset");
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif
        }

        public GameObject Stand(float radius, float height)
        {
            var go = new GameObject("Stand");
            Part(go, "Stand", Mesher.Cylinder(radius, radius, height, 12), Tint(StandWhite));
            Part(go, "Skirt", Mesher.Cylinder(radius * 0.72f, radius * 0.72f, 0.06f, 12), Tint(UITokens.Colors.Raspberry), Vector3.up * height);
            return go;
        }

        public GameObject Tree(float height, float radius)
        {
            var go = new GameObject("Christmas tree");
            Part(go, "Trunk", Mesher.Cylinder(radius * 0.16f, radius * 0.13f, height * 0.16f, 7), Tint(Bark));
            float[] at = { 0.1f, 0.3f, 0.5f, 0.68f }, rad = { 1f, 0.8f, 0.6f, 0.4f }, tall = { 0.34f, 0.32f, 0.28f, 0.26f };
            for (int i = 0; i < at.Length; i++)
            {
                var tier = Part(go, "Tier", Mesher.Cone(radius * rad[i], height * tall[i], 9), Tint(i % 2 == 0 ? Pine : PineLight), Vector3.up * height * at[i]);
                tier.transform.localRotation = Quaternion.Euler(0f, i * 20f, 0f);
            }
            // Ornaments round each tier's rim: little burgers and baubles in turn.
            var bauble = new[] { UITokens.Colors.Raspberry, Gold, new Color(0.4f, 0.6f, 0.98f) };
            int n = 0;
            for (int i = 0; i < at.Length; i++)
            {
                int count = 6 - i;
                for (int k = 0; k < count; k++)
                {
                    float a = (k / (float)count) * Mathf.PI * 2f + i * 0.9f;
                    var pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * rad[i] * 0.88f + Vector3.up * (height * at[i] + 0.1f);
                    if (n++ % 2 == 0) MiniBurger(go, pos, radius * 0.24f);
                    else Part(go, "Bauble", Mesher.Ball(radius * 0.1f, 8, 5), Tint(bauble[n % bauble.Length]), pos);
                }
            }
            if (glow != null) Part(go, "Star", Mesher.Star(radius * 0.32f, Flame), glow, Vector3.up * height * 0.97f);
            return go;
        }

        // A tiny burger ornament: bun, patty, cheese, lettuce, bun.
        private static void MiniBurger(GameObject tree, Vector3 at, float size)
        {
            var go = new GameObject("Burger ornament");
            go.transform.SetParent(tree.transform, false);
            go.transform.localPosition = at;
            float h = size * 0.16f, y = 0f;
            void Layer(Color c, float r, float t) { Part(go, "Layer", Mesher.Cylinder(r, r, t, 8), Tint(c), Vector3.up * y); y += t; }
            Layer(BunBrown, size * 0.42f, h);
            Layer(Patty, size * 0.46f, h * 0.9f);
            Layer(Cheese, size * 0.5f, h * 0.35f);
            Layer(Leaf, size * 0.5f, h * 0.35f);
            Part(go, "Top", Mesher.Dome(size * 0.44f, h * 2.2f), Tint(BunBrown), Vector3.up * y);
        }

        public GameObject Present(float size, Color box, Color ribbon)
        {
            var go = new GameObject("Present");
            var at = new Vector3(0f, size * 0.4f, 0f);
            Part(go, "Box", Mesher.Box(new Vector3(size, size * 0.8f, size)), Tint(box), at);
            Part(go, "Ribbon", Mesher.Box(new Vector3(size * 1.02f, size * 0.82f, size * 0.18f)), Tint(ribbon), at);
            Part(go, "Ribbon", Mesher.Box(new Vector3(size * 0.18f, size * 0.82f, size * 1.02f)), Tint(ribbon), at);
            Part(go, "Bow", Mesher.Ball(size * 0.14f, 6, 4), Tint(ribbon), new Vector3(0f, size * 0.84f, 0f));
            return go;
        }

        public GameObject Snowman(float footprint)
        {
            var go = new GameObject("Snowman");
            // Snow lying on the ground round him, low and uneven like terrain, as wide as the tomato he stands for
            // (it covers the tomato's baked shadow), and a few little clumps apart from it. He stands on the floor.
            Part(go, "Snow", Mesher.Drift(footprint * 1.05f, footprint * 0.13f, 11), Tint(SnowWhite));
            float[] clumpAngle = { -70f, -20f, 35f, 80f, 140f };
            for (int i = 0; i < clumpAngle.Length; i++)
            {
                var dir = Quaternion.Euler(0f, clumpAngle[i], 0f) * toViewer;
                float size = footprint * (0.1f + 0.05f * (i % 3));
                Part(go, "Clump", Mesher.Drift(size, size * 0.45f, 20 + i), Tint(SnowWhite), dir * footprint * (1.15f + 0.12f * (i % 2)));
            }
            // He stands at the camera's side of the snow: lower on the screen, so his head clears the HUD's cards.
            var ground = go;
            go = new GameObject("Snowman body");
            go.transform.SetParent(ground.transform, false);
            go.transform.localPosition = toViewer * footprint * 0.5f;
            float r0 = footprint * 0.56f, r1 = r0 * 0.72f, r2 = r0 * 0.52f;
            float y0 = r0 * 0.85f, y1 = y0 + r0 * 0.9f + r1 * 0.55f, y2 = y1 + r1 * 0.85f + r2 * 0.6f;
            Part(go, "Base", Mesher.Ball(r0, 12, 8), Tint(SnowWhite), Vector3.up * y0);
            Part(go, "Middle", Mesher.Ball(r1, 12, 8), Tint(SnowWhite), Vector3.up * y1);
            Part(go, "Head", Mesher.Ball(r2, 12, 8), Tint(SnowWhite), Vector3.up * y2);
            var front = toViewer;
            var side = Vector3.Cross(Vector3.up, front);
            // Coal eyes and buttons, a carrot nose, all on the side the camera sees.
            foreach (float s in new[] { -1f, 1f })
                Part(go, "Eye", Mesher.Ball(r2 * 0.12f, 6, 4), Tint(Coal), Vector3.up * (y2 + r2 * 0.25f) + front * r2 * 0.88f + side * s * r2 * 0.35f);
            for (int i = 0; i < 3; i++)
                Part(go, "Button", Mesher.Ball(r1 * 0.09f, 6, 4), Tint(Coal), Vector3.up * (y1 + r1 * (0.45f - i * 0.4f)) + front * r1 * 0.95f);
            var nose = Part(go, "Nose", Mesher.Cone(r2 * 0.14f, r2 * 0.75f, 6), Tint(Orange), Vector3.up * y2 + front * r2 * 0.85f);
            nose.transform.rotation = Quaternion.FromToRotation(Vector3.up, (front + Vector3.down * 0.15f).normalized);
            // A scarf and a top hat.
            Part(go, "Scarf", Mesher.Cylinder(r2 * 0.95f, r2 * 0.9f, r2 * 0.28f, 12), Tint(UITokens.Colors.Raspberry), Vector3.up * (y2 - r2 * 0.85f));
            var tail = Part(go, "Scarf tail", Mesher.Box(new Vector3(r2 * 0.3f, r2 * 0.9f, r2 * 0.08f)), Tint(UITokens.Colors.Raspberry),
                Vector3.up * (y2 - r2 * 1.15f) + front * r2 * 0.78f + side * r2 * 0.35f);
            tail.transform.rotation = Quaternion.LookRotation(front) * Quaternion.Euler(0f, 0f, 12f);
            Part(go, "Brim", Mesher.Cylinder(r2 * 0.95f, r2 * 0.95f, r2 * 0.08f, 12), Tint(Coal), Vector3.up * (y2 + r2 * 0.8f));
            Part(go, "Hat", Mesher.Cylinder(r2 * 0.62f, r2 * 0.62f, r2 * 0.85f, 12), Tint(Coal), Vector3.up * (y2 + r2 * 0.86f));
            Part(go, "Band", Mesher.Cylinder(r2 * 0.64f, r2 * 0.64f, r2 * 0.16f, 12), Tint(UITokens.Colors.Raspberry), Vector3.up * (y2 + r2 * 0.92f));
            // Stick arms.
            foreach (float s in new[] { -1f, 1f })
            {
                var arm = Part(go, "Arm", Mesher.Cylinder(r1 * 0.06f, r1 * 0.035f, r1 * 1.3f, 5), Tint(Bark), Vector3.up * (y1 + r1 * 0.3f) + side * s * r1 * 0.8f);
                arm.transform.rotation = Quaternion.FromToRotation(Vector3.up, (side * s + Vector3.up * 0.7f).normalized);
            }
            return ground;
        }

        // Snow drifting down the screen: flakes just in front of the camera, so nothing in the cafe hides them and
        // they fall the same wherever the camera is.
        public GameObject Snow(Camera camera) => Weather(camera, "Snow", Flake(), 20f, (0.18f, 0.36f), new Color(1f, 1f, 1f, 0.9f), -1f);

        // ---- Valentine ----

        // Little sparks drifting about in no hurry, orange and violet, flickering.
        public GameObject Sparks(Camera camera)
        {
            var go = Weather(camera, "Sparks", Flake(), 7f, (0.14f, 0.26f), new Color(1f, 0.7f, 0.3f, 1f), 0.15f);
            if (go == null) return null;
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.72f, 0.3f), new Color(0.78f, 0.55f, 1f));
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            var flicker = new AnimationCurve();
            for (int i = 0; i <= 12; i++) flicker.AddKey(i / 12f, i % 2 == 0 ? 0.6f : 1f);
            size.size = new ParticleSystem.MinMaxCurve(1f, flicker);
            var noise = ps.noise;
            noise.strength = 0.5f;
            noise.frequency = 0.4f;
            return go;
        }

        public GameObject Hearts(Camera camera) => Weather(camera, "Hearts", HeartTexture(), 3f, (0.3f, 0.5f), new Color(1f, 0.55f, 0.7f, 0.8f), 1f);

        // A puffy heart on a round pink cushion as wide as the tomato it stands for.
        public GameObject BigHeart(float size, float footprint)
        {
            var go = new GameObject("Big heart");
            Part(go, "Cushion", Mesher.Cylinder(footprint, footprint * 0.92f, size * 0.32f, 14), Tint(new Color(1f, 0.72f, 0.82f)));
            Part(go, "Trim", Mesher.Cylinder(footprint * 0.93f, footprint * 0.93f, size * 0.06f, 14), Tint(Cream), Vector3.up * size * 0.32f);
            var h = Part(go, "Heart", Mesher.Heart(size, size * 0.8f), Tint(UITokens.Colors.Raspberry), Vector3.up * size * 1.2f);
            h.transform.rotation = Quaternion.LookRotation(-toViewer);
            return go;
        }

        public GameObject Balloon(float stringLength, int i)
        {
            var go = new GameObject("Heart balloon");
            var colors = new[] { UITokens.Colors.Raspberry, new Color(1f, 0.6f, 0.75f), UITokens.Colors.Tomato };
            var line = Part(go, "String", Mesher.Cylinder(0.045f, 0.045f, stringLength, 5), Tint(Cream));
            var h = Part(go, "Heart", Mesher.Heart(0.55f, 0.25f), Tint(colors[i % colors.Length]), Vector3.up * (stringLength + 0.5f));
            // Their shadows on the burger look lovely (user): real ones, for these.
            line.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            h.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
            h.transform.rotation = Quaternion.LookRotation(-toViewer) * Quaternion.Euler(0f, 0f, (i - 1) * 10f);
            go.AddComponent<SeasonalGhost>().body = h.transform;   // (bobs gently)
            return go;
        }

        // ---- weather ----

        private GameObject Weather(Camera camera, string name, Texture2D texture, float rate, (float, float) size, Color color, float direction)
        {
            if (particles == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(camera.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, camera.nearClipPlane + 1.5f);
            go.transform.localRotation = Quaternion.identity;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 10f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size.Item1, size.Item2);
            main.startColor = color;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 0.6f);
            main.maxParticles = 200;
            main.prewarm = true;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            float h = camera.orthographicSize, w = h * Mathf.Max(1f, camera.aspect);
            shape.scale = new Vector3(w * 2.4f, h * 2.4f, 0.01f);
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            velocity.y = new ParticleSystem.MinMaxCurve(direction * 0.5f, direction * 0.95f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.25f;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = new Material(particles) { mainTexture = texture, name = name };
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return go;
        }

        private static Texture2D Flake() => flake != null ? flake : flake = Paint(32, (x, y) => 1f - Mathf.SmoothStep(0.6f, 1f, Mathf.Sqrt(x * x + y * y)));

        private static Texture2D HeartTexture() => heart != null ? heart : heart = Paint(64, (x, y) =>
        {
            y = y * 1.1f + 0.15f;   // the classic heart curve: inside where it is below zero
            float a = x * x + y * y - 0.42f;
            return a * a * a - x * x * y * y * y < 0f ? 1f : 0f;
        });

        private static Texture2D Paint(int n, System.Func<float, float, float> alpha)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f)) * 255f));
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }

    // --- meshes: flat-shaded (every triangle its own vertices), like the KayKit models ------------
    private static class Mesher
    {
        private sealed class Builder
        {
            public readonly List<Vector3> v = new();
            public readonly List<Color> c = new();
            public Color color = Color.white;
            public void Tri(Vector3 a, Vector3 b, Vector3 d) { v.Add(a); v.Add(b); v.Add(d); c.Add(color); c.Add(color); c.Add(color); }
            // Both ways round, each corner its own colour (soft, fading edges).
            public void Shaded(Vector3 a, Color ca, Vector3 b, Color cb, Vector3 d, Color cd)
            {
                v.Add(a); v.Add(b); v.Add(d); c.Add(ca); c.Add(cb); c.Add(cd);
                v.Add(a); v.Add(d); v.Add(b); c.Add(ca); c.Add(cd); c.Add(cb);
            }
            public void Both(Vector3 a, Vector3 b, Vector3 d) { Tri(a, b, d); Tri(a, d, b); }
            public void Quad(Vector3 a, Vector3 b, Vector3 d, Vector3 e) { Tri(a, b, d); Tri(a, d, e); }
            public Mesh Build(string name)
            {
                var m = new Mesh { name = name };
                m.SetVertices(v);
                m.SetColors(c);
                var idx = new int[v.Count];
                for (int i = 0; i < idx.Length; i++) idx[i] = i;
                m.SetTriangles(idx, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }

        // A copy of a flat-shaded mesh coloured `color` lit by hand from above and in front (for unlit materials),
        // with `alpha`.
        public static Mesh Shaded(Mesh source, Color color, float alpha)
        {
            var m = Object.Instantiate(source);
            var normals = m.normals;
            var colors = new Color[normals.Length];
            var light = new Vector3(-0.3f, 0.8f, -0.5f).normalized;
            for (int i = 0; i < normals.Length; i++)
            {
                float lit = 0.72f + 0.28f * Mathf.Max(0f, Vector3.Dot(normals[i], light));
                var c = color * lit;
                c.a = alpha;
                colors[i] = c;
            }
            m.colors = colors;
            return m;
        }

        // A surface of revolution, ring by ring: radius(u 0..1 bottom to top, angle) and height(u, angle).
        private static Mesh Lathe(string name, int lon, int lat, System.Func<float, float, float> radius, System.Func<float, float, float> height)
        {
            var p = new Vector3[lat + 1, lon];
            for (int i = 0; i <= lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    float u = i / (float)lat, th = Mathf.PI * 2f * j / lon;
                    float r = radius(u, th);
                    p[i, j] = new Vector3(Mathf.Cos(th) * r, height(u, th), Mathf.Sin(th) * r);
                }
            var b = new Builder();
            for (int i = 0; i < lat; i++)
                for (int j = 0; j < lon; j++)
                {
                    int k = (j + 1) % lon;
                    b.Quad(p[i, j], p[i + 1, j], p[i + 1, k], p[i, k]);
                }
            return b.Build(name);
        }

        // A ribbed, squashed sphere sitting on y = 0.
        public static Mesh Pumpkin(float r) => Lathe("Pumpkin", 16, 7,
            (u, th) => Mathf.Sin(Mathf.PI * u) * r * (Mathf.RoundToInt(th / (Mathf.PI / 8f)) % 2 == 0 ? 1f : 0.88f),
            (u, th) => (-Mathf.Cos(Mathf.PI * u) * 0.72f + 0.72f - (u >= 1f ? 0.12f : 0f)) * r);

        public static Mesh Ball(float r, int lon = 6, int lat = 4) => Lathe("Ball", lon, lat,
            (u, th) => Mathf.Sin(Mathf.PI * u) * r, (u, th) => -Mathf.Cos(Mathf.PI * u) * r);

        // Snow on the ground: a low, uneven patch on y = 0 - an irregular edge that thins to nothing, gentle humps
        // (seed: each one different).
        public static Mesh Drift(float r, float h, int seed)
        {
            float a = seed * 1.7f, c = seed * 0.9f;
            return Lathe("Snow", 22, 5,
                (u, th) => (1f - u) * r * (1f + 0.16f * Mathf.Sin(th * 3f + a) + 0.08f * Mathf.Sin(th * 7f + c)),
                (u, th) => h * Mathf.Pow(Mathf.Sin(Mathf.PI * 0.5f * u), 0.6f) * (1f + 0.35f * Mathf.Sin(th * 5f + a) * (1f - u)));
        }

        // A half ball on y = 0 (the top bun).
        public static Mesh Dome(float r, float h) => Lathe("Dome", 8, 3,
            (u, th) => Mathf.Cos(Mathf.PI * 0.5f * u) * r, (u, th) => Mathf.Sin(Mathf.PI * 0.5f * u) * h);

        // A sheet ghost, 1.4 tall: a round head over a body that flares to a wavy hem at y = 0 (open below).
        public static Mesh Ghost() => Lathe("Ghost", 14, 8,
            (u, th) => u <= 0.5f ? Mathf.Lerp(0.6f, 0.46f, u / 0.5f) : Mathf.Cos(Mathf.PI * 0.5f * (u - 0.5f) / 0.5f) * 0.46f,
            (u, th) => u <= 0.5f ? u / 0.5f * 0.85f + (u == 0f ? Mathf.Sin(th * 7f) * 0.08f : 0f)
                                 : 0.85f + Mathf.Sin(Mathf.PI * 0.5f * (u - 0.5f) / 0.5f) * 0.55f);

        public static Mesh Cylinder(float r0, float r1, float h, int sides)
        {
            var b = new Builder();
            for (int j = 0; j < sides; j++)
            {
                float a0 = Mathf.PI * 2f * j / sides, a1 = Mathf.PI * 2f * (j + 1) / sides;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)); var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                b.Quad(d0 * r0, d0 * r1 + Vector3.up * h, d1 * r1 + Vector3.up * h, d1 * r0);
                b.Tri(Vector3.up * h, d1 * r1 + Vector3.up * h, d0 * r1 + Vector3.up * h);
            }
            return b.Build("Cylinder");
        }

        // One band of a candy corn (a flattened cone `h` tall, from..to of its height).
        public static Mesh CornBand(float h, float from, float to)
        {
            var b = new Builder();
            float w = h * 0.55f;
            const int sides = 6;
            for (int j = 0; j < sides; j++)
            {
                float a0 = Mathf.PI * 2f * j / sides, a1 = Mathf.PI * 2f * (j + 1) / sides;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0) * 0.6f); var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1) * 0.6f);
                float r0 = w * (1f - from), r1 = w * (1f - to);
                b.Quad(d0 * r0 + Vector3.up * h * from, d0 * r1 + Vector3.up * h * to, d1 * r1 + Vector3.up * h * to, d1 * r0 + Vector3.up * h * from);
                if (from == 0f) b.Tri(Vector3.zero, d0 * r0, d1 * r0);
            }
            return b.Build("Candy corn band");
        }

        // A sweet's wrapper twisted into a fan: narrow where it meets the sweet (at x = 0), opening out to a crinkled
        // edge at x = length; flat, a little raised in the middle, both sides.
        public static Mesh Fan(float length, float width)
        {
            var b = new Builder();
            var neck = new Vector3(0f, 0f, 0f);
            const int folds = 5;
            for (int i = 0; i < folds; i++)
            {
                float z0 = Mathf.Lerp(-width, width, i / (float)folds), z1 = Mathf.Lerp(-width, width, (i + 1) / (float)folds);
                var p0 = new Vector3(length * (i % 2 == 0 ? 1f : 0.88f), (i % 2 == 0 ? 0.015f : -0.015f), z0);
                var p1 = new Vector3(length * ((i + 1) % 2 == 0 ? 1f : 0.88f), ((i + 1) % 2 == 0 ? 0.015f : -0.015f), z1);
                b.Both(neck, p0, p1);
            }
            return b.Build("Wrapper");
        }

        // Every other wedge of a flat disc (start 0 or 1): two of these in two colours make a peppermint.
        public static Mesh Sectors(float r, float h, int count, int start)
        {
            var b = new Builder();
            for (int j = start; j < count; j += 2)
            {
                float a0 = Mathf.PI * 2f * j / count, a1 = Mathf.PI * 2f * (j + 1) / count;
                const int sub = 3;
                for (int k = 0; k < sub; k++)
                {
                    float s0 = Mathf.Lerp(a0, a1, k / (float)sub), s1 = Mathf.Lerp(a0, a1, (k + 1) / (float)sub);
                    var d0 = new Vector3(Mathf.Cos(s0), 0f, Mathf.Sin(s0)) * r; var d1 = new Vector3(Mathf.Cos(s1), 0f, Mathf.Sin(s1)) * r;
                    var up = Vector3.up * h;
                    b.Tri(up, d1 + up, d0 + up);
                    b.Quad(d0, d0 + up, d1 + up, d1);
                }
            }
            return b.Build("Sectors");
        }

        public static Mesh Cone(float r, float h, int sides)
        {
            var b = new Builder();
            for (int j = 0; j < sides; j++)
            {
                float a0 = Mathf.PI * 2f * j / sides, a1 = Mathf.PI * 2f * (j + 1) / sides;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * r; var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * r;
                b.Tri(d0, Vector3.up * h, d1);
                b.Tri(Vector3.zero, d0, d1);
            }
            return b.Build("Cone");
        }

        public static Mesh Box(Vector3 size)
        {
            var h = size * 0.5f;
            var b = new Builder();
            Vector3 P(float x, float y, float z) => new(x * h.x, y * h.y, z * h.z);
            b.Quad(P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1));
            b.Quad(P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1));
            b.Quad(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1));
            b.Quad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));
            b.Quad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1));
            b.Quad(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1));
            return b.Build("Box");
        }

        // A heart, standing in the XY plane, extruded `depth` (both faces and the rim).
        public static Mesh Heart(float size, float depth)
        {
            const int n = 28;
            var rim = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.PI * 2f * i / n;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                rim[i] = new Vector3(x, y, 0f) * (size / 17f);
            }
            var b = new Builder();
            var front = Vector3.back * depth * 0.5f; var back = Vector3.forward * depth * 0.5f;
            var center = new Vector3(0f, size * 0.1f, 0f);
            for (int i = 0; i < n; i++)
            {
                var a = rim[i]; var c = rim[(i + 1) % n];
                b.Both(center + front, a + front, c + front);
                b.Both(center + back, a + back, c + back);
                b.Both(a + front, c + front, c + back);
                b.Both(a + front, c + back, a + back);
            }
            return b.Build("Heart");
        }

        // ---- flat pieces (stickers: in the XY plane, drawn both ways round) ----

        public static Mesh LanternFace(float r, Color color)
        {
            var b = new Builder { color = color };
            float z = -r * 1.04f;
            foreach (float side in new[] { -1f, 1f })
            {
                var e = new Vector3(side * r * 0.34f, r * 0.24f, z);
                b.Both(e + new Vector3(-r * 0.22f, -r * 0.13f, 0f), e + new Vector3(0f, r * 0.24f, 0f), e + new Vector3(r * 0.22f, -r * 0.13f, 0f));
            }
            const int steps = 10;
            for (int i = 0; i < steps; i++)
            {
                float u0 = Mathf.Lerp(-1f, 1f, i / (float)steps), u1 = Mathf.Lerp(-1f, 1f, (i + 1) / (float)steps);
                Vector3 Top(float u) => new(u * r * 0.5f, -r * 0.04f - r * 0.12f * (1f - u * u)
                                                   - (Mathf.Abs(Mathf.Abs(u) - 0.35f) < 0.12f ? r * 0.07f : 0f), z);
                Vector3 Low(float u) => new(u * r * 0.5f, -r * 0.04f - r * 0.3f * (1f - u * u), z);
                b.Both(Top(u0), Top(u1), Low(u1));
                b.Both(Top(u0), Low(u1), Low(u0));
            }
            return b.Build("Lantern face");
        }

        private static void Disc(Builder b, Vector2 at, float rx, float ry, Color color, float z = 0f)
        {
            b.color = color;
            const int n = 18;
            for (int i = 0; i < n; i++)
            {
                float a0 = Mathf.PI * 2f * i / n, a1 = Mathf.PI * 2f * (i + 1) / n;
                b.Both(new Vector3(at.x, at.y, z), new Vector3(at.x + Mathf.Cos(a0) * rx, at.y + Mathf.Sin(a0) * ry, z),
                       new Vector3(at.x + Mathf.Cos(a1) * rx, at.y + Mathf.Sin(a1) * ry, z));
            }
        }

        // A crescent moon, opening to the right: the band between the moon's left edge and a smaller circle's,
        // their ends meeting in two points.
        public static Mesh Crescent(float r, Color moon)
        {
            var b = new Builder { color = moon };
            const int n = 20;
            float from = 65f * Mathf.Deg2Rad, to = 295f * Mathf.Deg2Rad;
            var inner = new Vector2(r * 0.42f, 0f);
            float ir = r * 0.8f;
            Vector3 Outer(float t) { float a = Mathf.Lerp(from, to, t); return new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f); }
            Vector3 Inner(float t)
            {
                if (t <= 0f) return Outer(0f);
                if (t >= 1f) return Outer(1f);
                float a = Mathf.Lerp(from + 0.35f, to - 0.35f, t);
                return new Vector3(inner.x + Mathf.Cos(a) * ir, inner.y + Mathf.Sin(a) * ir, 0f);
            }
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n, t1 = (i + 1) / (float)n;
                b.Both(Outer(t0), Outer(t1), Inner(t1));
                b.Both(Outer(t0), Inner(t1), Inner(t0));
            }
            return b.Build("Crescent");
        }

        public const int GhostFaces = 7;   // 0 the classic + six emo looks

        private static readonly Color FaceInk = new(0.12f, 0.15f, 0.24f), Blush = new(1f, 0.62f, 0.72f),
            Paint = new(1f, 0.58f, 0.18f), Tongue = new(0.95f, 0.42f, 0.52f);

        private static void Line(Builder b, Vector2 a, Vector2 c, float width, Color color, float z = 0f)
        {
            b.color = color;
            var n = Vector2.Perpendicular((c - a).normalized) * width * 0.5f;
            Vector3 P(Vector2 v) => new(v.x, v.y, z);
            b.Both(P(a - n), P(a + n), P(c + n));
            b.Both(P(a - n), P(c + n), P(c - n));
        }

        private static void Triangle(Builder b, Vector2 a, Vector2 c, Vector2 d, Color color, float z = 0f)
        {
            b.color = color;
            b.Both(new Vector3(a.x, a.y, z), new Vector3(c.x, c.y, z), new Vector3(d.x, d.y, z));
        }

        // A soft blot: `color` in the middle fading to nothing at the edge (smudged make-up, blush).
        private static void Smudge(Builder b, Vector2 at, float rx, float ry, Color color, float z = 0f)
        {
            var clear = color; clear.a = 0f;
            const int n = 20;
            for (int i = 0; i < n; i++)
            {
                float a0 = Mathf.PI * 2f * i / n, a1 = Mathf.PI * 2f * (i + 1) / n;
                b.Shaded(new Vector3(at.x, at.y, z), color,
                         new Vector3(at.x + Mathf.Cos(a0) * rx, at.y + Mathf.Sin(a0) * ry, z), clear,
                         new Vector3(at.x + Mathf.Cos(a1) * rx, at.y + Mathf.Sin(a1) * ry, z), clear);
            }
        }

        // A run of make-up: a streak narrowing as it goes down, ending in a drop.
        private static void Drip(Builder b, Vector2 top, float length, float width, Color color, float z = 0f)
        {
            var end = top + Vector2.down * length;
            b.color = color;
            Vector3 P(Vector2 q) => new(q.x, q.y, z);
            b.Both(P(top + Vector2.left * width * 0.5f), P(top + Vector2.right * width * 0.5f), P(end + Vector2.right * width * 0.25f));
            b.Both(P(top + Vector2.left * width * 0.5f), P(end + Vector2.right * width * 0.25f), P(end + Vector2.left * width * 0.25f));
            Disc(b, end, width * 0.38f, width * 0.45f, color, z);
        }

        private readonly struct Emo
        {
            public readonly Color Shadow;   // the smudged eye make-up
            public readonly int Drips, Mouth, Fringe;   // runs of make-up; 0 pout, 1 flat, 2 "o", 3 a smile with a fang; fringe -1 left, 0 none, 1 right
            public readonly bool Blush;
            public Emo(Color shadow, int drips, int mouth, int fringe, bool blush) { Shadow = shadow; Drips = drips; Mouth = mouth; Fringe = fringe; Blush = blush; }
        }

        // Six looks: purple, violet, plum, magenta and smoky make-up. (A pointed fringe over one eye was tried and
        // taken off - user: "no sharp triangle on its head"; the code for it stays, unused.)
        private static readonly Emo[] Emos =
        {
            new(new Color(0.55f, 0.25f, 0.85f, 0.75f), 2, 0, 0, false),
            new(new Color(0.42f, 0.18f, 0.66f, 0.8f), 1, 1, 0, false),
            new(new Color(0.78f, 0.25f, 0.7f, 0.7f), 2, 2, 0, true),
            new(new Color(0.35f, 0.2f, 0.55f, 0.8f), 3, 3, 0, false),
            new(new Color(0.62f, 0.3f, 0.95f, 0.7f), 1, 0, 0, true),
            new(new Color(0.28f, 0.22f, 0.38f, 0.75f), 2, 1, 0, false),
        };

        // A ghost's face, r the head's size. 0: the classic (oval eyes, an "o"), the big ghost's. 1-6, the
        // customers' (user: "bigger black eyes, smudged purple make-up - an emo face"): big glossy black eyes set low,
        // a flick of liner, purple shadow smudged round them and running down, a small sulky mouth, now and then a
        // fringe swept over one eye or a little blush. Cute-creepy: kawaii proportions, inky black, pastel sheet.
        public static Mesh GhostFace(float r, int variant)
        {
            var b = new Builder();
            if (variant <= 0)
            {
                Disc(b, new Vector2(-r * 0.32f, r * 0.1f), r * 0.13f, r * 0.2f, FaceInk);
                Disc(b, new Vector2(r * 0.32f, r * 0.1f), r * 0.13f, r * 0.2f, FaceInk);
                Disc(b, new Vector2(0f, -r * 0.28f), r * 0.1f, r * 0.12f, FaceInk);
                return b.Build("Ghost face");
            }
            var look = Emos[(variant - 1) % Emos.Length];
            var black = new Color(0.03f, 0.02f, 0.05f, 1f);   // (true black: under the night grade the face ink read navy)
            float ex = r * 0.3f, ey = -r * 0.04f, erx = r * 0.17f, ery = r * 0.21f;
            // Layers back to front (each a hair nearer the camera): make-up, blush, eyes, liner, glints, mouth, fringe.
            foreach (float side in new[] { -1f, 1f })
            {
                var eye = new Vector2(side * ex, ey);
                Smudge(b, eye + new Vector2(side * r * 0.03f, -r * 0.03f), erx * 2.1f, ery * 1.85f, look.Shadow);
                Smudge(b, eye + new Vector2(side * r * 0.02f, -r * 0.05f), erx * 1.5f, ery * 1.35f, look.Shadow, -0.0005f);
                for (int k = 0; k < look.Drips; k++)
                {
                    float x = side * ex + (k - (look.Drips - 1) * 0.5f) * r * 0.09f + side * r * 0.03f;
                    float len = r * (0.16f + 0.1f * ((k + (side > 0 ? 1 : 0)) % 2));
                    Drip(b, new Vector2(x, ey - ery * 0.8f), len, r * 0.05f, look.Shadow, -0.001f);
                }
                if (look.Blush) Smudge(b, new Vector2(side * r * 0.5f, ey - ery * 1.3f), r * 0.13f, r * 0.07f, new Color(1f, 0.55f, 0.7f, 0.65f), -0.002f);
                Disc(b, eye, erx, ery, black, -0.003f);
                // The liner's flick at the outer corner.
                Triangle(b, eye + new Vector2(side * erx * 0.7f, ery * 0.45f), eye + new Vector2(side * erx * 1.45f, ery * 0.85f),
                         eye + new Vector2(side * erx * 0.95f, ery * 0.05f), black, -0.003f);
                Disc(b, eye + new Vector2(-erx * 0.3f, ery * 0.35f), erx * 0.3f, erx * 0.3f, Color.white, -0.004f);
                Disc(b, eye + new Vector2(erx * 0.25f, -ery * 0.3f), erx * 0.13f, erx * 0.13f, Color.white, -0.004f);
            }
            float my = ey - ery * 1.75f, w = r * 0.05f;
            switch (look.Mouth)
            {
                case 0:   // a pout
                    Line(b, new Vector2(-r * 0.08f, my - r * 0.02f), new Vector2(0f, my + r * 0.01f), w, black, -0.004f);
                    Line(b, new Vector2(0f, my + r * 0.01f), new Vector2(r * 0.08f, my - r * 0.02f), w, black, -0.004f);
                    break;
                case 1:   // flat
                    Line(b, new Vector2(-r * 0.07f, my), new Vector2(r * 0.07f, my - r * 0.01f), w, black, -0.004f);
                    break;
                case 2:   // a little "o"
                    Disc(b, new Vector2(0f, my), r * 0.045f, r * 0.055f, black, -0.004f);
                    break;
                default:  // a lopsided smile with one fang
                    Line(b, new Vector2(-r * 0.09f, my + r * 0.015f), new Vector2(0f, my - r * 0.02f), w, black, -0.004f);
                    Line(b, new Vector2(0f, my - r * 0.02f), new Vector2(r * 0.09f, my + r * 0.02f), w, black, -0.004f);
                    Triangle(b, new Vector2(r * 0.025f, my - r * 0.025f), new Vector2(r * 0.065f, my - r * 0.015f), new Vector2(r * 0.04f, my - r * 0.075f), Color.white, -0.005f);
                    break;
            }
            if (look.Fringe != 0)
            {
                // A side-swept fringe over one eye: pointed locks from the crown, the longest past the eye.
                float s = look.Fringe;
                var fringe = new Color(0.07f, 0.05f, 0.12f, 1f);
                var crownA = new Vector2(-s * r * 0.22f, r * 0.46f);
                var crownB = new Vector2(s * r * 0.5f, r * 0.34f);
                var locks = new[]
                {
                    (from: 0.0f, to: 0.35f, tip: new Vector2(s * (ex - erx * 0.5f), ey + ery * 0.55f)),
                    (from: 0.2f, to: 0.7f, tip: new Vector2(s * (ex + erx * 0.35f), ey - ery * 0.35f)),
                    (from: 0.55f, to: 1.0f, tip: new Vector2(s * (ex + erx * 1.35f), ey + ery * 0.15f)),
                };
                foreach (var (from, to, tip) in locks)
                    Triangle(b, Vector2.Lerp(crownA, crownB, from), Vector2.Lerp(crownA, crownB, to), tip, fringe, -0.006f);
                Triangle(b, crownA, crownB, Vector2.Lerp(crownA, crownB, 0.5f) + Vector2.down * r * 0.12f, fringe, -0.006f);
            }
            return b.Build("Ghost face");
        }

        public static Mesh Star(float r, Color color)
        {
            var b = new Builder { color = color };
            for (int i = 0; i < 10; i++)
            {
                float a0 = Mathf.PI * 0.5f + Mathf.PI * i / 5f, a1 = Mathf.PI * 0.5f + Mathf.PI * (i + 1) / 5f;
                float r0 = i % 2 == 0 ? r : r * 0.45f, r1 = i % 2 == 0 ? r * 0.45f : r;
                b.Both(Vector3.zero, new Vector3(Mathf.Cos(a0) * r0, Mathf.Sin(a0) * r0, 0f), new Vector3(Mathf.Cos(a1) * r1, Mathf.Sin(a1) * r1, 0f));
            }
            return b.Build("Star");
        }
    }
}
