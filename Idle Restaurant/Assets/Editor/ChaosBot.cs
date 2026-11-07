using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// The chaos bot (2026-11-04, user: "whatever the player does there must be no bug"): plays the game in play mode the
// way nobody should - random taps on everything, the same thing ten times in a row, the wrong station, the floor,
// customers, Mochi, the bin, the HUD's buttons, tomatoes thrown at the chef - and meanwhile checks, twice a second,
// the things that must always hold; every break is written to Temp/chaos_report.txt (once per kind and object, with
// how often), with a screenshot of the first few. Editor only: nothing of it is in a build.
//   Tools/Chibi Cafe/Chaos Bot/Start (5 min) - in play mode; Stop writes the report. Start(minutes, timeScale).
// What it checks:
//   - an error or exception in the log;
//   - the chef's stack: everything he carries is under one of his hands, stacked in a column (no layer off to the
//     side, none inside another);
//   - plates: their layers stacked in the plate's middle, none inside another;
//   - food left lying about: switched on, belonging to nothing (no hand, plate, station, table, Mochi), not moving;
//   - stuck: the chef with somewhere to go not moving, or off the navmesh; Mochi on an errand not moving; a customer
//     waiting far past their patience, or walking without moving.
public static class ChaosBot
{
    const float ActEvery = 0.22f, CheckEvery = 0.5f;
    static bool running;
    static double endsAt, nextAct, nextCheck, startedAt;
    static float timeScale = 2f;
    static System.Random rng;
    static readonly Dictionary<string, (int count, double first, string detail)> breaks = new();
    static readonly Dictionary<string, double> since = new();      // a condition held since (realtime)
    static readonly Dictionary<UnityEngine.Object, Vector3> lastPos = new();
    static int shots, actions;
    static readonly Queue<string> recent = new();     // the last things it did, written under each new break
    static void Did(string what)
    {
        recent.Enqueue($"{EditorApplication.timeSinceStartup - startedAt:0.0}s {what}");
        while (recent.Count > 25) recent.Dequeue();
    }
    static string reportPath => Path.Combine(Application.dataPath, "../Temp/chaos_report.txt");

    [MenuItem("Tools/Chibi Cafe/Chaos Bot/Start (5 min)")]
    static void StartMenu() => Start("5", "2");

    [MenuItem("Tools/Chibi Cafe/Chaos Bot/Stop and report")]
    static void StopMenu() => Debug.Log(Stop());

    public static string Start(string minutes, string speed)
    {
        if (!Application.isPlaying) return "not playing";
        timeScale = float.Parse(speed, System.Globalization.CultureInfo.InvariantCulture);
        rng = new System.Random();
        breaks.Clear(); since.Clear(); lastPos.Clear(); recent.Clear(); shots = 0; actions = 0;
        startedAt = EditorApplication.timeSinceStartup;
        endsAt = startedAt + float.Parse(minutes, System.Globalization.CultureInfo.InvariantCulture) * 60f;
        if (!running) { EditorApplication.update += Tick; Application.logMessageReceived += OnLog; }
        running = true;
        return $"chaos for {minutes} min at x{timeScale}";
    }

    public static string Stop()
    {
        if (running) { EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog; }
        running = false;
        if (Application.isPlaying && Time.timeScale > 0f) Time.timeScale = 1f;
        return Write();
    }

    public static string Status() => $"running={running} actions={actions} breaks={breaks.Count} left={Math.Max(0, endsAt - EditorApplication.timeSinceStartup):0}s";

