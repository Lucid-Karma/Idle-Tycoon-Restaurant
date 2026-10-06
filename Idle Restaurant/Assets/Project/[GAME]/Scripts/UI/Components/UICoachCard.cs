using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Mochi's card along the bottom of the screen during the first shift (Tutorial): his face, one line, and
// on the two steps that are only a hello and a goodbye, a "tap to go on". It never covers the kitchen - it
// sits under it - and it is never modal: the player can keep playing straight through it.
public class UICoachCard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TMP_Text line;
    [SerializeField] private GameObject tapHint;
    [SerializeField] private RectTransform face;
    [SerializeField] private float fade = 0.18f;

    private Action onTap;
    private Vector2 restPosition;
    private float shownAt = -1f;

    private void Awake()
    {
        restPosition = ((RectTransform)transform).anchoredPosition;
        group.alpha = 0f;
        group.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void Show(string text, Action tap)
    {
        gameObject.SetActive(true);
        line.text = text;
        onTap = tap;
        SetHint("tap to go on", tap != null);
        group.blocksRaycasts = tap != null;
        shownAt = Time.unscaledTime;
    }

    // A step that waits for the player to act can still let them move on, if they want to: the card
    // becomes tappable and says so. Nothing happens unless they tap.
    public void OfferSkip(Action skip)
    {
        onTap = skip;
        SetHint("tap to skip", true);
        group.blocksRaycasts = true;
    }

    private void SetHint(string text, bool visible)
    {
        if (tapHint == null) return;
        tapHint.SetActive(visible);
        if (tapHint.TryGetComponent<TMP_Text>(out var label)) label.text = text;
    }

    // The same card saying something new because what the player is doing changed ("grab something ready"
    // → "now toss it"): no fade, no slide, it just answers.
    public void SetLine(string text) => line.text = text;

    public void Hide()
    {
        onTap = null;
        shownAt = -1f;
        group.blocksRaycasts = false;
        if (gameObject.activeSelf) StartCoroutine(FadeOut());
    }

    public void OnPointerClick(PointerEventData _) => onTap?.Invoke();

    private System.Collections.IEnumerator FadeOut()
    {
        for (float t = group.alpha; t > 0f; t -= Time.unscaledDeltaTime / fade)
        {
            group.alpha = t;
            yield return null;
        }
        group.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (shownAt < 0f) return;
        float age = Time.unscaledTime - shownAt;
        group.alpha = Mathf.Clamp01(age / fade);
        // Slides up into place, and Mochi nods along while he is talking.
        var rt = (RectTransform)transform;
        rt.anchoredPosition = restPosition + Vector2.down * (40f * (1f - Mathf.Clamp01(age / (fade * 2f))));
        if (face != null)
        {
            float nod = Mathf.Sin(Time.unscaledTime * 4f);
            face.localRotation = Quaternion.Euler(0f, 0f, nod * 5f);
            face.localScale = Vector3.one * (1f + Mathf.Abs(nod) * 0.03f);
        }
    }
}
