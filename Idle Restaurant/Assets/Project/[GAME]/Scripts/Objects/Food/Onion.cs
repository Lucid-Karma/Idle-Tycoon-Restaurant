using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Onion : CuttableBase
{
    [SerializeField] private GameObject slicedOnion;

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
    }
}
