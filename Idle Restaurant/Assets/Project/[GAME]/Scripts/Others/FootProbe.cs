#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

// Editor-only measuring tool (not in builds): follows a character's feet every frame while it walks or runs, and works
// out how far its planted foot slides along the way it moves. Added and read through the editor bridge
// (StrideMeter.Probe / StrideMeter.ProbeResult) to tune a walk/run cycle's stride so the feet stay put.
public class FootProbe : MonoBehaviour
{
    private readonly List<(float t, Vector3 l, Vector3 r, Vector3 v)> rows = new();
    private Transform footL, footR;
    private NavMeshAgent agent;
    public float Until;
    private Vector3 last;
    private bool hasLast;

    private void Start()
    {
        footL = GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "foot.l");
        footR = GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "foot.r");
        agent = GetComponentInParent<NavMeshAgent>();
    }

    private void LateUpdate()
    {
        if (footL == null || Time.time > Until) return;
        // The body's own velocity from where it moved (a NavMeshAgent's velocity is not set when code moves it).
        var root = agent != null ? agent.transform : transform;
        var v = Time.deltaTime > 0f && hasLast ? (root.position - last) / Time.deltaTime : Vector3.zero;
        v.y = 0f;
        last = root.position; hasLast = true;
        rows.Add((Time.time, footL.position, footR.position, v));
    }

    // Every frame as CSV (t, left foot xyz, right foot xyz, body velocity xz), for analysis outside.
    public string Dump(string path)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;
        System.IO.File.WriteAllLines(path, rows.Select(r => string.Join(",", new[] { r.t, r.l.x, r.l.y, r.l.z, r.r.x, r.r.y, r.r.z, r.v.x, r.v.z }.Select(f => f.ToString("0.#####", ci)))));
        return rows.Count + " rows";
    }

    // Per foot: median slide along the motion while planted (+ = the foot is carried forward: the cycle is too slow),
    // the body's median speed, and the number of planted frames.
    public string Result()
    {
        var sb = new System.Text.StringBuilder();
        foreach (bool left in new[] { true, false })
        {
            var ys = rows.Select(r => (left ? r.l : r.r).y).ToList();
            if (ys.Count < 3) return "too few frames";
            float low = ys.Min();
            var slide = new List<float>();
            var speed = new List<float>();
            for (int i = 1; i < rows.Count; i++)
            {
                var a = left ? rows[i - 1].l : rows[i - 1].r;
                var b = left ? rows[i].l : rows[i].r;
                float dt = rows[i].t - rows[i - 1].t;
                float v = rows[i].v.magnitude;
                if (dt <= 0f || v < 1f) continue;
                speed.Add(v);
                if (a.y - low > 0.02f || b.y - low > 0.02f) continue;
                slide.Add(Vector3.Dot(b - a, rows[i].v / v) / dt);
            }
            slide.Sort(); speed.Sort();
            sb.Append($"{(left ? "L" : "R")}: slide {(slide.Count > 0 ? slide[slide.Count / 2] : 0f):+0.00;-0.00} m/s ({slide.Count} frames) body {(speed.Count > 0 ? speed[speed.Count / 2] : 0f):0.00} | ");
        }
        return sb.ToString();
    }
}
#endif
