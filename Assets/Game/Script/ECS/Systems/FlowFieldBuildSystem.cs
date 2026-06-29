using Unity.Entities;
using Game.Script.ECS.Components;

namespace Game.Script.ECS.Systems
{
    /// <summary>
    /// 流场重建触发系统
    /// 检测 FlowFieldBuildRequest 单例，触发后更新 FlowFieldData 元数据
    /// </summary>
    [UpdateAfter(typeof(SpatialHashBuildSystem))]
    public partial struct FlowFieldBuildSystem : ISystem
    {
        private EntityQuery requestQuery;

        public void OnCreate(ref SystemState state)
        {
            requestQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<FlowFieldBuildRequest>(),
                ComponentType.ReadWrite<FlowFieldData>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            // 检测是否有构建请求
            var requestEntities = requestQuery.ToEntityArray(state.WorldUpdateAllocator);
            if (requestEntities.Length == 0)
                return;

            // 处理第一个请求
            var entity = requestEntities[0];
            var request = state.EntityManager.GetComponentData<FlowFieldBuildRequest>(entity);
            var flowFieldData = state.EntityManager.GetComponentData<FlowFieldData>(entity);

            // 更新 FlowFieldData 元数据（方向数组由 PathFindingManager 管理）
            flowFieldData.PlayerPosition = request.PlayerPosition;
            flowFieldData.IsValid = 1;
            state.EntityManager.SetComponentData(entity, flowFieldData);

            // 移除请求标记
            state.EntityManager.RemoveComponent<FlowFieldBuildRequest>(entity);
        }
    }
}