using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Coin;
using Game.Script.ECS.Components.Pool;
using Game.Script.Manager;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Script.ECS.Systems.Coin
{
    /// <summary>
    /// 金币拾取 ：超时 / 磁吸 / 拾取 / 回池
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct CoinPickupSystem : ISystem
    {
        private EntityQuery coinQuery;
        private EntityQuery poolQuery;

        public void OnCreate(ref SystemState state)
        {
            coinQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<CoinData>(),
                ComponentType.ReadWrite<LocalTransform>(),
                ComponentType.ReadOnly<CoinTag>(),
                ComponentType.ReadOnly<ActiveTag>(),
                ComponentType.ReadOnly<PoolContainerRef>()
            );

            poolQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CoinPoolSingleton>(),
                ComponentType.ReadWrite<PooledEntity>()
            );

            state.RequireForUpdate(coinQuery);
            state.RequireForUpdate(poolQuery);
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        struct CoinPickupJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle EntityType;
            public ComponentTypeHandle<CoinData> CoinDataType;
            public ComponentTypeHandle<LocalTransform> LocalTransformType;

            public EntityCommandBuffer.ParallelWriter Ecb;

            [ReadOnly] public Entity PoolEntity;
            [ReadOnly] public float3 PlayerPos;
            [ReadOnly] public float AbsorbRangeSq;
            [ReadOnly] public float CollectRadiusSq;
            [ReadOnly] public float PullSpeed;
            [ReadOnly] public float DeltaTime;

            public NativeList<int>.ParallelWriter PendingCoinCounts;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(EntityType);
                var coinDatas = chunk.GetNativeArray(ref CoinDataType);
                var transforms = chunk.GetNativeArray(ref LocalTransformType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var coinEnt = entities[i];
                    var data = coinDatas[i];
                    var trans = transforms[i];

                    data.timer -= DeltaTime;

                    // 超时
                    if (data.timer <= 0f)
                    {
                        RecycleCoin(unfilteredChunkIndex, coinEnt);
                        continue;
                    }

                    float3 pos = trans.Position;
                    float distSq = math.distancesq(PlayerPos, pos);

                    // 磁吸范围
                    if (distSq <= AbsorbRangeSq)
                        data.isBeingPulled = true;

                    // 磁吸移动
                    if (data.isBeingPulled)
                    {
                        float3 dir = math.normalize(PlayerPos - pos);
                        trans.Position += dir * PullSpeed * DeltaTime;
                        distSq = math.distancesq(PlayerPos, trans.Position);
                    }

                    // 拾取
                    if (distSq <= CollectRadiusSq)
                    {
                        PendingCoinCounts.AddNoResize(data.count);
                        RecycleCoin(unfilteredChunkIndex, coinEnt);
                        continue;
                    }

                    coinDatas[i] = data;
                    transforms[i] = trans;
                }
            }

            private void RecycleCoin(int sortKey, Entity coinEnt)
            {
                Ecb.SetComponent(sortKey, coinEnt, LocalTransform.FromPosition(new float3(-9999f, -9999f, 0f)));
                Ecb.RemoveComponent<ActiveTag>(sortKey, coinEnt);
                Ecb.SetComponent(sortKey, coinEnt, new CoinData { count = 0, timer = 0f, isBeingPulled = false });
                Ecb.AppendToBuffer(sortKey, PoolEntity, new PooledEntity { Value = coinEnt });
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            float3 playerPos = float3.zero;
            var playerTrans = GameManager.Instance.playerTrans;
            if (playerTrans)
                playerPos = playerTrans.position;

            float absorbRange = CoinPickupBridge.GetAbsorbRange();

            var poolEntity = poolQuery.GetSingletonEntity();
            var poolSingleton = state.EntityManager.GetComponentData<CoinPoolSingleton>(poolEntity);

            var entityTypeHandle = SystemAPI.GetEntityTypeHandle();
            var coinDataTypeHandle = SystemAPI.GetComponentTypeHandle<CoinData>();
            var localTransformTypeHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>();

            // 并行度优化：TypeHandle 更新只需一次
            entityTypeHandle.Update(ref state);
            coinDataTypeHandle.Update(ref state);
            localTransformTypeHandle.Update(ref state);

            // 从 ECB System 获取并行安全的 ECB
            var ecbSystem = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var ecb = ecbSystem.CreateCommandBuffer();

            int estimatedCoinCount = coinQuery.CalculateEntityCount();
            var pendingCoins = new NativeList<int>(
                estimatedCoinCount > 0 ? estimatedCoinCount : 64, Allocator.TempJob);

            var job = new CoinPickupJob
            {
                EntityType = entityTypeHandle,
                CoinDataType = coinDataTypeHandle,
                LocalTransformType = localTransformTypeHandle,
                Ecb = ecb.AsParallelWriter(),
                PoolEntity = poolEntity,
                PlayerPos = playerPos,
                AbsorbRangeSq = absorbRange * absorbRange,
                CollectRadiusSq = poolSingleton.CollectRadius * poolSingleton.CollectRadius,
                PullSpeed = poolSingleton.PullSpeed,
                DeltaTime = deltaTime,
                PendingCoinCounts = pendingCoins.AsParallelWriter(),
            };

            var handle = job.ScheduleParallel(coinQuery, state.Dependency);
            handle.Complete();

            // 主线程处理拾取金币
            for (int i = 0; i < pendingCoins.Length; i++)
                CoinPickupBridge.Enqueue(pendingCoins[i]);

            pendingCoins.Dispose();

            CoinPickupBridge.Flush();

      
        }
    }
}