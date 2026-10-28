using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// The build to upload to CrazyGames (developer.crazygames.com, "Upload files": everything in the output folder).
// Differences from the beyzosh.com build, all restored afterwards:
// - 16:9 instead of 16:10: CrazyGames shows games in a 16:9 frame (and its own fullscreen), and the WebGL template
//   keeps the game at the canvas size's shape - at 16:10 it would sit between two bars.
// - Brotli, no decompression fallback: CrazyGames only takes compressed builds and serves them itself.
// - CrazyGames' own release recipe (CrazySDK/Scripts/Editor/Builder/Builder.cs, "Release"): IL2CPP code optimised
//   for size, wasm "Disk Size with LTO", engine stripping, files named by hash, no debug symbols, wasm 2023 features.
// - Managed code stripped at Medium instead of Low (2026-10-17, a smaller wasm and IL2CPP metadata for a quicker
//   first load); Assets/link.xml keeps the game, the SDK and the Input System whole.
// Never a development build (those don't run on CrazyGames). The Boot scene starts the CrazyGames SDK.
public static class CrazyGamesBuild
{
    static string OutDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "Chibi CrazyGames", "chibi-burger-cafe");

    [MenuItem("Tools/Chibi Cafe/Build for CrazyGames")]
    public static void Build() => BuildTo(OutDir);

    public static string BuildTo(string outDir) => Build(outDir, crazyGames: true);

    // The beyzosh.com build (games.beyzosh.com/<game>/ on R2, 2026-10-20): the same release recipe, but in the
    // project's own shape (16:10) and with the decompression fallback on (.unityweb files), so it opens even if
    // the files are served without their brotli header. Upload them WITH "Content-Encoding: br" all the same:
    // the browser then unpacks them natively, much faster than the JavaScript fallback.
    [MenuItem("Tools/Chibi Cafe/Build for beyzosh.com")]
    public static void BuildWebMenu() => BuildWeb(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Chibi R", "WebGL", "chibi-burger-cafe"));

    public static string BuildWeb(string outDir) => Build(outDir, crazyGames: false);

    static string Build(string outDir, bool crazyGames)
    {
        int width = PlayerSettings.defaultWebScreenWidth, height = PlayerSettings.defaultWebScreenHeight;
        var compression = PlayerSettings.WebGL.compressionFormat;
        bool fallback = PlayerSettings.WebGL.decompressionFallback;
        var codeGen = PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL);
        var optimization = UnityEditor.WebGL.UserBuildSettings.codeOptimization;
        bool hashes = PlayerSettings.WebGL.nameFilesAsHashes, strip = PlayerSettings.stripEngineCode;
        var symbols = PlayerSettings.WebGL.debugSymbolMode;
        var stripping = PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL);
        bool bigInt = PlayerSettings.WebGL.webAssemblyBigInt, table = PlayerSettings.WebGL.webAssemblyTable, wasm2023 = PlayerSettings.WebGL.wasm2023;
        try
        {
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.webAssemblyBigInt = true;
            PlayerSettings.WebGL.webAssemblyTable = true;
            PlayerSettings.WebGL.wasm2023 = true;
            if (crazyGames)
            {
                PlayerSettings.defaultWebScreenWidth = 1920;
                PlayerSettings.defaultWebScreenHeight = 1080;
            }
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = !crazyGames;
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.CleanBuildCache,
            });
            string summary = $"{report.summary.result}, {report.summary.totalSize / 1048576f:0.0} MB, errors={report.summary.totalErrors}";
            Debug.Log($"[{(crazyGames ? "CrazyGamesBuild" : "WebBuild")}] " + summary + " -> " + outDir);
            return summary;
        }
        finally
        {
            PlayerSettings.defaultWebScreenWidth = width;
            PlayerSettings.defaultWebScreenHeight = height;
            PlayerSettings.WebGL.compressionFormat = compression;
            PlayerSettings.WebGL.decompressionFallback = fallback;
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, codeGen);
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = optimization;
            PlayerSettings.WebGL.nameFilesAsHashes = hashes;
            PlayerSettings.stripEngineCode = strip;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, stripping);
            PlayerSettings.WebGL.debugSymbolMode = symbols;
            PlayerSettings.WebGL.webAssemblyBigInt = bigInt;
            PlayerSettings.WebGL.webAssemblyTable = table;
            PlayerSettings.WebGL.wasm2023 = wasm2023;
        }
    }
}
