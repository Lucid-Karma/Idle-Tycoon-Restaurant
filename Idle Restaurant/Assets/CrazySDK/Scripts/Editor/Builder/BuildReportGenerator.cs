using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
namespace CrazyGames
{
    [System.Serializable]
    public class BuildOptimization
    {
        public string optimization;
        public string initialValue;
        public string buildValue;

        public BuildOptimization(string optimization, string initialValue, string buildValue)
        {
            this.optimization = optimization;
            this.initialValue = initialValue;
            this.buildValue = buildValue;
        }
    }

    [System.Serializable]
    public class BuildData
    {
        public string buildDateTime;
        public string buildScriptVersion;
        public string sdkVersion;
        public string unityVersion;
        public string buildVariant;
        public AdditionalBuildOptions additionalOptions;
        public string buildResult;
        public double totalTime;
        public List<BuildOptimization> buildOptimizations;
        public List<GeneratedFile> generatedFiles;
    }

    [System.Serializable]
    public class GeneratedFile
    {
        public string filename;
        public string type;
        public ulong uncompressedSize;
        public ulong size;
    }

    /// <summary>
    /// Generates the report that will be uploaded to Developer Portal, contains information about the build.
    /// </summary>
    public class BuildReportGenerator
    {
        private const string BUILD_SCRIPT_VERSION = "1.4.0";

        public void GenerateReport(
            BuildReport report,
            BuildVariant variant,
            AdditionalBuildOptions additionalOptions,
            string buildPath,
            DateTime startTime,
            List<BuildOptimization> buildOptimizations,
            string astcDataFileName,
            LimitedMemoryFiles limitedMemoryFiles
        )
        {
            var generatedFiles = GetMainFiles(report);
            if (!string.IsNullOrEmpty(astcDataFileName))
            {
                var path = Path.Combine(buildPath, "Build", astcDataFileName);
                generatedFiles.Add(
                    new GeneratedFile
                    {
                        filename = astcDataFileName,
                        type = "data_astc",
                        uncompressedSize = GetUncompressedSize(path, astcDataFileName),
                        size = (ulong)new FileInfo(path).Length,
                    }
                );
            }

            // limited memory files will be missing if the game doesn't support mobile devices
            if (limitedMemoryFiles != null)
            {
                if (limitedMemoryFiles.max512mb != null)
                {
                    var path512 = Path.Combine(buildPath, "Build", limitedMemoryFiles.max512mb);
                    generatedFiles.Add(
                        new GeneratedFile
                        {
                            filename = limitedMemoryFiles.max512mb,
                            type = "wasm_max_512mb",
                            uncompressedSize = GetUncompressedSize(path512, limitedMemoryFiles.max512mb),
                            size = (ulong)new FileInfo(path512).Length,
                        }
                    );
                }
                if (limitedMemoryFiles.max1024mb != null)
                {
                    var path1024 = Path.Combine(buildPath, "Build", limitedMemoryFiles.max1024mb);
                    generatedFiles.Add(
                        new GeneratedFile
                        {
                            filename = limitedMemoryFiles.max1024mb,
                            type = "wasm_max_1024mb",
                            uncompressedSize = GetUncompressedSize(path1024, limitedMemoryFiles.max1024mb),
                            size = (ulong)new FileInfo(path1024).Length,
                        }
                    );
                }
            }

            var buildData = new BuildData
            {
                buildDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                buildScriptVersion = BUILD_SCRIPT_VERSION,
                sdkVersion = CrazySDK.Version,
                unityVersion = Application.unityVersion,
                buildVariant = variant.ToString(),
                additionalOptions = additionalOptions,
                buildResult = report.summary.result.ToString(),
                totalTime = (DateTime.Now - startTime).TotalSeconds,
                buildOptimizations = buildOptimizations,
                generatedFiles = generatedFiles,
            };

            var json = JsonUtility.ToJson(buildData, true);
            var reportPath = Path.Combine(buildPath, "crazygames_build_report.json");

            File.WriteAllText(reportPath, json);
        }

        public static List<GeneratedFile> GetMainFiles(BuildReport report)
        {
            var files = new List<GeneratedFile>();

            // initially, these are the files we are interested in
            var filePatterns = new Dictionary<string, string>
            {
                { ".data.br", "data" },
                { ".data", "data" },
                { ".framework.js.br", "framework" },
                { ".framework.js", "framework" },
                { ".loader.js", "loader" },
                { ".wasm.br", "wasm" },
                { ".wasm", "wasm" },
            };

            foreach (var file in report.GetFiles())
            {
                var filename = Path.GetFileName(file.path);
                foreach (var pattern in filePatterns)
                {
                    if (filename.EndsWith(pattern.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        var generatedFile = new GeneratedFile
                        {
                            filename = filename,
                            type = pattern.Value,
                            uncompressedSize = GetUncompressedSize(file.path, filename),
                            size = file.size,
                        };
                        files.Add(generatedFile);
                        break;
                    }
                }
            }

            return files.OrderBy(f => f.type).ToList();
        }

        private static ulong GetUncompressedSize(string filePath, string filename)
        {
            if (filename.EndsWith(".br", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using (var compressedFile = File.OpenRead(filePath))
                    using (var brotli = new BrotliStream(compressedFile, CompressionMode.Decompress))
                    using (var memoryStream = new MemoryStream())
                    {
                        brotli.CopyTo(memoryStream);
                        return (ulong)memoryStream.Length;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not get uncompressed size for {filename}: {e.Message}");
                    return (ulong)new FileInfo(filePath).Length;
                }
            }

            return (ulong)new FileInfo(filePath).Length;
        }
    }
}
#endif
