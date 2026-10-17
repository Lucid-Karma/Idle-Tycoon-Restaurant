using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Video;

// Cuts the store preview videos (CrazyGames wants 15-20 s, silent, 1080p landscape AND 2:3 portrait, starting on
// the static cover) out of the trailer's recorded shots - no ffmpeg on this machine, so Unity decodes the clips
// (VideoPlayer, every frame, in play mode) and encodes the result (MediaEncoder, H.264, no audio track).
// The cut is a text file, one line each:
//   out <file.mp4> <width> <height>
//   cover <image.png> <seconds>                  the still opening frame (scaled to cover the frame)
//   clip <file.mp4> <from s> <to s> [centerX] [zoom] [centerY] [speed]
//        centerX/centerY 0..1 (viewport, y up): where to centre the crop; zoom >= 1 pushes in a little;
//        speed 2 or 3 keeps every 2nd/3rd frame (a sped-up stretch)
// Every 15th frame is also written next to the video as a JPEG, to check the crops without a video player.
// Run (in play mode): PreviewVideoMaker.Make("<cut.txt>"), e.g. from the editor bridge with scall.
public static class PreviewVideoMaker
{
    const int Fps = 30;

    class Shot { public string Path; public double From, To; public float CenterX = 0.5f, CenterY = 0.5f, Zoom = 1f; public int Speed = 1; public bool Cover; public float Seconds; }

    static readonly Queue<Shot> shots = new();
    static MediaEncoder encoder;
    static RenderTexture frame;
    static Texture2D readback;
    static VideoPlayer player;
    static Shot current;
    static long lastFrame = -1, endFrame, firstFrame;
    static double lastFrameAt;
    static int written, width, height;
    static string outPath, checkDir;
    public static bool Busy { get; private set; }
    public static string Status { get; private set; } = "idle";

    public static string Make(string cutFile)
    {
        if (Busy) return "busy";
        shots.Clear();
        foreach (var raw in File.ReadAllLines(cutFile))
        {
            var p = Split(raw.Trim());
            if (p.Count == 0 || p[0].StartsWith("#")) continue;
            if (p[0] == "out") { outPath = p[1]; width = int.Parse(p[2]); height = int.Parse(p[3]); }
            else if (p[0] == "cover") shots.Enqueue(new Shot { Cover = true, Path = p[1], Seconds = F(p[2]) });
            else if (p[0] == "clip") shots.Enqueue(new Shot
            {
                Path = p[1], From = F(p[2]), To = F(p[3]), CenterX = p.Count > 4 ? F(p[4]) : 0.5f,
                Zoom = p.Count > 5 ? Mathf.Max(1f, F(p[5])) : 1f, CenterY = p.Count > 6 ? F(p[6]) : 0.5f,
                Speed = p.Count > 7 ? Mathf.Max(1, Mathf.RoundToInt(F(p[7]))) : 1,
            });
        }
        if (File.Exists(outPath)) File.Delete(outPath);
        checkDir = Path.Combine(Path.GetDirectoryName(outPath), Path.GetFileNameWithoutExtension(outPath) + "_frames");
        Directory.CreateDirectory(checkDir);
        foreach (var old in Directory.GetFiles(checkDir, "*.jpg")) File.Delete(old);

        var video = new VideoTrackAttributes
        {
            frameRate = new MediaRational(Fps),
            width = (uint)width,
            height = (uint)height,
            includeAlpha = false,
            bitRateMode = VideoBitrateMode.High,
        };
        encoder = new MediaEncoder(outPath, video);
        frame = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
        written = 0;
        Busy = true;
        EditorApplication.update += Tick;
        Next();
        return "started " + shots.Count + " shots";
    }

    static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

    // Splits on spaces, keeping "quoted paths" whole.
    static List<string> Split(string line)
    {
        var parts = new List<string>();
        bool quoted = false;
        var cur = new System.Text.StringBuilder();
        foreach (char ch in line)
        {
            if (ch == '"') { quoted = !quoted; continue; }
            if (ch == ' ' && !quoted) { if (cur.Length > 0) { parts.Add(cur.ToString()); cur.Clear(); } continue; }
            cur.Append(ch);
        }
        if (cur.Length > 0) parts.Add(cur.ToString());
        return parts;
    }

