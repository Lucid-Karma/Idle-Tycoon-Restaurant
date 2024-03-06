using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcComeState : NpcStates
{
    public override void EnterState(NpcFsm fsm)
    {
        fsm.OnNpcWalk.Invoke();
        fsm.MoveNpc();
    }

    public override void UpdateState(NpcFsm fsm)
    {
        if (fsm.executingNpcState == ExecutingNpcState.COME)
        {
            fsm.DoneWithPath();
        }
        else   ExitState(fsm);
    }

    public override void ExitState(NpcFsm fsm)
    {
        if(fsm.executingNpcState == ExecutingNpcState.ORDER)
        {
            fsm.SwitchState(fsm.orderState);
        }
    }
}
