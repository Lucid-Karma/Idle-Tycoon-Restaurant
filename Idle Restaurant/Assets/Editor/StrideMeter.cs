using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// How fast a walk or run clip carries a character, in metres per second at that character's size: the speed a foot
// on the ground moves backward under the body. A character moving at any other speed slides ("ice skating"). Samples
// the clip on a hidden copy of the character, so the scene is not touched. Read by hand (scall StrideMeter Report).
public static class StrideMeter
{
    public static float LastCheck;
    public static string LastFeet;
    public static float Measure(GameObject character, AnimationClip clip)
    {
        var animator = character.GetComponentInChildren<Animator>();
        if (animator == null || clip == null) return -1f;
        var copy = Object.Instantiate(animator.gameObject);
        copy.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            float scale = animator.transform.lossyScale.y;
            var feet = copy.GetComponentsInChildren<Transform>().Where(t => t.name.ToLower().StartsWith("foot") || t.name.ToLower().StartsWith("toes")).ToArray();
            if (feet.Length == 0) return -2f;
            const int steps = 120;
            float dt = clip.length / steps;
            var speeds = new System.Collections.Generic.List<float>();
            foreach (var foot in feet)
            {
                var pos = new Vector3[steps + 1];
                for (int i = 0; i <= steps; i++)
                {
                    clip.SampleAnimation(copy, i * dt);
                    pos[i] = copy.transform.InverseTransformPoint(foot.position);
                }
                float low = pos.Min(p => p.y);
                // On the ground: within 1.5 cm (at the character's size) of the lowest the foot gets.
                for (int i = 1; i <= steps; i++)
                {
                    if (pos[i].y - low > 0.015f / scale || pos[i - 1].y - low > 0.015f / scale) continue;
                    var d = pos[i] - pos[i - 1];
                    speeds.Add(new Vector2(d.x, d.z).magnitude * scale / dt);
                }
            }
            if (speeds.Count == 0) return -3f;
            speeds.Sort();
            // Cross-check: a foot swings back and forth over its stride once a cycle, and is on the ground about half
            // of it, carried back over that stride: ground speed ~ 2 x swing / cycle.
            float swing = 0f;
            foreach (var foot in feet)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                for (int i = 0; i <= steps; i++) { clip.SampleAnimation(copy, i * dt); float z = copy.transform.InverseTransformPoint(foot.position).z; lo = Mathf.Min(lo, z); hi = Mathf.Max(hi, z); }
                swing = Mathf.Max(swing, hi - lo);
            }
            LastCheck = 2f * swing * scale / clip.length;
            LastFeet = string.Join(",", feet.Select(f => f.name));
            return speeds[speeds.Count / 2];
        }
        finally { Object.DestroyImmediate(copy); }
    }

    // Every walk/run clip in the project, measured on the first customer.
    public static string AllClips()
    {
        var sb = new StringBuilder("\n");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Project/[GAME]/Prefabs/CharacterPrefabs/NpcPrefabs/Npc 1.prefab");
        if (prefab == null) return "no Npc 1";
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip"))
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<AnimationClip>())
                {
                    string n = clip.name.ToLower();
                    if (clip.name.StartsWith("__preview") || !(n.Contains("walk") || n.Contains("run") || n.Contains("jog")) || !seen.Add(clip.name)) continue;
                    sb.Append($"{clip.name}: {Measure(inst, clip):0.00} m/s check {LastCheck:0.00} len {clip.length:0.00}\n");
                }
        }
        finally { Object.DestroyImmediate(inst); }
        return sb.ToString();
    }

    // The customers' walk: Walking_A (0.84 m/s a cycle) rather than Walking_C (0.55 m/s - at their 2 m/s it had to play
    // nearly four times over). NpcAnimationController plays it as fast as they move.
    public static string SetNpcWalk()
    {
        const string controllerPath = "Assets/Project/[GAME]/Animations/CharacterAnims/NpcAnims/NpcAnim.controller";
        var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        // From the same file as the customers' other clips (sitting, standing up): its bones are named as theirs.
        var walk = AssetDatabase.LoadAllAssetsAtPath("Assets/Models/KayKit_Skeletons_1.0_FREE/characters/fbx/Skeleton_Minion.fbx")
            .OfType<AnimationClip>().FirstOrDefault(c => c.name == "Walking_A");
        if (controller == null || walk == null) return "missing controller or Walking_A";
        foreach (var layer in controller.layers)
            foreach (var cs in layer.stateMachine.states)
                if (cs.state.name == "Walk") { cs.state.motion = walk; EditorUtility.SetDirty(controller); }
        AssetDatabase.SaveAssets();
        return "Walk = " + walk.name + " (" + AssetDatabase.GetAssetPath(walk) + ")";
    }

    // In play: follows a character's feet for some seconds, every frame, and reports how fast a planted foot (within
    // 2 cm of the lowest it gets) moves over the floor while the character moves - 0 when the feet do not slide.
    static Transform watched;
    static Transform[] watchedFeet;
    static readonly System.Collections.Generic.List<(float t, Vector3[] feet, float speed, Vector3 dir)> track = new();
    static double watchUntil;

    public static string Watch(string path, float seconds)
    {
        var go = GameObject.Find(path.Replace("%20", " "));
        if (go == null) return "no " + path;
        watched = go.transform;
        watchedFeet = go.GetComponentsInChildren<Transform>().Where(t => t.name == "foot.l" || t.name == "foot.r").ToArray();
        track.Clear();
        watchUntil = EditorApplication.timeSinceStartup + seconds;
        EditorApplication.update -= Record;
        EditorApplication.update += Record;
        return "watching " + watchedFeet.Length + " feet";
    }

    static void Record()
    {
        if (watched == null || EditorApplication.timeSinceStartup > watchUntil) { EditorApplication.update -= Record; return; }
        var agent = watched.GetComponentInParent<NavMeshAgent>();
        var v = agent != null ? agent.velocity : Vector3.zero;
        v.y = 0f;
        track.Add((Time.time, watchedFeet.Select(f => f.position).ToArray(), v.magnitude, v.sqrMagnitude > 0.01f ? v.normalized : Vector3.zero));
    }

    public static string Watched()
    {
        if (track.Count < 3) return "too few frames";
        var sb = new StringBuilder();
        for (int f = 0; f < watchedFeet.Length; f++)
        {
            float low = track.Min(r => r.feet[f].y);
            var slips = new System.Collections.Generic.List<float>();
            var signed = new System.Collections.Generic.List<float>();
            var moving = new System.Collections.Generic.List<float>();
            for (int i = 1; i < track.Count; i++)
            {
                if (track[i].speed < 0.5f) continue;
                moving.Add(track[i].speed);
                if (track[i].feet[f].y - low > 0.02f || track[i - 1].feet[f].y - low > 0.02f) continue;
                var d = track[i].feet[f] - track[i - 1].feet[f];
                float dt = track[i].t - track[i - 1].t;
                if (dt > 0f) { slips.Add(new Vector2(d.x, d.z).magnitude / dt); signed.Add(Vector3.Dot(d, track[i].dir) / dt); }
            }
            slips.Sort(); moving.Sort(); signed.Sort();
            float high = track.Max(r => r.feet[f].y);
            sb.Append($"{watchedFeet[f].name} lifts {high - low:0.00} m: planted-foot speed median {(slips.Count > 0 ? slips[slips.Count / 2] : -1f):0.00} m/s (along the walk {(signed.Count > 0 ? signed[signed.Count / 2] : 0f):+0.00;-0.00}: + = feet too slow) over {slips.Count} frames, body {(moving.Count > 0 ? moving[moving.Count / 2] : 0f):0.00} m/s | ");
        }
        return sb.ToString();
    }

    // Ground speed as distance over time across each whole ground contact (foot below lowest + threshold), for a few
    // thresholds: steadier than step-by-step speeds, which catch the landing.
    public static string Stance(string clipName, string fbx)
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(fbx.Replace("%20", " ")).OfType<AnimationClip>().FirstOrDefault(c => c.name == clipName);
        var player = GameObject.Find("Player");
        if (clip == null || player == null) return "missing";
        var animator = player.GetComponentInChildren<Animator>();
        var copy = Object.Instantiate(animator.gameObject);
        copy.hideFlags = HideFlags.HideAndDontSave;
        var sb = new StringBuilder();
        try
        {
            const int steps = 400;
            float dt = clip.length / steps;
            foreach (var name in new[] { "foot.l", "foot.r", "toes.l", "toes.r" })
            {
                var foot = copy.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == name);
                if (foot == null) continue;
                var pos = new Vector3[steps + 1];
                for (int i = 0; i <= steps; i++) { clip.SampleAnimation(copy, i * dt); pos[i] = copy.transform.InverseTransformPoint(foot.position); }
                float low = pos.Min(q => q.y);
                sb.Append(name + ":");
                foreach (float cm in new[] { 0.02f, 0.03f, 0.05f })
                {
                    float dist = 0f, time = 0f;
                    for (int i = 1; i <= steps; i++)
                        if (pos[i].y - low < cm && pos[i - 1].y - low < cm) { dist += Vector3.Distance(new Vector3(pos[i].x, 0, pos[i].z), new Vector3(pos[i - 1].x, 0, pos[i - 1].z)); time += dt; }
                    sb.Append($" {cm * 100:0}cm {(time > 0 ? dist / time : 0):0.00}m/s({time / clip.length * 100:0}%)");
                }
                sb.Append(" | ");
            }
        }
        finally { Object.DestroyImmediate(copy); }
        return sb.ToString();
    }

    // A FootProbe on the character at `path` for some seconds of play (every frame, unlike Watch).
    public static string Probe(string path, float seconds)
    {
        var go = GameObject.Find(path.Replace("%20", " "));
        if (go == null) return "no " + path;
        var animator = go.GetComponentInChildren<Animator>();
        var old = animator.GetComponent<FootProbe>();
        if (old != null) Object.DestroyImmediate(old);
        var probe = animator.gameObject.AddComponent<FootProbe>();
        probe.Until = Time.time + seconds;
        return "probing";
    }

    public static string ProbeResult(string path)
    {
        var go = GameObject.Find(path.Replace("%20", " "));
        var probe = go != null ? go.GetComponentInChildren<FootProbe>() : null;
        return probe != null ? probe.Result() : "no probe";
    }

    public static string ProbeDump(string path, string file)
    {
        var go = GameObject.Find(path.Replace("%20", " "));
        var probe = go != null ? go.GetComponentInChildren<FootProbe>() : null;
        return probe != null ? probe.Dump(file) : "no probe";
    }

    public static string Report()
    {
        var sb = new StringBuilder("\n");
        var player = GameObject.Find("Player");
        var run = AssetDatabase.FindAssets("Running_B t:AnimationClip").Select(g => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g)).OfType<AnimationClip>().FirstOrDefault(c => c.name == "Running_B")).FirstOrDefault(c => c != null);
        var walk = AssetDatabase.FindAssets("Walking_C t:AnimationClip").Select(g => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g)).OfType<AnimationClip>().FirstOrDefault(c => c.name == "Walking_C")).FirstOrDefault(c => c != null);
        if (player != null) sb.Append($"chef Running_B: {Measure(player, run):0.00} m/s check {LastCheck:0.00} feet {LastFeet} scale {player.GetComponentInChildren<Animator>().transform.lossyScale.y:0.00} (agent {player.GetComponent<NavMeshAgent>()?.speed:0.00}) len={run?.length:0.00}\n");
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab Npc", new[] { "Assets/Project/[GAME]/Prefabs/CharacterPrefabs/NpcPrefabs" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab.GetComponent<NpcFsm>() == null) continue;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.hideFlags = HideFlags.HideAndDontSave;
            try { sb.Append($"{prefab.name} Walking_C: {Measure(inst, walk):0.00} m/s check {LastCheck:0.00} feet {LastFeet} scale {inst.GetComponentInChildren<Animator>().transform.lossyScale.y:0.00} (agent {prefab.GetComponent<NavMeshAgent>()?.speed:0.00})\n"); }
            finally { Object.DestroyImmediate(inst); }
        }
        return sb.ToString();
    }
}
