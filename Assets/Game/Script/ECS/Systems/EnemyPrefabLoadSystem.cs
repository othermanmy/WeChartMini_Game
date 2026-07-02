using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Enemy;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace Game.Script.ECS.Systems
{
 
    [DisableAutoCreation]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnemyPrefabLoadSystem : ISystem
    {
        private EntityQuery initQuery;

        public void OnCreate(ref SystemState state)
        {
            initQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EnemyPrefabRef>(),
                ComponentType.ReadOnly<EnemyTypeName>(),
                ComponentType.Exclude<RequestEntityPrefabLoaded>(),
                ComponentType.Exclude<PrefabLoadResult>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            if (initQuery.IsEmpty)
                return;
            var loaderEcB = new EntityCommandBuffer(Allocator.Temp);
            var entities = initQuery.ToEntityArray(Allocator.Temp);
            var refs = initQuery.ToComponentDataArray<EnemyPrefabRef>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                loaderEcB.AddComponent(entities[i], new RequestEntityPrefabLoaded { Prefab = refs[i].Value });
            }

            loaderEcB.Playback(state.EntityManager);
            loaderEcB.Dispose();
            entities.Dispose();
            refs.Dispose();

            // 加载完成后禁用此 System
            state.Enabled = false;
        }
    }
}