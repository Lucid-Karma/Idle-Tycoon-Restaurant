using UnityEngine;

// The UI's click. Silent while the sound is off (the speaker button, Audio.IsMusicOn), like everything else: it
// used to click on regardless (user, 2026-10-21).
public class ClickFx : MonoBehaviour
{
    private AudioSource clickFx;

    void Awake()
    {
        clickFx = gameObject.GetComponent<AudioSource>();
    }

    // A named method (not a lambda) so RemoveListener matches what AddListener registered.
    void OnEnable()
    {
        EventManager.OnClick.AddListener(PlayClick);
    }
    void OnDisable()
    {
        EventManager.OnClick.RemoveListener(PlayClick);
    }

    private void PlayClick()
    {
        if (Audio.IsMusicOn) clickFx.Play();
    }
}
