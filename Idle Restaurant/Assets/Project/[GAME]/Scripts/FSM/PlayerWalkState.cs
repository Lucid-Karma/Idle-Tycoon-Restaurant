using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkState : PlayerStates
{
    public override void EnterState(PlayerFSM fsm)
    {
        
    }

    public override void UpdateState(PlayerFSM fsm)
    {
        if (fsm.executingState == ExecutingState.WALK)
        {
            
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
