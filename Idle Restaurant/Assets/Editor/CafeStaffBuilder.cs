using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The waiter: a small round cat, built here out of low-poly balls and cones rather than imported, so it
// matches the KayKit facets without a rig. It has no skeleton at all — Waiter.cs hops it along, which is
// cuter than a walk cycle and costs a few transform writes a frame.
// Three submeshes (fur, ink, pink) so the body is one renderer; the tail is a second, small one of its own
// so that it can wag (Waiter.Hop turns it about its root).
public static partial class CafeGrowthBuilder
{
    const string WaiterMesh = MeshDir + "Waiter.asset";

    [MenuItem("Tools/Chibi Cafe/9 Build the Waiter")]
    public static void BuildWaiter()
    {
        var level = GameObject.Find("Level").transform;
        var upgrades = level.Find("Upgrades");
        var old = upgrades.Find("Upgrade Waiter");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var group = new GameObject("Upgrade Waiter").transform;
        group.SetParent(upgrades, false);

        var go = new GameObject("Waiter");
        go.transform.SetParent(group, false);
        var body = new GameObject("Cat");
        body.transform.SetParent(go.transform, false);
        var tail = FitBody(body);

        // The tray he carries the burger on, and the spot the burger rides.
        var tray = new GameObject("Tray");
        tray.transform.SetParent(go.transform, false);
        tray.transform.localPosition = TrayAt;
        Get<MeshFilter>(tray).sharedMesh = SaveMesh(TrayMesh());
        Get<MeshRenderer>(tray).sharedMaterial = FlatMaterial("WaiterTray", UITokens.Colors.Cream);
        var hold = new GameObject("Hold");
        hold.transform.SetParent(tray.transform, false);
        hold.transform.localPosition = new Vector3(0f, 0.03f, 0f);

        // He walks where the chef walks: the chef's mesh covers the kitchen and the dining room, the
        // customers' only covers the dining room.
        var chef = Object.FindFirstObjectByType<PlayerFSM>().GetComponent<UnityEngine.AI.NavMeshAgent>();
        var agent = Get<UnityEngine.AI.NavMeshAgent>(go);
        agent.agentTypeID = chef.agentTypeID;
        agent.radius = 0.35f;
        agent.height = 1.4f;
        // Speed, acceleration and how close he stops are set by Waiter itself (they are tuned by feel).
        agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        var waiter = Get<Waiter>(go);
        var so = new SerializedObject(waiter);
        so.FindProperty("cat").objectReferenceValue = body.transform;
        so.FindProperty("tail").objectReferenceValue = tail;
        so.FindProperty("tray").objectReferenceValue = tray.transform;
        so.FindProperty("hold").objectReferenceValue = hold.transform;
        so.FindProperty("homePoint").vector3Value = WaiterHome;
        so.ApplyModifiedPropertiesWithoutUndo();

        go.transform.position = WaiterHome;
        // He was built at the size of a cat, which is about a third of the chef: barely visible from the
        // game camera. The whole waiter (body, tray, the spot a burger rides) scales together.
        go.transform.localScale = Vector3.one * WaiterSize;
        group.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(level.gameObject.scene);
        Debug.Log("[CafeGrowth] built the waiter");
    }

    // Gives the Waiter that is already in the scene a new body (and a tail that wags) without rebuilding him:
    // rebuilding goes through "3 Place Upgrades", which rewrites the whole shop catalogue and its icons.
    [MenuItem("Tools/Chibi Cafe/9b Refit the Waiter's Body")]
    public static void RefitWaiterBody()
    {
        var waiter = Object.FindFirstObjectByType<Waiter>(FindObjectsInactive.Include);
        if (waiter == null) { Debug.LogError("[CafeGrowth] no Waiter in the scene: run 3 Place Upgrades first"); return; }
        var so = new SerializedObject(waiter);
        var body = (Transform)so.FindProperty("cat").objectReferenceValue;
        so.FindProperty("tail").objectReferenceValue = FitBody(body.gameObject);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(waiter.gameObject.scene);
        Debug.Log("[CafeGrowth] refitted the waiter's body");
    }

