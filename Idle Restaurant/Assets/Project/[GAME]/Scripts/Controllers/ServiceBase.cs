using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServiceBase : NonStackBase
{
    // Deliberately does not subscribe to the food held/dropped events: service points stay clickable.
    // The table is cleared by the customer who ate from it (NpcFsm.React) — it used to be cleared on every
    // score update, which also wiped burgers waiting on other tables (and purchases triggered it too).
    protected override void OnEnable() { }
    protected override void OnDisable() { }

    public override void UseFood(EdibleBase ingredient)
    {
        // Only a finished burger can be served; anything else used to make the customer leave at once.
        if (!IsSuitable(ingredient)) return;

        base.UseFood(ingredient);
        ingredient.untouchable = true;
    }

    // PlayerFSM.DropObject asks this again right after UseFood, so "already holding this burger" counts too.
    public override bool IsSuitable(EdibleBase ingredient)
    {
        return ingredient is Hamburger && (!IsHaveFood() || currentObject == ingredient.gameObject);
    }
}
