using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RestartButton : MonoBehaviour
{
    private void OnEnable() 
    {
        EventManager.OnGameStart.AddListener(Restart);
    }
    private void OnDisable() 
    {
        EventManager.OnGameStart.RemoveListener(Restart);    
    }
    public void Restart()
    {
        SceneManager.LoadScene( SceneManager.GetActiveScene().name );
        EventManager.OnRestart.Invoke();
    }
}
