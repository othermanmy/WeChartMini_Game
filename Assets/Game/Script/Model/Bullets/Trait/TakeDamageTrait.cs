using Cysharp.Threading.Tasks;
using Game.Script.Architecture;
using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Anim;
using Game.Script.ECS.Components.Enemy;
using Game.Script.Ui;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Script.Model.Bullets.Trait
{
    public class TakeDamageTrait:IBulletTrait
    {
        public int Priority => 0;

        private readonly PlayerModel playerModel;
        
        public TakeDamageTrait()
        {
            var arch = GameApp.Interface;
            playerModel = arch.GetModel<PlayerModel>();
        }
        
        public void Init()
        {
            
        }

        public void OnSpawn(BulletBase bullet)
        {
            bullet.currentDamage.Value = playerModel.currentDamage.Value;
        }

        public bool OnHit(BulletBase bulletBase, Entity hitEntity, EntityManager entityManager)
        {
            if (!entityManager.HasComponent<EnemyStats>(hitEntity)) return false;
            var enemyStats = entityManager.GetComponentData<EnemyStats>(hitEntity);
            var enemyPos = entityManager.GetComponentData<LocalTransform>(hitEntity);
            enemyStats.health -= bulletBase.currentDamage.Value;
           // Debug.Log($"子弹伤害:{bulletBase.currentDamage.Value},敌人剩余血量:{enemyStats.health}");
            entityManager.SetComponentData(hitEntity, enemyStats);
            if(entityManager.HasComponent<AnimState>(hitEntity) && enemyStats.health > 0)
                entityManager.SetComponentData(hitEntity,
                    new AnimState
                    {
                        State = "Hurt"
                    });
            ShowDamage(bulletBase.currentDamage.Value, enemyPos.Position.xy).Forget();
            return false;
        }

        private async UniTask ShowDamage(float damage, Vector2 Pos)
        {
           await DamageDumpPanel.DumpAsync(Pos, damage, Color.white);
        }

        public void OnUpdate(BulletBase bulletBase, float dt)
        {
         
        }

        public void OnRelease(BulletBase bulletBase)
        {
          
        }
    }
}