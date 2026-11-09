using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The island's plates are Mochi's (the waiter cat runs a burger left on a plate out to its table; the chef serves from
// his hands): they come into the kitchen with him, and until then the island holds a few little potted plants (user,
// 2026-11-07: "until Mochi comes, take away the plates he will use, or make them untappable - preferably they arrive
// with him; flowers on the table meanwhile"). Done with the shop's own show/hide lists on the "waiter" upgrade, so
// buying him reveals the plates with the same pop and the plants go. Re-runnable.
public static class MochiPlatesSetup
{
    const string PlatesPath = "Level/Selectables/Plates";
    const string DecorName = "Island Plants";
    const string Fbx = "Assets/Models/KayKit_Furniture_Bits_1.0_FREE/KayKit_Furniture_Bits_1.0_FREE/Assets/fbx (unity)/";

    [MenuItem("Tools/Chibi Cafe/L4 Plates Come With Mochi")]
    public static string Run()
    {
        var plates = GameObject.Find(PlatesPath);
        var shop = Object.FindFirstObjectByType<CafeShop>(FindObjectsInactive.Include);
        if (plates == null || shop == null) return "plates or shop not found";

        // One long, narrow runner down the middle of the island, as wide as the old mats and in their blue, hanging
        // down over both ends (user, 2026-11-07), covering the plates' shadows - baked into the island's light - with
        // three little potted plants down its middle.
        var parent = plates.transform.parent;
        var old = parent.Find(DecorName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var decor = new GameObject(DecorName);
        decor.transform.SetParent(parent, false);
        var rug = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx + "rug_oval_B.fbx");
        var rugRenderer = rug.GetComponentInChildren<MeshRenderer>();
        var rugMesh = rug.GetComponentInChildren<MeshFilter>().sharedMesh;
        var cloth = new GameObject("Island Runner", typeof(MeshFilter), typeof(MeshRenderer));
        cloth.transform.SetParent(decor.transform, false);
        cloth.transform.position = new Vector3(RunnerX, IslandTop, (IslandMin.y + IslandMax.y) * 0.5f);
        cloth.GetComponent<MeshFilter>().sharedMesh = RunnerMesh(MiddleUv(rugMesh));
        var clothRenderer = cloth.GetComponent<MeshRenderer>();
        clothRenderer.sharedMaterial = rugRenderer.sharedMaterial;   // (the mats' own: the same blue, the same light)
        clothRenderer.lightmapIndex = -1;
        // Down the runner's middle, one in the middle of the island and one either side at the same distance, well in
        // from its ends (user, 2026-11-07: "equal spacing, bigger, from the middle out, further from the edges").
        float mid = (IslandMin.y + IslandMax.y) * 0.5f;
        Place(decor.transform, "cactus_small_A", new Vector3(RunnerX, IslandTop + 0.006f, mid - PlantGap), 20f, PlantScale);
        Place(decor.transform, "cactus_small_B", new Vector3(RunnerX, IslandTop + 0.006f, mid), -35f, PlantScale);
        Place(decor.transform, "cactus_small_A", new Vector3(RunnerX, IslandTop + 0.006f, mid + PlantGap), 140f, PlantScale);
        // The ketchup and mustard stay where they always were - in front of the middle plant, which they hid - and
        // come with Mochi too (user, 2026-11-07: "keep the bottles where they were, or let them come with Mochi").
        var ketchup = Move("Level/Small Kitchen Utensil/ketchup", new Vector3(6f, IslandTop, 24.66f));
        var mustard = Move("Level/Small Kitchen Utensil/mustard", new Vector3(6f, IslandTop, 24.96f));
        foreach (var leftover in new[] { "IslandCloth.asset", "IslandCloth.mat", "IslandClothGingham.asset" })
            if (AssetDatabase.LoadAssetAtPath<Object>(GeneratedDir + "/" + leftover) != null) AssetDatabase.MoveAssetToTrash(GeneratedDir + "/" + leftover);

        var so = new SerializedObject(shop);
        var list = so.FindProperty("upgrades");
        SerializedProperty waiter = null;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == "waiter") waiter = list.GetArrayElementAtIndex(i);
        if (waiter == null) return "no waiter upgrade";
        Add(waiter.FindPropertyRelative("show"), plates);
        if (ketchup != null) Add(waiter.FindPropertyRelative("show"), ketchup);
        if (mustard != null) Add(waiter.FindPropertyRelative("show"), mustard);
        Add(waiter.FindPropertyRelative("hide"), decor);
        so.ApplyModifiedProperties();

        var scene = plates.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "ok: plates with Mochi; a runner and " + (decor.transform.childCount - 1) + " plants until then";
    }

    // The island: two kitchen tables end to end (world x 4.33 - 6.69, z 21.99 - 28.11, the top at y 2.5). The runner
    // lies along it over the line the plates stood on (x 5.26; their shadows spread to about 4.66 - 5.86).
    static readonly Vector2 IslandMin = new(4.33f, 21.99f), IslandMax = new(6.69f, 28.11f);
    const float IslandTop = 2.5f, RunnerX = 5.4f, RunnerWidth = 1.6f, Overhang = 0.03f, Drop = 0.32f, Flare = 0.05f;
    const float PlantGap = 1.9f, PlantScale = 1.6f;
    const string GeneratedDir = "Assets/Project/[GAME]/Graphics/Generated";

    // Found even while switched off (GameObject.Find skips inactive ones: the bottles are, until Mochi).
    static GameObject Move(string path, Vector3 to)
    {
        var parent = GameObject.Find(path.Substring(0, path.LastIndexOf('/')));
        var child = parent != null ? parent.transform.Find(path.Substring(path.LastIndexOf('/') + 1)) : null;
        if (child == null) { Debug.LogWarning("[MochiPlatesSetup] not found: " + path); return null; }
        child.position = to;
        return child.gameObject;
    }

    // The rug model's colour: the UV of the vertex nearest the middle of its top (its main blue).
    static Vector2 MiddleUv(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var uvs = mesh.uv;
        int best = 0;
        float bestScore = float.MaxValue;
        for (int i = 0; i < vertices.Length; i++)
        {
            float score = new Vector2(vertices[i].x, vertices[i].z).sqrMagnitude - vertices[i].y * 10f;
            if (score < bestScore) { bestScore = score; best = i; }
        }
        return uvs[best];
    }

    // A flat strip a few millimetres over the island, and over each end a flap hanging down, opening a little at the
    // hem (both faces, the flaps: seen from either side). One colour: every UV on the rug's blue.
    static Mesh RunnerMesh(Vector2 colour)
    {
        float hx = RunnerWidth * 0.5f, hz = (IslandMax.y - IslandMin.y) * 0.5f + Overhang;
        const float y = 0.006f;
        var v = new System.Collections.Generic.List<Vector3>();
        var nrm = new System.Collections.Generic.List<Vector3>();
        var tri = new System.Collections.Generic.List<int>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i0 = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            var n = Vector3.Cross(b - a, d - a).normalized;
            for (int k = 0; k < 4; k++) nrm.Add(n);
            tri.AddRange(new[] { i0, i0 + 1, i0 + 2, i0, i0 + 2, i0 + 3 });
        }
        Quad(new Vector3(-hx, y, -hz), new Vector3(-hx, y, hz), new Vector3(hx, y, hz), new Vector3(hx, y, -hz));
        foreach (float end in new[] { -1f, 1f })
        {
            var a = new Vector3(-hx, y, hz * end);
            var b = new Vector3(hx, y, hz * end);
            var down = new Vector3(0f, -Drop, Flare * end);
            Quad(a + down, b + down, b, a);
            Quad(b + down, a + down, a, b);
        }
        var mesh = new Mesh { name = "IslandRunner" };
        mesh.SetVertices(v);
        mesh.SetNormals(nrm);
        var uv = new Vector2[v.Count];
        for (int i = 0; i < uv.Length; i++) uv[i] = colour;
        mesh.uv = uv;
        mesh.SetTriangles(tri, 0);
        mesh.RecalculateBounds();
        return SaveAsset(mesh, "IslandRunner.asset");
    }

    static T SaveAsset<T>(T asset, string file) where T : Object
    {
        if (!AssetDatabase.IsValidFolder(GeneratedDir)) AssetDatabase.CreateFolder("Assets/Project/[GAME]/Graphics", "Generated");
        string path = GeneratedDir + "/" + file;
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    static void Place(Transform parent, string model, Vector3 at, float yaw, float scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx + model + ".fbx");
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, true);
        go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = Vector3.one * scale;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) r.lightmapIndex = -1;   // (not in the baked light)
    }

    static void Add(SerializedProperty array, GameObject go)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            var element = array.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue == go) return;
            if (element.objectReferenceValue == null) { element.objectReferenceValue = go; return; }
        }
        array.arraySize++;
        array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = go;
    }
}
