using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Coin;
using Game.Script.ECS.Components.Pool;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Script.ECS.Systems.Coin
{
    /// <summary>
    /// 金币对象池初始化
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct CoinPoolInitSystem : ISystem
    {
        private const int PrewarmCount = 200;

        private EntityQuery singletonQuery;
        private EntityQuery templateQuery;

        public void OnCreate(ref SystemState state)
        {
            singletonQuery = state.GetEntityQuery(ComponentType.ReadOnly<CoinPoolSingleton>());
            templateQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<CoinTemplate>());
        }

        public void OnUpdate(ref SystemState state)
        {
            // 仅运行一次
            if (!singletonQuery.IsEmpty)
            {
                state.Enabled = false;
                return;
            }

            // 等待模板实体就绪
            if (templateQuery.IsEmpty)
                return;

            var templateEntity = templateQuery.GetSingletonEntity();

            // 从模板读取 SharedComponent 配置
            var config = state.EntityManager.GetSharedComponent<CoinConfig>(templateEntity);

            // 创建 CoinPoolSingleton 单例，缓存配置
            var singletonEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singletonEntity, new CoinPoolSingleton
            {
                templateEntity = templateEntity,
                LifeTime = config.LifeTime,
                CollectRadius = config.CollectRadius,
                PullSpeed = config.PullSpeed,
            });
            state.EntityManager.AddBuffer<PooledEntity>(singletonEntity);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < PrewarmCount; i++)
            {
           
                var entity = ecb.Instantiate(templateEntity);
                ecb.AddComponent(entity, new PoolTag());
                ecb.AddComponent(entity, new PoolContainerRef { Value = singletonEntity });
                ecb.SetComponent(entity, LocalTransform.FromPosition(
                    new float3(-9999f, -9999f, 0f)));
                ecb.AppendToBuffer(singletonEntity, new PooledEntity { Value = entity });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();

#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[CoinPoolInitSystem] 金币池初始化完成，预生成 {PrewarmCount} 个");
#endif

            state.Enabled = false;
        }
    }
}