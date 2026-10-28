using UnityEngine;

public class AudioButton : MonoBehaviour
{
    // The click comes after the switch: turning the sound on clicks (it's back), turning it off is silent.
    public void MusicOnOff()
    {
        if(Audio.IsMusicOn)
        {
            EventManager.OnMusicOff.Invoke();
        }
        else
        {
            EventManager.OnMusicOn.Invoke();
        }
        EventManager.OnClick.Invoke();
    }
}
