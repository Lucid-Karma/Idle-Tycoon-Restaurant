using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Step 14 (re-runnable): a lighter download. Loading took 16.7 s on CrazyGames; textures are 80% of the data.
// - The title screen is gone from the game (it opens straight into the kitchen), but its pictures were still in
//   the scene and so in every build: the key-art chef (1.8 MB), the sun-burst (1.3 MB), the pattern, the logo, the
//   streaks. Welcome/Poster and Welcome/TitleCard are removed; the Welcome panel itself stays (GamePanels hides it).
//   Tools/Chibi UI/Build Title Poster can still rebuild the poster if it is ever wanted again.
// - TextMesh Pro's sample font (LiberationSans) and emoji sheet sit in a Resources folder, and everything in a
//   Resources folder goes into the build, used or not. The game's own Fredoka becomes TMP's default font, no
//   default sprite sheet, and those assets move out of Resources.
public static partial class CafeGrowthBuilder
{
    const string TmpSettings = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    const string TmpUnused = "Assets/TextMesh Pro/Unused";

    [MenuItem("Tools/Chibi Cafe/14 Lighter Start: Drop Title Screen and Unused Fonts")]
    public static void BuildLighterStart()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != MainScene) throw new System.Exception("Open " + MainScene + " first");
        var welcome = GameObject.Find("<<<UI>>>/StaticCanvas/Welcome").transform;
        foreach (var name in new[] { "Poster", "TitleCard" })
        {
            var t = welcome.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettings);
        var so = new SerializedObject(settings);
        so.FindProperty("m_defaultFontAsset").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LabelFontPath);
        so.FindProperty("m_defaultSpriteAsset").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        if (!AssetDatabase.IsValidFolder(TmpUnused)) AssetDatabase.CreateFolder("Assets/TextMesh Pro", "Unused");
        foreach (var path in new[]
                 {
                     "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
                     "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset",
                     "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Drop Shadow.mat",
                     "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat",
                     "Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset",
                 })
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null) continue;
            string error = AssetDatabase.MoveAsset(path, TmpUnused + "/" + Path.GetFileName(path));
            if (!string.IsNullOrEmpty(error)) Debug.LogWarning("[CafeGrowth] " + error);
        }
        Debug.Log("[CafeGrowth] lighter start: title screen and unused TMP assets out of the build");
    }
}