    static void Next()
    {
        if (shots.Count == 0) { Finish(); return; }
        current = shots.Dequeue();
        if (current.Cover)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(current.Path));
            Blit(tex, 0.5f);
            for (int i = 0; i < Mathf.RoundToInt(current.Seconds * Fps); i++) Write();
            Object.DestroyImmediate(tex);
            Next();
            return;
        }
        if (player == null)
        {
            var go = new GameObject("__PreviewVideoPlayer") { hideFlags = HideFlags.DontSave };
            player = go.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.renderMode = VideoRenderMode.APIOnly;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.skipOnDrop = false;
            player.sendFrameReadyEvents = true;
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.frameReady += OnFrame;
            player.prepareCompleted += OnPrepared;
            player.loopPointReached += EndShot;   // the clip ran out before the cut's end point
        }
        Status = "preparing " + Path.GetFileName(current.Path);
        player.url = current.Path;
        player.Prepare();
    }

    static void OnPrepared(VideoPlayer vp)
    {
        double fps = vp.frameRate > 0 ? vp.frameRate : Fps;
        lastFrame = (long)(current.From * fps) - 1;
        firstFrame = lastFrame + 1;
        endFrame = System.Math.Min((long)(current.To * fps), (long)vp.frameCount - 1);
        lastFrameAt = EditorApplication.timeSinceStartup;
        vp.frame = (long)(current.From * fps);
        vp.Play();
        Status = "cutting " + Path.GetFileName(current.Path);
    }

    static void OnFrame(VideoPlayer vp, long index)
    {
        if (current == null || current.Cover) return;
        if (index <= lastFrame) return;          // frames before the cut point (or repeats)
        lastFrame = index;
        lastFrameAt = EditorApplication.timeSinceStartup;
        if (index >= endFrame) { EndShot(vp); return; }
        if (current.Speed > 1 && (index - firstFrame) % current.Speed != 0) return;
        Blit(vp.texture, current.CenterX, current.Zoom, current.CenterY);
        Write();
    }

    static void EndShot(VideoPlayer vp)
    {
        if (current == null || current.Cover) return;
        vp.Stop();
        current = null;
        Next();
    }

    // Scale the source to cover the output frame (crop the excess), centred on (centerX, centerY), pushed in by zoom.
    static void Blit(Texture src, float centerX, float zoom = 1f, float centerY = 0.5f)
    {
        float scale = Mathf.Max(width / (float)src.width, height / (float)src.height) * zoom;
        float visibleW = width / (scale * src.width), visibleH = height / (scale * src.height);
        float x = Mathf.Clamp(centerX - visibleW / 2f, 0f, 1f - visibleW);
        float y = Mathf.Clamp(centerY - visibleH / 2f, 0f, 1f - visibleH);
        Graphics.Blit(src, frame, new Vector2(visibleW, visibleH), new Vector2(x, y));
    }

    static void Write()
    {
        var prev = RenderTexture.active;
        RenderTexture.active = frame;
        readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        readback.Apply();
        RenderTexture.active = prev;
        encoder.AddFrame(readback);
        if (written % 15 == 0) File.WriteAllBytes(Path.Combine(checkDir, $"f{written:0000}.jpg"), readback.EncodeToJPG(85));
        written++;
    }

    static void Tick()
    {
        if (!Busy) return;
        if (!Application.isPlaying) { Status = "stopped: needs play mode"; Finish(); return; }
        // A clip that stops sending frames (decoder stall, end of file) does not hold up the rest.
        if (current != null && !current.Cover && player != null && player.isPlaying
            && EditorApplication.timeSinceStartup - lastFrameAt > 5.0) EndShot(player);
    }

    static void Finish()
    {
        EditorApplication.update -= Tick;
        encoder?.Dispose();
        encoder = null;
        if (player != null) Object.DestroyImmediate(player.gameObject);
        player = null;
        if (frame != null) { frame.Release(); Object.DestroyImmediate(frame); }
        if (readback != null) Object.DestroyImmediate(readback);
        Busy = false;
        Status = $"done: {written} frames ({written / (float)Fps:0.0} s) -> {outPath}";
        Debug.Log("[PreviewVideo] " + Status);
    }
}
