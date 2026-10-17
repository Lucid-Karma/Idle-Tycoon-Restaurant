using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Chair : MonoBehaviour, ISedile
{
    public bool IsEmpty{ get; set; } = true;   // also before Start: chairs of a table bought mid-shift
    public NonStackBase service;

    // The navmesh was baked without the chairs, so customers walked straight through them, and through whoever sat
    // on them (pushing the seated one off their seat: two customers on one chair - fixed in NpcFsm.Seat, a seated
    // customer's agent is switched off). Each chair now cuts its own hole in the navmesh - the chair and the place the
    // customer sits in - so everyone walks round it. A carving obstacle, not a plain one: with plain obstacles the
    // path still ran through the chairs and avoidance pushed customers back off them, so one walked in circles beside
    // a table and never sat down. The customer coming to it (or getting up from it) opens it for themselves (Open),
    // so they can still step in and out. The chef still reaches a table with every chair taken (tested).
    private NavMeshObstacle block;
    private readonly HashSet<Object> openFor = new();

    private void Awake() => block = BuildBlock();

    void Start()
    {
        IsEmpty = true;
    }

    public Vector3 CalculateSitPos()
    {
        return transform.position + transform.forward * 0.65f;
    }

    public Quaternion GetSedileRot()
    {
        return transform.rotation;
    }

    public NonStackBase GetTableService()
    {
        return service;
    }

    public void UpdateChairState()
    {
        IsEmpty = !service.IsHaveFood();
    }

    public void Open(Object who, bool open)
    {
        if (open) openFor.Add(who);
        else openFor.Remove(who);
        if (block != null) block.enabled = openFor.Count == 0;
    }

    // The chair and the seat, in metres in the chair's own frame: 0.8 wide, from its back (0.42 behind the pivot) to
    // where the customer sits (0.65 in front), 1.2 high - chair_A's mesh bounds plus the seat. Fixed sizes on purpose:
    // the chairs are static-batched, and in a build their MeshFilter holds the scene's whole combined mesh, so a box
    // made from it covered the entire cafe and carved the navmesh away - customers froze at the door.
    private static readonly Vector3 BlockCenter = new(0f, 0.6f, 0.115f), BlockSize = new(0.8f, 1.2f, 1.07f);

    private NavMeshObstacle BuildBlock()
    {
        var scale = transform.lossyScale;
        var perMetre = new Vector3(1f / Mathf.Max(0.01f, scale.x), 1f / Mathf.Max(0.01f, scale.y), 1f / Mathf.Max(0.01f, scale.z));
        var obstacle = gameObject.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.center = Vector3.Scale(BlockCenter, perMetre);
        obstacle.size = Vector3.Scale(BlockSize, perMetre);
        obstacle.carving = true;
        obstacle.carveOnlyStationary = true;
        obstacle.carvingTimeToStationary = 0f;
        return obstacle;
    }
}
