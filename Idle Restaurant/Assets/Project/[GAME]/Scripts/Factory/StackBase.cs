using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackBase : PlaceableBase
{
    public override void EnableCollider()
    {
        
    }

    public override bool IsSuitable(EdibleBase ingredient)
    {
        throw new System.NotImplementedException();
    }

    public override void RemoveFood(EdibleBase ingredient)
    {
    }

    public override void UseFood(EdibleBase ingredient)
    {
    }
}
