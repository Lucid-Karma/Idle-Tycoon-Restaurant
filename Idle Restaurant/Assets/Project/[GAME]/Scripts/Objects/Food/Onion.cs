using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Onion : EdibleBase
{
    public override void Start()
    {
        pool = PoolingManager.onionPool;
        pureList = PoolingManager.onionPureList;

        base.Start();
    }

    public override GameObject SetFood()
    {
        if(prefab != null)  return prefab;

        return pure;
    }
}
