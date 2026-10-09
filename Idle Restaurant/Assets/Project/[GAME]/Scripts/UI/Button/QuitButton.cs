using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QuitButton : MonoBehaviour
{
    // In a browser there is nowhere to quit to (the page is the game): the Exit button is hidden there. It used
    // to send the player to another site's game list after the farewell card.
    private void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        foreach (var button in GetComponentsInChildren<Button>(true))
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentTarget(i) == this && button.onClick.GetPersistentMethodName(i) == nameof(Finish))
                    button.gameObject.SetActive(false);
#endif
    }

    public void Finish()
    {
        EventManager.OnGameEnd.Invoke();

#if (UNITY_EDITOR)
        UnityEditor.EditorApplication.isPlaying = false;
#elif (UNITY_STANDALONE)
        Invoke("QuitGame", 4.5f);
#endif
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}
