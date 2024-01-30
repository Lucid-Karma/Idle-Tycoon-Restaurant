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
                if (cookingTimer > burger.maxCookingTime)
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
        base.UseFood(ingredient);
        burger = ingredient.gameObject.GetComponent<Burger>();
        cookingTimer = burger.fryingTimer;

        if(!burger.isOver)   state = State.Cook;
        Debug.Log("pre: " + cookingTimer);
    }

    public override void RemoveFood(EdibleBase ingredient)
    {
        burger.fryingTimer = cookingTimer;
        base.RemoveFood(ingredient);
        state = State.Idle;
    }
}