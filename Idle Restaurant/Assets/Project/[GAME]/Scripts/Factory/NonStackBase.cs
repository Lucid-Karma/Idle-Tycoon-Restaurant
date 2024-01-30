using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NonStackBase : PlaceableBase
{
    protected GameObject currentObject;
    
    public override void EnableCollider()
    {
        if(currentObject == null)
            placeableCollider.enabled = true;
    }

    public override void UseFood(EdibleBase ingredient)
    {
        ingredient.GetPlaceable(this);
        currentObject = ingredient.SetFood();
        

        ingredient.gameObject.transform.parent = transform;
        ingredient.gameObject.transform.position = transform.position;
    }

    public override void RemoveFood(EdibleBase ingredient)
    {
        currentObject = null;
    }
}
