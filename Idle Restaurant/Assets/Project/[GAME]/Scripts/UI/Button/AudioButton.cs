using UnityEngine;

public class AudioButton : MonoBehaviour
{
    public void MusicOnOff()
    {
        EventManager.OnClick.Invoke();
        if(Audio.IsMusicOn)
        {
            EventManager.OnMusicOff.Invoke();
        }
        else
        {
            EventManager.OnMusicOn.Invoke();
        }
    }
}