    // The body mesh and its three colours, and the tail as its own piece pivoting at its root.
    static Transform FitBody(GameObject body)
    {
        Get<MeshFilter>(body).sharedMesh = SaveMesh(CatMesh());
        var fur = FlatMaterial("WaiterFur", UITokens.Colors.Cream);
        Get<MeshRenderer>(body).sharedMaterials = new[]
        {
            fur,
            FlatMaterial("WaiterInk", UITokens.Colors.Ink),
            FlatMaterial("WaiterPink", UITokens.Colors.Pink),
        };

        var existing = body.transform.Find("Tail");
        var tail = existing != null ? existing.gameObject : new GameObject("Tail");
        tail.transform.SetParent(body.transform, false);
        tail.transform.localPosition = TailRoot;
        tail.transform.localRotation = Quaternion.identity;
        Get<MeshFilter>(tail).sharedMesh = SaveMesh(TailMesh());
        Get<MeshRenderer>(tail).sharedMaterial = fur;
        return tail.transform;
    }

    // Where he waits between orders: the end of the kitchen pass, out of the chef's way.
    static readonly Vector3 WaiterHome = new Vector3(7.6f, 1.5f, 24.2f);
    // Measured against the chef (about 2.6 high with his hair): a cat at 1.9x was 1.45, barely over half of
    // him, and the user could hardly see him. 2.4x makes him about 1.8, a little under three quarters.
    const float WaiterSize = 2.4f;
    static readonly Vector3 TrayAt = new Vector3(0f, 0.34f, 0.3f);

    #region The cat
    // fur, ink, pink
    class Parts
    {
        public readonly List<Vector3>[] Verts = { new(), new(), new() };
        public readonly List<int>[] Tris = { new(), new(), new() };

        public void Ball(int part, Vector3 at, Vector3 radius, int segments = 10, int rings = 6)
        {
            var (v, t) = (Verts[part], Tris[part]);
            Vector3 P(int ring, int seg)
            {
                float phi = Mathf.PI * ring / rings;                 // 0 at the top
                float theta = 2f * Mathf.PI * seg / segments;
                return at + new Vector3(Mathf.Sin(phi) * Mathf.Sin(theta) * radius.x,
                                        Mathf.Cos(phi) * radius.y,
                                        Mathf.Sin(phi) * Mathf.Cos(theta) * radius.z);
            }
            for (int ring = 0; ring < rings; ring++)
                for (int seg = 0; seg < segments; seg++)
                    Quad(v, t, P(ring, seg), P(ring + 1, seg), P(ring + 1, seg + 1), P(ring, seg + 1));
        }

        public void Cone(int part, Vector3 bottom, Vector3 tip, float radius, int segments = 8)
        {
            var (v, t) = (Verts[part], Tris[part]);
            var up = (tip - bottom).normalized;
            var side = Vector3.Cross(up, Mathf.Abs(up.z) < 0.9f ? Vector3.forward : Vector3.right).normalized;
            var other = Vector3.Cross(up, side);
            Vector3 Rim(int i)
            {
                float a = 2f * Mathf.PI * i / segments;
                return bottom + (side * Mathf.Cos(a) + other * Mathf.Sin(a)) * radius;
            }
            for (int i = 0; i < segments; i++)
            {
                Tri(v, t, Rim(i), tip, Rim(i + 1));
                Tri(v, t, bottom, Rim(i), Rim(i + 1));      // the cap, so it is closed from below
            }
        }

        public void Box(int part, Vector3 at, Vector3 size)
        {
            var (v, t) = (Verts[part], Tris[part]);
            var e = size * 0.5f;
            Vector3 C(int i) => at + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
            Quad(v, t, C(0), C(2), C(3), C(1));   // back
            Quad(v, t, C(5), C(7), C(6), C(4));   // front
            Quad(v, t, C(4), C(6), C(2), C(0));   // left
            Quad(v, t, C(1), C(3), C(7), C(5));   // right
            Quad(v, t, C(2), C(6), C(7), C(3));   // top
            Quad(v, t, C(4), C(0), C(1), C(5));   // bottom
        }

        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
        }

        static void Tri(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            t.AddRange(new[] { i, i + 1, i + 2 });
        }

