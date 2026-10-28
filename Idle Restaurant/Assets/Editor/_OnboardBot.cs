using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

// TEMPORARY (2026-10-20): plays a brand-new player's first minutes in play mode and logs when things happen
// (lesson beats, first burger, first serve, first coins, end of the lesson, first $20 = the first shop buy), and
// measures how big every tappable thing is on a phone screen. Bridge: _OnboardBot Begin <reaction s> / Log /
// Sizes. Delete after use.
[InitializeOnLoad]
public static class _OnboardBot
{
    static readonly Stack<IEnumerator> stack = new();
    static float waitUntil, waitForTimeout, t0, delay = 0.8f;
    static Func<bool> waitFor;
    static readonly StringBuilder log = new();
    static string lastFocus, lastLine; static int lastCoins = -1; static bool lastRunning, afford20, served1;

    static _OnboardBot() => EditorApplication.update += Tick;

    public static string Begin(string reaction)
    {
        delay = float.Parse(reaction, System.Globalization.CultureInfo.InvariantCulture);
        stack.Clear(); log.Clear(); t0 = Time.time; waitFor = null; waitUntil = 0;
        lastFocus = lastLine = null; lastCoins = -1; lastRunning = false; afford20 = served1 = false;
        stack.Push(Run());
        return "on";
    }

    public static string Log() { File.WriteAllText("Temp/_onboard.txt", log.ToString()); return log.ToString(); }

    static void Note(string s) => log.AppendLine($"{Time.time - t0,6:0.0}s  {s}");

    static void Watch()
    {
        var focus = Tutorial.Focus != null ? Tutorial.Focus.name : null;
        if (focus != lastFocus) { Note($"arrow -> {focus ?? "none"}"); lastFocus = focus; }
        string line = CoachLine();
        if (line != lastLine) { Note($"card: {line}"); lastLine = line; }
        if (Tutorial.Running != lastRunning) { Note(Tutorial.Running ? "LESSON running" : "LESSON over"); lastRunning = Tutorial.Running; }
        int coins = CafeProgress.Coins;
        if (lastCoins >= 0 && coins != lastCoins) Note($"COINS {lastCoins} -> {coins}");
        lastCoins = coins;
        if (!afford20 && coins >= 20) { afford20 = true; Note("CAN BUY the first shop item ($20)"); }
    }

