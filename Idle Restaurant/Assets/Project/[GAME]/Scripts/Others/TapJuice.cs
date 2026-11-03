using UnityEngine;

// A one-shot bounce on anything (user, 2026-10-30: "every interaction should give feedback - the ingredient you tap
// grows and shrinks, or hops; even someone just poking at random things should enjoy it"): a squash and stretch
// (Boing) or a little hop (Hop). Like TapPulse, only the picture moves: scale and position are put back first thing
// every frame and the bounce is applied again in LateUpdate for the render, so nothing that stacks, lifts, walks or
// measures ever sees it. (Static-batched renderers can't move at all: the things you tap are not batched - step 17.)
[DefaultExecutionOrder(-10000)]
public class TapJuice : MonoBehaviour
{
    private const float BoingSeconds = 0.42f, HopSeconds = 0.38f;

    private float boingStart = -1f, boingAmount, hopStart = -1f, hopHeight;
    private Vector3 baseScale, appliedScale, basePos, appliedPos;
    private bool scaled, moved;

    // Squash, stretch, settle. `amount` ~0.15 for a tap.
    public static void Boing(Transform t, float amount = 0.15f)
    {
        if (t == null) return;
        var juice = t.GetComponent<TapJuice>();
        if (juice == null) juice = t.gameObject.AddComponent<TapJuice>();
        juice.boingStart = Time.unscaledTime;
        juice.boingAmount = amount;
    }

    // Up and down again (metres), with a squash as it lands.
    public static void Hop(Transform t, float height = 0.25f)
    {
        if (t == null) return;
        var juice = t.GetComponent<TapJuice>();
        if (juice == null) juice = t.gameObject.AddComponent<TapJuice>();
        juice.hopStart = Time.unscaledTime;
        juice.hopHeight = height;
    }

    private void FixedUpdate() => Restore();
    private void Update() => Restore();
    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();

    private void LateUpdate()
    {
        float now = Time.unscaledTime;
        bool boing = boingStart >= 0f && now - boingStart < BoingSeconds;
        bool hop = hopStart >= 0f && now - hopStart < HopSeconds;
        if (!boing && !hop)
        {
            boingStart = hopStart = -1f;
            return;
        }
        var squash = Vector3.one;
        if (boing)
        {
            float k = (now - boingStart) / BoingSeconds;
            // A quick squash (wide and low), a stretch (tall and thin), then a few small wobbles out.
            float s = -Mathf.Sin(k * Mathf.PI * 3f) * Mathf.Pow(1f - k, 2f) * boingAmount;
            squash = new Vector3(1f - s * 0.6f, 1f + s, 1f - s * 0.6f);
        }
        float lift = 0f;
        if (hop)
        {
            float k = (now - hopStart) / HopSeconds;
            if (k < 0.75f) lift = Mathf.Sin(k / 0.75f * Mathf.PI) * hopHeight;
            else
            {
                float land = (k - 0.75f) / 0.25f;   // a little squash as it lands
                float s = Mathf.Sin(land * Mathf.PI) * 0.12f;
                squash = Vector3.Scale(squash, new Vector3(1f + s * 0.6f, 1f - s, 1f + s * 0.6f));
            }
        }
        baseScale = transform.localScale;
        appliedScale = Vector3.Scale(baseScale, squash);
        transform.localScale = appliedScale;
        scaled = true;
        if (lift != 0f)
        {
            basePos = transform.localPosition;
            appliedPos = basePos + transform.parent switch
            {
                null => Vector3.up * lift,
                var p => p.InverseTransformVector(Vector3.up * lift),
            };
            transform.localPosition = appliedPos;
            moved = true;
        }
    }

    private void Restore()
    {
        if (scaled)
        {
            scaled = false;
            if (transform.localScale == appliedScale) transform.localScale = baseScale;
        }
        if (moved)
        {
            moved = false;
            if (transform.localPosition == appliedPos) transform.localPosition = basePos;
        }
    }
}
