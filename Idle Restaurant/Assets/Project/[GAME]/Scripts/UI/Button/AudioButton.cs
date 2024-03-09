using UnityEngine;

public class AudioButton : MonoBehaviour
{
    private bool isMusicOn = true;

    public void MusicOnOff()
    {
        if(isMusicOn)
        {
            EventManager.OnMusicOff.Invoke();
            isMusicOn = false;
        }
        else
        {
            EventManager.OnMusicOn.Invoke();
            isMusicOn = true;
        }
    }
}
