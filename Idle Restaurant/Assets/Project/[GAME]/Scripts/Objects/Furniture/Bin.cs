using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The kitchen bin (user, 2026-11-03: "a throwing-away animation - it can be thrown into the bin; what is in the bin should
// show a little; Mochi can rummage in it, scatter it or eat from it"):
//   - what the chef bins flies into it in an arc, tumbling, and plops in (Junk);
//   - the last few things thrown away lie in its open mouth, poking over the rim (copies of their meshes: the food itself
//     goes back to its pool);
//   - Mochi can dig one or two of them out onto the floor (Dig) and eat them.
public class Bin : MonoBehaviour, ISelectable
{
    [SerializeField] private Material defaultMaterial;
    public Material DefaultMaterial()
    {
        return defaultMaterial;
    }

    private const float FlySeconds = 0.42f, Arc = 0.9f;
    private const int MaxLeftovers = 5;
    private const float LeftoverSize = 0.75f;       // a little smaller than in the hand, so they sit in the mouth

    private readonly List<GameObject> leftovers = new();
    private Vector3 mouth;                          // the middle of its open top
    private float mouthRadius = 0.2f;

    private void Awake()
    {
        var box = GetComponent<Collider>() != null ? GetComponent<Collider>().bounds : new Bounds(transform.position, Vector3.one);
        mouth = new Vector3(box.center.x, box.max.y, box.center.z);
        mouthRadius = Mathf.Min(box.extents.x, box.extents.z) * 0.45f;
    }

    public bool HasLeftovers => leftovers.Count > 0;
    public Vector3 Mouth => mouth;

    // Thrown in: it flies from the chef's hands into the bin; `delay` staggers several thrown at once.
    public void Junk(EdibleBase edibleObject, float delay = 0f)
    {
        if (edibleObject == null) return;
        var body = edibleObject.transform;
        body.SetParent(null, true);
        var col = edibleObject.GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (!isActiveAndEnabled) { Land(edibleObject, col); return; }
        StartCoroutine(Throw(edibleObject, col, delay));
    }

    private IEnumerator Throw(EdibleBase food, Collider col, float delay)
    {
        var body = food.transform;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        var from = body.position;
        var axis = Random.onUnitSphere;
        var into = mouth + Vector3.down * 0.15f;
        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / FlySeconds);
            if (body == null || !body.gameObject.activeInHierarchy) yield break;
            body.position = Vector3.Lerp(from, into, t) + Vector3.up * (Arc * 4f * t * (1f - t));
            body.Rotate(axis, 620f * Time.deltaTime, Space.World);
            yield return null;
        }
        Land(food, col);
    }

    private void Land(EdibleBase food, Collider col)
    {
        Keep(food);
        if (col != null) col.enabled = true;
        food.gameObject.SetActive(false);   // back to its pool
        GameSfx.Play(GameSfx.Cue.Trash);
        TapJuice.Boing(transform, 0.12f);
        foreach (var piece in leftovers) if (piece != null) TapJuice.Hop(piece.transform, 0.05f);
    }

    // A copy of its look lying in the mouth (only its meshes); the oldest goes when there are too many.
    private void Keep(EdibleBase food)
    {
        var copy = new GameObject("Leftover " + food.name);
        copy.transform.SetPositionAndRotation(food.transform.position, food.transform.rotation);
        foreach (var renderer in food.GetComponentsInChildren<MeshRenderer>())
        {
            if (!renderer.enabled || !renderer.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null) continue;
            var part = new GameObject(renderer.name);
            part.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
            part.transform.localScale = renderer.transform.lossyScale;
            part.transform.SetParent(copy.transform, true);
            part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var r = part.AddComponent<MeshRenderer>();
            r.sharedMaterials = renderer.sharedMaterials;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        if (copy.transform.childCount == 0) { Destroy(copy); return; }
        copy.transform.SetParent(transform, true);
        copy.transform.localScale *= LeftoverSize;
        // Lying any which way in the mouth, the newest on top, half over the rim.
        var spot = Random.insideUnitCircle * mouthRadius;
        copy.transform.position = mouth + new Vector3(spot.x, -0.1f + 0.035f * leftovers.Count, spot.y);
        copy.transform.rotation = Quaternion.Euler(Random.Range(-35f, 35f), Random.Range(0f, 360f), Random.Range(-35f, 35f));
        leftovers.Add(copy);
        while (leftovers.Count > MaxLeftovers)
        {
            if (leftovers[0] != null) Destroy(leftovers[0]);
            leftovers.RemoveAt(0);
        }
    }

    // Mochi digs in: up to `count` things fly out of the mouth onto the floor around `near`, and are his now (he eats
    // them: Waiter). Returns what came out.
    public List<Transform> Dig(Vector3 near, int count)
    {
        var out_ = new List<Transform>();
        for (int i = 0; i < count && leftovers.Count > 0; i++)
        {
            var piece = leftovers[^1];
            leftovers.RemoveAt(leftovers.Count - 1);
            if (piece == null) { i--; continue; }
            piece.transform.SetParent(null, true);
            var side = Random.insideUnitCircle.normalized * Random.Range(0.35f, 0.7f);
            var floor = new Vector3(near.x + side.x, near.y + 0.05f, near.z + side.y);
            StartCoroutine(Out(piece.transform, floor, i * 0.18f));
            out_.Add(piece.transform);
        }
        TapJuice.Boing(transform, 0.2f);
        foreach (var piece in leftovers) if (piece != null) TapJuice.Hop(piece.transform, 0.08f);
        return out_;
    }

    private IEnumerator Out(Transform piece, Vector3 to, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        var from = piece.position;
        var axis = Random.onUnitSphere;
        for (float t = 0f; t < 1f;)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / 0.5f);
            if (piece == null) yield break;
            piece.position = Vector3.Lerp(from, to, t) + Vector3.up * (0.8f * 4f * t * (1f - t));
            piece.Rotate(axis, 500f * Time.deltaTime * (1f - t), Space.World);
            yield return null;
        }
        if (piece != null) TapJuice.Boing(piece, 0.25f);
    }

    public Vector3 SelectablePos()
    {
        return gameObject.transform.position;
    }
}
