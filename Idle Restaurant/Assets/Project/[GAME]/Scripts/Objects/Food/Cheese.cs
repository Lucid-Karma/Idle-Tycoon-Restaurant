using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cheese : CuttableBase
{
    [SerializeField] private GameObject slicedCheese;
    private Vector3 slicedCheeseColSize = new Vector3(0.8243364f, 0.1054935f, 0.8243367f);
    private Vector3 slicedCheeseColCenter = new Vector3(1.043081e-06f, -0.03467316f, -1.231838e-16f);

    public override void Start()
    {
        Name = "cheese";
        
        pool = PoolingManager.cheesePool;
        pureList = PoolingManager.cheesePureList;
        defaultPoint = 1f;
        point = defaultPoint;

        base.Start();
    }

    public override void SetSliced()
    {
        base.SetSliced();
        pool.GetObject(this.gameObject.transform, slicedCheese, PoolingManager.cheeseSlicedList);
        currentVersion = pool.currentObject;
        point = 10f;

        collider.size = slicedCheeseColSize;
        collider.center = slicedCheeseColCenter;
    }
}
