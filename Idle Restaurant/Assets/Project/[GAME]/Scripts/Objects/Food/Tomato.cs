using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tomato : CuttableBase
{
    [SerializeField] private GameObject slicedTomato;

    public override void Start()
    {
        pool = PoolingManager.tomatoPool;
        pureList = PoolingManager.tomatoPureList;

        base.Start();
    }

    public override void SetSliced()
    {
        base.SetSliced();
        pool.GetObject(this.gameObject.transform, slicedTomato, PoolingManager.tomatoSlicedList);
        currentVersion = pool.currentObject;
    }
}
