using System.Collections.Generic;
using UnityEditor;
#if PROJECT_AUDITOR_AVAILABLE
using Unity.ProjectAuditor.Editor;
#endif

namespace CrazyGames
{
    /// <summary>Contains the analysis report of the build with various optimization suggestions.</summary>
    public class AnalyzerReport
    {
        public List<AudioOptimization> audioOptimizations = new List<AudioOptimization>();
        public List<TextureOptimization> textureOptimizations = new List<TextureOptimization>();
        public List<string> resources = new List<string>();
        public BuildReportSummary buildReportSummary;

        public List<string> readWriteTexturePaths = new List<string>();
        public List<string> mipmapTexturePaths = new List<string>();
        public List<string> readWriteMeshPaths = new List<string>();

        public bool showUrpPostProcessing;
        public bool showAddressables;
        public bool showAntiAliasing;
        public bool showPhysicsTimestep;
        public Dictionary<string, List<string>> scenesWithObjectsWithNullScripts = new Dictionary<string, List<string>>();

#if PROJECT_AUDITOR_AVAILABLE
        public Report auditorReport;
#endif

        public void CleanFixedItems()
        {
            audioOptimizations.RemoveAll(a => a.optimizations.Count == 0);
            textureOptimizations.RemoveAll(t => t.optimizations.Count == 0);
        }

        public void FixRWTexture(string path)
        {
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer is TextureImporter textureImporter)
            {
                textureImporter.isReadable = false;
                AssetDatabase.ImportAsset(path);
            }
            readWriteTexturePaths.Remove(path);
        }

        public void FixMipmapTexture(string path)
        {
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer is TextureImporter textureImporter)
            {
                textureImporter.mipmapEnabled = false;
                AssetDatabase.ImportAsset(path);
            }
            mipmapTexturePaths.Remove(path);
        }

        public void FixRWMesh(string path)
        {
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (importer is ModelImporter modelImporter)
            {
                modelImporter.isReadable = false;
                AssetDatabase.ImportAsset(path);
            }
            readWriteMeshPaths.Remove(path);
        }
    }
}
