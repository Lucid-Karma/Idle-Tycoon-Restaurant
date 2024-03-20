using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerStates
{
    public override void EnterState(PlayerFSM fsm)
    {
        //Debug.Log("IDLE");
        fsm.OnPlayerIdle.Invoke();
        
        fsm.GetFoodFromSource();
        fsm.GetFood();
        fsm.PlaceFood();
        fsm.TrashEdible();
    }

    public override void UpdateState(PlayerFSM fsm)
    {
        if (fsm.executingState == ExecutingState.IDLE)
        {
            fsm.MovePlayer();
            
        }
        else   ExitState(fsm);
    }

    public override void ExitState(PlayerFSM fsm)
    {
        if(fsm.executingState == ExecutingState.RUN)
        {
            fsm.SwitchState(fsm.playerRunState);
        }
    }
}
