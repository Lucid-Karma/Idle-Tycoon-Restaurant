using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Builds the "grow the cafe" content from the project's own models (re-runnable, each step replaces what it
// built before):
// 1. Adventurer customers (KayKit Adventurers): copies of Npc 1 wearing an adventurer's meshes, re-bound to the
//    same rig so the existing walk/sit animations drive them; NpcFsm.kind set to their quirk (Customers).
// 2. Chef hats: adventurer hats baked into static meshes on the chef's head bone, fitted to his skull, plus a
//    chef's toque made here. CafeShop shows the worn one.
// See .claude/skills/chibi-burger-cafe-game-designer ("Growing the cafe").
public static partial class CafeGrowthBuilder
{
    const string Game = "Assets/Project/[GAME]/";
    const string AdvDir = Game + "ThirdPartyPackages/KayKit_Adventurers_2.0_FREE/Characters/fbx/";
    const string NpcDir = Game + "Prefabs/CharacterPrefabs/NpcPrefabs/";
    const string MeshDir = Game + "Graphics/Meshes/CafeUpgrades/";
    const string MatDir = Game + "Graphics/Materials/Model/";
    const string SkullFbx = "Assets/Models/KayKit_Skeletons_1.0_FREE/characters/fbx/Skeleton_Warrior.fbx";
    const string HeadPath = "Player/Model/Rig/root/hips/spine/chest/head";

    #region 1. Customers
    public static readonly (string model, CustomerKind kind)[] Adventurers =
    {
        ("Knight", CustomerKind.Knight),
        ("Barbarian", CustomerKind.Barbarian),
        ("Mage", CustomerKind.Mage),
        ("Rogue", CustomerKind.Rogue),
        ("Rogue_Hooded", CustomerKind.Rogue),
        ("Ranger", CustomerKind.Ranger),
    };

    public static string NpcPath(string model) => NpcDir + "Npc " + model + ".prefab";

