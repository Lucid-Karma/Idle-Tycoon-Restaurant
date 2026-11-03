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
// - Phones get their own data (2026-10-30; user: "on beyzosh.com from a phone it feels like a long wait - the game
//   must open fast, especially the CrazyGames build"): the textures are DXT, which phone GPUs cannot read, so every
//   one was unpacked by the CPU on every load. A second build in ASTC (what phones read as it is) gives its data file,
//   copied next to the first as "astc_<name>" - what the CrazySDK's own release builder does (Builder.DoASTCBuild).
//   For CrazyGames also its other two phone builds (the wasm with 512 MB and 1024 MB of memory at most) and its
//   report (crazygames_build_report.json, BuildReportGenerator): their site picks the files per device. On beyzosh.com
//   the page picks the data itself (WebGL template: astcDataFile, filled in here).
// - StreamingAssets (the music, the shop pictures) stays next to Build and is uploaded with it: CrazyGames serves it
//   and does not count it in the first download (docs.crazygames.com/resources/optimization-tips). The game asks for
//   those files only once the kitchen is on screen. (2026-10-31: they were put in the data for a day, after a QA run
//   without that folder - 3.6 MB more to download before the first frame.)
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
        var subtarget = EditorUserBuildSettings.webGLBuildSubtarget;
        int maxMemory = PlayerSettings.WebGL.maximumMemorySize;
        var started = DateTime.Now;
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
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = outDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.CleanBuildCache,
            };
            var report = BuildPipeline.BuildPlayer(options);
            string summary = $"{report.summary.result}, {report.summary.totalSize / 1048576f:0.0} MB, errors={report.summary.totalErrors}";
            if (report.summary.result == BuildResult.Succeeded)
            {
                // The phones' data (and for CrazyGames their smaller-memory wasm), from builds that differ in that only.
                string astc = Variant(options, ".data", "astc_", () => EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC);
                EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
                summary += astc != null ? ", + " + astc : ", NO ASTC DATA";
                if (crazyGames)
                {
                    var limited = new CrazyGames.LimitedMemoryFiles
                    {
                        max512mb = Variant(options, ".wasm", "max_512mb_", () => PlayerSettings.WebGL.maximumMemorySize = 512),
                        max1024mb = Variant(options, ".wasm", "max_1024mb_", () => PlayerSettings.WebGL.maximumMemorySize = 1024),
                    };
                    PlayerSettings.WebGL.maximumMemorySize = maxMemory;
                    var settings = new System.Collections.Generic.List<CrazyGames.BuildOptimization>
                    {
                        new("compressionFormat", compression.ToString(), "Brotli"),
                        new("codeOptimization", optimization.ToString(), "DiskSizeLTO"),
                        new("il2cppCodeGeneration", codeGen.ToString(), "OptimizeSize"),
                        new("managedStrippingLevel", stripping.ToString(), "Medium"),
                        new("stripEngineCode", strip.ToString(), "True"),
                        new("nameFilesAsHashes", hashes.ToString(), "True"),
                        new("debugSymbolMode", symbols.ToString(), "Off"),
                        new("dataCaching", PlayerSettings.WebGL.dataCaching.ToString(), PlayerSettings.WebGL.dataCaching.ToString()),
                    };
                    new CrazyGames.BuildReportGenerator().GenerateReport(report, CrazyGames.BuildVariant.Release,
                        new CrazyGames.AdditionalBuildOptions { supportsMobile = true }, outDir, started, settings, astc, limited);
                    summary += $", + {limited.max512mb}, {limited.max1024mb}, report";
                }
                else if (astc != null) PointPageAt(outDir, astc);
            }
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
            EditorUserBuildSettings.webGLBuildSubtarget = subtarget;
            PlayerSettings.WebGL.maximumMemorySize = maxMemory;
        }
    }

    // Builds again with `change` made, next to the build (without cleaning the cache: only that differs), and copies
    // its file of `kind` (".data" or ".wasm") into the build's Build folder as `prefix` + its name. Null if it failed.
    static string Variant(BuildPlayerOptions options, string kind, string prefix, Action change)
    {
        change();
        var other = options;
        other.locationPathName = options.locationPathName + "_variant";
        other.options = BuildOptions.None;
        if (Directory.Exists(other.locationPathName)) Directory.Delete(other.locationPathName, true);
        try
        {
            var report = BuildPipeline.BuildPlayer(other);
            if (report.summary.result != BuildResult.Succeeded) { Debug.LogError($"[WebBuild] {prefix} build failed"); return null; }
            var file = Directory.GetFiles(Path.Combine(other.locationPathName, "Build")).FirstOrDefault(f => Path.GetFileName(f).Contains(kind + "."));
            if (file == null) return null;
            string name = prefix + Path.GetFileName(file);
            File.Copy(file, Path.Combine(options.locationPathName, "Build", name), true);
            return name;
        }
        finally
        {
            if (Directory.Exists(other.locationPathName)) Directory.Delete(other.locationPathName, true);
        }
    }

    // The beyzosh.com page learns the name of the phones' data file (its template leaves astcDataFile empty).
    static void PointPageAt(string outDir, string astc)
    {
        var page = Path.Combine(outDir, "index.html");
        var html = File.ReadAllText(page);
        const string empty = "var astcDataFile = \"\";";
        if (!html.Contains(empty)) { Debug.LogError("[WebBuild] the page has no astcDataFile"); return; }
        File.WriteAllText(page, html.Replace(empty, $"var astcDataFile = \"{astc}\";"));
    }
}
