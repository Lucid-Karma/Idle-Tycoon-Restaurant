using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServiceBase : NonStackBase
{
    public override void UseFood(EdibleBase ingredient)
    {
        currentObject = ingredient.SetFood();
        ingredient.GetPlaceable(this);

        ingredient.gameObject.transform.parent = transform;
        ingredient.gameObject.transform.position = transform.position;
    }
}
