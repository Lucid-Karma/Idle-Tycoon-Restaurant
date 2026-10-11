using UnityEngine;
using UnityEngine.SceneManagement;

// The first scene of a build (Boot.unity, an empty camera in the game's background colour): starts the portal
// SDK, then loads the game. Saved progress lives in the player's CrazyGames account there, so the cafe must not
// read it before the SDK is ready. If the SDK never answers, the game starts anyway on local saves.
public class Boot : MonoBehaviour
{
    [SerializeField] private string gameScene = "GameScene";
    private const float MaxWait = 6f;

    private float startedAt;
    private bool loading;

    private void Start()
    {
        startedAt = Time.realtimeSinceStartup;
        GamePlatform.Init(Go);
    }

    private void Update()
    {
        if (loading || Time.realtimeSinceStartup - startedAt < MaxWait) return;
        GamePlatform.GiveUpWaiting();
        Go();
    }

    private void Go()
    {
        if (loading) return;
        loading = true;
        GoogleAnalytics.Start();
        PlatformSession.Ensure();
        SceneManager.LoadScene(gameScene);
    }
}
