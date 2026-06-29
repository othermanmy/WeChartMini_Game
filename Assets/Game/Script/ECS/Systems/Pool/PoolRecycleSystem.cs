using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Coin;
using Game.Script.ECS.Components.Enemy;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Script.ECS.Components.Pool;
using Unity.Mathematics;

namespace Game.Script.ECS.Systems.Pool
{
    /// <summary>
    /// 通用对象池回收 System
  
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct PoolRecycleSystem : ISystem
    {
        private EntityQuery genericRecycleQuery;
        private EntityQuery enemyRecycleQuery;

        public void OnCreate(ref SystemState state)
        {
            genericRecycleQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PoolTag>(),
                ComponentType.ReadOnly<PoolContainerRef>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<ActiveTag>(),
                ComponentType.Exclude<EnemyActiveTag>(),
                ComponentType.Exclude<CoinTag>()
                );

            enemyRecycleQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PoolTag>(),
                ComponentType.ReadOnly<PoolContainerRef>(),
                ComponentType.ReadOnly<LocalTransform>(),
                ComponentType.Exclude<EnemyActiveTag>(),
                ComponentType.Exclude<ActiveTag>(),
                ComponentType.Exclude<CoinTag>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            Recycle(ref state, genericRecycleQuery);
            Recycle(ref state, enemyRecycleQuery);
        }

        private void Recycle(ref SystemState state, EntityQuery query)
        {
            if (query.IsEmpty)
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entities = query.ToEntityArray(Allocator.Temp);
            var containerRefs = query.ToComponentDataArray<PoolContainerRef>(Allocator.Temp);
            var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var containerRef = containerRefs[i];

                if (!state.EntityManager.HasBuffer<PooledEntity>(containerRef.Value))
                    continue;

                if (math.all(transforms[i].Position == new float3(-9999f, -9999f, 0f)))
                {
                    ecb.AppendToBuffer(containerRef.Value, new PooledEntity { Value = entity });
                    continue;
                }

                ecb.SetComponent(entity, LocalTransform.FromPosition(new float3(-9999f, -9999f, 0f)));
                ecb.AppendToBuffer(containerRef.Value, new PooledEntity { Value = entity });
            }

            entities.Dispose();
            containerRefs.Dispose();
            transforms.Dispose();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}