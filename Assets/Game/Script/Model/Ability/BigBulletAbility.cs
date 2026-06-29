using Game.Script.Architecture;
using Game.Script.Manager;
using Game.Script.Model.Bullets.Trait;
using UnityEngine;

namespace Game.Script.Model.Ability
{
    public class BigBulletAbility:AbilityBase
    {
        private float addSize;
        private PlayerModel playerModel;
        private BulletManager bulletManager;
        
        public override void Init(AbilityConfig config, GameObject owner)
        {
            base.Init(config, owner);
            addSize=GetParameter(nameof(addSize),1f);
            var i = GameApp.Interface;
            bulletManager = i.GetSystem<BulletManager>();
            playerModel = i.GetModel<PlayerModel>();
        }


        public override void OnAcquire()
        {
            playerModel.bulletHitRadiusPercent.Value += addSize;
            bulletManager.AddTrait(new BiggerTrait());
        }

        public override void OnUpdate(float dt)
        {
        }

        public override void OnRemove()
        {
            playerModel.bulletHitRadiusPercent.Value-=addSize;
          bulletManager.RemoveTrait<BiggerTrait>();
        }
    }
}