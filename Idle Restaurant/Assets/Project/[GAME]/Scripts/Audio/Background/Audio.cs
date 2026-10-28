using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// Background music that keeps playing across scene reloads (Replay).
// The first <<<Audio>>> persists and owns the music. The copy that comes with each reloaded scene is kept alive,
// because that scene's objects reference its children (e.g. CafeShop.revealSound -> <<<Audio>>>/Fx), but it
// stays silent: disabling this component in Awake also skips OnEnable, so it never subscribes to the music events.
// The music is not in the build's data file (2026-10-17, to cut the first load): it is StreamingAssets/<streamedClip>
// (the AAC Unity made of the loop, 0.8 MB), fetched once the cafe is on screen and started when it arrives; the
// AudioSource in the scene has no clip. Until then the kitchen plays without music - nobody waits for it.
// A themed cafe (SeasonalDecor) plays the same loop made over for its theme instead: music_loop_<theme>.m4a, made
// offline from the original by Tools/Music/make_theme_music.py - same notes, same key and tempo (the game's sounds
// stay in tune with it), coloured and layered with the theme's sounds on the loop's own beats (2026-10-27: the first
// try made those layers in the game, out of step with the music, and shifted the music's pitch, which put the game's
// own dings out of tune). Only the one in use is downloaded.
public class Audio : MonoBehaviour
{
    public static Audio Instance { get; private set; }

    // The player's music on/off choice. Static so it outlives scene reloads: the music button, its icon and
    // other music-toggled sounds are recreated on Replay and read this instead of assuming "on".
    public static bool IsMusicOn { get; private set; } = true;

    [SerializeField] private string streamedClip = "music_loop.m4a";   // in Assets/StreamingAssets
    private AudioSource background;
    private bool wanted;
    private const float MusicVolume = 0.22f;
    private string loaded;   // the file the music is from (or being fetched)
#if UNITY_EDITOR
    private const string EditorMusic = "Assets/Audio/A-Symphony-of-Disarray_GameMusicPack/01 Loops/0110_A-Symphony-of-Disarray_Loop-1_114bpm4-4_L24M.wav";
#endif   // PlayMusic was asked for (before the clip had arrived, it starts when it does)

    void Awake()
    {
        background = gameObject.GetComponent<AudioSource>();
        // Under the kitchen (user, 2026-10-27: "the music shouldn't be loud like noise in any theme - the patty's
        // sizzle and the rest should be easy to hear"): it was 0.5, then 0.3; a little lower again (2026-10-28).
        background.volume = MusicVolume;

        if( Instance == null )
        {
            Instance = this;
            DontDestroyOnLoad( gameObject );
            PlayMusic();
            if (background.clip == null && !string.IsNullOrEmpty(streamedClip)) StartCoroutine(LoadMusic(FileFor(SeasonalDecor.Current)));
        }
        else
        {
            enabled = false;
        }
    }

    // No music under the opening titles (user, 2026-10-28: they have sounds of their own); it comes in softly once
    // they are over.
    void Update()
    {
        float target = LessonIntro.Playing ? 0f : MusicVolume;
        background.volume = Mathf.MoveTowards(background.volume, target, Time.unscaledDeltaTime * MusicVolume / (target > background.volume ? 1.5f : 0.15f));
    }

    void OnEnable()
    {
        EventManager.OnMusicOn.AddListener(MusicOn);
        EventManager.OnMusicOff.AddListener(MusicOff);
        EventManager.OnGameEnd.AddListener(PauseMusic);
    }
    void OnDisable()
    {
        EventManager.OnMusicOn.RemoveListener(MusicOn);
        EventManager.OnMusicOff.RemoveListener(MusicOff);
        EventManager.OnGameEnd.RemoveListener(PauseMusic);
    }

    private void MusicOn()
    {
        IsMusicOn = true;
        PlayMusic();
    }
    private void MusicOff()
    {
        IsMusicOn = false;
        PauseMusic();
    }

    public void PlayMusic()
    {
        wanted = true;
        if (background.clip != null) background.Play();
    }
    public void PauseMusic()
    {
        wanted = false;
        background.Pause();
    }

    // The theme's music (SeasonalDecor: the season, or one picked to try); the plain loop without one.
    public void MusicFor(SeasonalDecor.Season season)
    {
        if (string.IsNullOrEmpty(streamedClip)) return;
        var file = FileFor(season);
        if (file != loaded) StartCoroutine(LoadMusic(file));
    }

    private string FileFor(SeasonalDecor.Season season) =>
        season == SeasonalDecor.Season.None ? streamedClip
            : Path.GetFileNameWithoutExtension(streamedClip) + "_" + season.ToString().ToLowerInvariant() + Path.GetExtension(streamedClip);

    private IEnumerator LoadMusic(string file)
    {
        loaded = file;
        yield return null;   // the scene first
        string url = Path.Combine(Application.streamingAssetsPath, file);
        if (!url.Contains("://")) url = "file://" + url;   // the editor and desktop builds read it from disk
        var type = AudioType.ACC;
#if UNITY_EDITOR
        // The editor's audio engine can't decode AAC (the clip comes out 0 s long): there it plays the original WAV,
        // by path - a string, not a reference, so it stays out of builds.
        url = "file://" + Path.GetFullPath(file == streamedClip ? EditorMusic : "Tools/Music/" + Path.ChangeExtension(file, ".wav"));
        type = AudioType.WAV;
#endif
        using var request = UnityWebRequestMultimedia.GetAudioClip(url, type);
        yield return request.SendWebRequest();
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Audio] no music ({request.error})");
            yield break;
        }
        if (file != loaded) yield break;   // another theme was asked for meanwhile
        var clip = DownloadHandlerAudioClip.GetContent(request);
        clip.name = "music";
        var old = background.clip;
        bool playing = background.isPlaying;
        float at = playing && old != null && old.length > 0f ? background.time : 0f;
        background.clip = clip;
        if (old != null && old != clip) Destroy(old);
        if (wanted && IsMusicOn)
        {
            background.Play();
            // A new theme carries on from the same point in the loop (they are the same music).
            if (at > 0f && clip.loadState == AudioDataLoadState.Loaded && at < clip.length) background.time = at;
        }
    }
}
