using System.Collections;
using UnityEngine;

public class QuitButton : MonoBehaviour
{ 
    public void Finish()
    {
        EventManager.OnGameEnd.Invoke();

#if (UNITY_EDITOR || DEVELOPMENT_BUILD)
            Debug.Log(this.name + " : " + this.GetType() + " : " + System.Reflection.MethodBase.GetCurrentMethod().Name);
#endif
#if (UNITY_EDITOR)
                    UnityEditor.EditorApplication.isPlaying = false;
#elif (UNITY_STANDALONE)
                    Invoke("QuitGame", 4.5f);
#elif (UNITY_WEBGL)
                    Invoke("QuitWebGLGame", 4.5f);
#endif
    }

    private void QuitGame()
    {
        Application.Quit();
    }

    private void QuitWebGLGame()
    {
        Application.OpenURL("https://itch.io/games/newest");
    }
}
