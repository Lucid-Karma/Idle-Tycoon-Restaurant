public abstract class PlayerStates 
{
    public abstract void EnterState(PlayerFSM fsm);
    public abstract void UpdateState(PlayerFSM fsm);
    public abstract void ExitState(PlayerFSM fsm);
}
