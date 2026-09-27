using TMPro;
using UnityEngine;

// Gives a counter (money, customers, score) a short bounce whenever its text changes,
// so rewards feel earned without adding any extra UI.
[RequireComponent(typeof(TMP_Text))]
public class UIValuePunch : MonoBehaviour
{
    private TMP_Text label;
    private string last;
    private float scale = 1f, velocity;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        last = label.text;
    }

    private void LateUpdate()
    {
        if (label.text != last)
        {
            last = label.text;
            scale = UITokens.Motion.PunchScale;
            velocity = 0f;
        }
        if (scale == 1f && velocity == 0f) return;

        UITokens.Spring(ref scale, ref velocity, 1f, Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));
        if (Mathf.Abs(scale - 1f) < 0.001f && Mathf.Abs(velocity) < 0.01f) { scale = 1f; velocity = 0f; }
        transform.localScale = new Vector3(scale, scale, 1f);
    }
}
