using Unity.Entities;

namespace Game.Script.ECS.Components
{
    /// <summary>
    /// 对象池中的实体元素
    /// </summary>
    public struct PooledEntity : IBufferElementData
    {
        //池中休眠的实体
        public Entity Value;
    }
}