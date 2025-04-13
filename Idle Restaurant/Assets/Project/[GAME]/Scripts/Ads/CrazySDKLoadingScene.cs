using CrazyGames;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CrazySDKLoadingScene : MonoBehaviour
{
    public string nextSceneName;

    void Start()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("Please provide a nextSceneName in " + gameObject.name);
            return;
        }

        if (CrazySDK.IsAvailable)
        {
            CrazySDK.Init(() =>
            {
                Debug.Log("CrazySDK initialized");
                SceneManager.LoadScene(nextSceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
