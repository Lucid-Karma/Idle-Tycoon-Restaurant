using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Something given to the player, made seen whatever the camera shows (Tutorial's first-burger toque; user, 2026-11-07:
// "the moment he gets the hat doesn't show - he is behind the customer"): its picture pops up big in the middle of the
// screen on a warm disc, holds a beat, then flies to the chef shrinking, and `landed` runs as it gets there (the hat
// pops onto his head, confetti, "New hat!"). Built at runtime, on its own overlay canvas over the HUD; destroys itself.
public class UIGiftPop : MonoBehaviour
{
    private const float PopSeconds = 0.35f, HoldSeconds = 0.7f, FlySeconds = 0.5f;

    // `picture` is asked again every frame until it has one (the shop's pictures are fetched in the background: it
    // may arrive while the pop is already up).
    public static void Play(Func<Sprite> picture, Transform chef, Action landed)
    {
        var go = new GameObject("Gift Pop", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;   // over the HUD and the lesson's card
        go.AddComponent<UIGiftPop>().StartCoroutine(go.GetComponent<UIGiftPop>().Run(picture, chef, landed));
    }

    private IEnumerator Run(Func<Sprite> picture, Transform chef, Action landed)
    {
        var root = (RectTransform)transform;
        var item = new GameObject("Item", typeof(RectTransform)).GetComponent<RectTransform>();
        item.SetParent(root, false);
        float size = Screen.height * 0.3f;
        item.sizeDelta = new Vector2(size, size);
        var disc = item.gameObject.AddComponent<Image>();
        disc.sprite = UIOrderTags.Circle();
        disc.color = UITokens.Colors.WarmWhite;
        disc.raycastTarget = false;
        var image = new GameObject("Picture", typeof(RectTransform)).AddComponent<Image>();
        image.transform.SetParent(item, false);
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;
        var r = image.rectTransform;
        r.anchorMin = new Vector2(0.12f, 0.12f);
        r.anchorMax = new Vector2(0.88f, 0.88f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        void Fill()
        {
            if (image.enabled || picture == null) return;
            var sprite = picture();
            if (sprite != null) { image.sprite = sprite; image.enabled = true; }
        }
        Fill();
        // (no sound of its own: the lesson's cheer plays as it appears)

        Vector2 middle = new Vector2(Screen.width, Screen.height) * 0.5f;
        item.position = middle;
        // Up past its size and back (a little overshoot), unscaled: the game may be paused behind it.
        for (float t = 0f; t < PopSeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / PopSeconds;
            item.localScale = Vector3.one * (Mathf.Sin(k * Mathf.PI * 0.5f) * (1f + 0.15f * Mathf.Sin(k * Mathf.PI)));
            Fill();
            yield return null;
        }
        item.localScale = Vector3.one;
        for (float t = 0f; t < HoldSeconds; t += Time.unscaledDeltaTime) { Fill(); yield return null; }

        // To the chef's head on screen (it is followed while it flies: he may be walking).
        for (float t = 0f; t < FlySeconds; t += Time.unscaledDeltaTime)
        {
            float k = t / FlySeconds, ease = k * k * (3f - 2f * k);
            Vector2 to = HeadOnScreen(chef, middle);
            item.position = Vector2.Lerp(middle, to, ease) + Vector2.up * (Mathf.Sin(k * Mathf.PI) * size * 0.4f);
            item.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, ease);
            yield return null;
        }
        landed?.Invoke();
        if (chef != null)
        {
            var head = chef.position + Vector3.up * 2.1f;
            UICheerPop.OnSay.Invoke(head, "New hat!");
        }
        Destroy(gameObject);
    }

    private static Vector2 HeadOnScreen(Transform chef, Vector2 fallback)
    {
        var cam = Camera.main;
        if (chef == null || cam == null) return fallback;
        var p = cam.WorldToScreenPoint(chef.position + Vector3.up * 2.1f);
        return p.z > 0f ? (Vector2)p : fallback;
    }
}
