using Unity.Entities;

namespace Game.Script.Model.Bullets.Trait
{
    public class CollideDestroyTrait:IBulletTrait
    {
        public int Priority => -1;
        

        public void OnSpawn(BulletBase bullet)
        {
            
        }

        public bool OnHit(BulletBase bulletBase, Entity hitEntity, EntityManager entityManager)
        {

            return true;
        }
        
        public void OnUpdate(BulletBase bulletBase, float dt)
        {
        }

        public void OnRelease(BulletBase bulletBase)
        {
        }
    }
}