using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcProtestState : NpcStates
{
    public override void EnterState(NpcFsm fsm)
    {
        EventManager.OnCustomerProtest.Invoke();
    }

    public override void UpdateState(NpcFsm fsm)
    {
        if (fsm.executingNpcState == ExecutingNpcState.PROTEST)
        {
            fsm.Protest();
        }
        else   ExitState(fsm);
    }

    public override void ExitState(NpcFsm fsm)
    {
        if(fsm.executingNpcState == ExecutingNpcState.GO)
        {
            fsm.SwitchState(fsm.goState);
        }
    }
}
