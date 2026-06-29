using QFramework;

namespace Game.Script.Player
{
    public class PlayerFireState:AbstractState<PlayerFsm.State,PlayerFsm>
    {
        public PlayerFireState(FSM<PlayerFsm.State> fsm, PlayerFsm owner) : base(fsm, owner)
        {
        }
    }
}