using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Pool;
using Game.Script.ECS.Components.Coin;
using Game.Script.ECS.Components.Enemy;
using Unity.Mathematics;

namespace Game.Script.ECS.Systems
{
    /// <summary>
    /// 敌人对象池系统 — 处理死亡/距离剔除 + DeathEvent 发送
    /// 回收由 PoolRecycleSystem 统一处理
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct EnemyPoolSystem : ISystem
    {
        private EntityQuery activeEnemyQuery;
        private EntityQuery deathEventQuery;

        public void OnCreate(ref SystemState state)
        {
            activeEnemyQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EnemyActiveTag>(),
                ComponentType.ReadWrite<EnemyStats>(),
                ComponentType.ReadOnly<PoolContainerRef>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            deathEventQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EnemyDeathEventSingleton>(),
                ComponentType.ReadWrite<EnemyDeathEvent>()
            );

            // 确保 EnemyDeathEvent 单例存在
            var singleton = state.EntityManager.CreateSingleton<EnemyDeathEventSingleton>(
                "EnemyDeathEventSingleton");
            state.EntityManager.AddBuffer<EnemyDeathEvent>(singleton);
        }

        public void OnUpdate(ref SystemState state)
        {
            if (activeEnemyQuery.IsEmpty)
                return;

            var playerPos = float2.zero;
            var flowFieldValid = false;
            if (SystemAPI.TryGetSingleton<FlowFieldData>(out var flowData))
            {
                playerPos = flowData.PlayerPosition;
                flowFieldValid = flowData.IsValid == 1;
            }

            const float defaultCullDist = 500f;
            const float cullMultiplier = 1.5f;
            float cullDistanceSq = defaultCullDist * defaultCullDist;
            if (flowFieldValid && SystemAPI.TryGetSingleton<FlowFieldData>(out var ffData))
            {
                float mapDiagonal = math.sqrt(
                    ffData.GridWidth * ffData.GridWidth +
                    ffData.GridHeight * ffData.GridHeight) * ffData.CellSize;
                cullDistanceSq = mapDiagonal * mapDiagonal * cullMultiplier * cullMultiplier;
            }

            state.Dependency.Complete();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            var entities = activeEnemyQuery.ToEntityArray(Allocator.Temp);
            var statsArray = activeEnemyQuery.ToComponentDataArray<EnemyStats>(Allocator.Temp);
            var transforms = activeEnemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            Entity deathEventSingleton = Entity.Null;
            if (!deathEventQuery.IsEmpty)
                deathEventSingleton = deathEventQuery.GetSingletonEntity();

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (!state.EntityManager.Exists(entity))
                    continue;

                bool isDead = statsArray[i].health <= 0f;
                bool shouldRecycle = isDead;

                if (!shouldRecycle)
                {
                    var pos = transforms[i].Position;
                    var distSq = math.distancesq(pos.xy, playerPos);
                    if (distSq > cullDistanceSq)
                        shouldRecycle = true;
                }

                if (!shouldRecycle)
                    continue;

                // 死亡时发送 DeathEvent
                if (isDead && deathEventSingleton != Entity.Null)
                {
                    int minCoin = 0, maxCoin = 0;
                    if (state.EntityManager.HasComponent<EnemyCoinFall>(entity))
                    {
                        var coinFall = state.EntityManager.GetComponentData<EnemyCoinFall>(entity);
                        minCoin = coinFall.minCoin;
                        maxCoin = coinFall.maxCoin;
                    }

                    ecb.AppendToBuffer(deathEventSingleton, new EnemyDeathEvent
                    {
                        position = transforms[i].Position.xy,
                        minCoin = minCoin,
                        maxCoin = maxCoin,
                    });
                }

                // 移除 EnemyActiveTag → PoolRecycleSystem 自动回池
                ecb.RemoveComponent<EnemyActiveTag>(entity);
            }

            entities.Dispose();
            statsArray.Dispose();
            transforms.Dispose();

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}