        // One mesh, one submesh per colour that was actually used, in order: fur, ink, pink.
        public Mesh Build(string name)
        {
            var used = Enumerable.Range(0, 3).Where(i => Tris[i].Count > 0).ToArray();
            var mesh = new Mesh { name = name, subMeshCount = used.Length };
            var all = new List<Vector3>();
            var offsets = new int[3];
            foreach (int i in used) { offsets[i] = all.Count; all.AddRange(Verts[i]); }
            mesh.SetVertices(all);
            for (int s = 0; s < used.Length; s++)
                mesh.SetTriangles(Tris[used[s]].Select(x => x + offsets[used[s]]).ToList(), s);
            mesh.RecalculateNormals();   // flat: every face has its own vertices
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    const int Fur = 0, Ink = 1, Pink = 2;

    static Mesh CatMesh()
    {
        var c = new Parts();
        // Feet, body, head: three balls, each wider than it is tall, chibi proportions.
        c.Ball(Fur, new Vector3(-0.10f, 0.055f, 0.03f), new Vector3(0.075f, 0.055f, 0.095f), 8, 5);
        c.Ball(Fur, new Vector3(0.10f, 0.055f, 0.03f), new Vector3(0.075f, 0.055f, 0.095f), 8, 5);
        c.Ball(Fur, new Vector3(0f, 0.24f, 0f), new Vector3(0.20f, 0.19f, 0.18f));
        c.Ball(Fur, new Vector3(0f, 0.55f, 0.01f), new Vector3(0.22f, 0.21f, 0.21f));
        // Paws, held out in front to carry the tray.
        c.Ball(Fur, new Vector3(-0.16f, 0.33f, 0.16f), new Vector3(0.07f, 0.06f, 0.08f), 8, 5);
        c.Ball(Fur, new Vector3(0.16f, 0.33f, 0.16f), new Vector3(0.07f, 0.06f, 0.08f), 8, 5);

        // Ears: a fur cone with a smaller pink one just inside it.
        foreach (float x in new[] { -0.13f, 0.13f })
        {
            c.Cone(Fur, new Vector3(x, 0.68f, 0f), new Vector3(x * 1.25f, 0.84f, -0.01f), 0.075f);
            c.Cone(Pink, new Vector3(x, 0.70f, 0.02f), new Vector3(x * 1.2f, 0.81f, 0.015f), 0.042f);
        }

        // Face: muzzle, eyes, nose. The eyes are the same ink as the chef's.
        c.Ball(Fur, new Vector3(0f, 0.50f, 0.19f), new Vector3(0.095f, 0.065f, 0.055f), 8, 5);
        c.Ball(Ink, new Vector3(-0.085f, 0.59f, 0.185f), new Vector3(0.033f, 0.042f, 0.03f), 8, 5);
        c.Ball(Ink, new Vector3(0.085f, 0.59f, 0.185f), new Vector3(0.033f, 0.042f, 0.03f), 8, 5);
        c.Ball(Pink, new Vector3(0f, 0.525f, 0.235f), new Vector3(0.025f, 0.018f, 0.02f), 6, 4);

        // A little pink bow at his collar, so he reads as staff and not as a stray.
        c.Cone(Pink, new Vector3(-0.005f, 0.40f, 0.17f), new Vector3(-0.095f, 0.43f, 0.15f), 0.035f);
        c.Cone(Pink, new Vector3(0.005f, 0.40f, 0.17f), new Vector3(0.095f, 0.43f, 0.15f), 0.035f);
        c.Ball(Pink, new Vector3(0f, 0.405f, 0.175f), Vector3.one * 0.028f, 6, 4);
        return c.Build("Waiter");
    }

    // Where the tail joins his back; the tail's own mesh is built around this point so it can turn about it.
    static readonly Vector3 TailRoot = new Vector3(0.02f, 0.18f, -0.17f);

    // The tail: balls up a curve behind him, thinning as they go.
    static Mesh TailMesh()
    {
        var c = new Parts();
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            var at = new Vector3(0.02f, 0.18f + t * t * 0.34f, -0.17f - Mathf.Sin(t * 1.6f) * 0.09f);
            c.Ball(Fur, at - TailRoot, Vector3.one * Mathf.Lerp(0.055f, 0.03f, t), 7, 4);
        }
        return c.Build("WaiterTail");
    }

    static Mesh TrayMesh()
    {
        var t = new Parts();
        t.Box(Fur, Vector3.zero, new Vector3(0.30f, 0.025f, 0.22f));
        return t.Build("WaiterTray");
    }
    #endregion
}
