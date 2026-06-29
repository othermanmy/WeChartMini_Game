using QFramework;
using UnityEngine;

namespace Game.Script.Player
{
    public class PlayerFsm:MonoBehaviour
    {
        public enum State
        {
            PlayerIdleState,
            PlayerHurtState,
            PlayerDeathState,
            PlayerFireState,
        }
        
        public FSM<State> fsm=new();


        public void ChangeState(State state)
        {
            fsm.ChangeState(state);
        }
        
        private void Start()
        {
            fsm.AddState(State.PlayerIdleState,new PlayerIdleState(fsm,this));
            fsm.AddState(State.PlayerHurtState,new PlayerHurtState(fsm,this));
            fsm.AddState(State.PlayerDeathState,new PlayerDeathState(fsm,this));
            fsm.AddState(State.PlayerFireState,new PlayerFireState(fsm,this));
            fsm.StartState(State.PlayerIdleState);
        }


        private void Update()
        {
            fsm.Update();
        }

        private void FixedUpdate()
        {
            fsm.FixedUpdate();
        }
    }
}