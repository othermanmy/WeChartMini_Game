using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Coin;
using Game.Script.ECS.Components.Enemy;
using Game.Script.ECS.Components.Pool;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Script.ECS.Systems.Coin
{
    /// <summary>
    /// 金币掉落 
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyPoolSystem))]
    public partial struct CoinDropSystem : ISystem
    {
        private EntityQuery deathEventQuery;
        private EntityQuery coinPoolQuery;

        public void OnCreate(ref SystemState state)
        {
            deathEventQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EnemyDeathEventSingleton>(),
                ComponentType.ReadWrite<EnemyDeathEvent>()
            );
            state.RequireForUpdate(deathEventQuery);

            coinPoolQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CoinPoolSingleton>(),
                ComponentType.ReadWrite<PooledEntity>()
            );

            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        /// <summary>
        /// 初始化数据：金币 count、位置、timer
        /// </summary>
        private struct CoinInitData
        {
            public int Count;
            public float2 Position;
            public float LifeTime;
        }

       
        private struct CoinDropSetupJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> CoinEntities;
            [ReadOnly] public NativeArray<CoinInitData> InitData;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(int index)
            {
                var data = InitData[index];
                var coinEntity = CoinEntities[index];

                Ecb.SetComponent(index, coinEntity, new CoinData
                {
                    count = data.Count,
                    timer = data.LifeTime,
                    isBeingPulled = false,
                });
                Ecb.SetComponent(index, coinEntity,
                    LocalTransform.FromPosition(new float3(data.Position.x, data.Position.y, 0f)));
                Ecb.AddComponent(index, coinEntity, new ActiveTag());
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            if (deathEventQuery.IsEmpty || coinPoolQuery.IsEmpty)
                return;

            var singletonEntity = deathEventQuery.GetSingletonEntity();
            var buffer = state.EntityManager.GetBuffer<EnemyDeathEvent>(singletonEntity);

            int eventCount = buffer.Length;
            if (eventCount == 0)
                return;

            // 获取金币池
            var poolEntity = coinPoolQuery.GetSingletonEntity();
            var poolBuffer = state.EntityManager.GetBuffer<PooledEntity>(poolEntity);
            var poolSingleton = state.EntityManager.GetComponentData<CoinPoolSingleton>(poolEntity);

            var random = Random.CreateFromIndex((uint)(SystemAPI.Time.DeltaTime * 1000000 + 1));

            // 从 ECB System 获取并行安全的 ECB
            var ecbSystem = state.World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var ecb = ecbSystem.CreateCommandBuffer();

            // ---- 阶段 1：为每个死亡事件分配金币实体 + 构建初始化数据 ----
            var coinEntities = new NativeArray<Entity>(eventCount, Allocator.TempJob);
            var initDataArray = new NativeArray<CoinInitData>(eventCount, Allocator.TempJob);

            for (int i = 0; i < eventCount; i++)
            {
                var evt = buffer[i];
                int coinCount = random.NextInt(evt.minCoin, evt.maxCoin + 1);

                Entity coinEntity;

                // 从池中取
                if (poolBuffer.Length > 0)
                {
                    coinEntity = poolBuffer[poolBuffer.Length - 1].Value;
                    poolBuffer.RemoveAt(poolBuffer.Length - 1);
                }
                else if (poolSingleton.templateEntity != Entity.Null)
                {
                    coinEntity = ecb.Instantiate(poolSingleton.templateEntity);
                    ecb.AddComponent(coinEntity, new PoolTag());
                    ecb.AddComponent(coinEntity, new PoolContainerRef { Value = poolEntity });
                }
                else
                {
                    coinEntity = ecb.CreateEntity();
                    ecb.AddComponent(coinEntity, new PoolTag());
                    ecb.AddComponent(coinEntity, new CoinTag());
                    ecb.AddComponent(coinEntity, new PoolContainerRef { Value = poolEntity });
                }

                coinEntities[i] = coinEntity;
                initDataArray[i] = new CoinInitData
                {
                    Count = coinCount,
                    Position = evt.position,
                    LifeTime = poolSingleton.LifeTime,
                };
            }

            buffer.Clear();
            
            // ---- 阶段 2：并行设置金币组件（IJobParallelFor + ECB ParallelWriter） ----
            var setupJob = new CoinDropSetupJob
            {
                CoinEntities = coinEntities,
                InitData = initDataArray,
                Ecb = ecb.AsParallelWriter(),
            };

            var handle = setupJob.Schedule(eventCount, 32, state.Dependency);
            handle.Complete();

            // 清理
            coinEntities.Dispose();
            initDataArray.Dispose();

            // ECB System 自动播放和回收，无需手动 Playback/Dispose
        }
    }
}