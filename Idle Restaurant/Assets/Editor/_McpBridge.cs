// TEMPORARY — Claude test bridge (execute_code is broken in this project). Safe to delete.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
static class _McpBridge
{
    const string CmdFile = "Temp/_mcp_cmd.txt";
    const string OutFile = "Temp/_mcp_cmd_out.txt";
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static double next;

    static _McpBridge()
    {
        EditorApplication.update += Poll;
        Application.logMessageReceived += (msg, stack, type) =>
        {
            if (type == LogType.Exception || type == LogType.Error || (type == LogType.Warning && msg.StartsWith("[Highlight]")))
                File.AppendAllText("Temp/_mcp_errors.txt", $"[{DateTime.Now:HH:mm:ss}] [{type}] {msg}\n{stack}\n");
        };
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.EnteredPlayMode) { AudioListener.volume = 0f; Application.runInBackground = true; }
        };
    }

    // "trace <seconds>": the chef's movement every frame to Temp/_mcp_trace.txt.
    static double traceUntil;
    static void Trace()
    {
        if (!Application.isPlaying || EditorApplication.timeSinceStartup > traceUntil) return;
        var fsm = UnityEngine.Object.FindFirstObjectByType<PlayerFSM>();
        if (fsm == null) return;
        var a = fsm.Agent;
        File.AppendAllText("Temp/_mcp_trace.txt", string.Format(CultureInfo.InvariantCulture,
            "{0:0.000} f{1} pos={2} vel={3:0.00} rem={4:0.00} stop={5:0.00} pend={6} partial={7} st={8} dest={9} steer={10}\n",
            Time.time, Time.frameCount, V(fsm.transform.position), a.velocity.magnitude, a.remainingDistance, a.stoppingDistance,
            a.pathPending, a.pathStatus, fsm.executingState, V(a.destination), V(a.steeringTarget)));
    }

    static void Poll()
    {
        Trace();
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 0.2;
        if (!File.Exists(CmdFile)) return;
        string[] lines;
        try { lines = File.ReadAllLines(CmdFile); File.Delete(CmdFile); } catch { return; }
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            string res;
            try { res = Run(line); } catch (Exception e) { res = "ERR " + e.GetBaseException().GetType().Name + ": " + e.GetBaseException().Message + " @ " + string.Join(" <- ", (e.GetBaseException().StackTrace ?? "").Split('\n').Take(4).Select(x => x.Trim())); }
            File.AppendAllText(OutFile, line + " => " + res + "\n");
        }
        File.AppendAllText(OutFile, "##END\n");
    }

    static Type T(string name) =>
        AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t != null)
        ?? throw new Exception("type " + name);

    // Finds a scene object by path ("Level/Selectables/oven"), including inactive ones.
    static GameObject FindPath(string path)
    {
        var parts = path.Replace("%20", " ").Split('/');
        for (int s = 0; s < SceneManager.sceneCount; s++)
            foreach (var root in SceneManager.GetSceneAt(s).GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;
                Transform cur = root.transform;
                for (int i = 1; i < parts.Length && cur != null; i++)
                    cur = cur.Cast<Transform>().FirstOrDefault(c => c.name == parts[i]);
                if (cur != null) return cur.gameObject;
            }
        throw new Exception("no object " + path);
    }

    static object Target(string spec)
    {
        if (spec.Contains(":"))
        {
            var parts = spec.Split(':');
            var go = FindPath(parts[0]);
            return go.GetComponent(T(parts[1])) ?? throw new Exception("no component " + parts[1]);
        }
        int idx = 0;
        if (spec.Contains("#")) { idx = int.Parse(spec.Split('#')[1]); spec = spec.Split('#')[0]; }
        var objs = UnityEngine.Object.FindObjectsByType(T(spec), FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(o => o.name).ThenBy(o => o.GetInstanceID()).ToArray();
        if (idx >= objs.Length) throw new Exception($"only {objs.Length} {spec}");
        return objs[idx];
    }

    static object Member(object target, string path)
    {
        object cur = target;
        foreach (var name in path.Split('.'))
        {
            var t = cur is Type st ? st : cur.GetType();
            var inst = cur is Type ? null : cur;
            var f = FindField(t, name);
            if (f != null) { cur = f.GetValue(inst); continue; }
            var p = FindProp(t, name);
            if (p != null) { cur = p.GetValue(inst); continue; }
            throw new Exception("no member " + name + " on " + t.Name);
        }
        return cur;
    }

    static FieldInfo FindField(Type t, string n) { for (; t != null; t = t.BaseType) { var f = t.GetField(n, All); if (f != null) return f; } return null; }
    static PropertyInfo FindProp(Type t, string n) { for (; t != null; t = t.BaseType) { var p = t.GetProperty(n, All); if (p != null) return p; } return null; }
    static MethodInfo FindMethod(Type t, string n, int argc) { for (; t != null; t = t.BaseType) { var m = t.GetMethods(All).FirstOrDefault(x => x.Name == n && x.GetParameters().Length == argc); if (m != null) return m; } return null; }

    static object Parse(string s, Type t)
    {
        if (t == typeof(float)) return float.Parse(s, CultureInfo.InvariantCulture);
        if (t == typeof(int)) return int.Parse(s);
        if (t == typeof(bool)) return bool.Parse(s);
        if (t == typeof(string)) return s;
        if (t == typeof(Vector3)) { var v = s.Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray(); return new Vector3(v[0], v[1], v[2]); }
        if (t.IsEnum) return Enum.Parse(t, s);
        if (t == typeof(Color) && ColorUtility.TryParseHtmlString(s, out var color)) return color;
        throw new Exception("cannot parse " + t.Name);
    }

    static string Show(object o)
    {
        if (o == null) return "null";
        if (o is UnityEngine.Object uo) return uo ? uo.name + "(" + uo.GetType().Name + ")" : "destroyed";
        if (o is float f) return f.ToString("0.###", CultureInfo.InvariantCulture);
        if (o is Vector3 v) return V(v);
        if (o is System.Collections.IEnumerable e && !(o is string)) return "[" + string.Join(", ", e.Cast<object>().Select(Show)) + "]";
        return o.ToString();
    }

    static string V(Vector3 v) => string.Format(CultureInfo.InvariantCulture, "({0:0.##},{1:0.##},{2:0.##})", v.x, v.y, v.z);

    static void Dump(Transform t, int depth, int max, StringBuilder sb)
    {
        var go = t.gameObject;
        var comps = go.GetComponents<Component>().Where(c => c != null && !(c is Transform)).Select(c => c.GetType().Name).ToList();
        var mf = go.GetComponent<MeshFilter>();
        var smr = go.GetComponent<SkinnedMeshRenderer>();
        var r = go.GetComponent<Renderer>();
        string mesh = mf && mf.sharedMesh ? " mesh=" + mf.sharedMesh.name : smr && smr.sharedMesh ? " smesh=" + smr.sharedMesh.name : "";
        string mats = r ? " mats=" + string.Join(",", r.sharedMaterials.Select(m => m ? m.name : "null")) : "";
        string pf = PrefabUtility.IsAnyPrefabInstanceRoot(go) ? " prefab=" + Path.GetFileName(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go)) : "";
        sb.Append(new string(' ', depth * 2)).Append(go.name).Append(go.activeSelf ? "" : " (off)").Append(go.isStatic ? " [S]" : "")
          .Append(" pos=").Append(V(t.position)).Append(" rot=").Append(V(t.eulerAngles)).Append(" scl=").Append(V(t.lossyScale))
          .Append(" {").Append(string.Join(",", comps)).Append("}").Append(mesh).Append(mats).Append(pf).Append('\n');
        if (depth >= max) { if (t.childCount > 0) sb.Append(new string(' ', depth * 2 + 2)).Append("… ").Append(t.childCount).Append(" children\n"); return; }
        foreach (Transform c in t) Dump(c, depth + 1, max, sb);
    }

    static Vector3 ScreenPointOf(Component c)
    {
        if (c is NonStackBase utensil && utensil.Food != null && !utensil.GetComponent<Collider>().enabled) c = utensil.Food;
        var col = c.GetComponentsInChildren<Collider>().FirstOrDefault(x => x.enabled)
                  ?? c.GetComponentsInChildren<Collider>().FirstOrDefault(x => x.bounds.size != Vector3.zero);
        var p = col ? col.bounds.center : c.transform.position + (c is NpcFsm ? Vector3.up : Vector3.zero);
        return Camera.main.WorldToScreenPoint(p);
    }

    static string Run(string line)
    {
        var p = line.Split(' ');
        switch (p[0])
        {
            case "get": return Show(Member(Target(p[1]), p[2]));
            case "getall":
                return string.Join(" | ", UnityEngine.Object.FindObjectsByType(T(p[1]), FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .OrderBy(o => o.name).Select(o => o.name + "=" + Show(Member(o, p[2]))));
            case "static": return Show(Member(T(p[1]), p[2]));
            case "call":
            {
                object target = Target(p[1]);
                var m = FindMethod(target.GetType(), p[2], p.Length - 3) ?? throw new Exception("no method " + p[2]);
                var args = m.GetParameters().Select((x, i) => Parse(p[3 + i], x.ParameterType)).ToArray();
                return Show(m.Invoke(target, args));
            }
            case "tree":
            {
                var sb = new StringBuilder("\n");
                Dump(FindPath(p[1].Replace("%20", " ")).transform, 0, p.Length > 2 ? int.Parse(p[2]) : 2, sb);
                return sb.ToString();
            }
            case "ptree":
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p[1].Replace("%20", " "));
                var sb = new StringBuilder("\n");
                Dump(go.transform, 0, p.Length > 2 ? int.Parse(p[2]) : 3, sb);
                return sb.ToString();
            }
            case "fbx":
            {
                string path = p[1].Replace("%20", " ");
                var sb = new StringBuilder("\n");
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp) sb.Append($"animType={imp.animationType} avatarSetup={imp.avatarSetup} importAnim={imp.importAnimation} scale={imp.globalScale} useFileScale={imp.useFileScale}\n");
                foreach (var c in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")))
                    sb.Append($"  clip {c.name} len={c.length:0.00} loop={c.isLooping}\n");
                foreach (var a in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>())
                    sb.Append($"  avatar {a.name} human={a.isHuman} valid={a.isValid}\n");
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root) Dump(root.transform, 0, p.Length > 2 ? int.Parse(p[2]) : 3, sb);
                return sb.ToString();
            }
            case "clipbind":
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(p[1].Replace("%20", " ")).OfType<AnimationClip>().First(c => c.name == p[2]);
                var b = AnimationUtility.GetCurveBindings(clip);
                return b.Length + " curves: " + string.Join(" ; ", b.Select(x => x.path).Distinct().Take(p.Length > 3 ? int.Parse(p[3]) : 12));
            }
            case "mats":
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(p[1].Replace("%20", " "));
                return string.Join(" | ", root.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct()
                    .Select(m => m == null ? "null" : $"{m.name}:{m.shader.name}:{(m.mainTexture ? m.mainTexture.name : "-")}:{AssetDatabase.GetAssetPath(m)}"));
            }
            case "buildwebgl":
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = p[1].Replace("%20", " "),
                    target = BuildTarget.WebGL,
                    options = BuildOptions.CleanBuildCache,
                });
                File.WriteAllText("Temp/_mcp_build.txt", report.summary.result + " " + report.summary.totalSize + " errors=" + report.summary.totalErrors);
                return report.summary.result.ToString();
            }
            case "throwfrom":
            {
                var npc = NpcFsm.Active.First(n => n.executingNpcState == ExecutingNpcState.WAIT);
                FindField(typeof(NpcFsm), "kind").SetValue(npc, Enum.Parse(typeof(CustomerKind), p[1]));
                FoodFight.Instance.CustomerThrows(npc);
                return npc.name + " " + npc.Kind;
            }
            case "meshbounds":
            {
                var mf = AssetDatabase.LoadAssetAtPath<GameObject>(p[1].Replace("%20", " ")).GetComponentInChildren<MeshFilter>();
                var b = mf.sharedMesh.bounds;
                return $"center={V(b.center)} size={V(b.size)} rot={V(mf.transform.localEulerAngles)} scl={V(mf.transform.localScale)}";
            }
            case "anybounds":
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p[1].Replace("%20", " "));
                var sb = new StringBuilder();
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    var mesh = r is SkinnedMeshRenderer sk ? sk.sharedMesh : r.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null) continue;
                    var b = mesh.bounds;
                    sb.Append($"{r.name}: center={b.center.ToString("F4")} size={b.size.ToString("F4")} rot={V(r.transform.localEulerAngles)} scl={V(r.transform.localScale)} root={V(go.transform.localEulerAngles)} | ");
                }
                return sb.ToString();
            }
            case "setasset":
            {
                // setasset <Type> <field> <asset path>: point a scene component's serialized field at an asset
                var comp = (Component)UnityEngine.Object.FindFirstObjectByType(T(p[1]), FindObjectsInactive.Include);
                var so = new SerializedObject(comp);
                so.FindProperty(p[2]).objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p[3].Replace("%20", " "));
                so.ApplyModifiedProperties();
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
                return comp.name + "." + p[2] + " = " + so.FindProperty(p[2]).objectReferenceValue;
            }
            case "servenpc":
            {
                // tap the customer who has waited longest (and is still waiting)
                var npc = NpcFsm.Active.Where(n => n.executingNpcState == ExecutingNpcState.WAIT)
                    .OrderByDescending(n => (float)typeof(NpcFsm).GetField("waitingTimer", All).GetValue(n)).FirstOrDefault();
                if (npc == null) return "none";
                UnityEngine.Object.FindFirstObjectByType<PlayerFSM>().HandleScreenTap(Camera.main.WorldToScreenPoint(npc.transform.position + Vector3.up));
                return npc.name + " " + npc.Kind;
            }
            case "coach":
            {
                if (Tutorial.Instance == null) return "none";
                var card = (UICoachCard)FindField(typeof(Tutorial), "card").GetValue(Tutorial.Instance);
                card.OnPointerClick(null);
                return "ok";
            }
            case "slot":
            {
                // the food sitting in a station's slot: slot "OvenStuff%20FoodSlot"
                var name = p[1].Replace("%20", " ");
                var go = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t => t.name == name);
                if (go == null) return "no slot";
                var food = go.GetComponentsInChildren<EdibleBase>().FirstOrDefault();
                if (food == null) return "empty";
                return $"{food.name} prep={food.Preparation} sliced={(food is CuttableBase c && c.isSliced)}";
            }
            case "state":
            {
                var sm = ScoreManager.Instance;
                var chef = UnityEngine.Object.FindFirstObjectByType<PlayerFSM>();
                var npcs = string.Join(",", NpcFsm.Active.Select(n => $"{n.Kind}:{n.executingNpcState}:{(float)typeof(NpcFsm).GetField("waitingTimer", All).GetValue(n):0}/{n.Patience:0}"));
                var plates = string.Join(",", UnityEngine.Object.FindObjectsByType<Plate>(FindObjectsSortMode.None).OrderBy(x => x.name).Select(x => x.FinishedBurger != null ? "B" : x.LayerCount.ToString()));
                return $"t={Time.time:0} ts={Time.timeScale} served={sm.ServedCount} walkouts={sm.WalkoutCount} hosted={sm.HostedCustomerCount}/{sm.CustomersPerLevel} earned={sm.ShiftEarned} stars={sm.ShiftStars} streak={sm.RushStreak} held={(chef.HeldFood ? chef.HeldFood.name : "-")} plates={plates} npcs=[{npcs}]";
            }
            case "nouv2":
                return string.Join(" | ", UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                    .Where(r => r.lightmapIndex >= 0 && r.lightmapIndex < 0xFFFE)
                    .Select(r => (r, m: r.GetComponent<MeshFilter>()?.sharedMesh))
                    .Where(x => x.m != null && (x.m.uv2 == null || x.m.uv2.Length == 0))
                    .Select(x => x.r.name + "[" + x.m.name + "]"));
            case "set":
            {
                var target = Target(p[1]);
                var f = FindField(target.GetType(), p[2]);
                f.SetValue(target, Parse(p[3], f.FieldType));
                return "ok";
            }
            case "voices":
                return string.Join(" | ", UnityEngine.Object.FindFirstObjectByType<GameSfx>().GetComponents<AudioSource>().Select(a => a.isPlaying + ":" + a.pitch.ToString("0.00", CultureInfo.InvariantCulture)));
            case "animstate":
            {
                var an = FindPath(p[1]).GetComponent<Animator>();
                var info = an.GetCurrentAnimatorStateInfo(0);
                var clips = an.GetCurrentAnimatorClipInfo(0);
                return string.Join(",", clips.Select(c => c.clip.name)) + " t=" + info.normalizedTime.ToString("0.00", CultureInfo.InvariantCulture);
            }
            case "animctrl":
            {
                var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(p[1].Replace("%20", " "));
                var sb = new StringBuilder("\n params: " + string.Join(",", ac.parameters.Select(x => x.name + ":" + x.type)) + "\n");
                foreach (var layer in ac.layers)
                {
                    var sm = layer.stateMachine;
                    sb.Append($" layer {layer.name} default={sm.defaultState?.name}\n");
                    foreach (var t in sm.anyStateTransitions)
                        sb.Append($"  ANY -> {t.destinationState?.name} [{string.Join(",", t.conditions.Select(c => c.parameter + " " + c.mode))}] dur={t.duration}\n");
                    foreach (var cs in sm.states)
                    {
                        var s = cs.state;
                        sb.Append($"  state {s.name} motion={(s.motion ? s.motion.name : "-")} speed={s.speed} loop={(s.motion is AnimationClip c0 ? c0.isLooping : false)}\n");
                        foreach (var t in s.transitions)
                            sb.Append($"   -> {(t.isExit ? "EXIT" : t.destinationState?.name)} [{string.Join(",", t.conditions.Select(c => c.parameter + " " + c.mode))}] exit={t.hasExitTime}@{t.exitTime} dur={t.duration}\n");
                        foreach (var b in s.behaviours) sb.Append($"   behaviour {b.GetType().Name}\n");
                    }
                }
                return sb.ToString();
            }
            case "path":
            {
                var o = Member(Target(p[1]), p[2]);
                var tr = o is GameObject g ? g.transform : ((Component)o).transform;
                string s = tr.name;
                for (var x = tr.parent; x != null; x = x.parent) s = x.name + "/" + s;
                return s;
            }
            case "clipevents":
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(p[1].Replace("%20", " ")).OfType<AnimationClip>().First(c => c.name == p[2]);
                return string.Join(" ; ", AnimationUtility.GetAnimationEvents(clip).Select(e => e.functionName + "@" + e.time));
            }
            case "trace":
                File.Delete("Temp/_mcp_trace.txt");
                traceUntil = EditorApplication.timeSinceStartup + float.Parse(p[1], CultureInfo.InvariantCulture);
                return "tracing";
            case "scall":
            {
                var t = T(p[1]);
                var m = FindMethod(t, p[2], p.Length - 3) ?? throw new Exception("no method " + p[2]);
                var args = m.GetParameters().Select((x, i) => Parse(p[3 + i], x.ParameterType)).ToArray();
                return Show(m.Invoke(null, args));
            }
            case "buy":
            {
                var shop = UnityEngine.Object.FindFirstObjectByType<CafeShop>();
                var u = shop.Upgrades.First(x => x.id == p[1]);
                return shop.Buy(u) + " state=" + shop.StateOf(u);
            }
            case "rect":
            {
                var sb = new StringBuilder("\n");
                void R(Transform t, int d)
                {
                    var rt = t as RectTransform;
                    var extra = "";
                    var img = t.GetComponent<UnityEngine.UI.Image>();
                    if (img) extra += $" img={(img.sprite ? img.sprite.name : "-")} col=#{ColorUtility.ToHtmlStringRGBA(img.color)} type={img.type} ppu={img.pixelsPerUnitMultiplier:0.##}";
                    var tmp = t.GetComponent<TMPro.TMP_Text>();
                    if (tmp) extra += $" text='{tmp.text}' font={tmp.font?.name} size={tmp.fontSize} col=#{ColorUtility.ToHtmlStringRGBA(tmp.color)} align={tmp.alignment} cs={tmp.characterSpacing}";
                    if (rt) sb.Append(new string(' ', d * 2)).Append($"{t.name}{(t.gameObject.activeSelf ? "" : " (off)")} amin={rt.anchorMin} amax={rt.anchorMax} piv={rt.pivot} pos={rt.anchoredPosition} size={rt.sizeDelta}{extra}\n");
                    if (d < (p.Length > 2 ? int.Parse(p[2]) : 1)) foreach (Transform c in t) R(c, d + 1);
                }
                R(FindPath(p[1]).transform, 0);
                return sb.ToString();
            }
            case "active":
                FindPath(p[1]).SetActive(bool.Parse(p[2]));
                return "ok";
            case "activeall":
                foreach (Transform t in FindPath(p[1]).transform) t.gameObject.SetActive(bool.Parse(p[2]));
                return "ok";
            case "bounds":
            {
                var rs = FindPath(p[1]).GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
                var b = rs[0].bounds;
                foreach (var r in rs) b.Encapsulate(r.bounds);
                return $"min={V(b.min)} max={V(b.max)}";
            }
            case "w2s":
            {
                var v = p[1].Split(',').Select(x => float.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                var sp = Camera.main.WorldToScreenPoint(new Vector3(v[0], v[1], v[2]));
                return $"{sp.x:0},{sp.y:0} of {Screen.width}x{Screen.height} cam={Camera.main.pixelWidth}x{Camera.main.pixelHeight}";
            }
            case "headbounds":
            {
                // Bounds of a skinned piece in the space of one of its bones (bind pose): fbx path, mesh name, bone.
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(p[1].Replace("%20", " "));
                var smr = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).First(s => s.name == p[2]);
                int bi = Array.FindIndex(smr.bones, b => b.name == p[3]);
                var bp = smr.sharedMesh.bindposes[bi];
                var verts = smr.sharedMesh.vertices.Select(x => bp.MultiplyPoint3x4(x)).ToArray();
                var b = new Bounds(verts[0], Vector3.zero);
                foreach (var x in verts) b.Encapsulate(x);
                var w = smr.sharedMesh.boneWeights;
                int other = w.Count(x => x.boneIndex0 != bi || x.weight0 < 0.99f);
                return $"center={V(b.center)} size={V(b.size)} bones=[{string.Join(",", smr.bones.Select(x => x.name))}] notHead={other}/{w.Length}";
            }
            case "globalfloat": return Shader.GetGlobalFloat(p[1]).ToString("0.###", CultureInfo.InvariantCulture);
            case "click":
                FindPath(p[1]).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                return "ok";
            case "tap":
                UnityEngine.Object.FindFirstObjectByType<PlayerFSM>().HandleScreenTap(new Vector3(float.Parse(p[1], CultureInfo.InvariantCulture), float.Parse(p[2], CultureInfo.InvariantCulture)));
                return "ok";
            case "tapon":
            {
                var c = (Component)Target(p[1]);
                var sp = ScreenPointOf(c);
                UnityEngine.Object.FindFirstObjectByType<PlayerFSM>().HandleScreenTap(sp);
                return $"at {sp.x:0},{sp.y:0}";
            }
            case "src":
            {
                var src = UnityEngine.Object.FindObjectsByType<IngredientsSource>(FindObjectsSortMode.None)
                    .First(s => ((GameObject)FindField(typeof(IngredientsSource), "spawnPrefab").GetValue(s)).name.ToLower().Contains(p[1].ToLower()));
                UnityEngine.Object.FindFirstObjectByType<PlayerFSM>().HandleScreenTap(ScreenPointOf(src));
                return src.name;
            }
            case "npcscreen":
                return string.Join(" | ", UnityEngine.Object.FindObjectsByType<NpcFsm>(FindObjectsSortMode.None).Select(n =>
                {
                    var sp = Camera.main.WorldToScreenPoint(n.transform.position + Vector3.up);
                    return $"{n.name} {n.executingNpcState} head={sp.x:0},{sp.y:0} chair={(n.chair as Component)?.name}";
                }));
            case "timescale": Time.timeScale = float.Parse(p[1], CultureInfo.InvariantCulture); return "ok";
            case "shot":
                ScreenCapture.CaptureScreenshot(p[1], p.Length > 2 ? int.Parse(p[2]) : 1);
                return "ok";
            case "inputhandler":
            {
                // 0 = old input manager, 1 = Input System package, 2 = both
                var settings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
                var so = new SerializedObject(settings);
                var prop = so.FindProperty("activeInputHandler");
                if (p.Length > 1) { prop.intValue = int.Parse(p[1]); so.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }
                return "activeInputHandler=" + prop.intValue;
            }
            case "inputall":
                UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode =
                    UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                return "ok";
            case "keys":
            {
                // keys W D  |  keys none
                var pressed = p.Skip(1).Where(k => k != "none").Select(k => (UnityEngine.InputSystem.Key)Enum.Parse(typeof(UnityEngine.InputSystem.Key), k)).ToArray();
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Keyboard.current, new UnityEngine.InputSystem.LowLevel.KeyboardState(pressed));
                return "ok";
            }
            case "touch":
            {
                // touch Began|Moved|Ended x y
                var screen = UnityEngine.InputSystem.Touchscreen.current ?? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
                var phase = (UnityEngine.InputSystem.TouchPhase)Enum.Parse(typeof(UnityEngine.InputSystem.TouchPhase), p[1]);
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(screen, new UnityEngine.InputSystem.LowLevel.TouchState
                {
                    touchId = 1, phase = phase,
                    position = new Vector2(float.Parse(p[2], CultureInfo.InvariantCulture), float.Parse(p[3], CultureInfo.InvariantCulture))
                });
                return "ok";
            }
            case "notouch":
            {
                var screen = UnityEngine.InputSystem.Touchscreen.current;
                if (screen != null) UnityEngine.InputSystem.InputSystem.RemoveDevice(screen);
                return "ok";
            }
            case "reopen":
                EditorApplication.delayCall += () => EditorApplication.OpenProject(Directory.GetCurrentDirectory());
                return "restarting";
            default: return "unknown";
        }
    }
}
