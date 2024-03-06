using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pan : CookingBase
{
    Burger burger;

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
                if (cookingTimer > maxCookingTime)
                {
                    if(!burger.isOver)
                    {
                        burger.SetCooked();
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
        burger = ingredient.gameObject.GetComponent<Burger>();
        maxCookingTime = burger.maxCookingTime;

        if(!IsSuitable(ingredient)) return;

        base.UseFood(ingredient);
        cookingTimer = burger.fryingTimer;

        if(!burger.isOver)   state = State.Cook;
    }

    public override void RemoveFood(EdibleBase ingredient)
    {
        burger.fryingTimer = cookingTimer;
        base.RemoveFood(ingredient);
        state = State.Idle;
    }

    public override bool IsSuitable(EdibleBase ingredient)
    {
        if(ingredient != burger)    return false;
        else    return true;
    }
}
