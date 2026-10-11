using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrazyGames
{
    public class BuildWindow : EditorWindow
    {
        public ITab[] tabs;
        const string TAB_KEY = "CGBuildWindowTab";

        private bool WebGLSupportInstalled => BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        public static BuildWindow ShowWindow(int tab)
        {
            EditorPrefs.SetInt(TAB_KEY, tab);
            var window = GetWindow<BuildWindow>("CrazyGames Build");
            window.Repaint();
            return window;
        }

        private void OnEnable()
        {
            tabs = new ITab[] { new BuildTab(), new AnalyzeTab() };
        }

        void OnGUI()
        {
            RenderHeader();
            RenderMissingWebGL();
            RenderTabs();
        }

        private void RenderTabs()
        {
#if UNITY_6000_0_OR_NEWER
            if (!WebGLSupportInstalled)
            {
                return;
            }

            int selectedTab = EditorPrefs.GetInt(TAB_KEY, 0);

            int newTab = GUILayout.Toolbar(selectedTab, new string[] { "Build", "Analyze" });

            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            if (newTab != selectedTab)
            {
                EditorPrefs.SetInt(TAB_KEY, newTab);
            }

            GUILayout.Space(20);

            tabs[newTab].Render();
#else
            EditorGUILayout.HelpBox(
                "Custom builds are supported only in Unity 6.0.0 and newer. Please update your Unity version to use this feature.",
                MessageType.Info
            );
#endif
        }

        private void RenderHeader()
        {
            GUILayout.Space(20);
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("6e42eca4bb69143a6a4d31d23ffee40d"));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            Rect rect = GUILayoutUtility.GetRect(25, 25);
            GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit, true);
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 18;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleLeft;
            GUILayout.Label("CrazyGames", titleStyle, GUILayout.Height(25));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
#if UNITY_6000_0_OR_NEWER
            EditorGUILayout.HelpBox(
                "Custom builds are a new feature. If you encounter any issues, you can still build your project as usual and upload it on CrazyGames.",
                MessageType.Info
            );
            if (GUILayout.Button("Go to docs"))
            {
                Application.OpenURL("https://docs.crazygames.com/resources/unity-custom-build");
            }
            GUILayout.Space(10);
#endif
        }

        private void RenderMissingWebGL()
        {
            if (!WebGLSupportInstalled)
            {
                EditorGUILayout.HelpBox("WebGL build support not installed", MessageType.Error);
                return;
            }
        }
    }
}
