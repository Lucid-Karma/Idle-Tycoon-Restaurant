using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

#if UNITY_6000_0_OR_NEWER
namespace CrazyGames
{
    public class BuildCompleteHandler : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        /// <summary>
        /// When a build is copleted, if this is a release WebGL build, we generate a report summary and open the Analyzer tab if needed.
        /// This applies to both builds made from the CrazyGames SDK, but also to builds made from the Unity Editor.
        /// </summary>
        public void OnPostprocessBuild(BuildReport report)
        {
#pragma warning disable UDR0005
            // ignore unsubscribe warning, not relevant here as delayCall is called only once
            EditorApplication.delayCall += () =>
            {
                if (report.summary.outputPath.EndsWith(Builder.INTERMEDIATE_BUILD_SUFFIX))
                {
                    // these are secondary builds for getting ASTC compression or limited max memory size, so ignore them
                    return;
                }

                if (report.summary.result != BuildResult.Succeeded)
                {
                    return;
                }

                if (report.summary.options.HasFlag(BuildOptions.Development))
                {
                    return;
                }

                if (report.summary.platform != BuildTarget.WebGL)
                {
                    return;
                }

                GenerateReportSummary(report);
                GoToAnalysisIfNeeded();
            };
#pragma warning restore UDR0005
        }

        private void GenerateReportSummary(BuildReport report)
        {
            // store a summary of the build report, to be used by the Analyzer
            var assets = report
                .packedAssets.SelectMany(p => p.contents)
                // ordering is important before grouping, to ensure we keep the largest file for each sourceAssetPath, or there may be duplicates with 0 size
                .OrderByDescending(f => f.packedSize)
                .GroupBy(p => p.sourceAssetPath)
                .Select(g => g.First())
                .Where(f => !string.IsNullOrEmpty(f.sourceAssetPath))
                .ToList();

            var mainFiles = BuildReportGenerator.GetMainFiles(report);
            var reportSummary = new BuildReportSummary()
            {
                buildDateISO = System.DateTime.Now.ToString("o"),
                durationSeconds = report.summary.totalTime.TotalSeconds,
                totalSize = report.summary.totalSize,
                packagedFiles = assets.Select(a => new PackagedFileSummary { path = a.sourceAssetPath, size = a.packedSize }).ToList(),
                initialLoadSize = mainFiles.Aggregate(0UL, (acc, f) => acc + f.size),
            };
            var reportSummaryJson = JsonUtility.ToJson(reportSummary, true);
            var outputPath = Path.Combine("Library", Builder.SUMMARY_FILE_NAME);
            File.WriteAllText(outputPath, reportSummaryJson);
        }

        private static void GoToAnalysisIfNeeded()
        {
            // open analyzer tab and run analysis if agreed to
            if (EditorPrefs.GetBool(AnalyzeTab.AUTO_SHOW_AND_RUN_KEY, true))
            {
                var window = BuildWindow.ShowWindow(1);
                (window.tabs[1] as AnalyzeTab).Analyze();
            }
        }
    }
}
#endif
