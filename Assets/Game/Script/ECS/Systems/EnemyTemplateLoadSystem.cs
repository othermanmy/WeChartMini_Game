using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Enemy;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace Game.Script.ECS.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(EnemyPrefabLoadSystem))]
    public partial struct EnemyTemplateLoadSystem : ISystem
    {
        private EntityQuery loadedQuery;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EnemyTemplateRegistry>();
            // 创建单例 EnemyTemplateRegistry
            var singleton = state.EntityManager.CreateSingleton<EnemyTemplateRegistry>();
            state.EntityManager.SetComponentData(singleton, new EnemyTemplateRegistry
            {
                TemplateMap = new NativeParallelHashMap<FixedString64Bytes, Entity>(32, Allocator.Persistent)
            });

            // 查询已加载完成的 Prefab
            loadedQuery = state.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PrefabLoadResult>(),
                ComponentType.ReadOnly<EnemyPrefabRef>(),
                ComponentType.ReadOnly<EnemyTypeName>()
            );
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<EnemyTemplateRegistry>(out var registry))
            {
                registry.TemplateMap.Dispose();
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (loadedQuery.IsEmpty)
                return;

            var registry = SystemAPI.GetSingleton<EnemyTemplateRegistry>();
            var map = registry.TemplateMap;

            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entities = loadedQuery.ToEntityArray(Allocator.Temp);
            var results = loadedQuery.ToComponentDataArray<PrefabLoadResult>(Allocator.Temp);
            var typeNames = loadedQuery.ToComponentDataArray<EnemyTypeName>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                var prefabRoot = results[i].PrefabRoot;
                if (prefabRoot == Entity.Null)
                    continue;

                var key = typeNames[i].Value;

                if (!map.ContainsKey(key))
                {
                    var templateCopy = state.EntityManager.Instantiate(prefabRoot);
                    map.Add(key, templateCopy);

                    // 加载完成，移除 PrefabLoadResult 和 RequestEntityPrefabLoaded
                    ecb.RemoveComponent<PrefabLoadResult>(entities[i]);
                    ecb.RemoveComponent<RequestEntityPrefabLoaded>(entities[i]);

                    UnityEngine.Debug.Log($"[EnemyTemplateLoadSystem] Template registered: {key}");
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            entities.Dispose();
            results.Dispose();
            typeNames.Dispose();

            SystemAPI.SetSingleton(registry);

            // 全部加载完成后禁用
            if (map.Count() == typeNames.Length && typeNames.Length > 0)
                state.Enabled = false;
        }
    }
}