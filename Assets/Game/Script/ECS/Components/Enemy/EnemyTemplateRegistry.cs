using Unity.Entities;
using Unity.Collections;

namespace Game.Script.ECS.Components.Enemy
{
    public struct EnemyTemplateRegistry : IComponentData
    {
        public NativeParallelHashMap<FixedString64Bytes, Entity> TemplateMap;
    }
}