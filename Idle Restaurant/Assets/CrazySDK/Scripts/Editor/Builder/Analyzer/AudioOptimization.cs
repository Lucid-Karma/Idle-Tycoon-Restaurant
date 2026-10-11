using System.Collections.Generic;
using UnityEditor;

namespace CrazyGames
{
    public class AudioOptimization
    {
        public string name;
        public string path;
        public float length;
        public List<AudioOptimizationType> optimizations;
        private AudioImporter _importer;

        public AudioOptimization(string path, float length, List<AudioOptimizationType> optimizations)
        {
            name = path.Substring(path.LastIndexOf('/') + 1);
            this.path = path;
            this.length = length;
            this.optimizations = optimizations ?? new List<AudioOptimizationType>();
            _importer = AssetImporter.GetAtPath(path) as AudioImporter;
        }

        public void ForceToMono()
        {
            EditorUtility.DisplayProgressBar("Audio Optimization", $"Forcing {name} to mono...", 0.5f);
            _importer.forceToMono = true;
            AssetDatabase.ImportAsset(path);
            optimizations.Remove(AudioOptimizationType.ForceToMono);
            EditorUtility.ClearProgressBar();
        }

        public void ReduceQuality()
        {
            EditorUtility.DisplayProgressBar("Audio Optimization", $"Reducing quality of {name}...", 0.5f);
            var settings = _importer.defaultSampleSettings;
            settings.quality = 0.75f;
            _importer.defaultSampleSettings = settings;
            AssetDatabase.ImportAsset(path);
            optimizations.Remove(AudioOptimizationType.ReduceQuality);
            EditorUtility.ClearProgressBar();
        }
    }

    public enum AudioOptimizationType
    {
        ForceToMono,
        ReduceQuality,
    }
}
