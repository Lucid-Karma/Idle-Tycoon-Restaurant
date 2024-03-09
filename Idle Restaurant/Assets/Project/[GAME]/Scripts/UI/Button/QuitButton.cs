using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitButton : MonoBehaviour
{ 
    public void Finish()
    {
        EventManager.OnGameEnd.Invoke();
        Invoke("QuitGame", 4.5f);
    }

    private void QuitGame()
    {
        GameManager.Instance.EndGame();
        Application.Quit();
    }
}
