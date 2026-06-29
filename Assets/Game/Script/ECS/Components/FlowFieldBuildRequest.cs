using Unity.Entities;
using Unity.Mathematics;

namespace Game.Script.ECS.Components
{
    /// <summary>
    /// 标记组件：请求重建流场
    /// PathFindingManager 会添加此组件到某个 Entity，FlowFieldBuildSystem 检测到后执行 BFS 重建
    /// </summary>
    public struct FlowFieldBuildRequest : IComponentData
    {
        // 玩家当前世界坐标（流场目标点）
        public float2 PlayerPosition;
        
        // 是否强制重建
        public byte ForceRebuild;
    }
}