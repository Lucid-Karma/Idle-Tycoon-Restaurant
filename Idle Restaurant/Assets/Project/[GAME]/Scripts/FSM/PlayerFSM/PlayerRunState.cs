using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : PlayerStates
{
    public override void EnterState(PlayerFSM fsm)
    {
        fsm.OnPlayerRun.Invoke();
        fsm.ParticleSystem.Play();
    }

    public override void UpdateState(PlayerFSM fsm)
    {
        if (fsm.executingState == ExecutingState.RUN)
        {
            fsm.MovePlayer();
            fsm.DoneWithPath();
        }
        else   ExitState(fsm);
    }

    public override void ExitState(PlayerFSM fsm)
    {
        fsm.ParticleSystem.Stop();
        if(fsm.executingState == ExecutingState.IDLE)
        {
            fsm.SwitchState(fsm.playerIdleState);
        }
    }
}
