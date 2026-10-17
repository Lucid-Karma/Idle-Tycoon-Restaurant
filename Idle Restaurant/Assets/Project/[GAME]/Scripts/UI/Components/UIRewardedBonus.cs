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
    // On the HUD (shifts flow on without a result card, ShiftFlow) the offer is a short window after each
    // shift: it goes away by itself after this many seconds. 0 = it stays (the result card's copy).
    [SerializeField] private float offerSeconds;
    private float offeredAt;

    public static bool WatchedThisCard { get; private set; }
    // The day's money was paid a second time (how much): the day card counts it up (UIBreakCard).
    // Static: subscribe with named methods only.
    public static event System.Action<int> Doubled;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Doubled = null;
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
        // What it is worth, on the button itself: "Double +$58".
        if (label != null) label.text = $"Double +${bonus}";
        offeredAt = Time.unscaledTime;
    }

    private void Update()
    {
        if (offerSeconds > 0f && !GamePlatform.AdShowing && Time.unscaledTime - offeredAt > offerSeconds)
            gameObject.SetActive(false);
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
                if (label != null) label.text = "Doubled!";   // the day card counts the money up (Doubled)
                Doubled?.Invoke(bonus);
                break;
            case GamePlatform.AdOutcome.AdsOff:
                // CrazyGames' Basic Launch: no ads at all yet. Say so for a moment instead of vanishing without a
                // word (user: "I doubled it and nothing happened"), then leave for the session.
                adsOff = true;
                if (label != null) label.text = "No videos yet";
                StartCoroutine(GoAway());
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

    private IEnumerator GoAway()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        gameObject.SetActive(false);
    }

    private IEnumerator ReadyAgain()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        button.interactable = true;
        if (label != null) label.text = $"Double +${bonus}";
    }
}
