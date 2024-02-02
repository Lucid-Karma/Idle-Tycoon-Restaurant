using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oven : CookingBase
{
    Bun bun;

    void Update()
    {
        if (currentObject != null)
        {
            switch (state)
            {
                case State.Idle:
                break;

                case State.Cook:
                cookingTimer += Time.deltaTime;
                if (cookingTimer > bun.maxCookingTime)
                {
                    if(!bun.isOver)
                    {
                        bun.SetCookedBun();
                        Debug.Log(cookingTimer);
                        cookingTimer = 0f;
                    }
                }
                break;
            }
        }
    }

    public override void UseFood(EdibleBase ingredient)
    {
        bun = ingredient.gameObject.GetComponent<Bun>();
        if(!IsSuitable(ingredient)) return;

        base.UseFood(ingredient);
        cookingTimer = bun.bakeTimer;

        if(!bun.isOver)   state = State.Cook;
        Debug.Log("pre: " + cookingTimer);
    }

    public override bool IsSuitable(EdibleBase ingredient)
    {
        if(ingredient != bun)    return false;
        else    return true;
    }
}
