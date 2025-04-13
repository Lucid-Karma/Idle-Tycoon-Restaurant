using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrazyGames
{
    public class DemoController : MonoBehaviour
    {
        public void ChangeDemoScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        public void GoToMainDemo()
        {
            SceneManager.LoadScene("_Start");
        }

        public void LockPointer()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
