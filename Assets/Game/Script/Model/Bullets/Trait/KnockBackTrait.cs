using Game.Script.Architecture;
using Unity.Entities;

namespace Game.Script.Model.Bullets.Trait
{
    public class KnockBackTrait:IBulletTrait
    {
        
        private PlayerModel playerModel;

        public KnockBackTrait()
        {
            var gf = GameApp.Interface;
            playerModel = gf.GetModel<PlayerModel>();
        }
        
        public void OnSpawn(BulletBase bullet)
        {
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
        }
    }
}