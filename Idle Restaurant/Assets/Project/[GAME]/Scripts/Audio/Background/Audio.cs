using UnityEngine;

// Background music that keeps playing across scene reloads (Replay).
// The first <<<Audio>>> persists and owns the music. The copy that comes with each reloaded scene is kept alive,
// because that scene's objects reference its children (e.g. CafeShop.revealSound -> <<<Audio>>>/Fx), but it
// stays silent: disabling this component in Awake also skips OnEnable, so it never subscribes to the music events.
public class Audio : MonoBehaviour
{
    public static Audio Instance { get; private set; }

    // The player's music on/off choice. Static so it outlives scene reloads: the music button, its icon and
    // other music-toggled sounds are recreated on Replay and read this instead of assuming "on".
    public static bool IsMusicOn { get; private set; } = true;

    private AudioSource background;

    void Awake()
    {
        background = gameObject.GetComponent<AudioSource>();

        if( Instance == null )
        {
            Instance = this;
            DontDestroyOnLoad( gameObject );
            PlayMusic();
        }
        else
        {
            enabled = false;
        }
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
        background.Play();
    }
    public void PauseMusic()
    {
        background.Pause();
    }
}
