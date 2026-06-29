using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Script.ECS.Components
{
    /// <summary>
    /// 空间哈希条目：存储一个敌人的 Entity 和位置
    /// </summary>
    public struct EnemyHashEntry
    {
        public Entity Entity;
        public float2 Position;
        public float Radius;
    }

    /// <summary>
    /// 空间哈希元数据,全局一份
    /// </summary>
    public struct SpatialHashData : IComponentData,IDisposable
    {
        public float CellSize;
        public int GridWidth;
        public int GridHeight;
        public float2 GridOrigin;
        public NativeParallelMultiHashMap<int, EnemyHashEntry> HashGrid;
        public bool IsCreated;
        public void Dispose()
        {
            if (!IsCreated) return;
            HashGrid.Dispose();
            IsCreated = false;
        }
    }
}