    static string Write()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Chaos bot: {actions} actions over {(EditorApplication.timeSinceStartup - startedAt) / 60f:0.0} min, {breaks.Count} kinds of break");
        foreach (var (key, v) in breaks.OrderBy(x => x.Value.first))
            sb.AppendLine($"[{v.first:0}s x{v.count}] {key} :: {v.detail}");
        File.WriteAllText(reportPath, sb.ToString());
        return sb.ToString();
    }

    static void Break(string key, string detail)
    {
        double t = EditorApplication.timeSinceStartup - startedAt;
        if (breaks.TryGetValue(key, out var b)) { breaks[key] = (b.count + 1, b.first, b.detail); return; }
        breaks[key] = (1, t, detail + "\n      before it: " + string.Join(" | ", recent));
        if (shots < 12) ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, $"../Temp/chaos_{++shots:00}.png"));
        Write();
    }

    // A condition that must hold for `seconds` before it counts (springs and flights settle, people walk).
    static bool Held(string key, bool now, float seconds)
    {
        double t = EditorApplication.timeSinceStartup;
        if (!now) { since.Remove(key); return false; }
        if (!since.TryGetValue(key, out var from)) { since[key] = t; return false; }
        return t - from >= seconds;
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
        if (message.Contains("Render Graph") || message.Contains("shader") || message.Contains("Shader")) return;
        var where = (stack ?? "").Split('\n').FirstOrDefault(l => l.Contains(".cs:")) ?? "";
        Break("log " + type + ": " + message.Split('\n')[0], where.Trim());
    }

    static void Tick()
    {
        if (!Application.isPlaying) { Stop(); return; }
        double now = EditorApplication.timeSinceStartup;
        if (now > endsAt) { Debug.Log("[ChaosBot] done\n" + Stop()); return; }
        if (now >= nextCheck) { nextCheck = now + CheckEvery; try { Check(); } catch (Exception e) { Break("checker " + e.GetType().Name, e.Message); } }
        if (now >= nextAct) { nextAct = now + ActEvery / Mathf.Max(1f, timeScale * 0.5f); try { Act(); } catch (Exception e) { Break("bot " + e.GetType().Name, e.Message); } }
    }

    #region Playing badly
    static T Pick<T>(IList<T> list) => list.Count == 0 ? default : list[rng.Next(list.Count)];
    static int burst;
    static Vector3 burstAt;

    static void Act()
    {
        var chef = UnityEngine.Object.FindFirstObjectByType<PlayerFSM>();
        var cam = Camera.main;
        if (chef == null || cam == null) return;
        // Paused (a card, the shop, the help): press one of the buttons on screen.
        if (Time.timeScale == 0f)
        {
            if (Held("paused", true, 1.2f)) { PressSomeButton(); since.Remove("paused"); }
            return;
        }
        since.Remove("paused");
        if (Time.timeScale != timeScale) Time.timeScale = timeScale;
        actions++;
        if (burst > 0) { burst--; chef.HandleScreenTap(burstAt); Did("burst tap"); return; }

        int roll = rng.Next(100);
        Vector3 screen;
        Component what = null;
        if (roll < 22) what = Pick(UnityEngine.Object.FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None));
        else if (roll < 47) what = Pick(UnityEngine.Object.FindObjectsByType<PlaceableBase>(FindObjectsSortMode.None).Where(p => p.isActiveAndEnabled).ToList());
        else if (roll < 60) what = Pick(NpcFsm.Active);
        else if (roll < 65) what = UnityEngine.Object.FindFirstObjectByType<Bin>();
        else if (roll < 69) what = UnityEngine.Object.FindFirstObjectByType<Waiter>();
        else if (roll < 74) what = Pick(UnityEngine.Object.FindObjectsByType<EdibleBase>(FindObjectsSortMode.None));
        else if (roll < 86) { }
        else if (roll < 89) { if (FoodFight.Instance != null && Pick(NpcFsm.Active) is NpcFsm thrower) { FoodFight.Instance.CustomerThrows(thrower); Did("tomato from " + thrower.name); } return; }
        else if (roll < 91) { PressSomeButton(); return; }
        else
        {
            var at = Pick(UnityEngine.Object.FindObjectsByType<PlaceableBase>(FindObjectsSortMode.None));
            burst = rng.Next(3, 10); burstAt = Point(at); Did($"burst x{burst} on {(at ? at.name : "-")}"); return;
        }
        screen = what != null ? Point(what) : new Vector3(rng.Next(Screen.width), rng.Next(Screen.height), 0f);   // anywhere
        if (screen.z < 0f) return;
        Did("tap " + (what != null ? what.name : $"screen {screen.x:0},{screen.y:0}") + $" holding {chef.Carried.Count}");
        chef.HandleScreenTap(new Vector3(screen.x, screen.y, 0f));
    }

    static Vector3 Point(Component c)
    {
        if (c == null || Camera.main == null) return new Vector3(0, 0, -1);
        var col = c.GetComponentsInChildren<Collider>().FirstOrDefault(x => x.enabled);
        var p = col ? col.bounds.center : c.transform.position + (c is NpcFsm ? Vector3.up : Vector3.zero);
        var s = Camera.main.WorldToScreenPoint(p);
        return new Vector3(s.x + rng.Next(-8, 9), s.y + rng.Next(-8, 9), s.z);
    }

    static void PressSomeButton()
    {
        var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .Where(b => b.isActiveAndEnabled && b.interactable && b.gameObject.activeInHierarchy
                        && (b.GetComponentInParent<CanvasGroup>() == null || b.GetComponentInParent<CanvasGroup>().alpha > 0.5f)).ToList();
        var b = Pick(buttons);
        if (b != null) { Did("button " + b.name); b.onClick.Invoke(); }
        else
        {
            // A card that closes on a tap (the break cards' surface, the help screen): a click on the screen's middle.
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return;
            var data = new UnityEngine.EventSystems.PointerEventData(es) { position = new Vector2(Screen.width / 2f, Screen.height / 2f) };
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            es.RaycastAll(data, hits);
            if (hits.Count > 0) { Did("click " + hits[0].gameObject.name); UnityEngine.EventSystems.ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler); }
        }
    }
    #endregion

    #region What must always hold
    static readonly FieldInfo waiterJob = typeof(Waiter).GetField("job", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo waiterHold = typeof(Waiter).GetField("hold", BindingFlags.NonPublic | BindingFlags.Instance);

    static Bounds Look(Component c, out bool any)
    {
        any = false;
        var box = new Bounds(c.transform.position, Vector3.zero);
        foreach (var r in c.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!any) { box = r.bounds; any = true; } else box.Encapsulate(r.bounds);
        }
        return box;
    }

    // The floor for one kind of walker: is there any, where, and what might have taken it (big carving obstacles,
    // obstacles scaled up, the surfaces' data).
    static string NavReport(int agentType)
    {
        var sb = new StringBuilder();
        var filter = new UnityEngine.AI.NavMeshQueryFilter { agentTypeID = agentType, areaMask = UnityEngine.AI.NavMesh.AllAreas };
        foreach (var at in new[] { NpcFsm.Door, new Vector3(5f, 1.2f, 12f), new Vector3(10f, 1.5f, 24f) })
            sb.Append(UnityEngine.AI.NavMesh.SamplePosition(at, out var hit, 30f, filter) ? $"floor near {at} at {hit.position}; " : $"NO floor near {at}; ");
        foreach (var surface in UnityEngine.Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            sb.Append($"surface {surface.name} agent={surface.agentTypeID} active={surface.isActiveAndEnabled} data={(surface.navMeshData != null)}; ");
        foreach (var o in UnityEngine.Object.FindObjectsByType<UnityEngine.AI.NavMeshObstacle>(FindObjectsSortMode.None))
        {
            if (!o.isActiveAndEnabled || !o.carving) continue;
            var size = Vector3.Scale(o.size, o.transform.lossyScale);
            if (Mathf.Abs(size.x * size.z) > 3f) sb.Append($"big obstacle {o.name}<{o.transform.parent?.name} {size}; ");
        }
        sb.Append("triangles=" + UnityEngine.AI.NavMesh.CalculateTriangulation().indices.Length / 3);
        return sb.ToString();
    }

    static float Flat(Vector3 a, Vector3 b) => new Vector2(a.x - b.x, a.z - b.z).magnitude;

    // What a thing is drawn with, and where: to tell why it is off.
    static string Parts(Component c) => string.Join(", ", c.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled)
        .Select(r => $"{r.name}@{r.bounds.center}/{r.bounds.size.y:0.00}")) + $" (own {c.transform.position})";

    static void Check()
    {
        if (Time.timeScale == 0f) return;
        var chef = UnityEngine.Object.FindFirstObjectByType<PlayerFSM>();
        if (chef == null) return;
        var stack = chef.GetComponent<CarryStack>();
        bool scattering = stack != null && stack.Scattering;

        // The stack in his hands.
        var carried = chef.Carried.Where(f => f != null).ToList();
        foreach (var f in carried)
        {
            var parent = f.transform.parent;
            if (parent == null || (parent != chef.Hand && parent.name != "SideHand"))
                Break("carried thing not in a hand", $"{f.name} parent={(parent ? parent.name : "none")}");
        }
        if (!scattering)
        {
            foreach (var column in carried.GroupBy(f => f.transform.parent))
            {
                if (column.Key == null) continue;
                var list = column.OrderBy(f => f.transform.localPosition.y).ToList();   // as shown (a burger's bun goes to the bottom)
                for (int i = 0; i < list.Count; i++)
                {
                    var look = Look(list[i], out bool any);
                    if (!any) continue;
                    if (Held("off " + list[i].GetInstanceID(), Flat(look.center, column.Key.position) > 0.3f, 1.2f))
                        Break("stack layer off to the side", $"{list[i].name} {Flat(look.center, column.Key.position):0.00} m from {column.Key.name}: {Parts(list[i])}");
                    if (i == 0) continue;
                    var below = Look(list[i - 1], out bool anyBelow);
                    if (!anyBelow) continue;
                    float overlap = below.max.y - look.min.y;
                    float smaller = Mathf.Min(look.size.y, below.size.y);
                    if (Held("in " + list[i].GetInstanceID(), overlap > smaller * 0.45f, 1.2f))
                        Break("stack layer inside the one below", $"{list[i].name} sinks {overlap:0.00} m into {list[i - 1].name}: {Parts(list[i])} || {Parts(list[i - 1])}");
                }
            }
        }

        // Plates.
        foreach (var plate in UnityEngine.Object.FindObjectsByType<Plate>(FindObjectsSortMode.None))
        {
            var layers = UnityEngine.Object.FindObjectsByType<EdibleBase>(FindObjectsSortMode.None)
                .Where(f => f.Placeable == plate && f is not Hamburger && f.GetComponentInParent<Hamburger>() == null).OrderBy(f => f.transform.position.y).ToList();
            for (int i = 0; i < layers.Count; i++)
            {
                var look = Look(layers[i], out bool any);
                if (!any) continue;
                if (Held("poff " + layers[i].GetInstanceID(), Flat(look.center, plate.transform.position) > 0.3f, 0.8f))
                    Break("plate layer off the plate's middle", $"{layers[i].name} on {plate.name} at {plate.transform.position}: {Parts(layers[i])}");
                if (i == 0) continue;
                var below = Look(layers[i - 1], out bool anyBelow);
                float overlap = below.max.y - look.min.y;
                if (anyBelow && Held("pin " + layers[i].GetInstanceID(), overlap > Mathf.Min(look.size.y, below.size.y) * 0.45f, 0.8f))
                    Break("plate layer inside the one below", $"{layers[i].name} sinks {overlap:0.00} m into {layers[i - 1].name} on {plate.name}: {Parts(layers[i])} || {Parts(layers[i - 1])}");
            }
        }

        // Food that belongs to nothing.
        var mochi = UnityEngine.Object.FindFirstObjectByType<Waiter>();
        var tray = mochi != null ? waiterHold.GetValue(mochi) as Transform : null;
        foreach (var f in UnityEngine.Object.FindObjectsByType<EdibleBase>(FindObjectsSortMode.None))
        {
            bool owned = f.Placeable != null || carried.Contains(f) || (tray != null && f.transform.IsChildOf(tray))
                         || f.GetComponentInParent<IngredientsSource>() != null || f.GetComponentInParent<Hamburger>() is Hamburger h && h != f;
            var pos = f.transform.position;
            bool still = lastPos.TryGetValue(f, out var was) && (pos - was).sqrMagnitude < 0.0004f;
            lastPos[f] = pos;
            if (Held("loose " + f.GetInstanceID(), !owned && still, 5f))
                Break("food left lying about", $"{f.name} at {pos} parent={(f.transform.parent ? f.transform.parent.name : "none")}");
            if (float.IsNaN(pos.x) || pos.y < -5f) Break("food fell out of the world", $"{f.name} at {pos}");
        }

        // The chef.
        var agent = chef.Agent;
        if (agent != null)
        {
            if (Held("chef off mesh", agent.enabled && !agent.isOnNavMesh, 2f)) Break("chef off the navmesh", $"at {chef.transform.position}");
            bool wantsToGo = agent.isOnNavMesh && agent.hasPath && !agent.pathPending && agent.remainingDistance > 0.4f && !chef.IsStunned;
            bool stillChef = lastPos.TryGetValue(chef, out var c0) && Flat(c0, chef.transform.position) < 0.02f;
            lastPos[chef] = chef.transform.position;
            if (Held("chef stuck", wantsToGo && stillChef, 4f)) Break("chef stuck on his way", $"at {chef.transform.position}, {agent.remainingDistance:0.0} m to go");
        }

        // Mochi.
        if (mochi != null && mochi.isActiveAndEnabled)
        {
            var job = waiterJob.GetValue(mochi)?.ToString();
            bool stillMochi = lastPos.TryGetValue(mochi, out var m0) && Flat(m0, mochi.transform.position) < 0.02f;
            lastPos[mochi] = mochi.transform.position;
            bool errand = job == "Fetching" || job == "Delivering" || job == "GoingHome" || job == "Catching" || job == "Rummaging";
            var mnav = mochi.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (Held("mochi stuck", errand && stillMochi, 6f)) Break("Mochi stuck", $"job={job} at {mochi.transform.position} onMesh={mnav.isOnNavMesh} :: {NavReport(mnav.agentTypeID)}");
        }

        // Customers.
        foreach (var npc in NpcFsm.Active)
        {
            if (npc == null) continue;
            if (npc.executingNpcState == ExecutingNpcState.WAIT && npc.WaitedSeconds > npc.Patience + 15f)
                Break("customer waits past their patience", $"{npc.name} waited {npc.WaitedSeconds:0}s of {npc.Patience:0}");
            var nav = npc.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (nav != null && nav.enabled && !nav.isOnNavMesh) Break("customer off the navmesh", $"{npc.name} {npc.executingNpcState} at {npc.transform.position} :: {NavReport(nav.agentTypeID)}");
            bool walking = npc.executingNpcState == ExecutingNpcState.COME || npc.executingNpcState == ExecutingNpcState.GO;
            bool stillNpc = lastPos.TryGetValue(npc, out var n0) && Flat(n0, npc.transform.position) < 0.02f;
            lastPos[npc] = npc.transform.position;
            if (Held("npc stuck " + npc.GetInstanceID(), walking && stillNpc, 8f))
                Break("customer stuck walking", $"{npc.name} {npc.executingNpcState} at {npc.transform.position}");
        }
    }
    #endregion
}
