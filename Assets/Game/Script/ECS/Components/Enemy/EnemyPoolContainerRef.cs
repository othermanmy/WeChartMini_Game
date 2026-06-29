using Unity.Entities;

namespace Game.Script.ECS.Components.Enemy
{
    public struct EnemyPoolContainerRef : IComponentData
    {
        public Entity PoolContainerEntity;
    }
}