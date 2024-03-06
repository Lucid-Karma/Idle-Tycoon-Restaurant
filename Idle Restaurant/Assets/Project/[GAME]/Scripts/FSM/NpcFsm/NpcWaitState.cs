using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcWaitState : NpcStates
{
    public override void EnterState(NpcFsm fsm)
    {
        fsm.OnNpcSitChairIdle.Invoke();
    }

    public override void UpdateState(NpcFsm fsm)
    {
        if (fsm.executingNpcState == ExecutingNpcState.WAIT)
        {
            fsm.Wait();
        }
        else   ExitState(fsm);
    }

    public override void ExitState(NpcFsm fsm)
    {
        if(fsm.executingNpcState == ExecutingNpcState.EAT)
        {
            fsm.SwitchState(fsm.eatState);
        }
        else if(fsm.executingNpcState == ExecutingNpcState.PROTEST)
        {
            fsm.SwitchState(fsm.protestState);
        }
    }
}
