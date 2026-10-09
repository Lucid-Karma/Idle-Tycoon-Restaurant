using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pan : CookingBase
{
    Burger burger;

    void Update()
    {
        // The patty sizzles while it is on the heat (raw or done; a burnt one has gone quiet).
        GameSfx.Sizzle(this, currentObject != null && state == State.Cook && burger != null && burger.Preparation != Prep.Burnt);
        if (currentObject != null)
        {
            switch (state)
            {
                case State.Idle:
                break;

                case State.Cook:
                // During the first-shift lesson a fried patty waits for as long as it takes: no burning.
                if (Tutorial.Running && burger.Preparation == Prep.Good) break;
                cookingTimer += Time.deltaTime;
                if (cookingTimer > maxCookingTime)
                {
                    if(!burger.isOver)
                    {
                        burger.SetCooked();
                        cookingTimer = 0f;
                    }
                }
                break;
            }
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameSfx.Sizzle(this, false);
    }

    public override void UseFood(EdibleBase ingredient)
    {
        burger = ingredient.gameObject.GetComponent<Burger>();
        if(!IsSuitable(ingredient)) return;

        maxCookingTime = burger.maxCookingTime;

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
