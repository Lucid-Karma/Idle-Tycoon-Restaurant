using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cheese : EdibleBase
{
    public override void Start()
    {
        pool = PoolingManager.cheesePool;
        pureList = PoolingManager.cheesePureList;

        base.Start();
    }

    public override GameObject SetFood()
    {
        if(prefab != null)  return prefab;

        return pure;
    }
}
