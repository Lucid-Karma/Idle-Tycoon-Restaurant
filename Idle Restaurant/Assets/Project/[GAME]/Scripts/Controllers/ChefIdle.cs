using System.Collections;
using UnityEngine;

// The chef is never just frozen (user, 2026-11-04: "life in the quiet moments"): left standing with nothing to do for a
// while, he does a little flourish - with empty hands a happy twirl, with things in his hands a juggle (they go up a
// little and come back down into his hands: CarryStack.Scatter, a small one). Once per idle spell, and then not
// again for a good while. Not in the first lesson. Added by PlayerFSM at runtime.
public class ChefIdle : MonoBehaviour
{
    private const float After = 7f, Again = 18f;
    private PlayerFSM chef;
    private CarryStack stack;
    private float idleSince = -1f, lastFlourish = -100f;
    private bool done;

    private void Start()
    {
        chef = GetComponent<PlayerFSM>();
        stack = GetComponent<CarryStack>();
    }

    private void Update()
    {
        bool idle = chef != null && chef.Idle && !chef.IsStunned && !chef.Steering && Time.timeScale > 0f && !Tutorial.Running
                    && (stack == null || !stack.Scattering);
        if (!idle) { idleSince = -1f; done = false; return; }
        if (idleSince < 0f) idleSince = Time.time;
        if (done || Time.time - idleSince < After || Time.time - lastFlourish < Again) return;
        done = true;
        lastFlourish = Time.time;
        if (chef.Carried.Count > 0 && stack != null) stack.Scatter(0.75f, height: 0.7f, spread: 0.12f);
        else StartCoroutine(Twirl());
    }

    // A full turn on the spot with a little hop.
    private IEnumerator Twirl()
    {
        var body = chef.GetComponentInChildren<Animator>();
        if (body == null || body.transform == transform) yield break;
        var model = body.transform;
        var rest = model.localRotation;
        TapJuice.Hop(model, 0.25f);
        for (float t = 0f; t < 1f; t += Time.deltaTime / 0.6f)
        {
            if (!chef.Idle) break;   // he was asked to do something: the turn ends at once
            float k = t * t * (3f - 2f * t);
            model.localRotation = rest * Quaternion.Euler(0f, 360f * k, 0f);
            yield return null;
        }
        model.localRotation = rest;
    }
}
