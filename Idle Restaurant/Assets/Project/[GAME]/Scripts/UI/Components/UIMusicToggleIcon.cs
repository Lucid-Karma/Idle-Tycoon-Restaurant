using UnityEngine;
using UnityEngine.UI;

// Shows the music button's current state (on / muted) instead of a static speaker icon.
public class UIMusicToggleIcon : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Sprite onSprite, offSprite;

    private void OnEnable()
    {
        EventManager.OnMusicOn.AddListener(ShowOn);
        EventManager.OnMusicOff.AddListener(ShowOff);

        // Start in the real state: the music may already be off (e.g. muted before a Replay).
        if (Audio.IsMusicOn) ShowOn(); else ShowOff();
    }

    private void OnDisable()
    {
        EventManager.OnMusicOn.RemoveListener(ShowOn);
        EventManager.OnMusicOff.RemoveListener(ShowOff);
    }

    private void ShowOn() => icon.sprite = onSprite;
    private void ShowOff() => icon.sprite = offSprite;
}
