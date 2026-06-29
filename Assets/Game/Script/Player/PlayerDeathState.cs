using Cysharp.Threading.Tasks;
using Game.Script.Manager;
using Game.Script.Ui;
using QFramework;

namespace Game.Script.Player
{
    public class PlayerDeathState: AbstractState<PlayerFsm.State,PlayerFsm>
    {
        public PlayerDeathState(FSM<PlayerFsm.State> fsm, PlayerFsm owner) : base(fsm, owner)
        {
        }


        protected override void OnEnter()
        {
            UiManager.Instance.OpenAsync(nameof(GameOverPanel),nameof(GameOverPanel)).Forget();
        }
    }
}