using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lettuce : EdibleBase
{
    public override void Start()
    {
        Name = "lettuce";
        
        pool = PoolingManager.lettucePool;
        pureList = PoolingManager.lettuceSlicedList;
        defaultPoint = 10f;
        point = defaultPoint;

        base.Start();
    }

    public override GameObject SetFood()
    {
        return currentVersion;
    }
}
