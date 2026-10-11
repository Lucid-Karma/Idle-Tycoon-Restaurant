using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
#if PROJECT_AUDITOR_AVAILABLE
using Unity.ProjectAuditor.Editor;
#endif

namespace CrazyGames
{
    /// <summary>Builds the AnalyzerReport based on the provided build report summary.</summary>
    public class Analyzer
    {
        private readonly List<string> MODEL_FORMATS = new List<string>() { ".fbx", ".dae", ".3ds", ".dxf", ".obj" };
        public const float DEFAULT_FIXED_TIMESTEP = 0.02f;

        public AnalyzerReport Analyze(BuildReportSummary reportSummary)
        {
            Debug.Log("Analyzing project...");
            var report = new AnalyzerReport();
            report.buildReportSummary = reportSummary;

            EditorUtility.DisplayProgressBar("Analyzing Assets", $"Analyzing Assets", 0.25f);

            var files = reportSummary.packagedFiles.OrderBy(f => f.path).ToList();
            foreach (var file in files)
            {
                var filePath = file.path;
                if (filePath.Contains("/Resources/") && filePath.StartsWith("Assets/"))
                {
                    report.resources.Add(filePath);
                }

                if (AssetDatabase.GetMainAssetTypeAtPath(filePath) == typeof(AudioClip))
                {
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(filePath);
                    float length = clip.length;
                    if (clip.length > 10)
                    {
                        AudioImporter importer = AssetImporter.GetAtPath(filePath) as AudioImporter;
                        var optimizations = new List<AudioOptimizationType>();
                        if (!importer.forceToMono)
                        {
                            optimizations.Add(AudioOptimizationType.ForceToMono);
                        }
                        if (importer.defaultSampleSettings.quality > 0.75f)
                        {
                            optimizations.Add(AudioOptimizationType.ReduceQuality);
                        }
                        if (optimizations.Count > 0)
                        {
                            report.audioOptimizations.Add(new AudioOptimization(filePath, length, optimizations));
                        }
                    }
                }

                if (AssetDatabase.GetMainAssetTypeAtPath(filePath) == typeof(Texture2D))
                {
                    Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(filePath);
                    TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
                    var optimizations = new List<TextureOptimizationType>();
                    // width/height will be clamped to the texture importer max size (original image size does not matter)
                    if (texture.width > 1024 || texture.height > 1024)
                    {
                        optimizations.Add(TextureOptimizationType.ReduceSize);
                    }
                    if (optimizations.Count > 0)
                    {
                        report.textureOptimizations.Add(new TextureOptimization(filePath, optimizations));
                    }
                    if (texture.isReadable)
                    {
                        report.readWriteTexturePaths.Add(filePath);
                    }
                    if (importer.mipmapEnabled)
                    {
                        report.mipmapTexturePaths.Add(filePath);
                    }
                }

                if (IsModelAtPath(filePath))
                {
                    var modelImporter = (ModelImporter)AssetImporter.GetAtPath(filePath);
                    if (modelImporter.isReadable)
                    {
                        report.readWriteMeshPaths.Add(filePath);
                    }
                }
            }

            report.showUrpPostProcessing = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null;

#if ADDRESSABLE_AVAILABLE
            report.showAddressables = false;
#else

            report.showAddressables = true;
#endif

            var pipelineAsset = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
            if (pipelineAsset != null)
            {
                var property = pipelineAsset.GetType().GetProperty("msaaSampleCount");
                if (property != null)
                {
                    var currentLevel = (int)property.GetValue(pipelineAsset);
                    report.showAntiAliasing = currentLevel > 2; // 1 is disabled, 2 is 2x, 4 is 4x, etc.
                }
            }

            report.showPhysicsTimestep = UnityEngine.Time.fixedDeltaTime < DEFAULT_FIXED_TIMESTEP;

            report.scenesWithObjectsWithNullScripts = FindScenesWithObjectsWithNullScripts();

#if PROJECT_AUDITOR_AVAILABLE
            EditorUtility.DisplayProgressBar("Analyzing Code", "Analyzing Code", 0.75f);
            report.auditorReport = new ProjectAuditor().Audit(
                new AnalysisParams()
                {
                    Categories = new IssueCategory[] { IssueCategory.Code },
                    AssemblyNames = new[] { "Assembly-CSharp", "Assembly-CSharp-Editor" },
                }
            );
#endif

            EditorUtility.ClearProgressBar();

            return report;
        }

        bool IsModelAtPath(string assetDependency)
        {
            return AssetDatabase.GetMainAssetTypeAtPath(assetDependency) == typeof(GameObject)
                && MODEL_FORMATS.Contains(System.IO.Path.GetExtension(assetDependency).ToLowerInvariant());
        }

        private Dictionary<string, List<string>> FindScenesWithObjectsWithNullScripts()
        {
            EditorUtility.DisplayProgressBar("Analyzing Scenes", "Analyzing Scenes", 0.5f);

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToList();
            var sceneCount = scenes.Count;

            var result = new Dictionary<string, List<string>>();

            foreach (var scene in scenes)
            {
                var loadedScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                var objectsWithMissingScripts = new List<string>();

                foreach (var go in loadedScene.GetRootGameObjects())
                {
                    foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    {
                        var components = t.gameObject.GetComponents<Component>();
                        foreach (var comp in components)
                        {
                            if (comp == null)
                            {
                                objectsWithMissingScripts.Add(t.gameObject.name);
                                break;
                            }
                        }
                    }
                }

                if (objectsWithMissingScripts.Count > 0)
                {
                    result[Path.GetFileNameWithoutExtension(scene.path)] = objectsWithMissingScripts;
                }
            }
            EditorUtility.ClearProgressBar();

            return result;
        }
    }
}
