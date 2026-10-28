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
    // - The farewell panel's two pictures (ExitPanel*.png) were shrunk to 32 px here; the panel and both pictures
    //   were deleted on 2026-10-17 (never shown), so that part now finds nothing.
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

    // Not done: the music loop (~800 KB AAC). forceToMono and a lower WebGL quality were set and the build came out
    // byte for byte the same size - Unity's WebGL audio encoding ignores both (2026-10-15).

    // The heaviest textures as they are stored for the active build target (bytes in the build before compression),
    // to measure an importer change without a 35-minute build. Bridge: WebSizeTrimmer.Sizes(30).
    public static string Sizes(int top)
    {
        var util = typeof(Editor).Assembly.GetType("UnityEditor.TextureUtil");
        var size = util.GetMethod("GetStorageMemorySizeLong", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        var rows = new System.Collections.Generic.List<(long bytes, string line)>();
        long total = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Project", "Packages/com.unity.render-pipelines.universal/Textures" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) continue;
            long bytes = (long)size.Invoke(null, new object[] { tex });
            total += bytes;
            rows.Add((bytes, $"{bytes / 1024,7} KB  {tex.width}x{tex.height} {tex.format}  {path}"));
        }
        rows.Sort((x, y) => y.bytes.CompareTo(x.bytes));
        var sb = new System.Text.StringBuilder($"target={EditorUserBuildSettings.activeBuildTarget} all={total / 1024} KB\n");
        foreach (var r in rows.GetRange(0, System.Math.Min(top, rows.Count))) sb.AppendLine(r.line);
        System.IO.File.WriteAllText("Temp/_sizes.txt", sb.ToString());
        return "Temp/_sizes.txt";
    }

    // URP's post-processing data carries ten film-grain noise textures (2.5 MB, noise does not compress) that ship
    // with every build, used or not; no volume here uses film grain. The renderer gets its own copy of the data with
    // the film-grain list empty (the package's asset can't be changed; the global settings route did not persist).
    const string LeanPostData = "Assets/Project/[GAME]/Settings/PostProcessData_Web.asset";
    const string GrainStub = "Assets/Project/[GAME]/Settings/FilmGrainStub.png";   // 4x4 grey

    // URP keeps a second copy of the same textures in its global settings (PostProcessData.TextureResources), and
    // fills any EMPTY entry back from the package as soon as the asset is loaded - emptying the list never stuck.
    // Pointing the ten entries at a 4x4 stand-in does: nothing is empty, nothing is reloaded. Done as a text edit
    // of the asset, because saving the global settings from the editor also wipes its m_RuntimeSettings list.
    // Re-run after anything resets that file (a git checkout of it).
    public static string StubFilmGrain()
    {
        string stubGuid = AssetDatabase.AssetPathToGUID(GrainStub);
        string text = File.ReadAllText(GlobalSettings);
        // The file can have either line ending (git turns it into CRLF on checkout).
        var match = System.Text.RegularExpressions.Regex.Match(text, @"\n([ ]+)filmGrainTex:(\r?\n)((?:\1- \{fileID: 2800000, guid: [0-9a-f]+, type: 3\}\r?\n)+)");
        if (!match.Success) return "no filmGrainTex list";
        string indent = match.Groups[1].Value, newline = match.Groups[2].Value;
        var entries = match.Groups[3];
        int count = System.Text.RegularExpressions.Regex.Matches(entries.Value, "guid:").Count;
        var list = new System.Text.StringBuilder();
        for (int i = 0; i < count; i++) list.Append(indent).Append("- {fileID: 2800000, guid: ").Append(stubGuid).Append(", type: 3}").Append(newline);
        text = text.Substring(0, entries.Index) + list + text.Substring(entries.Index + entries.Length);
        File.WriteAllText(GlobalSettings, text);
        AssetDatabase.ImportAsset(GlobalSettings, ImportAssetOptions.ForceSynchronousImport);
        return $"{count} film grain entries -> stub {stubGuid}";
    }
    const string RendererPath = "Assets/Project/[GAME]/Settings/New Universal Render Pipeline Asset_Renderer.asset";

    public static string LeanPostProcess()
    {
        var renderer = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(RendererPath);
        var rendererSo = new SerializedObject(renderer);
        var current = rendererSo.FindProperty("postProcessData");
        var data = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.PostProcessData>(LeanPostData);
        if (data == null)
        {
            var source = (UnityEngine.Rendering.Universal.PostProcessData)current.objectReferenceValue;
            data = Object.Instantiate(source);
            AssetDatabase.CreateAsset(data, LeanPostData);
        }
        var stub = AssetDatabase.LoadAssetAtPath<Texture2D>(GrainStub);
        var so = new SerializedObject(data);
        var grain = so.FindProperty("textures.filmGrainTex");
        int before = grain.arraySize;
        grain.arraySize = 10;   // as many as URP has (an empty list would be filled back from the package)
        for (int i = 0; i < grain.arraySize; i++) grain.GetArrayElementAtIndex(i).objectReferenceValue = stub;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        current.objectReferenceValue = data;
        rendererSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer);
        AssetDatabase.SaveAssets();
        return $"film grain {before} -> {new SerializedObject(data).FindProperty("textures.filmGrainTex").arraySize}, renderer -> {AssetDatabase.GetAssetPath(new SerializedObject(renderer).FindProperty("postProcessData").objectReferenceValue)}";
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
