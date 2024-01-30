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

    public void ExtendCollider(GameObject stackedObj)
    {
        hamCollider = GetComponent<BoxCollider>();

        hamCollider.size = new Vector3(hamCollider.size.x, hamCollider.size.y + stackedObj.transform.localScale.y / 2, hamCollider.size.z);
        hamCollider.center = new Vector3(hamCollider.center.x, hamCollider.center.y + stackedObj.transform.localScale.y / 4, hamCollider.center.z);
    }

    public void PutLastBun(Transform bunTransform)
    {
        pool.GetObject(bunTransform, finishBun, PoolingManager.bunTopList);
        ExtendCollider(finishBun);
    }

    public override GameObject SetFood()
    {
        return gameObject;
    }
}
