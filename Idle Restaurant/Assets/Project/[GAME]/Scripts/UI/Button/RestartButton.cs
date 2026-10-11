using UnityEngine;
using UnityEngine.SceneManagement;

// "Play again" on the result card. Between two shifts is where a midgame video ad belongs (CrazyGames; the SDK
// decides whether it is time for one, at most every few minutes). The card is blocked while it is asked for or
// playing, and the next shift starts when it is over - or at once if there is no ad, or if the player just
// watched the "Double it" video (never two ads on one transition).
public class RestartButton : MonoBehaviour
{
    private bool restarting;

    public void Restart()
    {
        if (restarting) return;
        restarting = true;
        var card = GetComponentInParent<CanvasGroup>();
        if (card != null) card.interactable = false;
        if (UIRewardedBonus.WatchedThisCard) Reload();
        else GamePlatform.Midgame(Reload);
    }

    private void Reload()
    {
        SceneManager.LoadScene( SceneManager.GetActiveScene().name );
        EventManager.OnRestart.Invoke();
        GameManager.IsGameRestarted = true;
    }
}
