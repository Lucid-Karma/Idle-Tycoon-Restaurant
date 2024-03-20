using System.Collections;
using UnityEngine;

public class QuitButton : MonoBehaviour
{ 
    public void Finish()
    {
        EventManager.OnGameEnd.Invoke();
        //StartCoroutine(Quit());
        Invoke("QuitGame", 4.5f);
    }

    IEnumerator Quit()
    {
        yield return new WaitForSeconds(4.5f);
        Application.Quit();
    }

    private void QuitGame()
    {
        Application.Quit();
    }
}
