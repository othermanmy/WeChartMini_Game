using Game.Script.Architecture;
using UnityEngine;

namespace Game.Script.Model.Ability
{
    public class FastShootAbility:AbilityBase
    {
        private float intervalDes;
        private PlayerModel player;
        public override void Init(AbilityConfig config, GameObject owner)
        {
            base.Init(config, owner);
            intervalDes = GetParameter(nameof(intervalDes), 1f);
            var i = GameApp.Interface;
            player = i.GetModel<PlayerModel>();
        }

        public override void OnAcquire()
        {
            player.shootInterval.Value*=intervalDes;
        }

        public override void OnUpdate(float dt)
        {
        }

        public override void OnRemove()
        {
            player.shootInterval.Value/=intervalDes;
        }
    }
}