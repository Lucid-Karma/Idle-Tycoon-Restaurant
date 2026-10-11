using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Double it" on the result card: watch a video ad (CrazyGames rewarded ad) and the shift's earnings are paid a
// second time. Optional and only offered here, between shifts, never during play; the video icon says an ad
// comes with it, and "Play again" stays the big main button: not watching is always the easy choice.
// Shown only where ads exist (GamePlatform.AdsSupported) and when the shift earned something. The reward is paid
// only when the ad finished; a missing ad says so and leaves the money alone. During CrazyGames' Basic Launch
// ads are off: the button goes away for the session instead of doing nothing.
// A player who watched it goes straight into the next shift (no midgame ad on top: one ad per transition).
[RequireComponent(typeof(Button))]
public class UIRewardedBonus : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private CanvasGroup card;   // blocked while the ad is up

    public static bool WatchedThisCard { get; private set; }
    private static bool adsOff;
    // A special offer, not an expectation (CrazyGames' rule): after a watched video it rests for a while.
    private const float RestAfterWatching = 6f * 60f;
    private static float watchedAt = float.NegativeInfinity;

    private Button button;
    private int bonus;
    private string idleText;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Watch);
        idleText = label != null ? label.text : "";
        // Subscribed for the object's lifetime, not while enabled: the button hides itself until the shift ends.
        EventManager.OnLevelFinish.AddListener(Offer);
        WatchedThisCard = false;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        EventManager.OnLevelFinish.RemoveListener(Offer);
    }

    private void Offer()
    {
        WatchedThisCard = false;
        bonus = ScoreManager.Instance != null ? ScoreManager.Instance.ShiftEarned : 0;
        bool rested = Time.realtimeSinceStartup - watchedAt >= RestAfterWatching;
        bool show = GamePlatform.AdsSupported && !adsOff && bonus > 0 && rested;
        gameObject.SetActive(show);
        if (!show) return;
        button.interactable = true;
        if (label != null) label.text = idleText;
    }

    private void Watch()
    {
        if (GamePlatform.AdShowing) return;
        button.interactable = false;
        if (card != null) card.interactable = false;
        GamePlatform.Rewarded(Done);
    }

    private void Done(GamePlatform.AdOutcome outcome)
    {
        if (card != null) card.interactable = true;
        switch (outcome)
        {
            case GamePlatform.AdOutcome.Watched:
                WatchedThisCard = true;
                watchedAt = Time.realtimeSinceStartup;
                ScoreManager.Instance.AddTip(bonus);
                EventManager.OnScoreUpdate.Invoke();
                GameSfx.Play(GameSfx.Cue.Coin);
                if (label != null) label.text = "Doubled!";   // the EARNED tile above shows the new total
                break;
            case GamePlatform.AdOutcome.AdsOff:
                adsOff = true;
                gameObject.SetActive(false);
                break;
            case GamePlatform.AdOutcome.AdBlock:
                if (label != null) label.text = "Ad blocker on";
                break;
            default:
                if (label != null) label.text = "No video right now";
                StartCoroutine(ReadyAgain());
                break;
        }
    }

    private IEnumerator ReadyAgain()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        button.interactable = true;
        if (label != null) label.text = idleText;
    }
}
