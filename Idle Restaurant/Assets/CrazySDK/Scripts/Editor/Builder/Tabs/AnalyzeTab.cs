using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if PROJECT_AUDITOR_AVAILABLE
using Unity.ProjectAuditor.Editor;
#endif

namespace CrazyGames
{
    /// <summary>Displays the tab content with the analyzer suggestions. Also takes care of running the analysis whenever required.</summary>
    class AnalyzeTab : ITab
    {
        private bool _showResources = false;
        private bool _showAudio = false;
        private bool _showTexture = false;
        private bool _showRWTextures = false;
        private bool _showMipmapTextures = false;
        private bool _showRWMeshes = false;
        private bool _showOthers = false;
        private bool _showPackagedAssets = false;
        private bool _showOnlyPackagedAssetsInAssets = true;
        private bool _excludeSmallPackagedAssets = true;
        private bool _showCodeIssues = false;
        private GUIStyle textButtonDefault;
        private GUIStyle textButtonOrange;
        private Vector2 _scrollPos;

        private static AnalyzerReport _report;
        private static bool _showHelp;

        public const string AUTO_SHOW_AND_RUN_KEY = "CrazySDK.ShowAndAnalyzeAfterBuild";

        [RuntimeInitializeOnLoadMethod]
        private static void InitOnLoad()
        {
            _showHelp = false;
            _report = null;
        }

        public void Render()
        {
#if UNITY_6000_0_OR_NEWER
            string filePath = System.IO.Path.Combine("Library", Builder.SUMMARY_FILE_NAME);
            if (!System.IO.File.Exists(filePath))
            {
                EditorGUILayout.HelpBox("No build report found, please do a Release build.", MessageType.Warning);
                return;
            }
#endif

            // need to be called from render method
            textButtonDefault = new GUIStyle(GUI.skin.label);
            textButtonOrange = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color32(0xf5, 0x9e, 0x0b, 0xFF) },
                hover = { textColor = new Color32(0xf5, 0x9e, 0x0b, 0xFF) },
                active = { textColor = new Color32(0xf5, 0x9e, 0x0b, 0xFF) },
            };
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            RenderControlButtons();

            if (_report != null)
            {
                _report.CleanFixedItems();
                RenderInfo();
                GUILayout.Label("Build analysis:", EditorStyles.boldLabel);
                RenderPackagedAssets();
#if PROJECT_AUDITOR_AVAILABLE
                RenderCodeIssues();
#endif
                RenderTextureSizeOptimizations();
                RenderAudio();
                RenderResources();
                RenderRWTextures();
                RenderMipmapTextures();
                RenderRWMeshes();
                RenderOthers();
#if !PROJECT_AUDITOR_AVAILABLE
                RenderProjectAuditorInstallPrompt();
#endif
            }
            else
            {
                GUIStyle bigButton = new GUIStyle(GUI.skin.button);
                bigButton.fixedHeight = 50;
                if (GUILayout.Button("Analyze", bigButton, GUILayout.ExpandWidth(true)))
                {
                    Analyze();
                }
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }

