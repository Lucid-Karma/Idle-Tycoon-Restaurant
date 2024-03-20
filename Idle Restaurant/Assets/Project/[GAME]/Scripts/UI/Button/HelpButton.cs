using UnityEngine;

public class HelpButton : MonoBehaviour
{
    public void Help()
    {
        EventManager.OnClick.Invoke();
        EventManager.OnHelpRequest.Invoke();
    }
}
