using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lettuce : EdibleBase
{
    public override void Start()
    {
        pool = PoolingManager.lettucePool;
        pureList = PoolingManager.lettuceSlicedList;
        point = 10f;

        base.Start();
    }

    public override GameObject SetFood()
    {
        if(prefab != null)  return prefab;

        return pure;
    }
}
