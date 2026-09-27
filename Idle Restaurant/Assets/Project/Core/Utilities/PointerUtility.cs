using UnityEngine;
using UnityEngine.EventSystems;

public static class PointerUtility
{
    // EventSystem.IsPointerOverGameObject() without an id only checks the mouse pointer, so on touch screens
    // taps on HUD buttons also went through to the world. Check every active finger as well.
    public static bool IsOverUI()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return false;
        if (eventSystem.IsPointerOverGameObject()) return true;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (eventSystem.IsPointerOverGameObject(Input.GetTouch(i).fingerId)) return true;
        }
        return false;
    }
}
