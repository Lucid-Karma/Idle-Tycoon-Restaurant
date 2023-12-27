using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkState : PlayerStates
{
    public override void EnterState(PlayerFSM fsm)
    {
        //Debug.Log("WALK");
    }

    public override void UpdateState(PlayerFSM fsm)
    {
        if (fsm.executingState == ExecutingState.WALK)
        {
            fsm.MovePlayer();
            fsm.DoneWithPath();
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
