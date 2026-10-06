using UnityEngine;

// Lights a static object like a moving one: its baked lightmap is ignored. For objects whose baked data is
// unusable. The bin's and the onion crate's meshes (combined in the scene, no lightmap UVs) baked with
// overlapping UVs, and since static objects read their shadows from the baked shadowmask
// (PerformanceGovernor), the bin came out black and the onions blotchy.
// Static batching is untouched; only the lightmap assignment (set when the scene loads) is dropped.
[RequireComponent(typeof(Renderer))]
public class RealtimeLit : MonoBehaviour
{
    private void Awake() => GetComponent<Renderer>().lightmapIndex = -1;
}
