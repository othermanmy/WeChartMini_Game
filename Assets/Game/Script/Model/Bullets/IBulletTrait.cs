using UnityEngine;
using  Unity.Entities;

namespace Game.Script.Model.Bullets
{
    public interface IBulletTrait
    {
        public  int Priority => 0;//特性优先级，数值越大优先级越高
        public void OnSpawn(BulletBase bullet);
        /// <summary>
        /// true销毁，false存活
        /// </summary>
        public bool OnHit(BulletBase bulletBase, Entity hitEntity,EntityManager entityManager);
        public void OnUpdate(BulletBase bulletBase, float dt);
        
        public void OnRelease(BulletBase bulletBase);
    }
}