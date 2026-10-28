using UnityEngine;

// A customer who comes to the Halloween cafe as a ghost (SeasonalDecor.Dress): their own body is hidden and a
// little sheet-ghost floats where they are - under their model, so it walks, sits, pops in and out with them -
// bobbing gently, its face always turned to the camera. Their sheets flutter: the hem trails behind as they glide
// and ripples a little all the time, and they lean into where they are going (a few hundred vertices moved on the
// CPU per ghost: cheap). Also the big ghost by the burger and the heart balloons (just the bob).
public class SeasonalGhost : MonoBehaviour
{
    public Transform body;   // the sheet (bobs)
    public Transform face;   // the eyes and mouth (turned to the camera)
    public float bob = 0.08f, speed = 2.2f;
    public bool flutter;     // the sheet's hem moves (customers)
    private Renderer[] hidden;
    private float phase;
    private float? baseY;
    private Mesh sheet;
    private Vector3[] rest, moved;
    private Vector3 lastPosition;
    private Vector3 trail;   // smoothed, in the sheet's own space: where the hem is pulled to

    // Hides the customer's own renderers (put back by Undress).
    public void Hide(Renderer[] own)
    {
        hidden = own;
        foreach (var r in own) if (r != null) r.enabled = false;
    }

    public void Undress()
    {
        if (hidden != null) foreach (var r in hidden) if (r != null) r.enabled = true;
        hidden = null;
        if (body != null) Destroy(body.gameObject);
        if (face != null) Destroy(face.gameObject);
        if (sheet != null) Destroy(sheet);
        Destroy(this);
    }

    private void Awake() => phase = Random.value * 10f;

    private void LateUpdate()
    {
        if (body == null) return;
        var p = body.localPosition;
        baseY ??= p.y;
        p.y = baseY.Value + Mathf.Sin((Time.time + phase) * speed) * bob;
        body.localPosition = p;
        if (flutter) Flutter();
        var cam = Camera.main;
        if (face != null && cam != null)
        {
            face.position = body.position + Vector3.up * (body.lossyScale.y * 1.05f) - cam.transform.forward * body.lossyScale.x * 0.62f;
            face.rotation = Quaternion.LookRotation(cam.transform.forward);
        }
    }

    private void Flutter()
    {
        if (sheet == null)
        {
            var mf = body.GetComponent<MeshFilter>();
            if (mf == null) return;
            sheet = mf.mesh;   // its own copy
            rest = sheet.vertices;
            moved = new Vector3[rest.Length];
            lastPosition = body.position;
        }
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        var velocity = (body.position - lastPosition) / dt;
        lastPosition = body.position;
        velocity.y = 0f;
        float pace = Mathf.Clamp01(velocity.magnitude / 2f);   // (customers walk at about 2 m/s: full flutter)
        var behind = body.InverseTransformDirection(-velocity.normalized) * pace;
        trail = Vector3.Lerp(trail, behind, 1f - Mathf.Exp(-6f * dt));
        // Leaning into the glide.
        var axis = Vector3.Cross(Vector3.up, velocity.normalized);
        var lean = axis.sqrMagnitude > 1e-4f ? Quaternion.AngleAxis(12f * pace, axis) : Quaternion.identity;
        body.rotation = Quaternion.Slerp(body.rotation, lean * body.parent.rotation, 1f - Mathf.Exp(-6f * dt));

        float t = Time.time * 5f + phase;
        for (int i = 0; i < rest.Length; i++)
        {
            var v = rest[i];
            float hem = 1f - Mathf.Clamp01(v.y / 0.85f);   // 1 at the hem, 0 from the shoulders up
            if (hem <= 0f) { moved[i] = v; continue; }
            hem *= hem;
            float angle = Mathf.Atan2(v.z, v.x);
            float ripple = Mathf.Sin(angle * 4f + t) * (0.045f + 0.07f * trail.magnitude);
            var outward = new Vector3(v.x, 0f, v.z).normalized;
            moved[i] = v + (outward * ripple + trail * 0.4f + Vector3.up * trail.magnitude * 0.1f) * hem;
        }
        sheet.vertices = moved;
        sheet.RecalculateNormals();
    }
}
