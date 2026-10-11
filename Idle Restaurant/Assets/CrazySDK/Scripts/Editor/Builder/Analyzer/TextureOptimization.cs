using System.Collections.Generic;
using UnityEditor;

namespace CrazyGames
{
    public class TextureOptimization
    {
        public string name;
        public string path;
        public List<TextureOptimizationType> optimizations;
        private TextureImporter _importer;
        public int width;
        public int height;
        public int importedSize;

        public TextureOptimization(string path, List<TextureOptimizationType> optimizations)
        {
#if UNITY_6000_0_OR_NEWER
            name = path.Substring(path.LastIndexOf('/') + 1);
            this.path = path;
            this.optimizations = optimizations ?? new List<TextureOptimizationType>();
            _importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (_importer != null)
            {
                _importer.GetSourceTextureWidthAndHeight(out width, out height);
                importedSize = _importer.maxTextureSize;
            }
#endif
        }

        public void ReduceMaxSize(int newSize)
        {
            EditorUtility.DisplayProgressBar("Texture Optimization", $"Reducing size of {name} to {newSize}...", 0.5f);
            _importer.maxTextureSize = newSize;
            AssetDatabase.ImportAsset(path);
            optimizations.Remove(TextureOptimizationType.ReduceSize);
            EditorUtility.ClearProgressBar();
        }
    }

    public enum TextureOptimizationType
    {
        ReduceSize,
    }
}
