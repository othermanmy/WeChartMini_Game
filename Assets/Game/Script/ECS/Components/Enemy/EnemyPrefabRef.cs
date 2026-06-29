using Unity.Entities;
using Unity.Entities.Serialization;

namespace Game.Script.ECS.Components.Enemy
{
    public struct EnemyPrefabRef : IComponentData
    {
        public EntityPrefabReference Value;
    }
}