using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcReactState : NpcStates
{
    public override void EnterState(NpcFsm fsm)
    {
        
    }

    public override void UpdateState(NpcFsm fsm)
    {
        if (fsm.executingNpcState == ExecutingNpcState.REACT)
        {
            fsm.React();
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
