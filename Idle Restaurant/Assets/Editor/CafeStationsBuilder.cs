using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Step 16 (re-runnable): the bacon griddle and the fry station (Stations, KitchenStations; 2026-10-28, user: "go on with
// the bacon and potato stations - the models are up to you, you can make them yourself"). KayKit has ham legs (nothing
// to lay in a burger), a potato chunk (no fries) and no fryer, so the bacon, the fries and both cookers are made here as
// small meshes coloured from the palette textures the kitchen already uses (the food's KayKit palette, the furniture's
// pink one): nothing is added to the download but a few kilobytes of meshes. The plate under the bacon and the potato
// crate are the pack's own.
// Both stand on a new kitchen line - steel tables and the potato crate along the inside of the kitchen's front-right
// edge, a diner's pass (PlaceStations) - which opens with the griddle at level 3.
public static partial class CafeGrowthBuilder
{
    const string StationMeshDir = Game + "Graphics/Meshes/Stations/";
    const string StationFoodDir = Game + "Prefabs/FoodPrefabs/Stations/";
    const string SinkCounter = "Level/Kitchen Utensils/kitchencounter_sink_backsplash";
    const string RackCounter = "Level/Kitchen Utensils/kitchencounter_straight_A_decorated";
    const string PlainCounter = "Level/Kitchen Utensils/kitchencounter_straight_A_backsplash (1)";

    [MenuItem("Tools/Chibi Cafe/16 Bacon & Fries Stations")]
    public static void BuildStations()
    {
        Directory.CreateDirectory(StationMeshDir);
        Directory.CreateDirectory(StationFoodDir);
        var food = AssetDatabase.LoadAllAssetsAtPath(RestFbx + "food_ingredient_burger_uncooked.fbx").OfType<Material>().First();
        var furniture = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "Rest.1.mat");

        // The food: bacon in three looks, the potato and the fries in two.
        var baconRaw = VersionPrefab("BaconRaw", SaveMesh(BaconLayer(Sw(4, 5, 0.55f), Sw(2, 4, 0.25f), Sw(4, 5, 0.85f)), "bacon_raw"), food, Vector3.one);
        var baconCooked = VersionPrefab("BaconCooked", SaveMesh(BaconLayer(Sw(1, 5, 0.3f), Sw(3, 5, 0.15f), Sw(1, 5, 0.75f)), "bacon_cooked"), food, Vector3.one);
        var baconBurnt = VersionPrefab("BaconBurnt", SaveMesh(BaconLayer(Sw(1, 1, 0.3f), Sw(1, 2, 0.4f), Sw(1, 1, 0.8f)), "bacon_burnt"), food, Vector3.one);
        var potatoMesh = AssetDatabase.LoadAllAssetsAtPath(RestFbx + "food_ingredient_potato.fbx").OfType<Mesh>().First();
        var potato = VersionPrefab("PotatoRaw", potatoMesh, food, Vector3.one * 0.7f);
        var fries = VersionPrefab("FriesGolden", SaveMesh(FriesCarton(Sw(4, 2, 0.15f), Sw(4, 3, 0.3f), 7), "fries_golden"), food, Vector3.one * 1.3f);
        var friesBurnt = VersionPrefab("FriesBurnt", SaveMesh(FriesCarton(Sw(1, 6, 0.65f), Sw(1, 1, 0.4f), 7), "fries_burnt"), food, Vector3.one * 1.3f);

        // Bacon lies on the burger like the patty does: its box is what the next layer stacks on.
        var bacon = CookedPrefab<Bacon>("Bacon", baconRaw, baconCooked, baconBurnt, food, new Vector3(0.8f, 0.09f, 0.6f), new Vector3(0f, 0.045f, 0f));
        var friesFood = CookedPrefab<Fries>("Fries", potato, fries, friesBurnt, food, new Vector3(0.46f, 0.56f, 0.46f), new Vector3(0f, 0.28f, 0f));

        PlaceStations(bacon, friesFood, food, furniture);
        StationOrderSprites();
        RenderStationIcons();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[CafeGrowth] bacon & fries stations built");
    }

    #region Palette
    // The palette textures are 8 x 8 swatches, each lighter at its top: a swatch's colour at a shade (0 light .. 1 dark).
    static Vector2 Sw(int row, int col, float shade = 0.3f) => new((col + 0.5f) / 8f, 1f - (row + 0.12f + 0.76f * shade) / 8f);
    #endregion

    #region Meshes
    // Faces with a colour each; quads are listed clockwise as seen from the side they face (Unity's front faces).
    sealed class MeshKit
    {
        readonly List<Vector3> v = new();
        readonly List<Vector2> uv = new();
        readonly List<int> t = new();

        public void Face(Vector2 colour, params Vector3[] p)
        {
            int b = v.Count;
            v.AddRange(p);
            for (int i = 0; i < p.Length; i++) uv.Add(colour);
            for (int i = 1; i + 1 < p.Length; i++) { t.Add(b); t.Add(b + i); t.Add(b + i + 1); }
        }

        // A box at c (its middle), turned by r: its top in one colour, its sides (and bottom) in another.
        public void Box(Vector3 c, Vector3 size, Quaternion r, Vector2 top, Vector2 side, bool bottom = false)
        {
            var e = size * 0.5f;
            Vector3 P(float x, float y, float z) => c + r * new Vector3(x * e.x, y * e.y, z * e.z);
            Face(top, P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1));
            if (bottom) Face(side, P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1));
            Face(side, P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));
            Face(side, P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1));
            Face(side, P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1));
            Face(side, P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1));
        }

        public void Box(Vector3 c, Vector3 size, Vector2 top, Vector2 side, bool bottom = false) => Box(c, size, Quaternion.identity, top, side, bottom);

        // Smooth: corners of the same colour at the same place are shared, so soft things (bacon) shade softly.
        public Mesh Build(string name, bool smooth)
        {
            var mesh = new Mesh { name = name };
            if (smooth)
            {
                var index = new Dictionary<(Vector3, Vector2), int>();
                var sv = new List<Vector3>();
                var suv = new List<Vector2>();
                var st = new List<int>();
                foreach (int i in t)
                {
                    var key = (Snap(v[i]), uv[i]);
                    if (!index.TryGetValue(key, out int at))
                    {
                        at = sv.Count;
                        index[key] = at;
                        sv.Add(v[i]);
                        suv.Add(uv[i]);
                    }
                    st.Add(at);
                }
                mesh.SetVertices(sv);
                mesh.SetUVs(0, suv);
                mesh.SetTriangles(st, 0);
            }
            else
            {
                mesh.SetVertices(v);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(t, 0);
            }
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static Vector3 Snap(Vector3 p) => new(Mathf.Round(p.x * 10000f), Mathf.Round(p.y * 10000f), Mathf.Round(p.z * 10000f));
    }

    // One rasher along x, wavy, its top in three bands across (meat, fat, meat), underside and sides in meat.
    static void Rasher(MeshKit k, Vector3 at, float yaw, float length, float width, float thick, float amp, float phase,
                       Vector2 meat, Vector2 fat, Vector2 under)
    {
        const int n = 14;
        var rot = Quaternion.Euler(0f, yaw, 0f);
        float[] zs = { -width * 0.5f, -width * 0.12f, width * 0.12f, width * 0.5f };
        Vector3 P(float x, float z, float dy)
        {
            float y = amp * Mathf.Sin(x / length * Mathf.PI * 3f + phase);
            return at + rot * new Vector3(x, y + dy, z);
        }
        for (int i = 0; i < n; i++)
        {
            float x0 = -length * 0.5f + length * i / n, x1 = -length * 0.5f + length * (i + 1) / n;
            for (int b = 0; b < 3; b++)
                k.Face(b == 1 ? fat : meat, P(x0, zs[b], thick), P(x0, zs[b + 1], thick), P(x1, zs[b + 1], thick), P(x1, zs[b], thick));
            k.Face(under, P(x0, zs[0], 0f), P(x1, zs[0], 0f), P(x1, zs[3], 0f), P(x0, zs[3], 0f));
            k.Face(meat, P(x0, zs[0], 0f), P(x0, zs[0], thick), P(x1, zs[0], thick), P(x1, zs[0], 0f));
            k.Face(meat, P(x1, zs[3], 0f), P(x1, zs[3], thick), P(x0, zs[3], thick), P(x0, zs[3], 0f));
        }
        float xa = -length * 0.5f, xb = length * 0.5f;
        k.Face(meat, P(xa, zs[3], 0f), P(xa, zs[3], thick), P(xa, zs[0], thick), P(xa, zs[0], 0f));
        k.Face(meat, P(xb, zs[0], 0f), P(xb, zs[0], thick), P(xb, zs[3], thick), P(xb, zs[3], 0f));
    }

    // Bacon as it goes on a burger: two rashers side by side, a little longer than the bun is wide.
    static Mesh BaconLayer(Vector2 meat, Vector2 fat, Vector2 under)
    {
        var k = new MeshKit();
        Rasher(k, new Vector3(0f, 0.022f, -0.1f), 5f, 0.82f, 0.18f, 0.045f, 0.02f, 0f, meat, fat, under);
        Rasher(k, new Vector3(0.02f, 0.03f, 0.1f), -6f, 0.8f, 0.18f, 0.045f, 0.02f, 1.7f, meat, fat, under);
        return k.Build("bacon", smooth: true);
    }

    // The raw bacon waiting on its plate: three rashers, loosely piled.
    static Mesh BaconPile()
    {
        var k = new MeshKit();
        var (meat, fat, under) = (Sw(4, 5, 0.55f), Sw(2, 4, 0.25f), Sw(4, 5, 0.85f));
        Rasher(k, new Vector3(0f, 0.022f, -0.2f), -9f, 0.7f, 0.19f, 0.045f, 0.02f, 0.3f, meat, fat, under);
        Rasher(k, new Vector3(0.02f, 0.06f, -0.06f), 7f, 0.72f, 0.19f, 0.045f, 0.02f, 1.9f, meat, fat, under);
        Rasher(k, new Vector3(-0.02f, 0.1f, 0.08f), -3f, 0.68f, 0.19f, 0.045f, 0.02f, 1.1f, meat, fat, under);
        Rasher(k, new Vector3(0.01f, 0.14f, 0.2f), 10f, 0.7f, 0.19f, 0.045f, 0.02f, 2.6f, meat, fat, under);
        return k.Build("bacon_pile", smooth: true);
    }

    // A cup, wider at the top: from a rectangle (half sizes) at y0 to another at y1; sides only (and its bottom).
    static void Cup(MeshKit k, float y0, float y1, Vector2 h0, Vector2 h1, Vector2 colour, bool bottom)
    {
        Vector3 A(float x, float z) => new(x * h0.x, y0, z * h0.y);
        Vector3 B(float x, float z) => new(x * h1.x, y1, z * h1.y);
        k.Face(colour, A(-1, -1), B(-1, -1), B(1, -1), A(1, -1));     // front (-z)
        k.Face(colour, A(1, 1), B(1, 1), B(-1, 1), A(-1, 1));         // back (+z)
        k.Face(colour, A(1, -1), B(1, -1), B(1, 1), A(1, 1));         // right (+x)
        k.Face(colour, A(-1, 1), B(-1, 1), B(-1, -1), A(-1, -1));     // left (-x)
        if (bottom) k.Face(colour, A(-1, -1), A(1, -1), A(1, 1), A(-1, 1));
    }

    // A red carton of fries: the cup with a light band under its rim, golden fries standing up out of it.
    static Mesh FriesCarton(Vector2 fry, Vector2 fryTop, int seed)
    {
        var k = new MeshKit();
        var (red, band) = (Sw(3, 1, 0.15f), Sw(0, 4, 0.2f));
        Cup(k, 0f, 0.2f, new Vector2(0.12f, 0.085f), new Vector2(0.15f, 0.105f), red, true);
        Cup(k, 0.2f, 0.25f, new Vector2(0.15f, 0.105f), new Vector2(0.158f, 0.11f), band, false);
        Cup(k, 0.25f, 0.29f, new Vector2(0.158f, 0.11f), new Vector2(0.166f, 0.116f), red, false);
        // the inside of the carton's mouth, seen past the fries
        k.Face(fry, new Vector3(-0.145f, 0.24f, -0.1f), new Vector3(-0.145f, 0.24f, 0.1f), new Vector3(0.145f, 0.24f, 0.1f), new Vector3(0.145f, 0.24f, -0.1f));
        var rng = new System.Random(seed);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        for (int ix = 0; ix < 4; ix++)
            for (int iz = 0; iz < 3; iz++)
            {
                float x = -0.105f + ix * 0.07f + R(-0.012f, 0.012f), z = -0.065f + iz * 0.065f + R(-0.01f, 0.01f);
                float h = R(0.2f, 0.3f);
                var tilt = Quaternion.Euler(R(-12f, 12f), R(0f, 90f), R(-12f, 12f));
                k.Box(new Vector3(x, 0.12f + h * 0.5f, z), new Vector3(0.045f, h, 0.045f), tilt, fryTop, fry);
            }
        return k.Build("fries", smooth: false);
    }

    // The griddle, its cooking plate's top at y 0 (where the bacon lies): a steel base, a dark plate with rails round
    // it, a grease trough at the front and two pink knobs like the stove's.
    static Mesh GriddleBody()
    {
        var k = new MeshKit();
        var (steel, steelSide, plate, plateSide, knob) = (Sw(0, 3, 0.1f), Sw(0, 3, 0.45f), Sw(1, 2, 0.35f), Sw(1, 2, 0.7f), Sw(0, 1, 0.3f));
        k.Box(new Vector3(0f, -0.1f, 0f), new Vector3(0.94f, 0.14f, 0.84f), steel, steelSide);
        k.Box(new Vector3(0f, -0.015f, 0.01f), new Vector3(0.86f, 0.03f, 0.7f), plate, plateSide);
        k.Box(new Vector3(0f, 0.09f, 0.395f), new Vector3(0.94f, 0.24f, 0.05f), steel, steelSide);
        k.Box(new Vector3(-0.455f, 0.01f, 0.02f), new Vector3(0.03f, 0.08f, 0.8f), steel, steelSide);
        k.Box(new Vector3(0.455f, 0.01f, 0.02f), new Vector3(0.03f, 0.08f, 0.8f), steel, steelSide);
        k.Box(new Vector3(0f, -0.022f, -0.385f), new Vector3(0.86f, 0.016f, 0.06f), plateSide, plateSide);
        k.Box(new Vector3(-0.26f, -0.11f, -0.44f), new Vector3(0.1f, 0.075f, 0.05f), knob, knob);
        k.Box(new Vector3(0.26f, -0.11f, -0.44f), new Vector3(0.1f, 0.075f, 0.05f), knob, knob);
        return k.Build("griddle", smooth: false);
    }

    // The fryer, in its food spot's space (y 0 is where the potato sits): a steel box on the counter (base at -0.3) with
    // a well of golden oil (at +0.06), a wire basket in it, the basket's handle over the front rim and two pink knobs.
    static Mesh FryerBody()
    {
        var k = new MeshKit();
        var (steel, steelSide, inside, oil, shine, wire, knob, grip) =
            (Sw(0, 3, 0.1f), Sw(0, 3, 0.45f), Sw(1, 2, 0.6f), Sw(3, 6, 0.25f), Sw(2, 6, 0.15f), Sw(1, 2, 0.4f), Sw(0, 1, 0.3f), Sw(5, 6, 0.3f));
        const float x0 = 0.47f, z0 = 0.42f, xi = 0.36f, zi = 0.3f, y0 = -0.3f, rim = 0.12f, oilY = 0.06f;
        // outer sides
        Cup(k, y0, rim, new Vector2(x0, z0), new Vector2(x0, z0), steelSide, false);
        // the rim round the well
        k.Face(steel, new Vector3(-x0, rim, -z0), new Vector3(-xi, rim, -zi), new Vector3(xi, rim, -zi), new Vector3(x0, rim, -z0));
        k.Face(steel, new Vector3(x0, rim, z0), new Vector3(xi, rim, zi), new Vector3(-xi, rim, zi), new Vector3(-x0, rim, z0));
        k.Face(steel, new Vector3(-x0, rim, z0), new Vector3(-xi, rim, zi), new Vector3(-xi, rim, -zi), new Vector3(-x0, rim, -z0));
        k.Face(steel, new Vector3(x0, rim, -z0), new Vector3(xi, rim, -zi), new Vector3(xi, rim, zi), new Vector3(x0, rim, z0));
        // the well's walls, seen from inside, down to the oil
        k.Face(inside, new Vector3(xi, oilY, -zi), new Vector3(xi, rim, -zi), new Vector3(-xi, rim, -zi), new Vector3(-xi, oilY, -zi));
        k.Face(inside, new Vector3(-xi, oilY, zi), new Vector3(-xi, rim, zi), new Vector3(xi, rim, zi), new Vector3(xi, oilY, zi));
        k.Face(inside, new Vector3(-xi, oilY, -zi), new Vector3(-xi, rim, -zi), new Vector3(-xi, rim, zi), new Vector3(-xi, oilY, zi));
        k.Face(inside, new Vector3(xi, oilY, zi), new Vector3(xi, rim, zi), new Vector3(xi, rim, -zi), new Vector3(xi, oilY, -zi));
        // the oil, with a lighter glint
        k.Face(oil, new Vector3(-xi, oilY, -zi), new Vector3(-xi, oilY, zi), new Vector3(xi, oilY, zi), new Vector3(xi, oilY, -zi));
        k.Face(shine, new Vector3(0.12f, oilY + 0.002f, 0.08f), new Vector3(0.12f, oilY + 0.002f, 0.22f), new Vector3(0.3f, oilY + 0.002f, 0.22f), new Vector3(0.3f, oilY + 0.002f, 0.08f));
        // the basket's wire frame, just above the oil, and its handle over the front rim
        const float bx = 0.3f, bz = 0.24f, by = oilY + 0.02f, w = 0.022f;
        k.Box(new Vector3(0f, by, -bz), new Vector3(bx * 2f, w, w), wire, wire);
        k.Box(new Vector3(0f, by, bz), new Vector3(bx * 2f, w, w), wire, wire);
        k.Box(new Vector3(-bx, by, 0f), new Vector3(w, w, bz * 2f), wire, wire);
        k.Box(new Vector3(bx, by, 0f), new Vector3(w, w, bz * 2f), wire, wire);
        var handle = Quaternion.Euler(-28f, 0f, 0f);
        k.Box(new Vector3(0f, rim + 0.04f, -z0 + 0.02f), new Vector3(0.035f, 0.035f, 0.42f), handle, wire, wire);
        k.Box(new Vector3(0f, rim + 0.13f, -z0 - 0.15f), new Vector3(0.06f, 0.06f, 0.14f), handle, grip, grip);
        // knobs on the front
        k.Box(new Vector3(-0.27f, -0.12f, -z0 - 0.025f), new Vector3(0.1f, 0.075f, 0.05f), knob, knob);
        k.Box(new Vector3(0.27f, -0.12f, -z0 - 0.025f), new Vector3(0.1f, 0.075f, 0.05f), knob, knob);
        return k.Build("fryer", smooth: false);
    }

    // Saved in place (the same asset, so prefabs and the scene keep pointing at it when the step is run again).
    static Mesh SaveMesh(Mesh mesh, string name)
    {
        string path = StationMeshDir + name + ".asset";
        mesh.name = name;
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        // Through the mesh's own API (CopySerialized changed the data but not what the editor drew).
        existing.Clear();
        existing.SetVertices(mesh.vertices);
        existing.SetNormals(mesh.normals);
        existing.SetTangents(mesh.tangents);
        existing.SetUVs(0, mesh.uv);
        existing.SetTriangles(mesh.triangles, 0);
        existing.RecalculateBounds();
        EditorUtility.SetDirty(existing);
        return existing;
    }
    #endregion

    #region Prefabs
    static GameObject VersionPrefab(string name, Mesh mesh, Material material, Vector3 scale)
    {
        var go = new GameObject(name);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        go.transform.localScale = scale;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, StationFoodDir + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // The patty's prefab (its collider, its cooking bubble) with a CookedBase of its own and its three looks.
    static GameObject CookedPrefab<T>(string name, GameObject raw, GameObject cooked, GameObject burnt, Material material,
                                      Vector3 colliderSize, Vector3 colliderCenter) where T : CookedBase
    {
        string path = Game + "Prefabs/FoodPrefabs/" + name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            AssetDatabase.CopyAsset(Game + "Prefabs/FoodPrefabs/Uncooked_Burger.prefab", path);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = name;
            foreach (var old in root.GetComponents<EdibleBase>()) Object.DestroyImmediate(old);
            var food = root.AddComponent<T>();
            var bar = root.transform.Find("BurgerCookProgressBar");
            if (bar == null) bar = root.transform.Find("CookProgressBar");
            bar.name = "CookProgressBar";
            foreach (var old in bar.GetComponents<BurgerCanvasController>()) Object.DestroyImmediate(old);
            if (bar.GetComponent<CookedCanvasController>() == null) bar.gameObject.AddComponent<CookedCanvasController>();
            var so = new SerializedObject(food);
            so.FindProperty("purePrefab").objectReferenceValue = raw;
            so.FindProperty("defaultMaterial").objectReferenceValue = material;
            so.FindProperty("progressBar").objectReferenceValue = bar.gameObject;
            so.FindProperty("cookedVersion").objectReferenceValue = cooked;
            so.FindProperty("burntVersion").objectReferenceValue = burnt;
            so.ApplyModifiedPropertiesWithoutUndo();
            var box = root.GetComponent<BoxCollider>();
            box.size = colliderSize;
            box.center = colliderCenter;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
    #endregion

    #region The kitchen line
    // Where they stand (user, 2026-10-28, on a first corner that sat in a dining table's lap: "move it or the table, or go
    // off the map - you decide; put the potato crate on the floor like the bun crate; ask yourself whether better games
    // would do it this way"). Dining tables cannot move (their shadows are baked into the floor), and a kitchen apart
    // from the kitchen would send the chef through the customers. So the kitchen gets a line the way a diner has a pass:
    // along the inside of its own front-right edge, on its own floor tiles (a strip of new floor out into the dining room
    // was tried and dropped - user: "why a new strip instead of the edge you have?" - one seam less, and 4 m from the
    // nearest table), a line of steel tables like the plate island (good from every side) worked from the kitchen side:
    // the potato crate on the floor (the same crate as the buns'), the fryer, then the bacon and its griddle. The
    // customers' walkways are untouched (Tools/Chibi Cafe/16c Check Seats, every upgrade bought). Nothing is baked: a
    // carving obstacle takes the line out of the chef's navmesh while it is there. The line and the griddle open at level 3, the crate and the fryer at
    // level 5 (until then the fryer's table stands bare: the ribboned sheet that covered it was dropped, 2026-11-01 -
    // user: "what is this ugly model?").
    static readonly Vector3 LineMin = new(10.26f, 1.5f, 17.63f);
    static readonly Vector3 LineSize = new(2f, 0.5f, 6f);

    static void PlaceStations(GameObject bacon, GameObject fries, Material food, Material furniture)
    {
        var level = GameObject.Find("Level").transform;
        var old = level.Find("Stations");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("Stations").transform;
        root.SetParent(level, false);
        // (a first version stood the stations where the sink and the dish rack were: they are the kitchen's again)
        foreach (var path in new[] { SinkCounter, RackCounter })
        {
            var counter = GameObject.Find(path) ?? FindInactive(path);
            if (counter != null) counter.SetActive(true);
        }

        float x = LineMin.x + LineSize.x * 0.5f, top = 2.5f;
        float Z(float slot) => LineMin.z + 1f + 2f * slot;     // slot 0: the crate, 1: the fryer, 2: the bacon
        var face = Quaternion.Euler(0f, 90f, 0f);               // fronts turned to the kitchen (-x)

        var line = new GameObject("Kitchen Line").transform;
        line.SetParent(root, false);
        line.position = new Vector3(x, LineMin.y, LineMin.z + LineSize.z * 0.5f);
        var carve = line.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        carve.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        carve.center = new Vector3(0f, 0.6f, 0f);
        carve.size = new Vector3(LineSize.x, 2f, LineSize.z);
        carve.carving = true;
        carve.carveOnlyStationary = true;
        for (int slot = 1; slot <= 2; slot++)
            Place(Model(RestFbx + "kitchentable_A.fbx", furniture), line, new Vector3(x, 1.5f, Z(slot)), 90f).name = "table " + (slot == 1 ? "fryer" : "bacon");

        // Bacon: the plate of raw bacon at the far end, the griddle beside it.
        var baconStation = Group(line, "Station Bacon");
        baconStation.position = new Vector3(x, top, Z(2));
        var plate = new GameObject("BaconPlate").transform;
        plate.SetParent(baconStation, false);
        plate.position = new Vector3(x, top, Z(2) + 0.5f);
        Place(Model(RestFbx + "plate.fbx", furniture), plate, plate.position, 0f, 1.4f).name = "plate";
        var pile = MeshObject("BaconPile", SaveMesh(BaconPile(), "bacon_pile"), food, plate);
        pile.localPosition = new Vector3(0f, 0.05f, 0f);
        pile.localRotation = Quaternion.Euler(0f, 30f, 0f);
        Source(plate.gameObject, bacon, food, new Vector3(0.92f, 0.45f, 0.92f), new Vector3(0f, 0.2f, 0f));

        var griddle = new GameObject("Griddle").transform;
        griddle.SetParent(baconStation, false);
        griddle.SetPositionAndRotation(new Vector3(x, top + 0.17f, Z(2) - 0.48f), face);
        MeshObject("GriddleBody", SaveMesh(GriddleBody(), "griddle"), furniture, griddle).localPosition = Vector3.zero;
        var griddleBox = griddle.gameObject.AddComponent<BoxCollider>();
        griddleBox.size = new Vector3(0.94f, 0.3f, 0.84f);
        griddleBox.center = new Vector3(0f, -0.05f, 0f);
        griddleBox.enabled = false;   // like the pan's: on only while the chef carries food (PlaceableBase)
        var griddleStation = griddle.gameObject.AddComponent<Griddle>();
        var gso = new SerializedObject(griddleStation);
        gso.FindProperty("bubbles").objectReferenceValue = Bubbles(griddle, steam: true);
        gso.ApplyModifiedPropertiesWithoutUndo();

        // Fries: a full potato crate on the floor (like the bun crate), then the fryer on its table.
        var friesStation = Group(line, "Station Fries");
        friesStation.position = new Vector3(x, 1.5f, Z(0));
        // the bun crate's own size, height and material, so the two sources read as one family
        var bunCrate = GameObject.Find("Level/Selectables/BunSource").GetComponent<MeshRenderer>().sharedMaterial;
        var crate = Place(Model(RestFbx + "crate_potatoes.fbx", bunCrate), friesStation, new Vector3(x, 1.5f, Z(0)), 90f);
        crate.name = "PotatoCrate";
        Source(crate.gameObject, fries, food, null, null);

        var fryer = new GameObject("Fryer").transform;
        fryer.SetParent(friesStation, false);
        fryer.SetPositionAndRotation(new Vector3(x, top + 0.3f, Z(1)), face);
        MeshObject("FryerBody", SaveMesh(FryerBody(), "fryer"), furniture, fryer).localPosition = Vector3.zero;
        var fryerBox = fryer.gameObject.AddComponent<BoxCollider>();
        fryerBox.size = new Vector3(0.94f, 0.52f, 0.84f);
        fryerBox.center = new Vector3(0f, -0.04f, 0f);
        fryerBox.enabled = false;
        var fryerStation = fryer.gameObject.AddComponent<Fryer>();
        var fso = new SerializedObject(fryerStation);
        fso.FindProperty("bubbles").objectReferenceValue = Bubbles(fryer);
        fso.ApplyModifiedPropertiesWithoutUndo();

        var kitchen = root.gameObject.AddComponent<KitchenStations>();
        var so = new SerializedObject(kitchen);
        var entries = so.FindProperty("entries");
        entries.arraySize = 2;
        void Entry(int i, Stations.Kind kind, Transform station, GameObject replaced)
        {
            var e = entries.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            e.FindPropertyRelative("station").objectReferenceValue = station.gameObject;
            var list = e.FindPropertyRelative("replaces");
            list.arraySize = replaced != null ? 1 : 0;
            if (replaced != null) list.GetArrayElementAtIndex(0).objectReferenceValue = replaced;
        }
        Entry(0, Stations.Kind.Bacon, line, null);
        Entry(1, Stations.Kind.Fries, friesStation, null);
        so.ApplyModifiedPropertiesWithoutUndo();
        friesStation.gameObject.SetActive(false);
        line.gameObject.SetActive(false);
    }

    // The customers' agent type: the one that is not the chef's.
    static int CustomerAgentType()
    {
        var chef = Object.FindFirstObjectByType<PlayerFSM>(FindObjectsInactive.Include).GetComponent<UnityEngine.AI.NavMeshAgent>().agentTypeID;
        for (int i = 0; i < UnityEngine.AI.NavMesh.GetSettingsCount(); i++)
        {
            int id = UnityEngine.AI.NavMesh.GetSettingsByIndex(i).agentTypeID;
            if (id != chef) return id;
        }
        return 0;
    }

    static GameObject FindInactive(string path)
    {
        var parts = path.Split('/');
        var top = GameObject.Find(parts[0]);
        if (top == null) return null;
        var t = top.transform;
        for (int i = 1; i < parts.Length && t != null; i++) t = t.Cast<Transform>().FirstOrDefault(c => c.name == parts[i]);
        return t != null ? t.gameObject : null;
    }

    static Transform MeshObject(string name, Mesh mesh, Material material, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go.transform;
    }

    // 16c (play mode, every upgrade bought): every seat a customer can be sent to has a whole path from the door.
    [MenuItem("Tools/Chibi Cafe/16c Check Seats")]
    public static string CheckSeats()
    {
        var door = new Vector3(21.35f, 1.2f, 8.79f);
        var filter = new UnityEngine.AI.NavMeshQueryFilter { agentTypeID = CustomerAgentType(), areaMask = UnityEngine.AI.NavMesh.AllAreas };
        var sb = new System.Text.StringBuilder();
        int ok = 0, bad = 0;
        foreach (var chair in Object.FindObjectsByType<Chair>(FindObjectsSortMode.None))
        {
            var seat = ((ISedile)chair).CalculateSitPos();
            var path = new UnityEngine.AI.NavMeshPath();
            bool found = UnityEngine.AI.NavMesh.SamplePosition(seat, out var near, 1.2f, filter)
                         && UnityEngine.AI.NavMesh.CalculatePath(door, near.position, filter, path);
            if (found && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete) ok++;
            else { bad++; sb.Append($"{chair.name} at {seat:F1}: {(found ? path.status.ToString() : "no navmesh")}; "); }
        }
        return $"{ok} seats reachable, {bad} not. {sb}";
    }

    // A crate or plate to take food from: tapped (or walked into) with empty hands, it puts one in the chef's.
    static void Source(GameObject go, GameObject prefab, Material material, Vector3? size, Vector3? center)
    {
        var box = go.GetComponent<BoxCollider>();
        if (box == null) box = go.AddComponent<BoxCollider>();
        if (size.HasValue)
        {
            box.size = size.Value;
            box.center = center.Value;
        }
        var source = go.AddComponent<IngredientsSource>();
        var so = new SerializedObject(source);
        so.FindProperty("spawnPrefab").objectReferenceValue = prefab;
        so.FindProperty("defaultMaterial").objectReferenceValue = material;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Oil bubbling while something fries - small soft blobs rising off the oil - or, on the griddle, a thin steam over
    // the bacon (BurnSmoke's particle material: one made in code renders nothing here).
    static ParticleSystem Bubbles(Transform station, bool steam = false)
    {
        var go = new GameObject(steam ? "Steam" : "Bubbles");
        go.transform.SetParent(station, false);
        go.transform.localPosition = new Vector3(0f, steam ? 0.05f : 0.08f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.startLifetime = steam ? new ParticleSystem.MinMaxCurve(0.8f, 1.3f) : new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
        main.startSpeed = steam ? new ParticleSystem.MinMaxCurve(0.25f, 0.45f) : new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startSize = steam ? new ParticleSystem.MinMaxCurve(0.12f, 0.22f) : new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startColor = steam ? new Color(1f, 1f, 1f, 0.45f) : new Color(1f, 0.96f, 0.82f, 0.9f);
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var emission = ps.emission;
        emission.rateOverTime = steam ? 6f : 16f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = steam ? new Vector3(0.5f, 0.3f, 0.01f) : new Vector3(0.6f, 0.46f, 0.01f);
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.2f));
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        var smoke = Object.FindFirstObjectByType<BurnSmoke>(FindObjectsInactive.Include);
        renderer.sharedMaterial = smoke != null ? (Material)new SerializedObject(smoke).FindProperty("particleMaterial").objectReferenceValue : null;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }
    #endregion


    #region Pictures
    // The order bubbles' bacon layer and fries carton (drawn by Tools/UI/order_layers.py), on the customers' bubble.
    static void StationOrderSprites()
    {
        foreach (var name in new[] { "layer_bacon", "side_fries" })
        {
            string path = OrderSprites + name + ".png";
            var imp = (TextureImporter)AssetImporter.GetAtPath(path) ?? throw new System.Exception("no " + path + " (run Tools/UI/order_layers.py bacon fries)");
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        var root = PrefabUtility.LoadPrefabContents(NpcCanvasPrefab);
        try
        {
            var ticket = root.GetComponentInChildren<UIOrderTicket>(true);
            var so = new SerializedObject(ticket);
            so.FindProperty("bacon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(OrderSprites + "layer_bacon.png");
            so.FindProperty("fries").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(OrderSprites + "side_fries.png");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, NpcCanvasPrefab);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // The order card's tiles for bacon and fries, rendered like the other ingredients' (from the game camera's angle).
    static void RenderStationIcons()
    {
        var card = Object.FindFirstObjectByType<UIOrderCard>(FindObjectsInactive.Include);
        if (card == null) { Debug.LogWarning("[CafeGrowth] no order card in the scene"); return; }
        Directory.CreateDirectory(IngredientDir);
        var view = Camera.main.transform.rotation;
        var so = new SerializedObject(card);
        foreach (var (field, file, prefab, visual) in new[] { ("baconIcon", "bacon", "Bacon", "cookedVersion"), ("friesIcon", "fries", "Fries", "cookedVersion") })
        {
            string path = IngredientDir + "ing_" + file + ".png";
            var stage = new GameObject("__IconStage") { hideFlags = HideFlags.DontSave };
            try
            {
                var subject = new GameObject("Subject").transform;
                subject.SetParent(stage.transform, false);
                var item = KeyArtRenderer.Place(KeyArtRenderer.Visual(prefab, visual), subject, file);
                item.SetPositionAndRotation(IconStage, Quaternion.identity);
                var cam = IconCamera(stage.transform, view);
                File.WriteAllBytes(path, RenderMatted(cam, 160, 160));
            }
            finally
            {
                Object.DestroyImmediate(stage);
            }
            ImportUISprite(path);
            so.FindProperty(field).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    #endregion
}
