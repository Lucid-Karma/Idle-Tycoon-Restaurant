using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
namespace CrazyGames
{
    public class Builder
    {
        private WebGLCompressionFormat _initialCompressionFormat;
        private WebGLExceptionSupport _initialExceptionSupport;
        private WebGLDebugSymbolMode _initialDebugSymbolMode;
        private bool _initialDataCaching;
        private bool _initialNameFilesAsHashes;
        private bool _initialStripEngineCode;
        private UnityEditor.WebGL.WasmCodeOptimization _initialCodeOptimization;
        private Il2CppCodeGeneration _initialIl2CppCodeGeneration;
        private bool _initialShowSplashScreen;
        private bool _initialShowUnityLogo;
        private bool _initialWasm2023;
        private bool _initialWebAssemblyTable;
        private bool _initialWebAssemblyBigInt;
        private WebGLTextureSubtarget _initialWebGLTextureSubtarget;

        public const string SUMMARY_FILE_NAME = "CGReleaseBuildReportSummary-v1.json";
        public const string INTERMEDIATE_BUILD_SUFFIX = "_cg_intermediate_build";

        public void DoDevelopmentBuild()
        {
            Build(BuildVariant.Development, null);
        }

        public void DoReleaseBuild(AdditionalBuildOptions additionalOptions)
        {
            Build(BuildVariant.Release, additionalOptions);
        }

        private void Build(BuildVariant selectedVariant, AdditionalBuildOptions additionalOptions)
        {
            var startTime = System.DateTime.Now;

            var scenePaths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();

            if (scenePaths.Length == 0)
            {
                Debug.LogError("There aren't any enabled scenes in the build settings. Please add at least one scene.");
                return;
            }

            var buildPath = GetBuildPath(selectedVariant);

            var buildOptions = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = GetBuildOptions(selectedVariant),
            };
            if (Directory.Exists(buildOptions.locationPathName))
            {
                Directory.Delete(buildOptions.locationPathName, true);
            }

            StoreInitialSettings();
            var buildOptimizations = ConfigureWebGLSettings(selectedVariant);
            var optimizationsDebugString = string.Join(
                "\n",
                buildOptimizations.Select(o => $"{o.optimization}: {o.initialValue} => {o.buildValue}")
            );

