using UnityEngine;
//using CrazyGames;

public class StartButton : MonoBehaviour
{
    public void StartGameScene()
    {
        EventManager.OnClick.Invoke();
        EventManager.OnLevelStart.Invoke();
        //CrazySDK.Game.GameplayStart();
    }
}