    [MenuItem("Tools/Chibi Cafe/1 Build Adventurer Customers")]
    public static void BuildCustomers()
    {
        const string template = NpcDir + "Npc 1.prefab";
        foreach (var (model, kind) in Adventurers)
        {
            string path = NpcPath(model);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CopyAsset(template, path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = Path.GetFileNameWithoutExtension(path);
                var body = root.transform.Find("Model");
                var rig = body.Find("Rig");
                foreach (var child in body.Cast<Transform>().Where(c => c != rig).ToList())
                    Object.DestroyImmediate(child.gameObject);
                // The skeleton's own props ride on its bones (Skeleton_Rogue's hood on the head bone).
                foreach (var prop in rig.GetComponentsInChildren<Renderer>(true).Select(r => r.gameObject).ToList())
                    Object.DestroyImmediate(prop);

                var bones = rig.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(AdvDir + model + ".fbx");
                var looks = new GameObject(model).transform;
                looks.SetParent(body, false);
                foreach (var piece in source.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var go = new GameObject(piece.name);
                    go.transform.SetParent(looks, false);
                    var skin = go.AddComponent<SkinnedMeshRenderer>();
                    skin.sharedMesh = piece.sharedMesh;
                    skin.sharedMaterials = piece.sharedMaterials;
                    skin.bones = piece.bones.Select(b => bones[b.name]).ToArray();
                    skin.rootBone = bones[piece.rootBone.name];
                    skin.localBounds = piece.localBounds;
                    skin.shadowCastingMode = piece.shadowCastingMode;
                }

                var fsm = new SerializedObject(root.GetComponent<NpcFsm>());
                fsm.FindProperty("kind").enumValueIndex = (int)kind;
                fsm.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        AssetDatabase.SaveAssets();

        // The spawner pools every customer prefab; only kinds the cafe has unlocked are sent in.
        var spawner = Object.FindFirstObjectByType<NpcSpawnController>();
        var so = new SerializedObject(spawner);
        var list = so.FindProperty("npcPrefabs");
        var prefabs = new List<GameObject>();
        for (int i = 0; i < list.arraySize; i++)
        {
            var p = list.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (p != null && !Adventurers.Any(a => p.name == "Npc " + a.model)) prefabs.Add(p);
        }
        prefabs.AddRange(Adventurers.Select(a => AssetDatabase.LoadAssetAtPath<GameObject>(NpcPath(a.model))));
        list.arraySize = prefabs.Count;
        for (int i = 0; i < prefabs.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        Debug.Log($"[CafeGrowth] built {Adventurers.Length} adventurer customers");
    }
    #endregion

    #region 2. Chef hats
    // name, model, pieces, that model's head (to fit the hat to the chef's skull), extra scale, extra offset
    static readonly (string name, string model, string[] pieces, string head, float scale, Vector3 offset)[] Hats =
    {
        ("Hat_Knight", "Knight", new[] { "Knight_Helmet", "Knight_HelmetVisor" }, "Knight_Head", 1f, Vector3.zero),
        ("Hat_Wizard", "Mage", new[] { "Mage_Hat" }, "Mage_Head", 1f, Vector3.zero),
        ("Hat_Bear", "Barbarian", new[] { "Barbarian_BearHat" }, "Barbarian_Head", 1f, Vector3.zero),
    };

    // A skeleton's skull is smaller than an adventurer's head, so their hats had to be shrunk onto it and
    // re-centred. Now that the chef is an adventurer himself (ChefModel), every hat is baked exactly where
    // its own character wears it: same rig, same head, nothing to fit.
    static bool AdventurerChef => !string.IsNullOrEmpty(ChefModel);
    public static float HatFit => AdventurerChef ? 1f : 0.84f;

    // The head every hat has to sit on: whoever the chef is.
    public static Bounds ChefHeadBounds() => string.IsNullOrEmpty(ChefModel)
        ? HeadSpaceBounds(SkullFbx, "Skeleton_Warrior_Head")
        : HeadSpaceBounds(AdvDir + ChefModel + ".fbx", HeadPiece(ChefModel));

    [MenuItem("Tools/Chibi Cafe/2 Build Chef Hats")]
    public static void BuildHats()
    {
        Directory.CreateDirectory(MeshDir);
        var head = GameObject.Find(HeadPath).transform;
        var skull = ChefHeadBounds();

        foreach (var (name, model, pieces, headPiece, scale, offset) in Hats)
        {
            string fbx = AdvDir + model + ".fbx";
            var theirHead = HeadSpaceBounds(fbx, headPiece);
            var anchor = AdventurerChef ? theirHead.center : skull.center;
            float s = HatFit * scale;
            var mesh = new Mesh { name = name };
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            Material material = null;
            foreach (var pieceName in pieces)
            {
                var piece = Skinned(fbx, pieceName);
                material = piece.sharedMaterial;
                var bind = piece.sharedMesh.bindposes[System.Array.FindIndex(piece.bones, b => b.name == "head")];
                int start = verts.Count;
                verts.AddRange(piece.sharedMesh.vertices.Select(v => (bind.MultiplyPoint3x4(v) - theirHead.center) * s + anchor + offset));
                normals.AddRange(piece.sharedMesh.normals.Select(n => bind.MultiplyVector(n).normalized));
                uvs.AddRange(piece.sharedMesh.uv);
                tris.AddRange(piece.sharedMesh.triangles.Select(t => t + start));
            }
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            PlaceHat(head, name, SaveMesh(mesh), material);
        }

        PlaceHat(head, "Hat_Toque", SaveMesh(BuildToque(skull)), ToqueMaterial());
        EditorSceneManager.MarkSceneDirty(head.gameObject.scene);
        Debug.Log("[CafeGrowth] built chef hats");
    }

    static SkinnedMeshRenderer Skinned(string fbx, string piece) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(fbx).GetComponentsInChildren<SkinnedMeshRenderer>(true).First(s => s.name == piece);

    // A skinned piece's bounds in its head bone's space (bind pose).
    static Bounds HeadSpaceBounds(string fbx, string piece)
    {
        var smr = Skinned(fbx, piece);
        var bind = smr.sharedMesh.bindposes[System.Array.FindIndex(smr.bones, b => b.name == "head")];
        var verts = smr.sharedMesh.vertices;
        var bounds = new Bounds(bind.MultiplyPoint3x4(verts[0]), Vector3.zero);
        foreach (var v in verts) bounds.Encapsulate(bind.MultiplyPoint3x4(v));
        return bounds;
    }

    // Replaces the asset (the hat objects referencing it are rebuilt right after).
    static Mesh SaveMesh(Mesh mesh)
    {
        string path = MeshDir + mesh.name + ".asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    // Reuses an existing hat object, so the shop's references to it survive a rebuild.
    static void PlaceHat(Transform head, string name, Mesh mesh, params Material[] materials)
    {
        var existing = head.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(head, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        Get<MeshFilter>(go).sharedMesh = mesh;
        Get<MeshRenderer>(go).sharedMaterials = materials;
        go.SetActive(false);
    }

    // GetComponent or AddComponent (not `??`: in the editor a missing component is a fake null).
    static T Get<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    // A pleated chef's toque: a band around the top of the head and a puffy, slightly mushrooming crown.
    // On an adventurer's rounder head it sits higher and tighter, so their hair still shows under it.
    public static float ToqueBand => AdventurerChef ? 0.66f : 0.63f;      // band bottom, as a share of the head height from its bottom
    public static float ToqueRadius => AdventurerChef ? 0.90f : 1.02f;    // band radius, as a share of the head's half width

    static Mesh BuildToque(Bounds skull) => BuildToque(skull, ToqueBand, ToqueRadius);

    // All one white: a toque with a coloured band around it was tried and the user wants the whole hat white.
    public static Mesh BuildToque(Bounds skull, float bandAt, float bandRadius)
    {
        float r0 = skull.extents.x * bandRadius;
        float y0 = skull.min.y + skull.size.y * bandAt;
        // (radius, height) up the side, in units of the band radius; the crown rows are pleated.
        var profile = new (float r, float y, bool pleat)[]
        {
            (1.00f, 0.00f, false), (1.02f, 0.42f, false), (1.10f, 0.50f, true), (1.24f, 0.72f, true),
            (1.30f, 1.00f, true), (1.24f, 1.26f, true), (1.04f, 1.44f, true), (0.66f, 1.56f, true), (0f, 1.60f, false),
        };
        const int segments = 14;
        var verts = new List<Vector3>();
        var tris = new List<int>();
        Vector3 At(int row, int seg)
        {
            var (r, y, pleat) = profile[row];
            float radius = r * r0 * (pleat && seg % 2 == 1 ? 0.9f : 1f);
            float a = seg * Mathf.PI * 2f / segments;
            return new Vector3(skull.center.x + Mathf.Sin(a) * radius, y0 + y * r0, skull.center.z + Mathf.Cos(a) * radius);
        }
        // Flat shaded: every quad gets its own vertices.
        for (int row = 0; row < profile.Length - 1; row++)
            for (int seg = 0; seg < segments; seg++)
            {
                int next = (seg + 1) % segments;
                var quad = new[] { At(row, seg), At(row + 1, seg), At(row + 1, next), At(row, next) };
                int i = verts.Count;
                verts.AddRange(quad);
                tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }
        var mesh = new Mesh { name = "Hat_Toque" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    #region 3. Upgrades in the scene
    const string RestFbx = "Assets/Models/KayKit_Restaurant_Bits_1.0_FREE/Assets/fbx (unity)/";
    const string FurnFbx = "Assets/Models/KayKit_Furniture_Bits_1.0_FREE/KayKit_Furniture_Bits_1.0_FREE/Assets/fbx (unity)/";
    const string AdvProps = Game + "ThirdPartyPackages/KayKit_Adventurers_2.0_FREE/Assets/fbx(unity)/";
    const string CounterForStove = "Level/Kitchen Utensils/kitchencounter_straight_A_backsplash (2)";

    // Everything a purchase adds lives under Level/Upgrades, one group per upgrade, inactive until bought
    // (CafeShop). Built from the scene's own furniture where possible, so materials match. Not batching
    // static: bought things pop in with a scale bounce (the existing kitchen stays static).
    [MenuItem("Tools/Chibi Cafe/3 Place Upgrades")]
    public static void PlaceUpgrades()
    {
        var level = GameObject.Find("Level").transform;
        var old = level.Find("Upgrades");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("Upgrades").transform;
        root.SetParent(level, false);
        var rest = AssetDatabase.LoadAssetAtPath<Material>(MatDir + "Rest.1.mat");
        var furniture = GameObject.Find("cactus_medium_A").GetComponent<MeshRenderer>().sharedMaterial;

        // Tip jar (+ a little stack of coins) at the end of the pass, where the burgers leave the kitchen.
        var tipJar = Group(root, "Upgrade TipJar");
        Place(Model(RestFbx + "jar_B_medium.fbx", rest), tipJar, new Vector3(6.2f, 2.5f, 22.5f), 25f);
        var coin = CoinMaterial();
        for (int i = 0; i < 3; i++)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(c.GetComponent<Collider>());
            c.name = "Coin";
            c.GetComponent<MeshRenderer>().sharedMaterial = coin;
            c.transform.SetParent(tipJar, false);
            c.transform.position = new Vector3(5.75f + i * 0.05f, 2.53f + i * 0.06f, 22.35f - i * 0.03f);
            c.transform.localScale = new Vector3(0.22f, 0.025f, 0.22f);
        }

        // Second stove, where the counter between the sink and the dish rack was.
        var stove = Group(root, "Upgrade Stove");
        var stoveBody = Clone("Level/Selectables/stove_single", stove, "stove_single (2)");
        stoveBody.SetPositionAndRotation(new Vector3(7.26f, 1.5f, 32.63f), Quaternion.Euler(0f, 180f, 0f));
        var pan = Clone("Level/Small Kitchen Utensil/pan_B", stove, "pan_B (2)");
        pan.SetPositionAndRotation(new Vector3(7.22f, 2.67f, 32.44f), Quaternion.Euler(0f, 148.7f, 0f));

        // Second chopping board, on a new prep table in the open front corner of the kitchen.
        var board = Group(root, "Upgrade Board");
        Place(Model(RestFbx + "kitchentable_A.fbx", rest), board, new Vector3(2.4f, 1.5f, 17.6f), 0f);
        var chop = Clone("Level/Selectables/ChoppingArea", board, "ChoppingArea (2)");
        chop.SetPositionAndRotation(new Vector3(2.4f, 2.65f, 17.6f), Quaternion.Euler(0f, 0f, 0f));

        // Table for two: the right-hand table again, further back.
        CloneTable(root, "Upgrade TableForTwo", "table_round_A_small (1)", new Vector3(0f, 0f, 6.3f));
        // Big table: the four-seat table again, in the open floor at the bottom.
        CloneTable(root, "Upgrade BigTable", "table_round_A_small", new Vector3(15.0f, 0f, 5.8f));

        // Cozy corner: rugs under the tables, a lamp and two plants.
        var cozy = Group(root, "Upgrade Cozy");
        Place(Model(FurnFbx + "rug_oval_A.fbx", furniture), cozy, new Vector3(4.57f, 1.2f, 11.41f), 0f, 2.2f);
        Place(Model(FurnFbx + "rug_oval_B.fbx", furniture), cozy, new Vector3(17.68f, 1.2f, 24.32f), 90f, 2.2f);
        Place(Model(FurnFbx + "lamp_standing.fbx", furniture), cozy, new Vector3(21.4f, 1.2f, 28.2f), 0f, 1.4f);
        Place(Model(FurnFbx + "cactus_medium_B.fbx", furniture), cozy, new Vector3(21.5f, 1.2f, 20.6f), 30f, 1.6f);
        Place(Model(FurnFbx + "cactus_small_B.fbx", furniture), cozy, new Vector3(13.2f, 1.2f, 9.0f), 0f, 1.6f);

        // Chef's coffee: a big frothy mug on the pass.
        var coffee = Group(root, "Upgrade Coffee");
        Place(Model(AdvProps + "mug_full.fbx", null), coffee, new Vector3(4.75f, 2.81f, 27.7f), 200f, 1.3f);

        // The terrace: the dining room spreads into the empty half of the floor it already has, with two
        // more tables, plants and a lamp. (Nothing is laid outside the baked walkable mesh - see below.)
        var terrace = Group(root, "Upgrade Terrace");
        CloneTableInto(terrace, "table_round_A_small", TerraceRound);
        CloneTableInto(terrace, "table_round_A_small (1)", TerracePair);
        Place(Model(FurnFbx + "cactus_medium_A.fbx", furniture), terrace, TerraceCactus, 20f, 1.7f);
        Place(Model(FurnFbx + "cactus_small_A.fbx", furniture), terrace, TerraceSmallCactus, 0f, 1.7f);
        Place(Model(FurnFbx + "lamp_standing.fbx", furniture), terrace, TerraceLamp, 0f, 1.4f);
        Place(Model(FurnFbx + "rug_oval_A.fbx", furniture), terrace, new Vector3(4.57f, 1.21f, 11.41f) + TerraceRound, 0f, 2.2f);
        Place(Model(FurnFbx + "rug_oval_B.fbx", furniture), terrace, new Vector3(17.68f, 1.21f, 24.32f) + TerracePair, 90f, 2.2f);

        // The walkable meshes are the original bake (without any upgrade); bought furniture cuts its own hole.
        // (Baking with every upgrade in place left invisible obstacles and made the rugs raised walkable
        // platforms: the chef hovered over them and jittered next to the customers.)
        foreach (var table in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("table_round")))
            Carve(table, round: true);
        Carve(root.Find("Upgrade Board/kitchentable_A"), round: false);
        foreach (var name in new[] { "lamp_standing", "cactus_medium_B", "cactus_small_B" })
            Carve(root.Find("Upgrade Cozy/" + name), round: true);
        foreach (var name in new[] { "lamp_standing", "cactus_medium_A", "cactus_small_A" })
            Carve(root.Find("Upgrade Terrace/" + name), round: true);

        BuildWaiter();   // another thing the shop sells, so he has to exist before the catalogue is written
        foreach (Transform group in root) group.gameObject.SetActive(false);
        ConfigureShop(root);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Debug.Log("[CafeGrowth] placed upgrades");
    }

    // A carving NavMeshObstacle the size of the object's footprint: active only once the upgrade is bought.
    static void Carve(Transform t, bool round)
    {
        var bounds = RendererBounds(t);
        var obstacle = Get<UnityEngine.AI.NavMeshObstacle>(t.gameObject);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        var size = t.InverseTransformVector(bounds.size);
        size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        obstacle.center = t.InverseTransformPoint(bounds.center);
        if (round)
        {
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule;
            obstacle.radius = 0.5f * Mathf.Max(size.x, size.z) * 0.9f;
            obstacle.height = size.y;
        }
        else
        {
            obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
            obstacle.size = size;
        }
    }

    static Transform Group(Transform root, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        return go.transform;
    }

    static GameObject Model(string fbx, Material material)
    {
        var go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
        go.name = Path.GetFileNameWithoutExtension(fbx);
        if (material != null)
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) r.sharedMaterial = material;
        return go;
    }

    static Transform Place(GameObject go, Transform parent, Vector3 position, float yaw, float scale = 1f)
    {
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        go.transform.localScale = Vector3.one * scale;
        Unstatic(go);
        return go.transform;
    }

    static Transform Clone(string path, Transform parent, string name)
    {
        var source = GameObject.Find(path) ?? throw new System.Exception("no " + path);
        var go = Object.Instantiate(source, parent);
        go.name = name;
        Unstatic(go);
        return go.transform;
    }

    static void Unstatic(GameObject go)
    {
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
    }

    // A table with its chairs, their service spots and the table dressing, moved by `offset`. Each new chair
    // is pointed at the new service spot that matches the original's.
    static void CloneTable(Transform root, string name, string tableName, Vector3 offset) =>
        CloneTableInto(Group(root, name), tableName, offset);

    // Where the terrace's two tables go, as offsets from the tables they are copies of, and its decoration.
    // Everything has to stand on the *customers'* floor (the teal dining room: the bottom strip, z 8-15, and
    // the column on the right, x 13-22): the first version put the terrace on the kitchen's pink floor, where
    // the customers' navmesh doesn't reach - a customer walked as near to the chair as they could and sat
    // down on the floor there - and the lamp stood right in front of the plates on the pass.
    static readonly Vector3 TerraceRound = new Vector3(6.6f, 0f, 0f);      // round table: middle of the bottom strip
    static readonly Vector3 TerracePair = new Vector3(-3.2f, 0f, 6.7f);    // table for two: top of the right column
    static readonly Vector3 TerraceCactus = new Vector3(14.2f, 1.2f, 8.6f);
    static readonly Vector3 TerraceSmallCactus = new Vector3(13.6f, 1.2f, 35.0f);
    static readonly Vector3 TerraceLamp = new Vector3(19.0f, 1.2f, 35.2f);

    // Moves the terrace already in the scene to where the builder now puts it, without rebuilding the
    // upgrades (that rewrites the whole shop catalogue and its icons).
    [MenuItem("Tools/Chibi Cafe/3b Move the Terrace")]
    public static void MoveTerrace()
    {
        var terrace = GameObject.Find("Level").transform.Find("Upgrades/Upgrade Terrace");
        var oldRound = new Vector3(4.57f, 0f, 19.41f);       // where the first version put the two tables
        var oldPair = new Vector3(11.68f, 0f, 23.32f);
        var newRound = new Vector3(4.57f, 0f, 11.41f) + TerraceRound;
        var newPair = new Vector3(17.68f, 0f, 24.32f) + TerracePair;
        float Flat(Transform t, Vector3 c) => Vector2.Distance(new Vector2(t.position.x, t.position.z), new Vector2(c.x, c.z));
        foreach (Transform t in terrace)
        {
            if (t.name.StartsWith("cactus_medium")) t.position = TerraceCactus;
            else if (t.name.StartsWith("cactus_small")) t.position = TerraceSmallCactus;
            else if (t.name.StartsWith("lamp")) t.position = TerraceLamp;
            else if (Flat(t, oldRound) < 3.2f) t.position += newRound - oldRound;
            else if (Flat(t, oldPair) < 3.2f) t.position += newPair - oldPair;
            else Debug.LogWarning("[CafeGrowth] terrace part left where it was: " + t.name);
        }
        EditorSceneManager.MarkSceneDirty(terrace.gameObject.scene);
        Debug.Log("[CafeGrowth] moved the terrace into the dining room");
    }

    // One upgrade can lay out more than one table (the terrace), so the group is passed in.
    static void CloneTableInto(Transform group, string tableName, Vector3 offset)
    {
        var table = GameObject.Find("Level/Kitchen/CustomerArea/" + tableName).transform;
        float reach = 3.2f;
        bool Near(Transform t) => Vector2.Distance(new Vector2(t.position.x, t.position.z), new Vector2(table.position.x, table.position.z)) < reach;

        CopyAt(table, group, offset);
        foreach (Transform t in GameObject.Find("Level/Kitchen/CustomerArea").transform)
            if (t != table && t.GetComponent<Chair>() == null && Near(t)) CopyAt(t, group, offset);
        foreach (Transform t in GameObject.Find("Level/Small Kitchen Utensil").transform)
            if (Near(t)) CopyAt(t, group, offset);

        var services = new Dictionary<NonStackBase, NonStackBase>();
        foreach (Transform t in GameObject.Find("Level/Selectables/ServicePoints").transform)
            if (Near(t)) services[t.GetComponent<NonStackBase>()] = CopyAt(t, group, offset).GetComponent<NonStackBase>();
        foreach (Transform t in GameObject.Find("Level/Kitchen/CustomerArea").transform)
        {
            var chair = t.GetComponent<Chair>();
            if (chair == null || !services.ContainsKey(chair.service)) continue;
            var copy = CopyAt(t, group, offset).GetComponent<Chair>();
            copy.service = services[chair.service];
        }
    }

    static Transform CopyAt(Transform source, Transform parent, Vector3 offset)
    {
        var go = Object.Instantiate(source.gameObject, parent);
        go.name = source.name;
        go.transform.SetPositionAndRotation(source.position + offset, source.rotation);
        go.transform.localScale = source.lossyScale;
        Unstatic(go);
        return go.transform;
    }

    static Material CoinMaterial()
    {
        string path = MatDir + "TipCoin.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", UITokens.Colors.Yellow);
        mat.SetFloat("_Metallic", 0.3f);
        mat.SetFloat("_Smoothness", 0.55f);
        EditorUtility.SetDirty(mat);
        return mat;
    }
    #endregion

    #region Shop catalogue
    // The catalogue, in the order it opens up. Prices assume ~$60-80 a shift; levels come from stars
    // (CafeProgress.LevelStars: level 2 after the first shift, level 6 after about ten).
    static void ConfigureShop(Transform upgrades)
    {
        var managers = GameObject.Find("<<<Managers>>>");
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(managers); // the old ProductManager
        var shop = Get<CafeShop>(managers);
        GameObject G(string name) => upgrades.Find(name).gameObject;
        GameObject Hat(string name) => GameObject.Find(HeadPath).transform.Find(name).gameObject;
        // Anywhere under him: the apron rides on his hips bone, not on the model root (CafePolishBuilder).
        var apron = GameObject.Find("Player/Model").GetComponentsInChildren<Transform>(true)
            .First(t => t.name == "Kitchen Apron").gameObject;

        var list = new List<Upgrade>
        {
            new() { id = "apron", title = "Chef's Apron", blurb = "Look the part", price = 20, level = 1, kind = UpgradeKind.Chef, show = new[] { apron } },
            new() { id = "tipjar", title = "Tip Jar", blurb = "An extra $1 on great orders", price = 35, level = 1, kind = UpgradeKind.Kitchen, show = new[] { G("Upgrade TipJar") }, tipJar = 1 },
            new() { id = "toque", title = "Chef's Toque", blurb = "A proper chef's hat", price = 30, level = 2, kind = UpgradeKind.Chef, isHat = true, show = new[] { Hat("Hat_Toque") } },
            new() { id = "stove", title = "Second Stove", blurb = "Cook two patties at once", price = 60, level = 2, kind = UpgradeKind.Kitchen, show = new[] { G("Upgrade Stove") }, hide = new[] { GameObject.Find(CounterForStove) } },
            new() { id = "table2", title = "Table for Two", blurb = "+2 seats, +1 customer a shift", price = 70, level = 2, kind = UpgradeKind.Dining, show = new[] { G("Upgrade TableForTwo") }, extraCustomers = 1 },
            new() { id = "cozy", title = "Cozy Corner", blurb = "Customers wait 15% longer", price = 50, level = 3, kind = UpgradeKind.Dining, show = new[] { G("Upgrade Cozy") }, patienceBonus = 0.15f },
            new() { id = "helmet", title = "Knight's Helmet", blurb = "Brave in the kitchen", price = 60, level = 3, kind = UpgradeKind.Chef, isHat = true, show = new[] { Hat("Hat_Knight") } },
            new() { id = "board", title = "Second Board", blurb = "Chop two things at once", price = 80, level = 4, kind = UpgradeKind.Kitchen, show = new[] { G("Upgrade Board") } },
            new() { id = "bigtable", title = "Big Table", blurb = "+4 seats, +1 customer a shift", price = 110, level = 4, kind = UpgradeKind.Dining, show = new[] { G("Upgrade BigTable") }, extraCustomers = 1 },
            new() { id = "coffee", title = "Chef's Coffee", blurb = "The chef moves 15% faster", price = 90, level = 5, kind = UpgradeKind.Chef, show = new[] { G("Upgrade Coffee") }, chefSpeed = 0.15f },
            new() { id = "wizard", title = "Wizard Hat", blurb = "Magic not included", price = 90, level = 5, kind = UpgradeKind.Chef, isHat = true, show = new[] { Hat("Hat_Wizard") } },
            new() { id = "bear", title = "Bear Hood", blurb = "Rawr.", price = 120, level = 6, kind = UpgradeKind.Chef, isHat = true, show = new[] { Hat("Hat_Bear") } },
            new() { id = "terrace", title = "The Terrace", blurb = "+6 seats, +2 customers a shift", price = 180, level = 5, kind = UpgradeKind.Dining, show = new[] { G("Upgrade Terrace") }, extraCustomers = 2 },
            new() { id = "waiter", title = "Waiter Cat", blurb = "Runs the burgers out to the tables", price = 140, level = 4, kind = UpgradeKind.Staff, show = new[] { G("Upgrade Waiter") } },
        };

        // Keep icons already rendered for these ids (step 4).
        var so = new SerializedObject(shop);
        var existing = so.FindProperty("upgrades");
        for (int i = 0; i < existing.arraySize; i++)
        {
            var e = existing.GetArrayElementAtIndex(i);
            var match = list.FirstOrDefault(u => u.id == e.FindPropertyRelative("id").stringValue);
            if (match != null) match.icon = e.FindPropertyRelative("icon").objectReferenceValue as Sprite;
        }
        var field = typeof(CafeShop).GetField("upgrades", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(shop, list);

        so = new SerializedObject(shop);
        so.FindProperty("revealVfx").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Lana Studio/Hyper Casual FX/Prefabs/Confetti/Confetti_blast_multicolor.prefab");
        so.FindProperty("revealSound").objectReferenceValue = GameObject.Find("<<<Audio>>>/Fx").GetComponent<AudioSource>();
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(shop);

        // New chairs join the spawner's list (inactive until their table is bought).
        var spawner = Object.FindFirstObjectByType<NpcSpawnController>();
        var sso = new SerializedObject(spawner);
        var chairs = sso.FindProperty("targetChairs");
        var all = new List<GameObject>();
        for (int i = 0; i < chairs.arraySize; i++)
        {
            var c = chairs.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (c != null) all.Add(c);
        }
        all.AddRange(upgrades.GetComponentsInChildren<Chair>(true).Select(c => c.gameObject));
        chairs.arraySize = all.Count;
        for (int i = 0; i < all.Count; i++) chairs.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
        sso.ApplyModifiedPropertiesWithoutUndo();
    }
    #endregion


    #region 4. Shop icons
    const string IconDir = Game + "Graphics/Sprites/Shop/";
    static readonly Vector3 IconStage = new Vector3(-400f, -300f, 400f);

    // One picture per upgrade, rendered from the very objects it puts in the kitchen, seen from the game's
    // camera angle, on a transparent background (same matting as the key art).
    [MenuItem("Tools/Chibi Cafe/4 Render Shop Icons")]
    public static void RenderIcons()
    {
        Directory.CreateDirectory(IconDir);
        var shop = Object.FindFirstObjectByType<CafeShop>();
        var so = new SerializedObject(shop);
        var list = so.FindProperty("upgrades");
        var view = Camera.main.transform.rotation;
        for (int i = 0; i < list.arraySize; i++)
        {
            var upgrade = shop.Upgrades[i];
            string path = IconDir + "shop_" + upgrade.id + ".png";
            var stage = new GameObject("__IconStage") { hideFlags = HideFlags.DontSave };
            try
            {
                IconSubject(upgrade, stage.transform);
                var cam = IconCamera(stage.transform, view);
                File.WriteAllBytes(path, RenderMatted(cam, 360, 360));
            }
            finally
            {
                Object.DestroyImmediate(stage);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = 512;
            imp.SaveAndReimport();
            list.GetArrayElementAtIndex(i).FindPropertyRelative("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(shop.gameObject.scene);
        Debug.Log("[CafeGrowth] rendered shop icons");
    }

    static void IconSubject(Upgrade upgrade, Transform stage)
    {
        var holder = new GameObject("Subject").transform;
        holder.SetParent(stage, false);
        foreach (var source in upgrade.show)
        {
            if (upgrade.id == "cozy")
            {
                // The cozy corner is spread over the dining room: gather a lamp, a plant and a rug.
                Pose(source.transform.Find("rug_oval_A"), holder, new Vector3(0f, 0f, 0f), 1.1f);
                Pose(source.transform.Find("lamp_standing"), holder, new Vector3(-0.2f, 0f, 0.9f), 1f);
                Pose(source.transform.Find("cactus_medium_B"), holder, new Vector3(0.9f, 0f, -0.2f), 1f);
                continue;
            }
            var copy = Object.Instantiate(source, source.transform.position, source.transform.rotation, holder);
            copy.transform.localScale = source.transform.lossyScale;
            copy.SetActive(true);
            foreach (var t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.SetActive(true);
            // Table service spots are gameplay markers, not furniture; chairs pulled in so the table reads bigger.
            foreach (var spot in copy.GetComponentsInChildren<ServiceBase>(true)) Object.DestroyImmediate(spot.gameObject);
            var table = copy.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.StartsWith("table_round"));
            if (table != null)
                foreach (var chair in copy.GetComponentsInChildren<Chair>(true))
                {
                    var offset = chair.transform.position - table.position;
                    chair.transform.position = table.position + new Vector3(offset.x * 0.62f, offset.y, offset.z * 0.62f);
                }
        }
        foreach (var b in holder.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(b);
        foreach (var a in holder.GetComponentsInChildren<Animator>(true)) a.enabled = false;
        foreach (var c in holder.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        var bounds = RendererBounds(holder);
        holder.position += IconStage - bounds.center;
    }

    static void Pose(Transform source, Transform holder, Vector3 at, float scale)
    {
        var copy = Object.Instantiate(source.gameObject, holder);
        copy.SetActive(true);
        copy.transform.localPosition = at;
        copy.transform.localScale = source.lossyScale / source.lossyScale.x * scale;
    }

    static Bounds RendererBounds(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
        var b = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    static Camera IconCamera(Transform stage, Quaternion view)
    {
        var go = new GameObject("IconCamera") { hideFlags = HideFlags.DontSave };
        go.transform.SetParent(stage, false);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;
        cam.allowHDR = false;
        cam.allowMSAA = true;
        cam.enabled = false;
        var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        data.renderPostProcessing = false;
        data.renderShadows = false;

        var bounds = RendererBounds(stage.Find("Subject"));
        go.transform.rotation = view;
        go.transform.position = bounds.center - (view * Vector3.forward) * 60f;
        // Fit the bounds' corners as the camera sees them.
        float half = 0f;
        for (int i = 0; i < 8; i++)
        {
            var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var local = go.transform.InverseTransformPoint(corner);
            half = Mathf.Max(half, Mathf.Abs(local.x), Mathf.Abs(local.y));
        }
        cam.orthographicSize = half * 1.08f;
        return cam;
    }

    // Two renders over black and white give exact coverage (alpha): see KeyArtRenderer.
    static byte[] RenderMatted(Camera cam, int width, int height)
    {
        var black = RenderOver(cam, width, height, Color.black);
        var white = RenderOver(cam, width, height, Color.white);
        var px = new Color32[black.Length];
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int i = 0; i < black.Length; i++)
        {
            Color b = black[i].linear, w = white[i].linear;
            float a = Mathf.Clamp01(1f - ((w.r - b.r) + (w.g - b.g) + (w.b - b.b)) / 3f);
            Color c = a > 0.004f ? new Color(b.r / a, b.g / a, b.b / a) : Color.black;
            c = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b)).gamma;
            px[i] = new Color(c.r, c.g, c.b, a);
            if (px[i].a > 3)
            {
                int x = i % width, y = i / width;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
        }
        var full = new Texture2D(width, height, TextureFormat.RGBA32, false);
        full.SetPixels32(px);
        int margin = 6;
        minX = Mathf.Max(0, minX - margin); minY = Mathf.Max(0, minY - margin);
        maxX = Mathf.Min(width - 1, maxX + margin); maxY = Mathf.Min(height - 1, maxY + margin);
        var trimmed = new Texture2D(maxX - minX + 1, maxY - minY + 1, TextureFormat.RGBA32, false);
        trimmed.SetPixels(full.GetPixels(minX, minY, trimmed.width, trimmed.height));
        var png = trimmed.EncodeToPNG();
        Object.DestroyImmediate(full);
        Object.DestroyImmediate(trimmed);
        return png;
    }

    static Color[] RenderOver(Camera cam, int width, int height, Color background)
    {
        var rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24) { msaaSamples = 8, sRGB = true });
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        cam.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        var px = tex.GetPixels();
        Object.DestroyImmediate(tex);
        return px;
    }
    #endregion

    #region Preview (not saved)
    // A line-up on the kitchen floor in an idle pose: every customer, and the chef in each hat. Remove with
    // "Clear Preview".
    [MenuItem("Tools/Chibi Cafe/Preview Lineup")]
    public static void PreviewLineup()
    {
        ClearPreview();
        var stage = new GameObject("__CafePreview") { hideFlags = HideFlags.DontSave };
        var idle = AssetDatabase.LoadAllAssetsAtPath(SkullFbx).OfType<AnimationClip>().First(c => c.name == "Idle");
        var sit = AssetDatabase.LoadAllAssetsAtPath(SkullFbx).OfType<AnimationClip>().First(c => c.name == "Sit_Chair_Idle");
        AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();

        var models = new List<string> { "Npc 1", "Npc 2", "Npc 3" };
        models.AddRange(Adventurers.Select(a => "Npc " + a.model));
        for (int i = 0; i < models.Count; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcDir + models[i] + ".prefab");
            var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage.transform);
            npc.hideFlags = HideFlags.DontSave;
            foreach (var b in npc.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
            npc.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
            npc.transform.position = new Vector3(1.2f + i * 1.25f, 1.5f, 17.2f);
            npc.transform.rotation = Quaternion.Euler(0f, 150f, 0f);
            var body = npc.transform.Find("Model").gameObject;
            AnimationMode.SampleAnimationClip(body, i % 2 == 0 ? idle : sit, 0.2f);
        }

        var chef = GameObject.Find("Player/Model");
        var hats = new[] { "", "Hat_Toque", "Hat_Knight", "Hat_Wizard", "Hat_Bear" };
        for (int i = 0; i < hats.Length; i++)
        {
            var copy = Object.Instantiate(chef, stage.transform);
            copy.hideFlags = HideFlags.DontSave;
            foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
            copy.transform.position = new Vector3(2.5f + i * 1.5f, 1.5f, 20.2f);
            copy.transform.rotation = Quaternion.Euler(0f, 160f, 0f);
            var head = copy.transform.Find("Rig/root/hips/spine/chest/head");
            foreach (Transform h in head) if (h.name.StartsWith("Hat_")) h.gameObject.SetActive(h.name == hats[i]);
            var apron = copy.transform.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Kitchen Apron");
            if (apron != null) apron.gameObject.SetActive(i > 0);
            AnimationMode.SampleAnimationClip(copy, idle, 0.2f);
        }
        AnimationMode.EndSampling();
        SceneView.RepaintAll();
    }

    // Chef clones in a row, each posed at one moment of the slip (fall, then getting up).
    public static (string clip, float time)[] SlipPoses =
    {
        ("Death_A", 0.2f), ("Death_A", 0.45f), ("Death_A", 0.8f),
        ("Lie_StandUp", 0f), ("Lie_StandUp", 0.8f), ("Lie_StandUp", 1.6f), ("Lie_StandUp", 2.3f),
    };

    [MenuItem("Tools/Chibi Cafe/Preview Slip Poses")]
    public static void PreviewSlip()
    {
        ClearPreview();
        var stage = new GameObject("__CafePreview") { hideFlags = HideFlags.DontSave };
        var clips = AssetDatabase.LoadAllAssetsAtPath(SkullFbx).OfType<AnimationClip>().ToArray();
        AnimationMode.StartAnimationMode();
        AnimationMode.BeginSampling();
        var chef = GameObject.Find("Player/Model");
        for (int i = 0; i < SlipPoses.Length; i++)
        {
            var copy = Object.Instantiate(chef, stage.transform);
            copy.hideFlags = HideFlags.DontSave;
            foreach (var b in copy.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
            copy.transform.position = new Vector3(1.5f + i * 1.3f, 1.5f, 19.5f);
            copy.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var clip = clips.First(c => c.name == SlipPoses[i].clip);
            AnimationMode.SampleAnimationClip(copy, clip, SlipPoses[i].time);
        }
        AnimationMode.EndSampling();
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/Chibi Cafe/Clear Preview")]
    public static void ClearPreview()
    {
        if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
        var stage = GameObject.Find("__CafePreview");
        if (stage != null) Object.DestroyImmediate(stage);
    }
    #endregion

    static Material ToqueMaterial() => FlatMaterial("ChefToque", UITokens.Colors.WarmWhite);

    public static Material FlatMaterial(string name, Color color)
    {
        string path = MatDir + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(mat);
        return mat;
    }
    #endregion
}
