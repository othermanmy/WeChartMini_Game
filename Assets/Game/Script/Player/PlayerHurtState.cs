using Game.Script.Architecture;
using Game.Script.Model;
using QFramework;
using UnityEngine;

namespace Game.Script.Player
{
    public class PlayerHurtState:AbstractState<PlayerFsm.State,PlayerFsm>
    {
        private static readonly int Hurt = Animator.StringToHash("Hurt");

        private PlayerModel model;
        private Animator animator;


        private float currentHurt;
        public PlayerHurtState(FSM<PlayerFsm.State> fsm, PlayerFsm owner) : base(fsm, owner)
        {
            var gameApp = GameApp.Interface;
            model = gameApp.GetModel<PlayerModel>();
            animator = owner.GetComponent<Animator>();
        }

        protected override void OnEnter()
        {
           animator.SetBool(Hurt,true);
           model.isInvincible.Value = true;
           model.canFire.Value = false;
           currentHurt = 0;
           if(model.hp.Value<=0)
               mFSM.ChangeState(PlayerFsm.State.PlayerDeathState);
        }

        protected override void OnUpdate()
        {
            currentHurt += Time.deltaTime;
            if(currentHurt>=model.hurtInvincibleTime.Value)
            {
               mFSM.ChangeState(PlayerFsm.State.PlayerIdleState);
            }
        }

        protected override void OnExit()
        {
            model.isInvincible.Value = false;
            model.canFire.Value = true;
            animator.SetBool(Hurt,false);
        }
    }
}