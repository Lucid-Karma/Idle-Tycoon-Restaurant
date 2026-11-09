using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcGoState : NpcStates
{
    public override void EnterState(NpcFsm fsm)
    {
        fsm.OnNpcWalk.Invoke();
        fsm.OnNpcEatEnd.Invoke();
        fsm.chair.IsEmpty = true;   // !!!
        NpcFsm.SeatFreed?.Invoke(fsm.chair.CalculateSitPos());   // the next customer sets off now (NpcSpawnController)
        fsm.LeaveChair(fsm.ExitPoint, 1f);
    }

    public override void UpdateState(NpcFsm fsm)
    {
        if (fsm.executingNpcState == ExecutingNpcState.GO)
        {
            fsm.Go();
        }
        else   ExitState(fsm);
    }

    public override void ExitState(NpcFsm fsm)
    {
        
    }
}
