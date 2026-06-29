using Unity.Entities;
using Unity.Mathematics;

namespace Game.Script.ECS.Components
{
    /// <summary>
    /// 流场数据，保存流场网格的元数据,全局1份
    /// </summary>
    public struct FlowFieldData : IComponentData
    {
        // 流场网格原点（世界坐标左下角）
        public float2 GridOrigin;
        
        // 每个格子的世界单位大小
        public float CellSize;
        
        // 流场网格尺寸
        public int GridWidth;
        public int GridHeight;
        
        // 流场是否有效（0 = 无效，1 = 有效）
        public byte IsValid;
        
        // 玩家当前位置（世界坐标），用于 fallback 方向
        public float2 PlayerPosition;
    }
}