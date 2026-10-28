using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The first scene of a build (Boot.unity, an empty camera in the game's background colour): starts the portal
// SDK, then shows the game. Saved progress lives in the player's CrazyGames account there, so the cafe must not
// read it before the SDK is ready. If the SDK never answers, the game starts anyway on local saves.
// The game scene loads in the background while the SDK starts (one wait instead of two in a row: loading time was
// 16.7 s on CrazyGames); it is only switched on once the SDK has answered, so its Awake/Start read the right save.
// Meanwhile a quiet "Loading..." sits in the middle of the screen: a flat colour with nothing on it for several
// seconds (measured in a local build) looks like a game that has frozen.
// The background load runs at High priority (2026-10-17): WebGL has no loading thread, so an async load only gets
// a slice of each frame (Normal = ~10 ms, and the page's frame limiter adds idle time between frames) - the game
// scene's 12,000 objects took 13 s to arrive in a local build while the engine had been ready after 5. High gives
// it ~50 ms a frame (the screen only shows "Loading..." meanwhile); it goes back to Normal once the scene is in.
// On the web the page's own loading screen (WebGL template ChibiCafe) stays up until the game scene is in
// (ChibiGameReady), so this scene shows nothing of its own there: one loading screen, not two (2026-10-20).
public class Boot : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void ChibiGameReady();
    private const bool PageShowsLoading = true;
#else
    private static void ChibiGameReady() { }
    private const bool PageShowsLoading = false;
#endif

    [SerializeField] private string gameScene = "GameScene";
    private const float MaxWait = 6f;
    private const float ShowLoadingAfter = 0.4f;   // a fast load never flashes it

    private float startedAt;
    private bool sdkDone, activated;
    private AsyncOperation loading;
    private TextMeshProUGUI loadingText;

    private void Start()
    {
        startedAt = Time.realtimeSinceStartup;
        Application.backgroundLoadingPriority = ThreadPriority.High;
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        loading = SceneManager.LoadSceneAsync(gameScene);
        loading.allowSceneActivation = false;
        GamePlatform.Init(OnSdkReady);
        if (!PageShowsLoading) BuildLoadingText();
    }

    private void OnSdkReady() => sdkDone = true;

    private static void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        Application.backgroundLoadingPriority = ThreadPriority.Normal;
        ChibiGameReady();
    }

    private void Update()
    {
        AnimateLoadingText();
        if (activated) return;
        if (!sdkDone && Time.realtimeSinceStartup - startedAt < MaxWait) return;
        if (!sdkDone) GamePlatform.GiveUpWaiting();
        activated = true;
        PlatformSession.Ensure();
        loading.allowSceneActivation = true;
    }

    // Made here rather than in the scene: TMP's default font is the game's own (Fredoka), so it needs nothing else.
    private void BuildLoadingText()
    {
        var canvasGo = new GameObject("Loading", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);
        loadingText = textGo.AddComponent<TextMeshProUGUI>();
        loadingText.text = "Loading";
        loadingText.fontSize = 44f;
        loadingText.alignment = TextAlignmentOptions.Center;
        loadingText.color = new Color(1f, 0.976f, 0.957f, 0f);   // warm white, faded in
        var rt = (RectTransform)textGo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 80f);
    }

    // "Loading", "Loading.", "Loading.."... fading in after a moment.
    private void AnimateLoadingText()
    {
        if (loadingText == null) return;
        float t = Time.realtimeSinceStartup - startedAt;
        var c = loadingText.color;
        c.a = Mathf.Clamp01((t - ShowLoadingAfter) / 0.3f) * 0.85f;
        loadingText.color = c;
        int dots = (int)(t * 2.5f) % 4;
        loadingText.text = "Loading" + new string('.', dots) + new string(' ', 3 - dots);
    }
}
