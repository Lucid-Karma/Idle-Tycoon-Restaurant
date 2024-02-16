using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractState : PlayerStates
{
    public override void EnterState(PlayerFSM fsm)
    {
        //Debug.Log("INTERACT");
        fsm.OnPlayerInteract.Invoke();
        
        fsm.GetFoodFromSource();
        fsm.GetFood();
        fsm.PlaceFood();
    }

    public override void UpdateState(PlayerFSM fsm)
    {
        if (fsm.executingState == ExecutingState.INTERACT)
        {
            // nothing here, since the next state is going to be determined by an animation event.
        }
        else   ExitState(fsm);
    }

    public override void ExitState(PlayerFSM fsm)
    {
        if(fsm.executingState == ExecutingState.IDLE)
        {
            fsm.SwitchState(fsm.playerIdleState);
        }
    }
}
