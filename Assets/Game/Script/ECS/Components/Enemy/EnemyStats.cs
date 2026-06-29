using Unity.Entities;

namespace Game.Script.ECS.Components.Enemy
{
    public struct EnemyStats:IComponentData
    {
        public float health;
        public float damage;

    }
}