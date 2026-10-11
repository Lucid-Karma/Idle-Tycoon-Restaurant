using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Step 12 (re-runnable): the web build's size. CrazyGames wants the first download under 50 MB (20 MB to be
// shown on the mobile home page). What was in the 24 MB build, by weight: the baked lightmaps (6 maps x light,
// directional and shadowmask = 28 MB before compression), 13 KayKit colour palettes at 1024 px (a grid of flat
// swatches), a 2048 px metallic map for the neon sign's letters, a 1000 px soft circle for particles, and Unity's
// own splash logo (the splash screen is already off). Importer limits are lowered and the splash logo is dropped.
// Not done: URP packs ten film-grain noise textures (2.5 MB, incompressible) although no volume uses film grain;
// emptying the list in UniversalRenderPipelineGlobalSettings through a SerializedObject does not persist, and
// saving that asset from the editor rewrites its m_RuntimeSettings list - left alone.
public static class WebSizeTrimmer
{
    const string GlobalSettings = "Assets/UniversalRenderPipelineGlobalSettings.asset";

    [MenuItem("Tools/Chibi Cafe/12 Shrink Textures for Web")]
    public static void Shrink()
    {
        int changed = 0;
        changed += ModelsAndPanels();
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            int limit = LimitFor(path, importer);
            if (limit <= 0 || importer.maxTextureSize <= limit) continue;
            importer.maxTextureSize = limit;
            importer.SaveAndReimport();
            changed++;
        }
        PlayerSettings.SplashScreen.showUnityLogo = false;
        AssetDatabase.SaveAssets();
        Debug.Log($"[WebSize] {changed} assets changed, Unity logo off");
    }

    // - FBX models (characters, furniture): mesh compression Medium (positions/UVs stored at lower precision).
    // - The farewell panel's two pictures (ExitPanel*.png) are only shown after the Exit button, which the web build
    //   hides: 32 px in WebGL builds instead of 512x362 uncompressed.
    static int ModelsAndPanels()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Models", "Assets/Project/[GAME]/ThirdPartyPackages" }))
        {
            var model = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as ModelImporter;
            if (model == null || model.meshCompression == ModelImporterMeshCompression.Medium || model.meshCompression == ModelImporterMeshCompression.High) continue;
            model.meshCompression = ModelImporterMeshCompression.Medium;
            model.SaveAndReimport();
            n++;
        }
        foreach (var name in new[] { "ExitPanel", "ExitPanel thanks" })
        {
            string path = $"Assets/Project/[GAME]/Graphics/Textures/Helpers/{name}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            var web = importer.GetPlatformTextureSettings("WebGL");
            if (web.overridden && web.maxTextureSize <= 32) continue;
            web.overridden = true;
            web.maxTextureSize = 32;
            importer.SetPlatformTextureSettings(web);
            importer.SaveAndReimport();
            n++;
        }
        return n;
    }

    static int LimitFor(string path, TextureImporter importer)
    {
        string file = Path.GetFileName(path);
        if (path.Contains("/Scenes/GameScene/Lightmap-"))
        {
            // Directional maps are smooth gradients; shadowmasks hold baked shadow edges, so they keep more.
            if (file.EndsWith("_dir.png")) return 256;
            if (file.EndsWith("_shadowmask.png")) return file.StartsWith("Lightmap-5") ? 256 : 512;
            // The light maps hold indirect light only (the direct light is realtime, its shadows in the shadowmask):
            // smooth, so half the resolution is not visible at game size. They are also the heaviest thing left
            // in the build (HDR, nearly incompressible).
            if (file.EndsWith("_light.exr")) return file.StartsWith("Lightmap-5") ? 256 : 512;
            return 0;
        }
        if (file.Contains("FuturisticLettersProject_initialShadingGroup_MetallicSmoothness")) return 64;
        if (path.EndsWith("Graphics/Textures/Elips 1.png")) return 128;
        // Flat colour palettes: a grid of swatches, 32 px per swatch at 256.
        if (importer.textureType == TextureImporterType.Default && !path.Contains("/Packages/")
            && (file.EndsWith("_texture.png") || path.EndsWith("Graphics/Textures/texture 0.png") || path.EndsWith("Characters/chef.png")))
            return 256;
        return 0;
    }
}
