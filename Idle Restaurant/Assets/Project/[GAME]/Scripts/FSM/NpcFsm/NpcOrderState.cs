using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcOrderState : NpcStates
{
    public override void EnterState(NpcFsm fsm)
    {
        
    }

    public override void UpdateState(NpcFsm fsm)
    {
        if (fsm.executingNpcState == ExecutingNpcState.ORDER)
        {
            fsm.Order();
        }
        else   ExitState(fsm);
    }

    public override void ExitState(NpcFsm fsm)
    {
        if(fsm.executingNpcState == ExecutingNpcState.WAIT)
        {
            fsm.SwitchState(fsm.waitState);
        }
    }
}
