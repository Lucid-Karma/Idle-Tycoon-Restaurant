using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrazyGames
{
    class BuildTab : ITab
    {
        private bool _showDevelopmentInfo = false;
        private bool _showReleaseInfo = false;

        public const string SUPPORTS_MOBILE_KEY = "CGBuildSupportsMobile";

        public void Render()
        {
            RenderPreloadedShadersWarning();
            RenderBuildButtons();
        }

        private void RenderPreloadedShadersWarning()
        {
#if UNITY_6000_0_OR_NEWER
            // Unity is currently missing an API for accessing the GraphicsSettings preloaded shaders, so these need to be read from a serialized object
            var serializedGraphicsSettings = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var preloadedShadersCount = serializedGraphicsSettings.FindProperty("m_PreloadedShaders").arraySize;
            if (preloadedShadersCount > 0)
            {
                EditorGUILayout.HelpBox(
                    "Your project is preloading "
                        + preloadedShadersCount
                        + " shader(s). On WebGL, preloading shaders may considerably slow down the loading of the game.",
                    MessageType.Warning
                );
            }
#endif
        }

        private void RenderBuildButtons()
        {
#if UNITY_6000_0_OR_NEWER
            var supportsMobile = EditorPrefs.GetBool(SUPPORTS_MOBILE_KEY, false);

            GUIStyle bigButton = new GUIStyle(GUI.skin.button);
            bigButton.fixedHeight = 50;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Development build", bigButton, GUILayout.ExpandWidth(true)))
            {
                new Builder().DoDevelopmentBuild();
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            if (GUILayout.Button("Release build", bigButton, GUILayout.ExpandWidth(true)))
            {
                new Builder().DoReleaseBuild(new AdditionalBuildOptions() { supportsMobile = supportsMobile });
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            supportsMobile = EditorGUILayout.ToggleLeft("Runs on mobile web", supportsMobile);
            EditorPrefs.SetBool(SUPPORTS_MOBILE_KEY, supportsMobile);

            GUILayout.Space(10);

            RenderInfoDropdowns();
#endif
        }

        private void RenderInfoDropdowns()
        {
            _showDevelopmentInfo = EditorGUILayout.Foldout(_showDevelopmentInfo, "Development build info");
            if (_showDevelopmentInfo)
            {
                EditorGUILayout.LabelField(
                    "These are quick builds meant for local testing.\n"
                        + "They won't work on CrazyGames.\n"
                        + "They won't be used in the Analyze tab.",
                    EditorStyles.wordWrappedLabel
                );
            }

            _showReleaseInfo = EditorGUILayout.Foldout(_showReleaseInfo, "Release build info");
            if (_showReleaseInfo)
            {
                EditorGUILayout.LabelField(
                    "These builds are meant to be uploaded on CrazyGames.\n"
                        + "They will take more time to build.\n"
                        + "After the main build, a couple of secondary quicker builds are made, that contain various mobile optimizations.\n"
                        + "The build can be found in the Builds/CrazyGamesRelease folder.\n"
                        + "The CrazyGamesRelease folder with all its contents should be uploaded to CrazyGames.\n"
                        + "\"Runs on mobile web\" avoids building some mobile only files, reducing build time.",
                    EditorStyles.wordWrappedLabel
                );
            }
        }
    }
}