            EditorGUILayout.EndScrollView();
        }

        public void Analyze()
        {
#if UNITY_6000_0_OR_NEWER
            string filePath = System.IO.Path.Combine("Library", Builder.SUMMARY_FILE_NAME);
            if (!System.IO.File.Exists(filePath))
            {
                return;
            }

            var json = System.IO.File.ReadAllText(filePath);
            var buildReportSummary = JsonUtility.FromJson<BuildReportSummary>(json);
            _report = new Analyzer().Analyze(buildReportSummary);
#endif
        }

        private void RenderProjectAuditorInstallPrompt()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Project Auditor Missing", EditorStyles.boldLabel);
            var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
            GUILayout.Label("Install project auditor to get code analysis.", style);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Install Project Auditor"))
            {
                UnityEditor.PackageManager.UI.Window.Open("com.unity.project-auditor");
            }
            GUILayout.EndHorizontal();
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            GUILayout.EndVertical();
        }

        private void RenderInfo()
        {
            GUILayout.Label("Last build info:", EditorStyles.boldLabel);
            var buildDate = System.DateTime.Parse(_report.buildReportSummary.buildDateISO);
            EditorGUILayout.LabelField("Date:", buildDate.ToString("yyyy-MM-dd HH:mm:ss"));
            var duration = System.TimeSpan.FromSeconds(_report.buildReportSummary.durationSeconds);
            EditorGUILayout.LabelField("Duration:", $"{duration.Minutes:D2}m:{duration.Seconds:D2}s");
            EditorGUILayout.LabelField("Total size:", $"{(_report.buildReportSummary.totalSize / (1024f * 1024f)):F2} MB");
            EditorGUILayout.LabelField("Initial load size:", $"{(_report.buildReportSummary.initialLoadSize / (1024f * 1024f)):F2} MB");
            RenderSizeWarnings();
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Initial load size is the size of the 4 main files (wasm, framework, loader, data) in the Build directory. This may be different from the \"Load size\" displayed on QA, as that one includes all the loaded files, including asset bundles, until the first gameplay start event.",
                    MessageType.Info
                );
            }
            GUILayout.Space(20);
        }

        private void RenderSizeWarnings()
        {
            var totalSizeMb = _report.buildReportSummary.totalSize / (1024f * 1024f);
            var initialLoadSizeMb = _report.buildReportSummary.initialLoadSize / (1024f * 1024f);
            var maxSizeMb = 250;
            var maxInitialLoadSizeMb = 50;
            var maxMobileInitialLoadSizeMb = 20;
            if (totalSizeMb > maxSizeMb)
            {
                EditorGUILayout.HelpBox(
                    $"Games larger than {maxSizeMb} MB are not accepted on CrazyGames. Please reduce your build size.",
                    MessageType.Error
                );
                return;
            }
            if (initialLoadSizeMb > maxInitialLoadSizeMb)
            {
                EditorGUILayout.HelpBox(
                    $"Initial load size is greater than {maxInitialLoadSizeMb} MB, your game may be rejected.",
                    MessageType.Warning
                );
                return;
            }
            if (initialLoadSizeMb > maxMobileInitialLoadSizeMb)
            {
                EditorGUILayout.HelpBox(
                    $"Initial load size is greater than {maxMobileInitialLoadSizeMb} MB, your game may be disabled on mobile.",
                    MessageType.Warning
                );
            }
        }

        private void RenderPackagedAssets()
        {
            _showPackagedAssets = EditorGUILayout.Foldout(
                _showPackagedAssets,
                "Packaged assets (" + _report.buildReportSummary.packagedFiles.Count + ")"
            );

            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "This section shows all the assets that are included in the build. Please check carefully if unused assets are included, and remove them from the project. You can click on an asset to select it in Project view. This section will also contain some Unity assets that are included by default.",
                    MessageType.Info
                );
            }
            if (_showPackagedAssets)
            {
                var files = _report.buildReportSummary.packagedFiles;
                if (_excludeSmallPackagedAssets)
                {
                    files = files.Where(f => f.size >= 1024).ToList();
                }
                if (_showOnlyPackagedAssetsInAssets)
                {
                    files = files.Where(f => f.path.StartsWith("Assets/")).ToList();
                }

                _showOnlyPackagedAssetsInAssets = EditorGUILayout.ToggleLeft(
                    "Show only assets inside Assets folder",
                    _showOnlyPackagedAssetsInAssets
                );
                _excludeSmallPackagedAssets = EditorGUILayout.ToggleLeft(
                    "Exclude small assets (less than 1 KB)",
                    _excludeSmallPackagedAssets
                );
                foreach (var item in files)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{(int)(item.size / 1024f)} KB", GUILayout.Width(70));
                    if (GUILayout.Button(item.path, textButtonDefault))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item.path);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void RenderTextureSizeOptimizations()
        {
            _showTexture = EditorGUILayout.Foldout(_showTexture, "Texture size (" + _report.textureOptimizations.Count + ")");
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Only textures for which we have suggestions will be shown here. You can click on a texture to select it in the Project view. The following suggestions may be provided:\n"
                        + "- Reduce size to 512/1024: Reduces imported size, thus reducing build size\n",
                    MessageType.Info
                );
            }
            if (_showTexture)
            {
                foreach (var item in _report.textureOptimizations)
                {
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(EditorGUIUtility.IconContent("Texture Icon"), GUILayout.Width(20), GUILayout.Height(20));
                    if (GUILayout.Button(item.name, textButtonDefault))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item.path);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    GUILayout.EndHorizontal();
                    GUILayout.Label($"Size: {item.width}x{item.height}   Imported size: {item.importedSize}");
                    if (item.optimizations.Contains(TextureOptimizationType.ReduceSize))
                    {
                        if (GUILayout.Button("Reduce size to 1024", textButtonOrange))
                        {
                            item.ReduceMaxSize(1024);
                        }
                        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    }

                    if (item.optimizations.Contains(TextureOptimizationType.ReduceSize))
                    {
                        if (GUILayout.Button("Reduce size to 512", textButtonOrange))
                        {
                            item.ReduceMaxSize(512);
                        }
                        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    }

                    GUILayout.EndVertical();
                    GUILayout.Space(10);
                }
            }
        }

        private void RenderAudio()
        {
            _showAudio = EditorGUILayout.Foldout(_showAudio, "Audio (" + _report.audioOptimizations.Count + ")");
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Only audio clips for which we have suggestions will be shown here. You can click on an audio clip to select it in the Project view. Audio clips that are longer than 10 seconds are considered background audio clips, and the following suggestiosn may be provided:\n"
                        + "- Force to mono: Reduces file size, also slighly reducing the quality\n"
                        + "- Reduce quality: Reduces file size, also reducing the quality",
                    MessageType.Info
                );
            }
            if (_showAudio)
            {
                foreach (var item in _report.audioOptimizations)
                {
                    GUILayout.BeginVertical();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(EditorGUIUtility.IconContent("AudioClip Icon"), GUILayout.Width(20), GUILayout.Height(20));
                    if (GUILayout.Button(item.name + " " + item.length.ToString("F2") + "s", textButtonDefault))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item.path);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

                    GUILayout.EndHorizontal();
                    if (item.optimizations.Contains(AudioOptimizationType.ForceToMono))
                    {
                        if (GUILayout.Button("Force to mono", textButtonOrange))
                        {
                            item.ForceToMono();
                        }
                        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    }
                    if (item.optimizations.Contains(AudioOptimizationType.ReduceQuality))
                    {
                        if (GUILayout.Button("Reduce quality", textButtonOrange))
                        {
                            item.ReduceQuality();
                        }
                        EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    }

                    GUILayout.EndVertical();
                    GUILayout.Space(10);
                }
            }
        }

        private void RenderResources()
        {
            _showResources = EditorGUILayout.Foldout(_showResources, "Assets in \"Resources\" folders (" + _report.resources.Count + ")");
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Resources are assets you place in folders called 'Resources'. Unity includes these files in your build regardless if they are used in any scenes or not. Please ensure you only have the necessary files here. You can click on the assets in this section to select them in the Project view.",
                    MessageType.Info
                );
            }
            if (_showResources)
            {
                foreach (var item in _report.resources)
                {
                    if (GUILayout.Button(item, textButtonDefault))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                }
            }
        }

        private void RenderRWTextures()
        {
            _showRWTextures = EditorGUILayout.Foldout(_showRWTextures, "R/W Textures (" + _report.readWriteTexturePaths.Count + ")");
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Read/Write textures use more memory. Click \"Fix\" button to disable Read/Write. Click texture path to select it in Project view.",
                    MessageType.Info
                );
            }
            if (_showRWTextures)
            {
                var listCopy = new List<string>(_report.readWriteTexturePaths);
                foreach (var item in listCopy)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Fix", textButtonOrange, GUILayout.Width(30)))
                    {
                        _report.FixRWTexture(item);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    if (GUILayout.Button(item, textButtonDefault))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void RenderMipmapTextures()
        {
            _showMipmapTextures = EditorGUILayout.Foldout(
                _showMipmapTextures,
                "Mipmap Textures (" + _report.mipmapTexturePaths.Count + ")"
            );
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Textures with mipmaps enabled use more memory. If your texture is not supposed to be viewed at an angle or far away, you can disable mipmaps. Click \"Fix\" to disable mipmaps. Click texture path to select it in Project view.",
                    MessageType.Info
                );
            }
            if (_showMipmapTextures)
            {
                var listCopy = new List<string>(_report.mipmapTexturePaths);
                foreach (var item in listCopy)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Fix", textButtonOrange, GUILayout.Width(30)))
                    {
                        _report.FixMipmapTexture(item);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

                    if (GUILayout.Button(item, textButtonDefault))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

                    GUILayout.EndHorizontal();
                }
            }
        }

        private void RenderRWMeshes()
        {
            _showRWMeshes = EditorGUILayout.Foldout(_showRWMeshes, "R/W Meshes (" + _report.readWriteMeshPaths.Count + ")");
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Meshes with Read/Write enabled use more memory. Click \"Fix\" to disable Read/Write. Click mesh path to select it in Project view.",
                    MessageType.Info
                );
            }
            if (_showRWMeshes)
            {
                var listCopy = new List<string>(_report.readWriteMeshPaths);
                foreach (var item in listCopy)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Fix", textButtonOrange, GUILayout.Width(30)))
                    {
                        _report.FixRWMesh(item);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

                    if (GUILayout.Button(item, textButtonDefault))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(item);
                    }
                    EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

                    GUILayout.EndHorizontal();
                }
            }
        }

        private void RenderControlButtons()
        {
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "If you do changes directly in the Project view, you can click the 'Regenerate' button to re-analyze the build report. 'Show and analyze after build' will automatically run the analysis after each build and show this window.",
                    MessageType.Info
                );
            }
            GUILayout.BeginHorizontal();

            var showAndAnalyzerAfterBuild = EditorPrefs.GetBool(AUTO_SHOW_AND_RUN_KEY, true);
            showAndAnalyzerAfterBuild = EditorGUILayout.ToggleLeft("Show and analyze after build", showAndAnalyzerAfterBuild);
            EditorPrefs.SetBool(AUTO_SHOW_AND_RUN_KEY, showAndAnalyzerAfterBuild);

            GUILayout.FlexibleSpace();
            if (_report != null && GUILayout.Button("Regenerate"))
            {
                Analyze();
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            if (GUILayout.Button(_showHelp ? "Hide help" : "Show help"))
            {
                _showHelp = !_showHelp;
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
        }

        private void RenderItem(string title, string text, string docsLink)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(text))
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
                GUILayout.Label(text, style);
            }
            if (!string.IsNullOrEmpty(docsLink))
            {
                GUIStyle linkStyle = new GUIStyle(EditorStyles.linkLabel) { normal = { textColor = new Color(0.2f, 0.4f, 1f) } };
                if (GUILayout.Button("Read Docs", linkStyle))
                {
                    Application.OpenURL(docsLink);
                }
                EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            }
            GUILayout.EndVertical();
        }

        private void RenderScenesNullScripts()
        {
            if (_report.scenesWithObjectsWithNullScripts.Count == 0)
            {
                return;
            }
            GUILayout.BeginVertical("box");
            GUILayout.Label("Null scripts", EditorStyles.boldLabel);
            var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
            GUILayout.Label("The following objects with null scripts were found:", style);
            foreach (var scene in _report.scenesWithObjectsWithNullScripts)
            {
                GUILayout.Label($"Scene: {scene.Key}", EditorStyles.boldLabel);
                foreach (var obj in scene.Value)
                {
                    GUILayout.Label($"- {obj}", style);
                }
            }
            GUILayout.EndVertical();
        }

        private void RenderOthers()
        {
            _showOthers = EditorGUILayout.Foldout(_showOthers, "Other");
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "This section contains other suggestions that will help you improve the performance and build size of your game.",
                    MessageType.Info
                );
            }
            if (_showOthers)
            {
                if (_report.showUrpPostProcessing)
                {
                    RenderItem(
                        "Post-processing",
                        "The project uses URP. If you are not using post-processing, consider disabling it to reduce build size.",
                        "https://docs.crazygames.com/resources/export-tips/"
                    );
                }
                if (_report.showAddressables)
                {
                    RenderItem(
                        "Addressables",
                        "The Addressables package is not used in the project. Using Addressables can drastically reduce the initial load size.",
                        "https://docs.crazygames.com/resources/unity-addressables-guide/"
                    );
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Install Addressables"))
                    {
                        UnityEditor.PackageManager.UI.Window.Open("com.unity.addressables");
                    }
                    GUILayout.EndHorizontal();
                }
                if (_report.showAntiAliasing)
                {
                    RenderItem(
                        "Anti-aliasing",
                        "Your project has a high anti-aliasing level. To improve performance, consider reducing it to 2x or disabling it if not needed.",
                        null
                    );
                }
                if (_report.showPhysicsTimestep)
                {
                    RenderItem(
                        "Physics timestep",
                        "Your physics timestep "
                            + UnityEngine.Time.fixedDeltaTime
                            + " is lower than the default Unity value "
                            + Analyzer.DEFAULT_FIXED_TIMESTEP
                            + ". This can cause performance issues in some cases. Consider increasing it to the default value or higher.",
                        null
                    );
                }
                RenderScenesNullScripts();
            }
        }

#if PROJECT_AUDITOR_AVAILABLE
        private void RenderCodeIssues()
        {
            var codeIssues = _report
                .auditorReport.GetAllIssues()
                .ToList()
                .Where(a => a.RelativePath.StartsWith("Assets/") && a.Severity != Severity.Info)
                .ToList();
            _showCodeIssues = EditorGUILayout.Foldout(_showCodeIssues, "Code(" + codeIssues.Count + ")");
            if (_showHelp)
            {
                EditorGUILayout.HelpBox(
                    "Code issues are detected with the helpt of Project Auditor. You can click on an issue to select the file in the Project view.",
                    MessageType.Info
                );
            }
            if (_showCodeIssues)
            {
                codeIssues.ForEach(RenderProjectAuditorIssue);
            }
        }

        private void RenderProjectAuditorIssue(ReportItem issue)
        {
            GUILayout.BeginVertical();

            GUILayout.Label(issue.Description, EditorStyles.wordWrappedLabel);

            var pathStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.gray } };

            if (GUILayout.Button(issue.RelativePath, pathStyle))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(issue.RelativePath);
                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            GUILayout.EndVertical();
            GUILayout.Space(2);
        }
#endif
    }
}
