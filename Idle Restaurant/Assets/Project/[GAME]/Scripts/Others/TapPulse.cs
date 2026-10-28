using UnityEngine;

// A thing to tap swelling and settling once a beat, so it is found without reading (Tutorial, user 2026-10-21: "the
// burger to finish could grow and shrink, so a player who doesn't read the card still sees they have to tap it").
// Only the picture swells: the scale is put back at the start of every frame (Update, first of all scripts) and
// applied again in LateUpdate for the render, so nothing that stacks, lifts or measures food ever sees a scaled
// plate - food lifted off a scaled plate would keep the extra size for good.
[DefaultExecutionOrder(-10000)]
public class TapPulse : MonoBehaviour
{
    private float amount, period, started;
    private Vector3 baseScale, applied;
    private bool scaled;

    public static TapPulse On(Transform target, float amount, float period)
    {
        var pulse = target.gameObject.AddComponent<TapPulse>();
        pulse.amount = amount;
        pulse.period = period;
        pulse.started = Time.time;
        return pulse;
    }

    public void Stop()
    {
        Restore();
        Destroy(this);
    }

    private void FixedUpdate() => Restore();   // physics too: a swollen collider would brush triggers
    private void Update() => Restore();
    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();

    private void LateUpdate()
    {
        float k = Beat((Time.time - started) % period);
        if (Mathf.Approximately(k, 0f)) return;
        baseScale = transform.localScale;
        applied = baseScale * (1f + amount * k);
        transform.localScale = applied;
        scaled = true;
    }

    private void Restore()
    {
        if (!scaled) return;
        scaled = false;
        if (transform.localScale == applied) transform.localScale = baseScale;   // (unless something else set it since)
    }

    // Up quickly, down past the rest size with a small squash, settle; then still until the next beat.
    private static float Beat(float s)
    {
        const float up = 0.14f, down = 0.42f;
        if (s < up) return Mathf.Sin(s / up * Mathf.PI * 0.5f);
        s -= up;
        if (s >= down) return 0f;
        float x = s / down;
        return Mathf.Cos(x * Mathf.PI * 1.5f) * (1f - x);
    }
}
