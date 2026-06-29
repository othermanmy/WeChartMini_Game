using Unity.Entities;

namespace Game.Script.ECS.Components.Pool
{
    /// <summary>
    /// 池容器引用
    /// </summary>
    public struct PoolContainerRef : IComponentData
    {
        public Entity Value;
    }
}