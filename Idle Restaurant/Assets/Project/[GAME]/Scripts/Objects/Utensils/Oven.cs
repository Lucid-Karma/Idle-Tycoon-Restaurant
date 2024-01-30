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
}
