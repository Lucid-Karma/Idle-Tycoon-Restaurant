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
        
        if(!foodToCut.isSliced)
        {
            foodToCut.gameObject.GetComponent<Collider>().enabled = false;
            Animator.SetTrigger("Chop");
            if (chopping != null) StopCoroutine(chopping);
            chopping = StartCoroutine(ChopSounds());
        }
    }

    // The knife comes down four times in the Chop clip (ChoppingBoardAnim: 0.5, 0.83, 1.17, 1.5 s).
    static readonly float[] KnifeHits = { 0.5f, 0.83f, 1.17f, 1.5f };
    Coroutine chopping;

    IEnumerator ChopSounds()
    {
        float t = 0f;
        foreach (var hit in KnifeHits)
        {
            yield return new WaitForSeconds(hit - t);
            t = hit;
            GameSfx.Play(GameSfx.Cue.Chop);
        }
        chopping = null;
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

        return true;
    }
}
