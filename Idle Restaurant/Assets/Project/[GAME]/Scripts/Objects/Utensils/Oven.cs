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

    // Like Pan: remember how long the bun has baked, so taking it out and back in resumes instead of
    // restarting (the cooking indicator resumes too, so they now agree).
    public override void RemoveFood(EdibleBase ingredient)
    {
        if (ingredient is Bun removed) removed.bakeTimer = cookingTimer;
        base.RemoveFood(ingredient);
        state = State.Idle;
    }

    public override bool IsSuitable(EdibleBase ingredient)
    {
        if(ingredient != bun)    return false;
        else    return true;
    }
}
