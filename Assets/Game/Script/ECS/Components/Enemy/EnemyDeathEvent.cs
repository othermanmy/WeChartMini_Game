using Unity.Entities;
using Unity.Mathematics;

namespace Game.Script.ECS.Components.Enemy
{
    /// <summary>
    /// 敌人死亡事件
    /// </summary>
    public struct EnemyDeathEvent : IBufferElementData
    {
        public float2 position;
        public int minCoin;
        public int maxCoin;
    }

    /// <summary>
    /// 标记挂载 EnemyDeathEvent DynamicBuffer 的单例 Entity
    /// </summary>
    public struct EnemyDeathEventSingleton : IComponentData
    {
    }
}