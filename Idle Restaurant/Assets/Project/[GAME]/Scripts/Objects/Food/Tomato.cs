using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tomato : CuttableBase
{
    [SerializeField] private GameObject slicedTomato;
    private Vector3 slicedTomatoColSize = new Vector3(0.65f, 0.09996963f, 0.65f);
    private Vector3 slicedTomatoColCenter = new Vector3(0f, 0.05001526f, 1.490116e-08f);

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
        
        collider.size = slicedTomatoColSize;
        collider.center = slicedTomatoColCenter;
    }
}
