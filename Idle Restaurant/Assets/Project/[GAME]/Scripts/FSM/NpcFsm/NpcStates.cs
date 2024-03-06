public abstract class NpcStates 
{
    public abstract void EnterState(NpcFsm fsm);
    public abstract void UpdateState(NpcFsm fsm);
    public abstract void ExitState(NpcFsm fsm);
}