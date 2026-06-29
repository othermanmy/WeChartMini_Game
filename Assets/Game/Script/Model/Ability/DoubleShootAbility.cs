using Cysharp.Threading.Tasks;
using Game.Script.Architecture;
using Game.Script.Event;
using Game.Script.Manager;
using QFramework;
using UnityEngine;

namespace Game.Script.Model.Ability
{
    public class DoubleShootAbility:AbilityBase
    {
        private BulletManager bulletManager;

        public override void Init(AbilityConfig config, GameObject owner)
        {
            base.Init(config, owner);
            var i = GameApp.Interface;
            bulletManager=i.GetSystem<BulletManager>();
        }

        public override void OnAcquire()
        {
            TypeEventSystem.Global.Register<OnPlayerShootEvent>(FollowBullet);
        }

        public override void OnUpdate(float dt)
        {
        }

        public override void OnRemove()
        {
           TypeEventSystem.Global.UnRegister<OnPlayerShootEvent>(FollowBullet);
        }

        private void FollowBullet(OnPlayerShootEvent e)
        {
            //延迟O.5秒发射
            ShootAsync(e).Forget();
        }

        private async UniTask ShootAsync(OnPlayerShootEvent e)
        {
            await UniTask.Delay(500); // 延迟0.5秒
            bulletManager.FireBullet(e.pos, e.dir);
        }
    }
}