            try
            {
                EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.DXT;
                var report = BuildPipeline.BuildPlayer(buildOptions);

                if (report.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log(
                        $"Build succeeded in {report.summary.totalTime.TotalMinutes:F1} minutes\nWith these settings:\n{optimizationsDebugString}"
                    );
                    if (selectedVariant == BuildVariant.Release)
                    {
                        var astcDataFileName = DoASTCBuild(buildOptions, report);
                        LimitedMemoryFiles limitedMemoryFiles = null;
                        if (additionalOptions.supportsMobile)
                        {
                            limitedMemoryFiles = DoLimitedMemoryBuilds(buildOptions, report);
                        }
                        var reportGenerator = new BuildReportGenerator();
                        reportGenerator.GenerateReport(
                            report,
                            selectedVariant,
                            additionalOptions,
                            buildPath,
                            startTime,
                            buildOptimizations,
                            astcDataFileName,
                            limitedMemoryFiles
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        $"Build failed! {report.summary.totalTime.TotalMinutes:F1} minutes\nWith these settings:\n{optimizationsDebugString}"
                    );
                    foreach (var step in report.steps)
                    {
                        foreach (var message in step.messages.Where(m => m.type == LogType.Error))
                        {
                            Debug.LogError($"Build Error: {message.content}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Build failed with exception: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                RestoreWebGLSettings();
                AssetDatabase.SaveAssets(); // restore changed PlayerSettings
            }
        }

        /// <summary>
        /// Does a second build with ASTC compression format.
        /// Copies the data file from the ASTC build to the original build folder, prefixed with "astc_".
        /// Returns the name of the copied data file, ex astc_cdadfac5d5a5d2c11639cfca63e6d4f8.data.br
        /// </summary>
        private string DoASTCBuild(BuildPlayerOptions originalBuildOptions, BuildReport originalReport)
        {
            string astcDataFileName = null;
            var initialWebGLBuildSubtarget = EditorUserBuildSettings.webGLBuildSubtarget;
            EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ASTC;
            var astcBuildOptions = originalBuildOptions;
            astcBuildOptions.locationPathName += INTERMEDIATE_BUILD_SUFFIX;
            if (Directory.Exists(astcBuildOptions.locationPathName))
            {
                Directory.Delete(astcBuildOptions.locationPathName, true);
            }
            // reset options, don't need autorun, and don't clean cache for quicker build
            astcBuildOptions.options = BuildOptions.None;
            var astcReport = BuildPipeline.BuildPlayer(astcBuildOptions);
            if (astcReport.summary.result == BuildResult.Succeeded)
            {
                var files = astcReport.GetFiles();
                var dataFile = files.First(f => Path.GetFileName(f.path).EndsWith(".data.br", StringComparison.OrdinalIgnoreCase));
                var dataFileName = Path.GetFileName(dataFile.path);
                astcDataFileName = "astc_" + dataFileName;
                var from = Path.Combine(astcReport.summary.outputPath, "Build", dataFileName);
                var to = Path.Combine(originalReport.summary.outputPath, "Build", astcDataFileName);
                File.Copy(from, to, true);
            }
            if (Directory.Exists(astcBuildOptions.locationPathName))
            {
                Directory.Delete(astcBuildOptions.locationPathName, true);
            }
            EditorUserBuildSettings.webGLBuildSubtarget = initialWebGLBuildSubtarget;
            return astcDataFileName;
        }

        /// <summary>
        /// Does 2 separate builds with 512MB and 1024MB max memory size.
        /// Copies the wasm files to the original build folder, prefixed with "max_{memory}mb_".
        /// Returns the names of the copied files, ex max_512mb_cdadfac5d5a5d2c11639cfca63e6d4f8.data.br
        /// </summary>
        private LimitedMemoryFiles DoLimitedMemoryBuilds(BuildPlayerOptions originalBuildOptions, BuildReport originalReport)
        {
            var fileNames = new LimitedMemoryFiles { max512mb = null, max1024mb = null };
            var initialMaxMemorySize = PlayerSettings.WebGL.maximumMemorySize;
            fileNames.max512mb = DoBuildWithMaxMemorySize(512, originalBuildOptions, originalReport);
            fileNames.max1024mb = DoBuildWithMaxMemorySize(1024, originalBuildOptions, originalReport);
            PlayerSettings.WebGL.maximumMemorySize = initialMaxMemorySize;
            return fileNames;
        }

        private string DoBuildWithMaxMemorySize(int memorySize, BuildPlayerOptions originalBuildOptions, BuildReport originalReport)
        {
            string prefixedFileName = null;
            PlayerSettings.WebGL.maximumMemorySize = memorySize;
            var buildOptions = originalBuildOptions;
            buildOptions.locationPathName += INTERMEDIATE_BUILD_SUFFIX;
            if (Directory.Exists(buildOptions.locationPathName))
            {
                Directory.Delete(buildOptions.locationPathName, true);
            }
            // reset options, don't need autorun, and don't clean cache for quicker build
            buildOptions.options = BuildOptions.None;
            var report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result == BuildResult.Succeeded)
            {
                var files = report.GetFiles();
                var file = files.First(f => Path.GetFileName(f.path).EndsWith(".wasm.br", StringComparison.OrdinalIgnoreCase));
                var fileName = Path.GetFileName(file.path);
                prefixedFileName = "max_" + memorySize + "mb_" + fileName;
                var from = Path.Combine(report.summary.outputPath, "Build", fileName);
                var to = Path.Combine(originalReport.summary.outputPath, "Build", prefixedFileName);
                File.Copy(from, to, true);
            }
            if (Directory.Exists(buildOptions.locationPathName))
            {
                Directory.Delete(buildOptions.locationPathName, true);
            }
            return prefixedFileName;
        }

        private BuildOptions GetBuildOptions(BuildVariant variant)
        {
            switch (variant)
            {
                case BuildVariant.Development:
                    return BuildOptions.Development | BuildOptions.AutoRunPlayer; // to start the game in browser;
                case BuildVariant.Release:
                    return BuildOptions.AutoRunPlayer // to start the game in browser
#if !UNITY_6000_4_OR_NEWER
                        // to avoid sometimes packedAssets being empty
                        // however Unity 6.4 has an issue and the build fails if CleanBuildCache is used (internal build error, backend has requested build program 6 times)
                        // but CleanBuildCache doesn't seems necessary for 6.4 anyways, as packedAssets was always populated there
                        | BuildOptions.CleanBuildCache
#endif
                    ;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(variant), variant, "Unknown build variant");
            }
        }

        private void StoreInitialSettings()
        {
            _initialCompressionFormat = PlayerSettings.WebGL.compressionFormat;
            _initialExceptionSupport = PlayerSettings.WebGL.exceptionSupport;
            _initialDebugSymbolMode = PlayerSettings.WebGL.debugSymbolMode;
            _initialDataCaching = PlayerSettings.WebGL.dataCaching;
            _initialNameFilesAsHashes = PlayerSettings.WebGL.nameFilesAsHashes;
            _initialStripEngineCode = PlayerSettings.stripEngineCode;
            _initialIl2CppCodeGeneration = PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL);
            _initialCodeOptimization = UnityEditor.WebGL.UserBuildSettings.codeOptimization;
            _initialShowSplashScreen = PlayerSettings.SplashScreen.show;
            _initialShowUnityLogo = PlayerSettings.SplashScreen.showUnityLogo;
            _initialWebAssemblyBigInt = PlayerSettings.WebGL.webAssemblyBigInt;
            _initialWebAssemblyTable = PlayerSettings.WebGL.webAssemblyTable;
            _initialWasm2023 = PlayerSettings.WebGL.wasm2023;
            _initialWebGLTextureSubtarget = EditorUserBuildSettings.webGLBuildSubtarget;
        }

        private List<BuildOptimization> ConfigureWebGLSettings(BuildVariant variant)
        {
            var optimizations = new List<BuildOptimization>();

            try
            {
                var webglTarget = NamedBuildTarget.WebGL;

                var buildIl2CppCodeGeneration = Il2CppCodeGeneration.OptimizeSize;
                optimizations.Add(
                    new BuildOptimization(
                        "il2CppCodeGeneration",
                        _initialIl2CppCodeGeneration.ToString(),
                        buildIl2CppCodeGeneration.ToString()
                    )
                );
                PlayerSettings.SetIl2CppCodeGeneration(webglTarget, buildIl2CppCodeGeneration);

                optimizations.Add(new BuildOptimization("showSplashScreen", _initialShowSplashScreen.ToString(), "False"));
                PlayerSettings.SplashScreen.show = false;

                optimizations.Add(new BuildOptimization("showUnityLogo", _initialShowUnityLogo.ToString(), "False"));
                PlayerSettings.SplashScreen.showUnityLogo = false;

                switch (variant)
                {
                    case BuildVariant.Development:
                        ApplyDevelopmentSettings(optimizations);
                        break;
                    case BuildVariant.Release:
                        ApplyReleaseSettings(optimizations);
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not configure WebGL: {e.Message}");
            }

            return optimizations;
        }

        private void ApplyDevelopmentSettings(List<BuildOptimization> optimizations)
        {
            var buildCompression = WebGLCompressionFormat.Disabled;
            optimizations.Add(
                new BuildOptimization("compressionFormat", _initialCompressionFormat.ToString(), buildCompression.ToString())
            );
            PlayerSettings.WebGL.compressionFormat = buildCompression;

            var buildExceptionSupport = WebGLExceptionSupport.FullWithStacktrace;
            optimizations.Add(
                new BuildOptimization("exceptionSupport", _initialExceptionSupport.ToString(), buildExceptionSupport.ToString())
            );
            PlayerSettings.WebGL.exceptionSupport = buildExceptionSupport;

            var buildDebugSymbols = WebGLDebugSymbolMode.External;
            optimizations.Add(new BuildOptimization("debugSymbolMode", _initialDebugSymbolMode.ToString(), buildDebugSymbols.ToString()));
            PlayerSettings.WebGL.debugSymbolMode = buildDebugSymbols;

            optimizations.Add(new BuildOptimization("dataCaching", _initialDataCaching.ToString(), "False"));
            PlayerSettings.WebGL.dataCaching = false;

            optimizations.Add(new BuildOptimization("nameFilesAsHashes", _initialNameFilesAsHashes.ToString(), "False"));
            PlayerSettings.WebGL.nameFilesAsHashes = false;

            optimizations.Add(new BuildOptimization("stripEngineCode", _initialStripEngineCode.ToString(), "False"));
            PlayerSettings.stripEngineCode = false;

            var buildCodeOptimization = UnityEditor.WebGL.WasmCodeOptimization.BuildTimes;
            optimizations.Add(
                new BuildOptimization("codeOptimization", _initialCodeOptimization.ToString(), buildCodeOptimization.ToString())
            );
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = buildCodeOptimization;
        }

        private void ApplyReleaseSettings(List<BuildOptimization> optimizations)
        {
            var buildCompression = WebGLCompressionFormat.Brotli;
            optimizations.Add(
                new BuildOptimization("compressionFormat", _initialCompressionFormat.ToString(), buildCompression.ToString())
            );
            PlayerSettings.WebGL.compressionFormat = buildCompression;

            if (_initialExceptionSupport == WebGLExceptionSupport.None)
            {
                // if exception support is set to None, which is even better than ExplicitlyThrownExceptionsOnly, we keep it as is
                optimizations.Add(
                    new BuildOptimization("exceptionSupport", _initialExceptionSupport.ToString(), _initialExceptionSupport.ToString())
                );
            }
            else
            {
                var buildExceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
                optimizations.Add(
                    new BuildOptimization("exceptionSupport", _initialExceptionSupport.ToString(), buildExceptionSupport.ToString())
                );
                PlayerSettings.WebGL.exceptionSupport = buildExceptionSupport;
            }

            var buildDebugSymbols = WebGLDebugSymbolMode.Off;
            optimizations.Add(new BuildOptimization("debugSymbolMode", _initialDebugSymbolMode.ToString(), buildDebugSymbols.ToString()));
            PlayerSettings.WebGL.debugSymbolMode = buildDebugSymbols;

            optimizations.Add(new BuildOptimization("dataCaching", _initialDataCaching.ToString(), "True"));
            PlayerSettings.WebGL.dataCaching = true;

            optimizations.Add(new BuildOptimization("nameFilesAsHashes", _initialNameFilesAsHashes.ToString(), "True"));
            PlayerSettings.WebGL.nameFilesAsHashes = true;

            optimizations.Add(new BuildOptimization("stripEngineCode", _initialStripEngineCode.ToString(), "True"));
            PlayerSettings.stripEngineCode = true;

            var buildCodeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
            optimizations.Add(
                new BuildOptimization("codeOptimization", _initialCodeOptimization.ToString(), buildCodeOptimization.ToString())
            );
            UnityEditor.WebGL.UserBuildSettings.codeOptimization = buildCodeOptimization;

            optimizations.Add(new BuildOptimization("webAssemblyBigInt", _initialWebAssemblyBigInt.ToString(), "True"));
            PlayerSettings.WebGL.webAssemblyBigInt = true;

            optimizations.Add(new BuildOptimization("webAssemblyTable", _initialWebAssemblyTable.ToString(), "True"));
            PlayerSettings.WebGL.webAssemblyTable = true;

            optimizations.Add(new BuildOptimization("wasm2023", _initialWasm2023.ToString(), "True"));
            PlayerSettings.WebGL.wasm2023 = true;
        }

        private void RestoreWebGLSettings()
        {
            try
            {
                PlayerSettings.WebGL.compressionFormat = _initialCompressionFormat;
                PlayerSettings.WebGL.exceptionSupport = _initialExceptionSupport;
                PlayerSettings.WebGL.debugSymbolMode = _initialDebugSymbolMode;
                PlayerSettings.WebGL.dataCaching = _initialDataCaching;
                PlayerSettings.WebGL.nameFilesAsHashes = _initialNameFilesAsHashes;
                PlayerSettings.stripEngineCode = _initialStripEngineCode;
                PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, _initialIl2CppCodeGeneration);
                UnityEditor.WebGL.UserBuildSettings.codeOptimization = _initialCodeOptimization;
                PlayerSettings.SplashScreen.show = _initialShowSplashScreen;
                PlayerSettings.SplashScreen.showUnityLogo = _initialShowUnityLogo;
                PlayerSettings.WebGL.webAssemblyBigInt = _initialWebAssemblyBigInt;
                PlayerSettings.WebGL.webAssemblyTable = _initialWebAssemblyTable;
                PlayerSettings.WebGL.wasm2023 = _initialWasm2023;
                EditorUserBuildSettings.webGLBuildSubtarget = _initialWebGLTextureSubtarget;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not restore WebGL settings: {e.Message}");
            }
        }

        private string GetBuildPath(BuildVariant buildVariant)
        {
            var folder = buildVariant == BuildVariant.Development ? "CrazyGamesDevelopment" : "CrazyGamesRelease";
            return Path.Combine("Builds", folder);
        }
    }

    public enum BuildVariant
    {
        Development,
        Release,
    }

    public class LimitedMemoryFiles
    {
        public string max512mb;
        public string max1024mb;
    }

    [System.Serializable]
    public class AdditionalBuildOptions
    {
        public bool supportsMobile;
    }
}
#endif
