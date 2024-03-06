using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServiceBase : NonStackBase
{
    protected override void OnEnable()
    {
        EventManager.OnScoreUpdate.AddListener(() => currentObject = null);
    }
    protected override void OnDisable()
    {
        EventManager.OnScoreUpdate.RemoveListener(() => currentObject = null);
    }
    
    public override void UseFood(EdibleBase ingredient)
    {
        base.UseFood(ingredient);

        if(ingredient is Hamburger)
            ingredient.untouchable = true;
    }
}
