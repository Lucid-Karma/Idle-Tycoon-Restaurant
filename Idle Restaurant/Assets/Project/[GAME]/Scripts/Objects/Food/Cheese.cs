using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cheese : CuttableBase
{
    [SerializeField] private GameObject slicedCheese;

    public override void Start()
    {
        pool = PoolingManager.cheesePool;
        pureList = PoolingManager.cheesePureList;

        base.Start();
    }

    public override void SetSliced()
    {
        base.SetSliced();
        pool.GetObject(this.gameObject.transform, slicedCheese, PoolingManager.cheeseSlicedList);
        currentVersion = pool.currentObject;
    }
}
