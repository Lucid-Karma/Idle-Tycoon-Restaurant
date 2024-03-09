using UnityEngine;

public class StartButton : MonoBehaviour
{
    public void StartGameScene()
    {
        EventManager.OnClick.Invoke();
        EventManager.OnLevelStart.Invoke();
    }
}
