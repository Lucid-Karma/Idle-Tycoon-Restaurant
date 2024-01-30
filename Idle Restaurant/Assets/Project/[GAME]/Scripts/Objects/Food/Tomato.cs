using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tomato : EdibleBase
{
    public override void Start()
    {
        pool = PoolingManager.tomatoPool;
        pureList = PoolingManager.tomatoPureList;

        base.Start();
    }

    public override GameObject SetFood()
    {
        if(prefab != null)  return prefab;

        return pure;
    }

    // public override void RemoveFood(List<GameObject> food)
    // {
    //     food.Remove(gameObject);
    // }
}
