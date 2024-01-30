using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookingBase : NonStackBase
{
    protected enum State
    {
        Idle,
        Cook
    }
    protected State state;
    protected float cookingTimer;

    public override void Start()
    {
        base.Start();
        state = State.Idle;
    }

    public override void UseFood(EdibleBase ingredient)
    {
        base.UseFood(ingredient);
    }
}
