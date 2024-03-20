using UnityEngine;

public class BackToLevelButton : MonoBehaviour
{
    public void Back()
    {
        EventManager.OnClick.Invoke();
        EventManager.OnLevelContine.Invoke();
    }
}
