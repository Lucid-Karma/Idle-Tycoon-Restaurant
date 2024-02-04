using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Onion : CuttableBase
{
    [SerializeField] private GameObject slicedOnion;
    private Vector3 slicedOnionColSize = new Vector3(0.7757592f, 0.1063608f, 0.7296072f);
    private Vector3 slicedOnionColCenter = new Vector3(-0.005801514f, 0.02580204f, -0.0207592f);

    public override void Start()
    {
        pool = PoolingManager.onionPool;
        pureList = PoolingManager.onionPureList;

        base.Start();
    }

    public override void SetSliced()
    {
        base.SetSliced();
        pool.GetObject(this.gameObject.transform, slicedOnion, PoolingManager.onionSlicedList);
        currentVersion = pool.currentObject;

        collider.size = slicedOnionColSize;
        collider.center = slicedOnionColCenter;
    }
}
