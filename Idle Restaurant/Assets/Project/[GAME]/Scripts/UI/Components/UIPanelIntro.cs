using UnityEngine;

// Plays a short "pop in" (fade + slight scale) on a card whenever its panel becomes visible —
// either by being activated or by its parent Panel's CanvasGroup going from hidden to shown.
[RequireComponent(typeof(CanvasGroup))]
public class UIPanelIntro : MonoBehaviour
{
    private CanvasGroup group;
    private CanvasGroup parentGroup;
    private bool parentVisible;
    private float t = 1f;
    private float scale = 1f, velocity;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        parentGroup = transform.parent != null ? transform.parent.GetComponentInParent<CanvasGroup>() : null;
    }

    private void OnEnable()
    {
        parentVisible = IsParentVisible();
        if (parentVisible) Play();
    }

    private bool IsParentVisible() => parentGroup == null || parentGroup.alpha > 0.01f;

    private void Play()
    {
        t = 0f;
        scale = 0.94f;
        velocity = 0f;
        group.alpha = 0f;
    }

    private void LateUpdate()
    {
        bool visible = IsParentVisible();
        if (visible && !parentVisible) Play();
        parentVisible = visible;
        if (t >= 1f && Mathf.Approximately(scale, 1f)) return;

        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
        t = Mathf.Min(1f, t + dt / UITokens.Motion.Normal);
        group.alpha = t;
        UITokens.Spring(ref scale, ref velocity, 1f, dt);
        if (t >= 1f && Mathf.Abs(scale - 1f) < 0.001f) scale = 1f;
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
