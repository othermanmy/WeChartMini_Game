using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Enemy;
using Game.Script.Manager;
using Unity.Burst.Intrinsics;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Game.Script.ECS.Systems
{
    /// <summary>
    /// 敌人移动系统（流场寻路 + 分离力避障）
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FlowFieldBuildSystem))]
    public partial struct EnemyMoveSystem : ISystem
    {
        private EntityQuery enemyQuery;
        private ComponentTypeHandle<EnemyMoveData> moveDataTypeHandle;
        private ComponentTypeHandle<LocalTransform> localTransformHandle;
        private ComponentTypeHandle<PhysicsVelocity> physicsVelocityHandle;

        private static PathFindingManager pathFindingManager;

        public static void SetPathFindingManager(PathFindingManager manager)
        {
            pathFindingManager = manager;
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlowFieldData>();
            state.RequireForUpdate<SpatialHashData>();
            enemyQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<EnemyActiveTag>(),
                ComponentType.ReadWrite<EnemyMoveData>(),
                ComponentType.ReadWrite<PhysicsVelocity>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            if (pathFindingManager == null)
                return;

            // 从 FlowFieldData 单例获取玩家位置
            var flowFieldEntity = SystemAPI.GetSingleton<FlowFieldData>();
            float2 playerPos = flowFieldEntity.PlayerPosition;

            // 确保所有前序 Job 完成后再做主线程数据访问，避免依赖链断裂
            state.Dependency.Complete();

            //  主线程预计算流场方向，存入 NativeHashMap 传给 Job
            var enemyEntities = enemyQuery.ToEntityArray(Allocator.TempJob);
            var flowDirectionMap = new NativeHashMap<Entity, float2>(enemyEntities.Length, Allocator.TempJob);
            for (int i = 0; i < enemyEntities.Length; i++)
            {
                var entity = enemyEntities[i];
                if (!state.EntityManager.Exists(entity)) continue;

                var lt = state.EntityManager.GetComponentData<LocalTransform>(entity);
                float2 dir = pathFindingManager.GetFlowDirection(lt.Position, playerPos);
                flowDirectionMap.TryAdd(entity, dir);
            }

            // 获取空间哈希
            var sp = SystemAPI.GetSingleton<SpatialHashData>();

            // 分离力 + 移动
            moveDataTypeHandle = SystemAPI.GetComponentTypeHandle<EnemyMoveData>();
            localTransformHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(true);
            physicsVelocityHandle = SystemAPI.GetComponentTypeHandle<PhysicsVelocity>();
            var entityTypeHandle = SystemAPI.GetEntityTypeHandle();

            var moveJob = new EnemyMoveJob
            {
                FlowDirectionMap = flowDirectionMap.AsReadOnly(),
                HashGrid = sp.HashGrid,
                MoveDataTypeHandle = moveDataTypeHandle,
                LocalTransformHandle = localTransformHandle,
                PhysicsVelocityHandle = physicsVelocityHandle,
                EntityTypeHandle = entityTypeHandle,
                HashCellSize = flowFieldEntity.CellSize,
            };

            state.Dependency = moveJob.ScheduleParallel(enemyQuery, state.Dependency);
            state.Dependency = JobHandle.CombineDependencies(
                state.Dependency,
                flowDirectionMap.Dispose(state.Dependency),
                enemyEntities.Dispose(state.Dependency)
            );
        }

        [BurstCompile]
        public struct EnemyMoveJob : IJobChunk
        {
            [ReadOnly] public float HashCellSize;
            [ReadOnly] public NativeParallelMultiHashMap<int, EnemyHashEntry> HashGrid;
            [ReadOnly] public NativeHashMap<Entity, float2>.ReadOnly FlowDirectionMap;
            public ComponentTypeHandle<EnemyMoveData> MoveDataTypeHandle;
            [ReadOnly] public ComponentTypeHandle<LocalTransform> LocalTransformHandle;
            public ComponentTypeHandle<PhysicsVelocity> PhysicsVelocityHandle;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;

            [BurstCompile]
            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var moveDataArray = chunk.GetNativeArray(ref MoveDataTypeHandle);
                var ltArray = chunk.GetNativeArray(ref LocalTransformHandle);
                var pvArray = chunk.GetNativeArray(ref PhysicsVelocityHandle);
                var entityArray = chunk.GetNativeArray(EntityTypeHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var entity = entityArray[i];
                    var moveData = moveDataArray[i];
                    float2 pos2d = ltArray[i].Position.xy;

                    // 从预计算 Map 中读取流场方向
                    float2 flowDir = float2.zero;
                    if (FlowDirectionMap.TryGetValue(entity, out float2 cachedDir))
                        flowDir = cachedDir;

                    // 计算分离力
                    float2 separationDir = float2.zero;
                    if (HashGrid.IsCreated && moveData.separationWeight > 0f)
                    {
                        int gx = (int)math.floor(pos2d.x / HashCellSize);
                        int gy = (int)math.floor(pos2d.y / HashCellSize);
                        int hash = (int)math.hash(new int2(gx, gy));

                        if (HashGrid.TryGetFirstValue(hash, out var entry, out var iterator))
                        {
                            do
                            {
                                float2 diff = pos2d - entry.Position;
                                float distSq = math.lengthsq(diff);
                                if (distSq < 0.001f) continue; // 跳过自己

                                float sepRadiusSq = moveData.separationRadius * moveData.separationRadius;
                                if (distSq < sepRadiusSq)
                                {
                                    float dist = math.sqrt(distSq);
                                    float strength = (moveData.separationRadius - dist) / moveData.separationRadius;
                                    separationDir += math.normalize(diff) * strength;
                                }
                            }
                            while (HashGrid.TryGetNextValue(out entry, ref iterator));
                        }
                    }

                    // 合并方向
                    float2 moveDir = flowDir;
                    float sepLen = math.length(separationDir);
                    if (sepLen > 0.001f)
                    {
                        float2 sepN = separationDir / sepLen;
                        float dot = math.dot(flowDir, sepN);

                        if (dot < 0.707f) // 分离力与流场方向夹角 > 45°
                        {
                            // 只取垂直于流场方向的分量
                            float2 perp = sepN - flowDir * dot;
                            perp = math.normalize(perp);
                            moveDir = math.normalize(flowDir + perp * moveData.separationWeight);
                        }
                        else
                        {
                            // 直接加权合并
                            moveDir = math.normalize(flowDir + sepN * sepLen * moveData.separationWeight);
                        }
                    }

                    // 更新速度
                    pvArray[i] = new PhysicsVelocity
                    {
                        Linear = new float3(moveDir.x * moveData.speed, moveDir.y * moveData.speed, 0f),
                        Angular = float3.zero
                    };
                    moveDataArray[i] = moveData;
                }
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}