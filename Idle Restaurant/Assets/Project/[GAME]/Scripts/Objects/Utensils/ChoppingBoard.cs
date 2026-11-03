using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoppingBoard : NonStackBase
{
    Animator animator;
    Animator Animator { get { return (animator == null)? animator = GetComponent<Animator>(): animator; }}

    CuttableBase foodToCut;

    // The knife was static when the scene's lighting was baked, so its shadow is painted into the board's lightmap:
    // when the knife came up to chop, a dark knife stayed on the board (user, 2026-10-27). The board and the knife
    // leave the lightmap here and are lit like anything that moves (light probes and the realtime sun), so the only
    // knife shadow is the real one, and it follows the knife. (Rebaking would relight the whole cafe.)
    void Awake()
    {
        foreach (var r in GetComponentsInChildren<MeshRenderer>(true)) r.lightmapIndex = -1;
    }

    public override void UseFood(EdibleBase ingredient)
    {
        foodToCut = ingredient.gameObject.GetComponent<CuttableBase>();
        if(!IsSuitable(ingredient)) return;
        base.UseFood(ingredient);

        // (Its collider stays on while it is chopped: a tap on it then means "take it when it's done" - the chef waits
        // by the board, PlayerFSM.WaitForReady. Switched off, a queued tap on it was taken for bare floor.)
        if(!foodToCut.isSliced)
        {
            Animator.ResetTrigger("Idle");
            Animator.SetTrigger("Chop");
            if (chopping != null) StopCoroutine(chopping);
            chopping = StartCoroutine(ChopSounds());
        }
    }

    // The knife comes down four times in the Chop clip (ChoppingBoardAnim: 0.5, 0.83, 1.17, 1.5 s): a chop sound and
    // a few small bits of the ingredient jumping off with each (KitchenFx.Chop; they used to burst after the chopping).
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
            KitchenFx.Chop(foodToCut);
        }
        // The clip's own event slices it (ChopFood). Two in a row (the chef chops everything he carries, PlayerFSM),
        // the second "Chop" could land while the animator was still going back to idle and never play: the tomato
        // stayed whole on the board and the burger went out without it. So it is sliced here if the event never came.
        yield return new WaitForSeconds(0.45f);
        if (foodToCut != null && !foodToCut.isSliced && (Object)Food == foodToCut) ChopFood();
        chopping = null;
    }

    public void ChopFood()
    {
        Animator.SetTrigger("Idle");

        if(foodToCut != null && !foodToCut.isSliced)   // (once: the fallback in ChopSounds may have done it)
        {
            foodToCut.SetSliced();
        }
    }

    public override bool IsSuitable(EdibleBase ingredient)
    {
        if(ingredient != foodToCut)    return false;

        return true;
    }
}
