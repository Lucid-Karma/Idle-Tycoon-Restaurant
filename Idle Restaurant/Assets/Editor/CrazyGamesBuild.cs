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
// Never a development build (those don't run on CrazyGames). The Boot scene starts the CrazyGames SDK.
public static class CrazyGamesBuild
{
    static string OutDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "Chibi CrazyGames", "chibi-burger-cafe");

    [MenuItem("Tools/Chibi Cafe/Build for CrazyGames")]
    public static void Build() => BuildTo(OutDir);

    public static string BuildTo(string outDir)
    {
        int width = PlayerSettings.defaultWebScreenWidth, height = PlayerSettings.defaultWebScreenHeight;
        var compression = PlayerSettings.WebGL.compressionFormat;
        bool fallback = PlayerSettings.WebGL.decompressionFallback;
        var codeGen = PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL);
        var optimization = UnityEditor.WebGL.UserBuildSettings.codeOptimization;
        bool hashes = PlayerSettings.WebGL.nameFilesAsHashes, strip = PlayerSettings.stripEngineCode;
        var symbols = PlayerSettings.WebGL.debugSymbolMode;
        bool bigInt = PlayerSettings.WebGL.webAssemblyBigInt, table = PlayerSettings.WebGL.webAssemblyTable, wasm2023 = PlayerSettings.WebGL.wasm2023;
        try
        {
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
            PlayerSettings.WebGL.webAssemblyBigInt = true;
            PlayerSettings.WebGL.webAssemblyTable = true;
            PlayerSettings.WebGL.wasm2023 = true;
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = false;
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.CleanBuildCache,
            });
            string summary = $"{report.summary.result}, {report.summary.totalSize / 1048576f:0.0} MB, errors={report.summary.totalErrors}";
            Debug.Log("[CrazyGamesBuild] " + summary + " -> " + outDir);
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
            PlayerSettings.WebGL.debugSymbolMode = symbols;
            PlayerSettings.WebGL.webAssemblyBigInt = bigInt;
            PlayerSettings.WebGL.webAssemblyTable = table;
            PlayerSettings.WebGL.wasm2023 = wasm2023;
        }
    }
}
