using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Oven : CookingBase
{
    Bun bun;

    void Update()
    {
        // Hums while a bun bakes; quiet once it has burnt (like the pan's sizzle).
        GameSfx.Bake(this, currentObject != null && state == State.Cook && bun != null && bun.Preparation != Prep.Burnt);
        if (currentObject != null)
        {
            switch (state)
            {
                case State.Idle:
                break;

                case State.Cook:
                // During the first-shift lesson a baked bun waits for as long as it takes: no burning.
                if (Tutorial.Running && bun.Preparation == Prep.Good) break;
                cookingTimer += Time.deltaTime * Tutorial.CookSpeed;
                if (cookingTimer > bun.maxCookingTime)
                {
                    if(!bun.isOver)
                    {
                        bun.SetCookedBun();
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
    }

    // Like Pan: remember how long the bun has baked, so taking it out and back in resumes instead of
    // restarting (the cooking indicator resumes too, so they now agree).
    public override void RemoveFood(EdibleBase ingredient)
    {
        if (ingredient is Bun removed) removed.bakeTimer = cookingTimer;
        base.RemoveFood(ingredient);
        state = State.Idle;
    }

    private void OnDisable() => GameSfx.Bake(this, false);

    public override bool IsSuitable(EdibleBase ingredient)
    {
        if(ingredient != bun)    return false;
        else    return true;
    }
}
