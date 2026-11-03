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
// The game scene is loaded additively and this one unloaded after it (2026-10-31): a scene loaded in Single mode makes
// Unity unload every unused asset and collect garbage before the first frame, which in a web build measured 1.4-3.2 s
// of the load - for an empty boot scene there is nothing to free. Roots the game made while this scene was still the
// active one (its Awake / OnEnable / sceneLoaded) are moved over first, so they do not go when this scene does.
public class Boot : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void ChibiGameReady();
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern double ChibiPageSeconds();
    [System.Runtime.InteropServices.DllImport("__Internal")] private static extern void ChibiConsole(string text);
    private const bool PageShowsLoading = true;
#else
    private static void ChibiGameReady() { }
    private static double ChibiPageSeconds() => Time.realtimeSinceStartupAsDouble;
    private static void ChibiConsole(string text) => Debug.Log(text);
    private const bool PageShowsLoading = false;
#endif

    // One line in the console at each step of the first load, timed from the start of the page, so a portal's QA tool
    // log shows where its load time went (2026-10-31: CrazyGames measured 27 s where a local test takes 7).
    public static void Mark(string step) => ChibiConsole($"[Chibi load] {step} at {ChibiPageSeconds():0.0} s");

    [SerializeField] private string gameScene = "GameScene";
    private const float MaxWait = 6f;
    private const float ShowLoadingAfter = 0.4f;   // a fast load never flashes it

    private float startedAt;
    private bool sdkDone, activated, markedLoaded;
    private AsyncOperation loading;
    private GameObject[] ownRoots;
    private TextMeshProUGUI loadingText;

    private void Start()
    {
        startedAt = Time.realtimeSinceStartup;
        Mark("engine started");
        Application.backgroundLoadingPriority = ThreadPriority.High;
        SceneManager.sceneLoaded += OnGameSceneLoaded;
        loading = SceneManager.LoadSceneAsync(gameScene, LoadSceneMode.Additive);
        loading.allowSceneActivation = false;
        GamePlatform.Init(OnSdkReady);
        if (!PageShowsLoading) BuildLoadingText();
        ownRoots = gameObject.scene.GetRootGameObjects();
    }

    private void OnSdkReady()
    {
        sdkDone = true;
        Mark("SDK ready");
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnGameSceneLoaded;
        var boot = gameObject.scene;
        SceneManager.SetActiveScene(scene);   // its lighting, and where new objects go from now on
        foreach (var root in boot.GetRootGameObjects())
            if (System.Array.IndexOf(ownRoots, root) < 0) SceneManager.MoveGameObjectToScene(root, scene);
        SceneManager.UnloadSceneAsync(boot).completed += _ => LightProbes.TetrahedralizeAsync();
        Application.backgroundLoadingPriority = ThreadPriority.Normal;
        Mark("kitchen in (every Awake run)");
        ChibiGameReady();
        new GameObject("FirstFrameMark").AddComponent<FirstFrameMark>();
    }

    // The two marks after the scene is in: every Start has run (the first Update), and the first frame is drawn.
    private sealed class FirstFrameMark : MonoBehaviour
    {
        private System.Collections.IEnumerator Start()
        {
            yield return null;
            Mark("first update done");
            yield return new WaitForEndOfFrame();
            Mark("first frame drawn");
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        AnimateLoadingText();
        if (activated) return;
        if (!markedLoaded && loading.progress >= 0.9f) { markedLoaded = true; Mark("kitchen loaded"); }
        if (!sdkDone && Time.realtimeSinceStartup - startedAt < MaxWait) return;
        if (!sdkDone) { GamePlatform.GiveUpWaiting(); Mark("SDK not ready, starting without it"); }
        activated = true;
        Mark("switching to the kitchen");
        // Its camera (and audio listener) off before the kitchen's scripts look for Camera.main (CameraAB, SeasonalDecor).
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None)) if (cam.gameObject.scene == gameObject.scene) cam.gameObject.SetActive(false);
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
