using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Keeps the cafe light, above all on phones (they heated up and drained their battery):
// - Frame rate: 30 on phones, 60 on computers, 30 while the game is paused (title, shop, help, result).
//   On the web the page's frame limiter (WebGL template "ChibiCafe" + ChibiPerf.jslib) drops frames at
//   vsync, so the cap also holds on 90/120 Hz screens (Unity's own targetFrameRate assumes 60 Hz there).
//   The template also caps the pixel ratio on phones at 1.5 (a phone's 3x means 4x the pixels to shade).
// - Phones render cheaper: half-size shadow map, FXAA instead of SMAA, lighter bloom, one-bone skinning.
//   (Ambient occlusion - what stops plates and bottles looking like they float - runs everywhere, at half
//   resolution: it is one screen-space pass and the cafe is a small scene.)
// - Everywhere: static objects keep their baked shadows (Shadowmask) instead of being drawn into the
//   realtime shadow map every frame; only characters and props that move cast realtime shadows.
// - Release builds only log warnings and errors (every Debug.Log goes to the browser console).
public class PerformanceGovernor : MonoBehaviour
{
    public const int PhoneFps = 30, DesktopFps = 60, PausedFps = 30;

#if UNITY_EDITOR
    // Editor testing of the phone settings (Tools/Chibi Cafe/Play As Phone).
    public const string PlayAsPhonePref = "Chibi.PlayAsPhone";
    public static bool IsPhone => Application.isMobilePlatform || UnityEditor.EditorPrefs.GetBool(PlayAsPhonePref, false);
#else
    public static bool IsPhone => Application.isMobilePlatform;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void ChibiSetMaxFps(int fps);
#endif

    private int appliedFps = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject(nameof(PerformanceGovernor));
        DontDestroyOnLoad(go);
        go.AddComponent<PerformanceGovernor>();

#if UNITY_EDITOR
        // Quality settings changed in play mode would stick to the project: put them back on exit.
        var (shadowmask, skin, vSync, pipeline) = (QualitySettings.shadowmaskMode, QualitySettings.skinWeights,
            QualitySettings.vSyncCount, QualitySettings.renderPipeline);
        UnityEditor.EditorApplication.playModeStateChanged += Restore;
        void Restore(UnityEditor.PlayModeStateChange change)
        {
            if (change != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
            UnityEditor.EditorApplication.playModeStateChanged -= Restore;
            QualitySettings.shadowmaskMode = shadowmask;
            QualitySettings.skinWeights = skin;
            QualitySettings.vSyncCount = vSync;
            QualitySettings.renderPipeline = pipeline;
            Application.targetFrameRate = -1;
        }
#endif
        if (!Debug.isDebugBuild) Debug.unityLogger.filterLogType = LogType.Warning;
        QualitySettings.shadowmaskMode = ShadowmaskMode.Shadowmask;
        if (IsPhone) CheaperPipeline();
    }

    // A copy of the pipeline asset with phone settings: the project's asset itself stays untouched.
    private static void CheaperPipeline()
    {
        QualitySettings.skinWeights = SkinWeights.OneBone;   // the KayKit parts are rigid anyway
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp) return;
        var phone = Instantiate(urp);
        phone.name = urp.name + " (phone)";
        phone.mainLightShadowmapResolution = Mathf.Min(urp.mainLightShadowmapResolution, 1024);
        phone.msaaSampleCount = 1;
        QualitySettings.renderPipeline = phone;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsPhone) CheaperScene();
    }

    private static void CheaperScene()
    {
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null && data.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
        }
        foreach (var volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (volume.sharedProfile == null || !volume.sharedProfile.Has<Bloom>()) continue;
            // .profile makes a runtime copy, so the profile asset isn't changed.
            if (!volume.profile.TryGet(out Bloom bloom)) continue;
            bloom.highQualityFiltering.Override(false);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);
            bloom.maxIterations.Override(Mathf.Min(bloom.maxIterations.value, 4));
        }
    }

    private void Update()
    {
        int fps = Time.timeScale == 0f ? PausedFps : IsPhone ? PhoneFps : DesktopFps;
        if (fps == appliedFps) return;
        appliedFps = fps;
#if UNITY_WEBGL && !UNITY_EDITOR
        ChibiSetMaxFps(fps);
#else
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
#endif
    }
}