    static string CoachLine()
    {
        var card = UnityEngine.Object.FindFirstObjectByType<UICoachCard>();
        if (card == null || !card.isActiveAndEnabled) return null;
        var text = card.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => t.text).FirstOrDefault(s => !string.IsNullOrEmpty(s));
        var group = card.GetComponent<CanvasGroup>();
        return group != null && group.alpha < 0.05f ? null : text;
    }

    static void Tick()
    {
        if (!Application.isPlaying || stack.Count == 0) return;
        Watch();
        if (waitFor != null) { if (!waitFor() && Time.time < waitForTimeout) return; waitFor = null; }
        if (Time.time < waitUntil) return;
        try
        {
            var top = stack.Peek();
            if (!top.MoveNext()) { stack.Pop(); return; }
            switch (top.Current)
            {
                case float s: waitUntil = Time.time + s; break;
                case Func<bool> f: waitFor = f; waitForTimeout = Time.time + 40f; break;
                case IEnumerator sub: stack.Push(sub); break;
            }
        }
        catch (Exception e) { Note("ERROR " + e.Message); stack.Clear(); }
    }

    static PlayerFSM chef;
    static PlayerFSM Chef => chef != null ? chef : chef = UnityEngine.Object.FindFirstObjectByType<PlayerFSM>();
    static bool Idle => Chef.executingState == ExecutingState.IDLE;
    static EdibleBase Held => Chef.HeldFood;

    static Vector3 ScreenOf(Component c)
    {
        if (c is NpcFsm npc) return Camera.main.WorldToScreenPoint(npc.transform.position + Vector3.up);
        if (c is NonStackBase u && u.Food != null && !u.GetComponent<Collider>().enabled) c = u.Food;
        var col = c.GetComponentsInChildren<Collider>().FirstOrDefault(x => x.enabled) ?? c.GetComponentsInChildren<Collider>().FirstOrDefault();
        return Camera.main.WorldToScreenPoint(col ? col.bounds.center : c.transform.position);
    }

    static IEnumerator Tap(Component c)
    {
        yield return (Func<bool>)(() => Idle);
        yield return delay;   // a player finds the thing and reaches for it
        if (c == null) yield break;
        Chef.HandleScreenTap(ScreenOf(c));
        yield return 0.15f;
        yield return (Func<bool>)(() => Idle);
    }

    static IEnumerator Run()
    {
        Note("game on screen");
        // The lesson: always the thing the arrow points at.
        while (Tutorial.Running || Time.time - t0 < 1.5f)
        {
            var f = Tutorial.Focus;
            if (f == null) { yield return 0.2f; continue; }
            if (!served1 && f is NpcFsm) { }
            yield return Tap(f);
            yield return 0.3f;
        }
        Note("bot: lesson finished, now serving like a player");
        // Then plain service until the first $20 (or two minutes).
        while (Time.time - t0 < 180f && !afford20)
        {
            var npc = NpcFsm.Active.Where(Orders.Waiting).OrderByDescending(n => n.WaitedSeconds).FirstOrDefault();
            var plate = Orders.AllPlates.FirstOrDefault(p => p != null && p.isActiveAndEnabled && !p.HasHamburger && p.LayerCount == 0);
            if (npc == null || plate == null) { yield return 0.5f; continue; }
            Note($"bot: making {npc.Ordered.Name} ({string.Join("+", npc.Ordered.Layers)})");
            yield return Make(npc.Ordered, plate);
            if (plate.HasHamburger) { yield return Tap(plate); if (Held is Hamburger h) { yield return Tap(npc); Note($"bot: served {npc.Kind}"); } }
        }
        Note("bot: stop");
    }

    static readonly FieldInfo spawn = typeof(IngredientsSource).GetField("spawnPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
    static IngredientsSource Source(string what) => UnityEngine.Object.FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None)
        .FirstOrDefault(s => ((GameObject)spawn.GetValue(s)).name.ToLower().Contains(what));
    static Oven Oven => UnityEngine.Object.FindFirstObjectByType<Oven>();
    static TapProxy OvenTap => TapProxy.All.FirstOrDefault(t => t.Target is Oven);
    static Pan Pan => UnityEngine.Object.FindObjectsByType<Pan>(FindObjectsSortMode.InstanceID).FirstOrDefault(p => p.isActiveAndEnabled && !p.IsHaveFood());
    static ChoppingBoard Board => UnityEngine.Object.FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.InstanceID).FirstOrDefault(b => b.isActiveAndEnabled);
    static Bin Bin => UnityEngine.Object.FindFirstObjectByType<Bin>();

    static IEnumerator Make(Recipe order, Plate plate)
    {
        var layers = order.Layers.ToList();
        bool bun = layers.Contains("bun"); int patties = layers.Count(l => l == "burger");
        foreach (var veg in layers.Where(l => l != "bun" && l != "burger" && l != "lettuce"))
        {
            yield return Tap(Source(veg)); var b = Board; yield return Tap(b);
            yield return (Func<bool>)(() => b.Food is CuttableBase c && c.isSliced); yield return Tap(b); yield return Tap(plate);
        }
        if (bun) { yield return Tap(Source("bun")); yield return Tap(OvenTap); }
        Pan pan = null;
        if (patties > 0) { pan = Pan; yield return Tap(Source("burger")); yield return Tap(pan); }
        if (layers.Contains("lettuce")) { yield return Tap(Source("lettuce")); yield return Tap(plate); }
        if (bun) { yield return (Func<bool>)(() => Oven.Food == null || Oven.Food.Preparation != Prep.Raw); yield return Tap(OvenTap); yield return Tap(plate); }
        for (int i = 0; i < patties; i++)
        {
            if (pan == null || pan.Food == null) { pan = Pan; yield return Tap(Source("burger")); yield return Tap(pan); }
            var p = pan; yield return (Func<bool>)(() => p.Food == null || p.Food.Preparation != Prep.Raw);
            yield return Tap(p); yield return Tap(plate); pan = null;
        }
        if (Held != null) yield return Tap(Bin);
        if (plate.LayerCount > 0 && !plate.HasHamburger) yield return Tap(plate);
    }

    // Taps beside a crate (outside its box on screen by `gap` of the screen height) and reports what the chef holds
    // two seconds later.
    public static string TapBeside(string what, string gap)
    {
        float g = float.Parse(gap, System.Globalization.CultureInfo.InvariantCulture) * Screen.height;
        var src = Source(what);
        var col = src.GetComponentInChildren<Collider>();
        var b = col.bounds; var cam = Camera.main;
        float maxX = float.MinValue, cy = 0f;
        for (int i = 0; i < 8; i++)
        {
            var p = cam.WorldToScreenPoint(new Vector3(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i < 4 ? b.min.z : b.max.z));
            maxX = Mathf.Max(maxX, p.x); cy += p.y / 8f;
        }
        var at = new Vector3(maxX + g, cy, 0f);
        bool hit = Physics.Raycast(cam.ScreenPointToRay(at), out var h, 100f);
        Chef.HandleScreenTap(at);
        stack.Clear();
        stack.Push(Report(what, hit ? h.collider.name : "nothing"));
        return $"tapped {g:0} px right of {src.name} (ray hits {(hit ? h.collider.name : "nothing")})";
    }

    static IEnumerator Report(string what, string rayHit)
    {
        yield return 2.5f;
        Note($"tap beside {what} (ray: {rayHit}) -> holding {(Held != null ? Held.name : "nothing")}");
        Log();
    }

    // Records the opening titles (GameplayRecorder.BeginDirect: an mp4 + a still per second) - frames are locked to
    // 30 fps of game time, so a slow editor doesn't skip any of it. Bridge: _OnboardBot RecordIntro <out>.
    public static string RecordIntro(string outPath)
    {
        GameplayRecorder.BeginDirect(outPath);
        Time.captureFramerate = 30;
        LessonIntro.Play(null, () => { });
        stack.Clear();
        stack.Push(StopAfter(6.5f));
        return "recording intro";
    }

    static IEnumerator StopAfter(float seconds)
    {
        yield return seconds;
        GameplayRecorder.EndDirect();
    }

    // How big each tappable thing is on screen: its collider's projected box, in pixels of this screen and as
    // CSS pixels on a phone held sideways (the game's 16:9 frame letterboxed into 740 x 360).
    // For each "x,z" (separated by ;): the height of whatever is drawn there (meshes get temporary colliders), what
    // it is, whether the navmesh is within 0.5 m, and where it is on screen.
    public static string Spots(string list)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var added = new List<MeshCollider>();
        foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            if (mf.sharedMesh != null && mf.GetComponent<Collider>() == null && mf.GetComponentInParent<NpcFsm>() == null && mf.GetComponent<MeshRenderer>() is { enabled: true })
            {
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                added.Add(mc);
            }
        Physics.SyncTransforms();
        var sb = new StringBuilder();
        foreach (var item in list.Split(';'))
        {
            var xz = item.Split(',');
            float x = float.Parse(xz[0], ci), z = float.Parse(xz[1], ci);
            string top = "nothing";
            if (Physics.Raycast(new Vector3(x, 30f, z), Vector3.down, out var hit, 60f))
                top = $"{hit.collider.name} y={hit.point.y:F2}";
            bool walk = UnityEngine.AI.NavMesh.SamplePosition(new Vector3(x, 1.2f, z), out var nh, 0.5f, UnityEngine.AI.NavMesh.AllAreas);
            var vp = Camera.main.WorldToViewportPoint(new Vector3(x, hit.collider != null ? hit.point.y : 1.2f, z));
            sb.AppendLine($"({x},{z}): {top} walk={(walk ? "YES" : "no")} vp=({vp.x:F2},{vp.y:F2})");
        }
        foreach (var mc in added) UnityEngine.Object.DestroyImmediate(mc);
        return sb.ToString();
    }

    // Renderers near a world point (x z, radius): path and bounds; and whether the navmesh is there.
    public static string NearPoint(string x, string z, string radius)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var at = new Vector3(float.Parse(x, ci), 1.2f, float.Parse(z, ci));
        float r = float.Parse(radius, ci);
        string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
        var list = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .Where(x2 => x2 is MeshRenderer && Vector2.Distance(new Vector2(x2.bounds.center.x, x2.bounds.center.z), new Vector2(at.x, at.z)) < r)
            .Select(x2 => $"{Path(x2.transform)} min={x2.bounds.min:F2} max={x2.bounds.max:F2}");
        bool walk = UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 0.8f, UnityEngine.AI.NavMesh.AllAreas);
        return $"navmesh within 0.8: {(walk ? hit.position.ToString("F2") : "no")}\n" + string.Join("\n", list);
    }

    // The children of a scene object, each with the world bounds of what it draws and where that is on screen.
    public static string Kids(string path)
    {
        var root = GameObject.Find(path.Replace("%20", " "));
        if (root == null) return "no " + path;
        var cam = Camera.main;
        var sb = new StringBuilder();
        foreach (Transform c in root.transform)
        {
            var rs = c.GetComponentsInChildren<Renderer>(false).Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
            if (rs.Length == 0) { sb.AppendLine($"{c.name} active={c.gameObject.activeInHierarchy} (no renderers) pos={c.position:F2}"); continue; }
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var vp = cam.WorldToViewportPoint(b.center);
            sb.AppendLine($"{c.name} active={c.gameObject.activeInHierarchy} min={b.min:F2} max={b.max:F2} vp=({vp.x:F2},{vp.y:F2})");
        }
        return sb.ToString();
    }

    // What is under a screen point (pixels from the bottom-left of the game view, as fractions 0..1): hit point,
    // collider, and whether the navmesh is there (walkable).
    public static string Under(string fx, string fy)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var cam = Camera.main;
        var ray = cam.ViewportPointToRay(new Vector3(float.Parse(fx, ci), float.Parse(fy, ci), 0f));
        var hits = Physics.RaycastAll(ray, 200f).OrderBy(h => h.distance).ToArray();
        string first = hits.Length > 0 ? $"{hits[0].collider.name} {hits[0].point:F2}" : "nothing";
        var floor = new Plane(Vector3.up, new Vector3(0f, 1.2f, 0f));
        string onFloor = floor.Raycast(ray, out float d) ? $"{ray.GetPoint(d):F2}" : "-";
        var p = ray.GetPoint(d);
        bool walk = UnityEngine.AI.NavMesh.SamplePosition(p, out var nh, 0.4f, UnityEngine.AI.NavMesh.AllAreas);
        return $"hit={first} floor1.2={onFloor} navmesh={(walk ? nh.position.ToString("F2") : "no")}";
    }

    // Renderers around the first chopping board: name, path, position, shadow casting, lightmap index.
    public static string NearBoard(string radius)
    {
        float r = float.Parse(radius, System.Globalization.CultureInfo.InvariantCulture);
        var board = UnityEngine.Object.FindObjectsByType<ChoppingBoard>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(b => b.isActiveAndEnabled);
        var at = board.transform.position;
        string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
        return string.Join("\n", UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => Vector3.Distance(x.bounds.center, at) < r)
            .Select(x => $"{Path(x.transform)} active={x.gameObject.activeInHierarchy} en={x.enabled} pos={x.transform.position:F2} shadow={x.shadowCastingMode} lm={x.lightmapIndex} static={x.gameObject.isStatic} type={x.GetType().Name}"));
    }

    // The connected pieces of a model's mesh (by shared positions): bounds and triangle count of each.
    public static string MeshIslands(string fbx)
    {
        var mesh = AssetDatabase.LoadAllAssetsAtPath(fbx.Replace("%20", " ")).OfType<Mesh>().First();
        var v = mesh.vertices; var t = mesh.triangles;
        var parent = Enumerable.Range(0, v.Length).ToArray();
        int Find(int i) { while (parent[i] != i) i = parent[i] = parent[parent[i]]; return i; }
        var byPos = new Dictionary<Vector3, int>();
        for (int i = 0; i < v.Length; i++) { var k = new Vector3(Mathf.Round(v[i].x * 1e4f), Mathf.Round(v[i].y * 1e4f), Mathf.Round(v[i].z * 1e4f)); if (byPos.TryGetValue(k, out int j)) parent[Find(i)] = Find(j); else byPos[k] = i; }
        for (int i = 0; i < t.Length; i += 3) { parent[Find(t[i])] = Find(t[i + 1]); parent[Find(t[i + 1])] = Find(t[i + 2]); }
        var islands = new Dictionary<int, (Bounds b, int tris)>();
        for (int i = 0; i < t.Length; i += 3)
        {
            int r = Find(t[i]);
            var b = islands.TryGetValue(r, out var e) ? e.b : new Bounds(v[t[i]], Vector3.zero);
            for (int k = 0; k < 3; k++) b.Encapsulate(v[t[i + k]]);
            islands[r] = (b, (e.tris) + 1);
        }
        return $"{mesh.name} v={v.Length} sub={mesh.subMeshCount} | " + string.Join(" | ", islands.Values.Select(x => $"tris={x.tris} c={x.b.center:F3} s={x.b.size:F3}"));
    }

    public static string WarpChef(string x, string z)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        var chef = UnityEngine.Object.FindFirstObjectByType<PlayerFSM>();
        var agent = chef.GetComponent<UnityEngine.AI.NavMeshAgent>();
        return agent.Warp(new Vector3(float.Parse(x, ci), chef.transform.position.y, float.Parse(z, ci))) + " " + chef.transform.position;
    }

    // The follow camera off and the whole-cafe picture back (for looking at the scene), or on again.
    public static string WholeCafe(string on)
    {
        var cam = Camera.main;
        var ab = cam.GetComponent<CameraAB>();
        if (ab == null) return "no CameraAB";
        bool whole = on == "1";
        ab.enabled = !whole;
        if (whole)
        {
            var t = typeof(CameraAB);
            cam.transform.position = (Vector3)t.GetField("homePos", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ab);
            cam.orthographicSize = (float)t.GetField("homeSize", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ab);
        }
        return whole ? "whole" : "follow";
    }

    // Every customer in the cafe as a ghost (re-dressed until the dice say so), for looking at the faces.
    public static string GhostAll()
    {
        int n = 0;
        foreach (var npc in NpcFsm.Active)
        {
            for (int i = 0; i < 40 && npc.GetComponent<SeasonalGhost>() == null; i++) SeasonalDecor.Dress(npc);
            if (npc.GetComponent<SeasonalGhost>() != null) n++;
        }
        return n + " ghosts";
    }

    // How far each ghost's sheet is bent from its rest shape right now (largest vertex offset, in sheet units),
    // and how fast the ghost is going.
    public static string Flutter()
    {
        var sb = new StringBuilder();
        foreach (var g in UnityEngine.Object.FindObjectsByType<SeasonalGhost>(FindObjectsSortMode.None))
        {
            if (g.body == null) continue;
            var mf = g.body.GetComponent<MeshFilter>();
            var now = mf.sharedMesh.vertices;
            var rest = (Vector3[])typeof(SeasonalGhost).GetField("rest", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(g);
            float max = 0f;
            if (rest != null) for (int i = 0; i < now.Length; i++) max = Mathf.Max(max, (now[i] - rest[i]).magnitude);
            var npc = g.GetComponent<NpcFsm>();
            float speed = npc != null && npc.Agent.enabled ? npc.Agent.velocity.magnitude : 0f;
            sb.Append($"{g.name} flutter={g.flutter} bend={max:F3} speed={speed:F2} tilt={Quaternion.Angle(g.body.rotation, g.body.parent.rotation):F1} | ");
        }
        return sb.ToString();
    }

    public static string SetSeason(string season)
    {
        SeasonalDecor.Force = (SeasonalDecor.Season)Enum.Parse(typeof(SeasonalDecor.Season), season);
        SeasonalDecor.Rebuild();
        return season;
    }

    // Fires an effect, then freezes the game `seconds` later (game time), so a screenshot sees it mid-flight.
    // A seated customer throws a tomato at the chef (as when they lose patience).
    public static string Throw()
    {
        var npc = NpcFsm.Active.FirstOrDefault(n => n != null && n.executingNpcState == ExecutingNpcState.WAIT);
        if (npc == null) return "nobody seated";
        FoodFight.Instance.CustomerThrows(npc);
        return npc.name;
    }

    // Loudness (RMS, peak) of the clips on the music's object, and the sources' volumes.
    public static string Loudness()
    {
        var sb = new StringBuilder();
        foreach (var src in Audio.Instance.GetComponents<AudioSource>())
        {
            var c = src.clip;
            if (c == null) { sb.Append("null | "); continue; }
            var data = new float[Mathf.Min(c.samples * c.channels, 48000 * 20)];
            bool ok = c.GetData(data, 0);
            double sum = 0; float peak = 0;
            foreach (var x in data) { sum += x * x; peak = Mathf.Max(peak, Mathf.Abs(x)); }
            sb.Append($"{c.name} ok={ok} rms={System.Math.Sqrt(sum / data.Length):F4} peak={peak:F3} vol={src.volume:F2} pitch={src.pitch:F2} | ");
        }
        return sb.ToString();
    }

    // Every renderer drawing with a material whose name contains `part`: path, world scale, bounds.
    public static string WithMaterial(string part)
    {
        string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
        return string.Join("\n", UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(r => r.sharedMaterials.Any(m => m != null && m.name.Contains(part)))
            .Select(r => $"{Path(r.transform)} active={r.gameObject.activeInHierarchy} lossy={r.transform.lossyScale:F2} rot={r.transform.eulerAngles:F0} min={r.bounds.min:F2} max={r.bounds.max:F2}"));
    }

    // The screens' strips: small and crunched, repeating, and the plain one on the screens' material.
    public static string SetupScreens()
    {
        var paths = new[] { "Assets/Project/[GAME]/Graphics/Textures/Screens/screen_default.png",
            "Assets/Resources/Screens/screen_halloween.png", "Assets/Resources/Screens/screen_winter.png", "Assets/Resources/Screens/screen_valentine.png" };
        foreach (var p in paths)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.None;
            ti.mipmapEnabled = true;
            ti.wrapModeU = TextureWrapMode.Repeat;
            ti.wrapModeV = TextureWrapMode.Clamp;
            ti.maxTextureSize = 1024;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.crunchedCompression = true;
            ti.compressionQuality = 60;
            ti.SaveAndReimport();
        }
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Project/[GAME]/Graphics/Shaders/Custom_ScrollingTexture_URP_Test.mat");
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(paths[0]);
        mat.SetTexture("_BaseMap", tex);
        mat.SetTexture("_MainTex", tex);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return string.Join(" | ", paths.Select(p => System.IO.Path.GetFileName(p) + " " + UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(AssetDatabase.LoadAssetAtPath<Texture2D>(p)) / 1024 + "KB"));
    }

    public static string Cam()
    {
        var cam = Camera.main;
        return $"pos={cam.transform.position:F2} size={cam.orthographicSize:F2} thrower={(FoodFight.Thrower != null ? FoodFight.Thrower.name : "-")}";
    }

    public static string FxFreeze(string which, string seconds)
    {
        float wait = float.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture);
        string r = Fx(which);
        float at = Time.time + wait;
        EditorApplication.CallbackFunction check = null;
        check = () => { if (Time.time >= at) { Time.timeScale = 0f; EditorApplication.update -= check; } };
        EditorApplication.update += check;
        return r;
    }

    // Fires one kitchen effect where it can be seen: ready / sliced / done / served / shocked / huff / dust.
    public static string Fx(string which)
    {
        var chef = UnityEngine.Object.FindFirstObjectByType<PlayerFSM>().transform;
        var food = UnityEngine.Object.FindObjectsByType<EdibleBase>(FindObjectsSortMode.None).Where(f => f.gameObject.activeInHierarchy)
            .OrderBy(f => Vector3.Distance(f.transform.position, chef.position)).FirstOrDefault();
        var plate = UnityEngine.Object.FindObjectsByType<Plate>(FindObjectsSortMode.None).Where(p => p.gameObject.activeInHierarchy)
            .OrderBy(p => Vector3.Distance(p.transform.position, chef.position)).First();
        NpcFsm npc = null;   // (on the chef: always in view)
        switch (which)
        {
            case "burn": BurnSmoke.On(food); return food.name + " " + BurnSmoke.InFrontOf(food);
            case "probe": case "probeS":
            {
                var ps = GameObject.Find(which == "probe" ? "<<<Controllers>>>/BurnSmoke/Dust" : "<<<Controllers>>>/BurnSmoke/Sparks").GetComponent<ParticleSystem>();
                var parts = new ParticleSystem.Particle[64];
                int n = ps.GetParticles(parts);
                return $"n={n} " + string.Join(" | ", parts.Take(Math.Min(n, 3)).Select(p =>
                    $"pos={p.position} size={p.GetCurrentSize(ps):0.00} col={p.GetCurrentColor(ps)} life={p.remainingLifetime:0.00}/{p.startLifetime:0.00}"))
                    + $" main.size={ps.main.startSize.constantMin}-{ps.main.startSize.constantMax} sim={ps.main.simulationSpeed}";
            }
            case "sliced": KitchenFx.Chop(food); return food.name;
            case "nop": return "";
            case "chop":
            {
                var board = UnityEngine.Object.FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None).FirstOrDefault(b => b.isActiveAndEnabled && !b.IsHaveFood());
                var cheese = UnityEngine.Object.FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None).FirstOrDefault(s => s.name.ToLower().Contains("cheese"));
                if (board == null || cheese == null) return "no board/cheese";
                var f = cheese.SpawnInto(board);
                return board.name + " " + Camera.main.WorldToScreenPoint(board.transform.position) + " " + (f != null ? f.name : "null");
            }
            case "done": KitchenFx.BurgerDone(plate.transform.position + Vector3.up * 0.8f); return plate.name;
        }
        return "?";
    }

    public static string Sizes()
    {
        float phoneScale = Mathf.Min(740f / Screen.width, 360f / Screen.height);
        var rows = new List<(float, string)>();
        void Add(string name, Collider col)
        {
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) return;
            var b = col.bounds; var cam = Camera.main;
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < 8; i++)
            {
                var p = cam.WorldToScreenPoint(new Vector3(i % 2 == 0 ? b.min.x : b.max.x, (i / 2) % 2 == 0 ? b.min.y : b.max.y, i / 4 == 0 ? b.min.z : b.max.z));
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
            }
            float w = (maxX - minX) * phoneScale, h = (maxY - minY) * phoneScale;
            rows.Add((Mathf.Min(w, h), $"{name,-28} phone {w,4:0} x {h,4:0} px"));
        }
        foreach (var s in UnityEngine.Object.FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None)) Add("crate " + ((GameObject)spawn.GetValue(s)).name, s.GetComponentInChildren<Collider>());
        foreach (var t in TapProxy.All) Add("oven (proxy)", t.GetComponent<Collider>());
        foreach (var p in UnityEngine.Object.FindObjectsByType<Pan>(FindObjectsSortMode.None)) Add("pan " + p.name, p.GetComponent<Collider>());
        foreach (var b in UnityEngine.Object.FindObjectsByType<ChoppingBoard>(FindObjectsSortMode.None)) Add("board", b.GetComponent<Collider>());
        foreach (var p in Orders.AllPlates) Add("plate " + p.name, p.GetComponent<Collider>());
        Add("bin", Bin != null ? Bin.GetComponent<Collider>() : null);
        foreach (var n in NpcFsm.Active) Add("customer " + n.Kind, n.GetComponentInChildren<Collider>());
        var sb = new StringBuilder($"screen {Screen.width}x{Screen.height}, phone scale {phoneScale:0.000} (smaller side first; 44 px is the usual minimum finger target)\n");
        foreach (var r in rows.OrderBy(r => r.Item1)) sb.AppendLine(r.Item2);
        File.WriteAllText("Temp/_sizes.txt", sb.ToString());
        return sb.ToString();
    }

    // How much of a decoration's footprint is walkable navmesh (grid points inside 0.85 of its radius).
    public static string NavIn(string name)
    {
        var t = GameObject.Find("Level").transform.Find("Decorations/" + name);
        var r = t.GetComponentInChildren<Renderer>();
        var b = r.bounds;
        float rad = Mathf.Min(b.size.x, b.size.z) * 0.5f;
        int on = 0, all = 0;
        for (float x = -rad; x <= rad; x += 0.2f)
            for (float z = -rad; z <= rad; z += 0.2f)
            {
                if (x * x + z * z > rad * rad * 0.72f) continue;
                all++;
                var p = new Vector3(b.center.x + x, b.min.y + 0.3f, b.center.z + z);
                if (UnityEngine.AI.NavMesh.SamplePosition(p, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas)
                    && new Vector2(hit.position.x - p.x, hit.position.z - p.z).magnitude < 0.05f) on++;
            }
        return name + " r=" + rad.ToString("F2") + " c=" + b.center + " min.y=" + b.min.y.ToString("F2") + " walkable " + on + "/" + all;
    }

    // Can the customers (from the door) and the chef still get to every chair: how far short of each a path ends.
    public static string Reach()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<NpcSpawnController>();
        var chairs = (System.Collections.Generic.List<GameObject>)typeof(NpcSpawnController).GetField("targetChairs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(spawner);
        var chef = GameObject.Find("Player").transform.position;
        var sb = new StringBuilder();
        foreach (var c in chairs)
        {
            if (c == null || !c.activeInHierarchy) continue;
            sb.Append(c.name);
            foreach (var from in new[] { new Vector3(21.35f, 1.2f, 8.79f), chef })
            {
                var path = new UnityEngine.AI.NavMeshPath();
                UnityEngine.AI.NavMesh.SamplePosition(from, out var a, 3f, UnityEngine.AI.NavMesh.AllAreas);
                UnityEngine.AI.NavMesh.SamplePosition(c.transform.position, out var b, 3f, UnityEngine.AI.NavMesh.AllAreas);
                UnityEngine.AI.NavMesh.CalculatePath(a.position, b.position, UnityEngine.AI.NavMesh.AllAreas, path);
                var end = path.corners.Length > 0 ? path.corners[path.corners.Length - 1] : a.position;
                float shortBy = new Vector2(end.x - c.transform.position.x, end.z - c.transform.position.z).magnitude;
                float len = 0; for (int k = 1; k < path.corners.Length; k++) len += Vector3.Distance(path.corners[k - 1], path.corners[k]);
                sb.Append(" ").Append(shortBy.ToString("F1")).Append("/").Append(len.ToString("F0"));
            }
            sb.Append("; ");
        }
        return sb.ToString();
    }

    // Every chair (bought or not) and its distance to the edge of the burger's and the tomato's obstacles.
    public static string ChairGaps()
    {
        var spawner = UnityEngine.Object.FindFirstObjectByType<NpcSpawnController>();
        var chairs = (System.Collections.Generic.List<GameObject>)typeof(NpcSpawnController).GetField("targetChairs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(spawner);
        var sb = new StringBuilder();
        foreach (var c in chairs)
        {
            if (c == null) continue;
            var p = c.transform.position;
            float gb = new Vector2(p.x + 3.04f, p.z - 14.99f).magnitude - 3.15f;
            float gt = new Vector2(p.x + 1.73f, p.z - 10.59f).magnitude - 1.63f;
            sb.Append(c.name).Append(c.activeInHierarchy ? "" : "(off)").Append(" ").Append(Mathf.Min(gb, gt).ToString("F1")).Append("; ");
        }
        return sb.ToString();
    }

    // Poses of a clip on copies of the chef, side by side, seen from the side: what a fall looks like.
    public static string PoseShot(string clipName, string path)
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/KayKit_Skeletons_1.0_FREE/characters/fbx/Skeleton_Warrior.fbx").OfType<AnimationClip>().ToArray();
        var clip = clips.First(c => c.name == clipName);
        var stage = new GameObject("__PoseShot") { hideFlags = HideFlags.DontSave };
        var chef = GameObject.Find("Player/Model");
        AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        float[] at = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        for (int i = 0; i < at.Length; i++)
        {
            var copy = UnityEngine.Object.Instantiate(chef, stage.transform);
            foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
            copy.transform.position = new Vector3(100f + i * 2.2f, 0f, 100f);
            copy.transform.rotation = Quaternion.Euler(0f, 90f, 0f);   // facing +x (right in the picture)
            AnimationMode.SampleAnimationClip(copy, clip, at[i] * clip.length);
        }
        AnimationMode.EndSampling();
        var camGo = new GameObject("__PoseCam") { hideFlags = HideFlags.DontSave };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 2.2f;
        cam.transform.position = new Vector3(104.4f, 1f, 90f);
        cam.transform.rotation = Quaternion.identity;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.gray;
        var rt = new RenderTexture(1200, 500, 24);
        cam.targetTexture = rt; cam.aspect = 1200f / 500f;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1200, 500, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1200, 500), 0, 0); tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null;
        AnimationMode.StopAnimationMode();
        UnityEngine.Object.DestroyImmediate(camGo); UnityEngine.Object.DestroyImmediate(stage); UnityEngine.Object.DestroyImmediate(rt);
        return clipName + " " + clip.length.ToString("F2") + "s";
    }

    // The chef's forward fall (a tomato from behind): Death_B from where he tips over, then a hop back up
    // (Spawn_Ground's spring), then idle. Any State -> SlipForward on the "SlipForward" trigger.
    public static string AddForwardFall()
    {
        const string path = "Assets/Project/[GAME]/Animations/CharacterAnims/PlayerAnims/PlayerAnim.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
        var clips = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/KayKit_Skeletons_1.0_FREE/characters/fbx/Skeleton_Warrior.fbx").OfType<AnimationClip>().ToArray();
        var sm = ctrl.layers[0].stateMachine;
        if (sm.states.Any(x => x.state.name == "SlipForward")) return "already";
        if (!ctrl.parameters.Any(x => x.name == "SlipForward")) ctrl.AddParameter("SlipForward", AnimatorControllerParameterType.Trigger);
        var idle = sm.states.First(x => x.state.name == "Idle").state;
        var fall = sm.AddState("SlipForward", new Vector3(500, 300));
        fall.motion = clips.First(c => c.name == "Death_B"); fall.speed = 1.3f;
        var hop = sm.AddState("HopUp", new Vector3(750, 300));
        hop.motion = clips.First(c => c.name == "Spawn_Ground"); hop.speed = 1.3f;
        var any = sm.AddAnyStateTransition(fall);
        any.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "SlipForward");
        any.hasExitTime = false; any.duration = 0.08f; any.offset = 0.42f; any.canTransitionToSelf = false;
        var up = fall.AddTransition(hop);
        up.hasExitTime = true; up.exitTime = 1f; up.duration = 0.2f; up.offset = 0.3f;
        var back = hop.AddTransition(idle);
        back.hasExitTime = true; back.exitTime = 0.95f; back.duration = 0.15f;
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        return "added";
    }

    public static string SlipTest(string forward, string prefix)
    {
        var chef = UnityEngine.Object.FindFirstObjectByType<PlayerFSM>();
        chef.Slip(2.1f, forward == "1");
        float start = Time.realtimeSinceStartup;
        int next = 0;
        float[] at = { 0.35f, 0.8f, 1.3f, 1.8f };
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (next >= at.Length) { EditorApplication.update -= tick; return; }
            if (Time.realtimeSinceStartup - start >= at[next]) { ScreenCapture.CaptureScreenshot(prefix + next + ".png", 1); next++; }
        };
        EditorApplication.update += tick;
        return "slip " + forward;
    }

    public static string OverviewTap()
    {
        var b = GameObject.Find("<<<UI>>>/StaticCanvas/InGamePanel/HUD/OverviewBTN");
        if (b == null) return "no button";
        b.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        return "tapped " + Camera.main.orthographicSize.ToString("F2");
    }
}
