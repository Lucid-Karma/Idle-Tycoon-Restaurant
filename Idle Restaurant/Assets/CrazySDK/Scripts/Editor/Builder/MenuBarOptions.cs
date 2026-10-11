using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrazyGames
{
    public class MenuBarOptions
    {
        [MenuItem("CrazySDK/Go to Build", false, 0)]
        public static void OpenBuildTools()
        {
            BuildWindow.ShowWindow(0);
        }

#if UNITY_6000_0_OR_NEWER
        [MenuItem("CrazySDK/Go to Analyzer", false, 1)]
        public static void OpenAnalyzer()
        {
            BuildWindow.ShowWindow(1);
        }

        [MenuItem("CrazySDK/Development Build", false, 2)]
        public static void QuickDevelopmentBuild()
        {
            new Builder().DoDevelopmentBuild();
        }

        [MenuItem("CrazySDK/Release Build", false, 3)]
        public static void QuickReleaseBuild()
        {
            new Builder().DoReleaseBuild(
                new AdditionalBuildOptions() { supportsMobile = EditorPrefs.GetBool(BuildTab.SUPPORTS_MOBILE_KEY, false) }
            );
        }
#endif
    }
}
