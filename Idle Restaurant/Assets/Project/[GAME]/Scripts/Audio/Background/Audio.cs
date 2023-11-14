using UnityEngine;
using UnityEngine.Audio;

public class Audio : MonoBehaviour
{
    public static Audio Instance { get; private set; }
    public static Audio audioObject = null;

    private AudioSource background;

    void Awake()
    {
        background = gameObject.GetComponent<AudioSource>();

        if( audioObject == null )
        {
            audioObject = this;
            DontDestroyOnLoad( this );
        }
        else if( this != audioObject )
        {
            Destroy( gameObject );
        }
    }

    void OnEnable()
    {
        EventManager.OnMusicOn.AddListener(PlayMusic);
        EventManager.OnMusicOff.AddListener(PauseMusic);
    }
    void OnDisable()
    {
        EventManager.OnMusicOn.RemoveListener(PlayMusic);
        EventManager.OnMusicOff.RemoveListener(PauseMusic);
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