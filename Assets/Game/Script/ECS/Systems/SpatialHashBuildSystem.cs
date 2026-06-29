using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Enemy;
using Unity.Transforms;

namespace Game.Script.ECS.Systems
{
    /// <summary>
    /// 每帧重建空间哈希，将敌人坐标映射到空间网格
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct SpatialHashBuildSystem : ISystem
    {
        private EntityQuery enemyQuery;
        private Entity spatialHashEntity;
        private NativeParallelMultiHashMap<int, EnemyHashEntry> hashGrid;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlowFieldData>();
            enemyQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EnemyActiveTag>(),
                ComponentType.ReadOnly<EnemyMoveData>()
            );

            // 初始化空间哈希元数据实体
            spatialHashEntity = state.EntityManager.CreateEntity();
            hashGrid = new NativeParallelMultiHashMap<int, EnemyHashEntry>(0, Allocator.Persistent);
            state.EntityManager.AddComponentData(spatialHashEntity, new SpatialHashData
            {
                CellSize = 1f,
                GridWidth = 0,
                GridHeight = 0,
                GridOrigin = float2.zero,
                HashGrid = hashGrid,
                IsCreated = true
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var enemyEntities = enemyQuery.ToEntityArray(Allocator.TempJob);
            var enemyPositions = new NativeArray<float3>(enemyEntities.Length, Allocator.TempJob);
            var enemyRadii = new NativeArray<float>(enemyEntities.Length, Allocator.TempJob);

            // 收集所有敌人位置，碰撞中心 = transform.position + centerOffset
            for (int i = 0; i < enemyEntities.Length; i++)
            {
                var entity = enemyEntities[i];
                var moveData = state.EntityManager.
                    GetComponentData<EnemyMoveData>(entity);
                var worldPos = state.EntityManager.
                    GetComponentData<LocalTransform>(entity).Position;
                enemyPositions[i] = new float3(
                    worldPos.x + moveData.centerOffset.x,
                    worldPos.y + moveData.centerOffset.y,
                    worldPos.z);
                enemyRadii[i] = moveData.hitRadius;
            }

            // 构建空间哈希
            float cellSize=SystemAPI.GetSingleton<FlowFieldData>().CellSize;
            int gridCellCount = math.max(64,math.ceilpow2(enemyEntities.Length * 18)); // 预分配
            hashGrid.Clear();

            if (hashGrid.Capacity < gridCellCount)
            {
                hashGrid.Dispose();
                hashGrid = new NativeParallelMultiHashMap<int, EnemyHashEntry>(gridCellCount, Allocator.Persistent);
            }

            var parallelWriter = hashGrid.AsParallelWriter();

            var buildJob = new BuildSpatialHashJob
            {
                EnemyEntities = enemyEntities,
                EnemyPositions = enemyPositions,
                CellSize = cellSize,
                EnemyRadii = enemyRadii,
                HashGridWriter = parallelWriter
            };

            var jobHandle = buildJob.Schedule(enemyEntities.Length, 64, state.Dependency);
            jobHandle.Complete();

            // 写入静态容器供 EnemyMoveSystem 读取
            var sp = SystemAPI.GetComponentRW<SpatialHashData>(spatialHashEntity);
            if(sp.IsValid) sp.ValueRW.HashGrid = hashGrid;
            enemyEntities.Dispose();
            enemyPositions.Dispose();
            enemyRadii.Dispose();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (hashGrid.IsCreated)
                hashGrid.Dispose();

            var sp=SystemAPI.GetComponentRW<SpatialHashData>(spatialHashEntity);
            if (sp.IsValid) sp.ValueRW.IsCreated = false;
        }

        [BurstCompile]
        public struct BuildSpatialHashJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> EnemyEntities;
            [ReadOnly] public NativeArray<float3> EnemyPositions;
            [ReadOnly] public NativeArray<float> EnemyRadii;
            [ReadOnly] public float CellSize;
            public NativeParallelMultiHashMap<int, EnemyHashEntry>.ParallelWriter HashGridWriter;

            [BurstCompile]
            public void Execute(int index)
            {
                var pos = EnemyPositions[index];
                // 计算所在网格坐标
                int gx = (int)math.floor(pos.x / CellSize);
                int gy = (int)math.floor(pos.y / CellSize);

                // 将当前敌人写入所在格子及相邻格子
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int hash = HashCell(gx + dx, gy + dy);
                        HashGridWriter.Add(hash, new EnemyHashEntry
                        {
                            Entity = EnemyEntities[index],
                            Position = pos.xy,
                            Radius = EnemyRadii[index]
                        });
                    }
                }
            }

            [BurstCompile]
            private static int HashCell(int gx, int gy)
            {
                return (int)math.hash(new int2(gx, gy));
            }
        }
    }
}