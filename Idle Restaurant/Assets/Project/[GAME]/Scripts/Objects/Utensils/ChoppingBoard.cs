using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoppingBoard : NonStackBase
{
    Animator animator;
    Animator Animator { get { return (animator == null)? animator = GetComponent<Animator>(): animator; }}

    CuttableBase foodToCut;

    public override void UseFood(EdibleBase ingredient)
    {
        foodToCut = ingredient.gameObject.GetComponent<CuttableBase>();
        if(!IsSuitable(ingredient)) return;
        base.UseFood(ingredient);

        foodToCut.gameObject.GetComponent<Collider>().enabled = false;
        
        Animator.SetTrigger("Chop");
    }

    public void ChopFood()
    {
        Animator.SetTrigger("Idle");

        if(foodToCut != null)
        {
            foodToCut.SetSliced();
        }

        foodToCut.gameObject.GetComponent<Collider>().enabled = true;
    }

    public override bool IsSuitable(EdibleBase ingredient)
    {
        if(ingredient != foodToCut)    return false;
        else    return true;
    }
}
