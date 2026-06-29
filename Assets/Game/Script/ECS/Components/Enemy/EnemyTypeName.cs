using Unity.Collections;
using Unity.Entities;

namespace Game.Script.ECS.Components.Enemy
{
    /// <summary>
    /// 敌人类型名称组件
    /// </summary>
    public struct EnemyTypeName : IComponentData
    {
        public FixedString64Bytes Value;
    }
}