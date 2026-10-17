using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

// Records play mode to an MP4 for the store preview videos (2026-10-15) - the Game view at a set size, 30 fps,
// constant frame rate (each recorded frame is 1/30 s of game time however slowly the editor runs), silent - and
// logs the moments worth cutting to (a burger served, a perfect one, a tomato on the chef or on Mochi, a customer
// walking out) by frame number next to it, so the cut (PreviewVideoMaker) can be written from the log instead of
// scrubbing two minutes of video. Use BeginDirect("<out without .mp4>") / EndDirect(): it records the Game view
// at its size (2880x1620 leaves room for the 2:3 portrait crop) with a still per second in <out>_frames, at ~14 fps
// of editor time; Begin(out, w, h) / End() go through the Recorder package, which stalls this editor at ~1 fps.
public static class GameplayRecorder
{
    static RecorderController controller;
    static int startFrame;
    static StringBuilder log;
    static string logPath;

    public static bool Recording => controller != null && controller.IsRecording();

    public static string Begin(string outPath, int width, int height)
    {
        if (!Application.isPlaying) return "needs play mode";
        if (Recording) return "already recording";
        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Gameplay";
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
            Codec = CoreEncoderSettings.OutputCodec.MP4,
        };
        movie.AudioInputSettings.PreserveAudio = false;
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = width, OutputHeight = height };
        movie.OutputFile = outPath;
        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRate = 30f;
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.CapFrameRate = true;
        controller = new RecorderController(settings);
        controller.PrepareRecording();
        controller.StartRecording();

        StartLog(outPath);
        return "recording -> " + outPath + ".mp4";
    }

    // The Recorder package stalls this editor at ~1 fps (idle CPU and GPU) at any size, so this records without it:
    // game time locked to 30 fps (Time.captureFramerate), the finished Game view grabbed at the end of each frame
    // and handed straight to MediaEncoder (H.264, as PreviewVideoMaker). Output = the Game view's size.
    static MediaEncoder direct;
    static bool directOn;
    static int directFrames;
    static string frameDir;   // a still each second of video (s012.jpg = 12 s in), to pick the cuts from

    public static string BeginDirect(string outPath)
    {
        if (!Application.isPlaying) return "needs play mode";
        if (directOn || Recording) return "already recording";
        var host = Object.FindFirstObjectByType<ScoreManager>();
        if (host == null) return "no host";
        direct = new MediaEncoder(outPath + ".mp4", new VideoTrackAttributes
        {
            frameRate = new MediaRational(30), width = (uint)Screen.width, height = (uint)Screen.height,
            includeAlpha = false, bitRateMode = VideoBitrateMode.High,
        });
        Time.captureFramerate = 30;
        directOn = true;
        directFrames = 0;
        frameDir = outPath + "_frames";
        Directory.CreateDirectory(frameDir);
        StartLog(outPath);
        host.StartCoroutine(Grab());
        return $"recording {Screen.width}x{Screen.height} -> {outPath}.mp4";
    }

    static IEnumerator Grab()
    {
        var end = new WaitForEndOfFrame();
        while (directOn && Application.isPlaying)
        {
            yield return end;
            if (!directOn) break;
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            direct.AddFrame(tex);
            if (directFrames % 30 == 0) File.WriteAllBytes(Path.Combine(frameDir, $"s{directFrames / 30:000}.jpg"), tex.EncodeToJPG(80));
            Object.Destroy(tex);
            directFrames++;
        }
    }

    public static string DirectStatus() => $"{(directOn ? "on" : "off")} frames={directFrames} ({directFrames / 30f:0.0} s)";

    public static string EndDirect()
    {
        if (!directOn) return "not recording";
        directOn = false;
        Time.captureFramerate = 0;
        direct.Dispose();
        direct = null;
        StopLog();
        return $"stopped after {directFrames} frames; events -> {logPath}";
    }

    public static string End()
    {
        if (controller == null) return "not recording";
        controller.StopRecording();
        controller = null;
        StopLog();
        return "stopped; events -> " + logPath;
    }

    static void StartLog(string outPath)
    {
        startFrame = Time.frameCount;
        log = new StringBuilder();
        logPath = outPath + "_events.txt";
        NpcFsm.OnRated.AddListener(OnRated);
        EventManager.OnCustomerProtest.AddListener(OnProtest);
        FoodFight.OnChefSplatted.AddListener(OnChefSplat);
        FoodFight.OnMochiSplatted.AddListener(OnMochiSplat);
    }

    static void StopLog()
    {
        NpcFsm.OnRated.RemoveListener(OnRated);
        EventManager.OnCustomerProtest.RemoveListener(OnProtest);
        FoodFight.OnChefSplatted.RemoveListener(OnChefSplat);
        FoodFight.OnMochiSplatted.RemoveListener(OnMochiSplat);
        File.WriteAllText(logPath, log.ToString());
    }

    // A note in the log at the current video time (frames since the start / 30), e.g. from the bridge.
    public static string Mark(string what)
    {
        if (log == null) return "no log";
        Note(what);
        return "ok";
    }

    static void Note(string what)
    {
        float t = (Time.frameCount - startFrame) / 30f;
        log.AppendLine(t.ToString("0.00", CultureInfo.InvariantCulture) + "  " + what);
    }

    static string Where(Vector3 world)
    {
        var cam = Camera.main;
        if (cam == null) return "";
        var s = cam.WorldToViewportPoint(world);
        return $" at x={s.x:0.00} y={s.y:0.00}";   // 0..1 across the frame: where a portrait crop should centre
    }

    static void OnRated(NpcFsm npc, OrderResult order)
    {
        if (npc == null) return;
        string what = order.Review.IsPerfect ? "PERFECT" : "served";
        if (order.RushStreak >= 3) what += $" rush x{order.RushStreak}";
        Note($"{what} {order.Review.Title} ({npc.Kind}){Where(npc.transform.position)}");
    }

    static void OnProtest() => Note("customer protest / walkout");
    static void OnChefSplat(Vector3 at, FoodFight.Hit hit) => Note($"chef hit by {hit}{Where(at)}");
    static void OnMochiSplat(Vector3 at) => Note($"Mochi hit{Where(at)}");
}
