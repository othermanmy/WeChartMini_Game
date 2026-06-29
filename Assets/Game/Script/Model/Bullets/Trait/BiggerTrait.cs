using Game.Script.Architecture;
using Unity.Entities;
using UnityEngine;

namespace Game.Script.Model.Bullets.Trait
{
    public class BiggerTrait:IBulletTrait
    {
        private PlayerModel model;
        public int Priority => 100;

        public BiggerTrait()
        {
            var arch = GameApp.Interface;
            model = arch.GetModel<PlayerModel>();
        }

        public void OnSpawn(BulletBase bullet)
        {
            var newScale=new Vector3(model.hitRadiusPercent.Value,model.hitRadiusPercent.Value,1f);
            bullet.trans.localScale = newScale;
            var scaleFactor = model.hitRadiusPercent.Value;
            bullet.SetTrailWidth(bullet.OriginalTrailWidth * scaleFactor);
        }

        public bool OnHit(BulletBase bulletBase, Entity hitEntity, EntityManager entityManager)
        {
            return false;
        }

        public void OnUpdate(BulletBase bulletBase, float dt)
        {
        }

        public void OnRelease(BulletBase bulletBase)
        {    
            bulletBase.trans.localScale=Vector3.one;
            bulletBase.SetTrailWidth(bulletBase.OriginalTrailWidth);
        }
    }
}