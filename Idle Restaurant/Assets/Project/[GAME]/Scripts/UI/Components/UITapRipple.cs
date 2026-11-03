using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The ring that opens where a finger lands (TapFeedback): warm white, quick, under the HUD.
public class UITapRipple : MonoBehaviour
{
    private const float Seconds = 0.38f;
    private static UITapRipple instance;
    private readonly List<(RectTransform ring, Image image, float start)> rings = new();

    public static void Show(Vector2 screen)
    {
        if (instance == null)
        {
            var go = new GameObject("Tap Ripples", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -1;
            instance = go.AddComponent<UITapRipple>();
        }
        instance.Add(screen);
    }

    private void Add(Vector2 screen)
    {
        var free = rings.FindIndex(r => !r.ring.gameObject.activeSelf);
        RectTransform ring;
        Image image;
        if (free >= 0) { ring = rings[free].ring; image = rings[free].image; rings.RemoveAt(free); }
        else
        {
            image = new GameObject("Ripple", typeof(RectTransform)).AddComponent<Image>();
            image.transform.SetParent(transform, false);
            image.sprite = UIShapes.Ring();
            image.raycastTarget = false;
            ring = image.rectTransform;
        }
        ring.gameObject.SetActive(true);
        ring.position = screen;
        rings.Add((ring, image, Time.unscaledTime));
    }

    private void LateUpdate()
    {
        float size = Screen.height * 0.075f;
        foreach (var (ring, image, start) in rings)
        {
            if (!ring.gameObject.activeSelf) continue;
            float k = (Time.unscaledTime - start) / Seconds;
            if (k >= 1f) { ring.gameObject.SetActive(false); continue; }
            float grow = 1f - (1f - k) * (1f - k);
            ring.sizeDelta = new Vector2(size, size * 0.62f) * Mathf.Lerp(0.35f, 1.2f, grow);
            image.color = UITokens.WithAlpha(UITokens.Colors.WarmWhite, 0.9f * (1f - k));
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
