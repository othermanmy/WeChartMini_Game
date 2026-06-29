using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Coin;
using Game.Script.ECS.Components.Enemy;
using Unity.Entities;
using UnityEngine;

namespace Game.Script.Model.Authoring
{
    public class EnemyAuthoring:CharacterAuthoring
    {
        public EnemyInitData data=new();
        [SerializeField] private bool drawCollider;
        private class EBaker:Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,new EnemyMoveData
                {
                    speed = authoring.data.baseSpeed,
                    separationRadius = authoring.data.separationRadius,
                    separationWeight = authoring.data.separationWeight,
                    hitRadius = authoring.data.hitRadius,
                    centerOffset = authoring.data.centerOffset,
                });
                AddComponent(entity,new EnemyStats());
                AddComponent(entity,new EnemyCoinFall());
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawCollider) return;
            var oldGizmos = Gizmos.color;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + new Vector3(data.centerOffset.x, 0, data.centerOffset.y), data.hitRadius);
            Gizmos.color = oldGizmos;
            
        }
    }
}