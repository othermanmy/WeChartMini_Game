using QFramework;

namespace Game.Script.Player
{
    public class PlayerIdleState:AbstractState<PlayerFsm.State,PlayerFsm>
    {
        public PlayerIdleState(FSM<PlayerFsm.State> fsm, PlayerFsm owner) : base(fsm, owner)
        {
            
        }
    }
}