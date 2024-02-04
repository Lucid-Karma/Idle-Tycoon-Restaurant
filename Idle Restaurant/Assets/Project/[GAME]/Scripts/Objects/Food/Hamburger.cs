using System.Collections.Generic;
using UnityEngine;

public class Hamburger : EdibleBase
{
    private BoxCollider hamCollider;
    [SerializeField] private GameObject finishBun;
    public GameObject bunHolder;

    public override void Start()
    {
        pool = PoolingManager.hamburgerPool;
        pureList = PoolingManager.plateList;

        base.Start();
    }

    public void ExtendCollider(EdibleBase stackedObj)
    {
        hamCollider = GetComponent<BoxCollider>();

        hamCollider.size = new Vector3(hamCollider.size.x, hamCollider.size.y + stackedObj.collider.size.y, hamCollider.size.z);
        hamCollider.center = new Vector3(hamCollider.center.x, hamCollider.center.y + stackedObj.collider.center.y, hamCollider.center.z);
    }

    public void PutLastBun(Transform bunTransform)
    {
        pool.GetObject(bunTransform, finishBun, PoolingManager.bunTopList);
        
        hamCollider = GetComponent<BoxCollider>();

        hamCollider.size = new Vector3(hamCollider.size.x, hamCollider.size.y + 0.3071972f, hamCollider.size.z);
        hamCollider.center = new Vector3(hamCollider.center.x, hamCollider.center.y + 0.1536008f, hamCollider.center.z);
    }

    public override GameObject SetFood()
    {
        return gameObject;
    }
}
