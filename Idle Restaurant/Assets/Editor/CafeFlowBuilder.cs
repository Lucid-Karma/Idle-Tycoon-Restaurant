using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Step 12 (re-runnable): the "arcade idle" flow - shifts follow each other without a result card (ShiftFlow,
// added at runtime), so what lived on that card needs a place in the kitchen:
// - <<<Controllers>>>/MoneyPiles: a paid order's money waits on its table as a stack of banknotes until the
//   chef walks by. Its look: Graphics/Sprites/FX/bill_face.png and sparkle.png (Tools/FX/money_art.py), and
//   materials made here (the note, its paper edge, the yellow band, the "$5" label with an ink outline).
// - HUD/BonusBTN: "Double it" (rewarded video), under the wallet for a short while after each shift.
// - The bun oven opens and shuts (OvenDoor): the model's oven_door swings and the tray slides, both taken out
//   of static batching.
public static partial class CafeGrowthBuilder
{
    const string FxDir = Game + "Graphics/Sprites/FX/";
    const string BillTexture = FxDir + "bill_face.png", SparkleTexture = FxDir + "sparkle.png";
    const string LabelFontPath = Game + "Graphics/Font/Fredoka/Fredoka-SemiBold SDF.asset";

    [MenuItem("Tools/Chibi Cafe/12 Arcade Flow: Money Piles and HUD Bonus")]
    public static void BuildArcadeFlow()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != MainScene) throw new System.Exception("Open " + MainScene + " first");
        BuildMoneyPiles();
        BuildHudBonus();
        SetUpOvenDoors();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CafeGrowth] arcade flow: money piles and HUD bonus built");
    }

    static void BuildMoneyPiles()
    {
        Texture(BillTexture, false);
        Texture(SparkleTexture, true);
        var glintImporter = (TextureImporter)AssetImporter.GetAtPath(SparkleTexture);
        glintImporter.textureType = TextureImporterType.Sprite;
        glintImporter.spriteImportMode = SpriteImportMode.Single;
        glintImporter.SaveAndReimport();

        var bill = LitMaterial("BillFace", Color.white, AssetDatabase.LoadAssetAtPath<Texture2D>(BillTexture));
        var edge = LitMaterial("BillEdge", new Color(0.86f, 0.95f, 0.86f), null);
        var band = LitMaterial("BillBand", UITokens.Colors.DeepYellow, null);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LabelFontPath);
        string labelPath = MatDir + "MoneyLabel.mat";
        AssetDatabase.DeleteAsset(labelPath);
        var label = new Material(font.material) { name = "MoneyLabel" };
        label.EnableKeyword("OUTLINE_ON");
        label.SetColor("_OutlineColor", UITokens.Colors.Ink);
        label.SetFloat("_OutlineWidth", 0.32f);
        label.SetFloat("_FaceDilate", 0.25f);
        AssetDatabase.CreateAsset(label, labelPath);

        var controllers = GameObject.Find("<<<Controllers>>>").transform;
        var old = controllers.Find("MoneyPiles");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var go = new GameObject("MoneyPiles");
        go.transform.SetParent(controllers, false);
        var piles = go.AddComponent<MoneyPiles>();
        var so = new SerializedObject(piles);
        so.FindProperty("billMaterial").objectReferenceValue = bill;
        so.FindProperty("edgeMaterial").objectReferenceValue = edge;
        so.FindProperty("bandMaterial").objectReferenceValue = band;
        so.FindProperty("sparkleSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SparkleTexture);
        so.FindProperty("labelFont").objectReferenceValue = font;
        so.FindProperty("labelMaterial").objectReferenceValue = label;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    static void Texture(string path, bool alpha)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path) ?? throw new System.Exception("no texture " + path + " (run Tools/FX/money_art.py)");
        imp.textureType = TextureImporterType.Default;
        imp.alphaIsTransparency = alpha;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.mipmapEnabled = true;
        imp.maxTextureSize = alpha ? 64 : 256;
        imp.SaveAndReimport();
    }

    static Material LitMaterial(string name, Color color, Texture2D map)
    {
        string path = MatDir + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetTexture("_BaseMap", map);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.25f);   // paper
        mat.SetFloat("_Cull", 0f);            // both sides: a thin note seen edge-on or from below never vanishes
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void SetUpOvenDoors()
    {
        foreach (var oven in Object.FindObjectsByType<Oven>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // The door is part of the oven model, which is tapped through its TapProxy.
            Transform door = null;
            foreach (var proxy in Object.FindObjectsByType<TapProxy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (proxy.Target == oven) door = proxy.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "oven_door");
            if (door == null) door = GameObject.Find("oven_door")?.transform;
            if (door == null) { Debug.LogWarning("[CafeGrowth] no oven_door for " + oven.name); continue; }

            // The door swings and the tray (the Oven itself) slides: neither can be statically batched.
            foreach (var moving in new[] { door.gameObject, oven.gameObject })
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(moving);
                GameObjectUtility.SetStaticEditorFlags(moving, flags & ~StaticEditorFlags.BatchingStatic);
            }
            var swing = oven.GetComponent<OvenDoor>() ?? oven.gameObject.AddComponent<OvenDoor>();
            var so = new SerializedObject(swing);
            so.FindProperty("door").objectReferenceValue = door;
            so.FindProperty("openAngle").floatValue = 90f;     // as modelled: lying open
            so.FindProperty("closedAngle").floatValue = 15f;   // flush with the oven's front (at 0 it sinks inside)
            so.FindProperty("trayTravel").floatValue = 0.4f;    // the bun stays in sight through the door's window
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[CafeGrowth] oven door: " + door.name + " on " + oven.name);
        }
    }

    static void BuildHudBonus()
    {
        var hud = GameObject.Find("<<<UI>>>/StaticCanvas/InGamePanel/HUD").transform;
        var old = hud.Find("BonusBTN");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var source = GameObject.Find("<<<UI>>>/StaticCanvas/ScorePanel/BackgroundIMG/ResultCard/InteractionArea/Row/BonusBTN");
        var bonus = Object.Instantiate(source, hud);
        bonus.name = "BonusBTN";
        // Under the wallet, right edges lined up.
        var rt = (RectTransform)bonus.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(196f, 64f);
        rt.anchoredPosition = new Vector2(-252f, -118f);
        var reward = bonus.GetComponent<UIRewardedBonus>();
        var so = new SerializedObject(reward);
        so.FindProperty("card").objectReferenceValue = null;
        so.FindProperty("offerSeconds").floatValue = 25f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
