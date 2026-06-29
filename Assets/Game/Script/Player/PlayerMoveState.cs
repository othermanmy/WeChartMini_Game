using QFramework;

namespace Game.Script.Player
{
    public class PlayerMoveState : AbstractState<PlayerFsm.State, PlayerFsm>
    {
        public PlayerMoveState(FSM<PlayerFsm.State> fsm, PlayerFsm owner) : base(fsm, owner)
        {

        }
    }
}