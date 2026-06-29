using Unity.Entities;
using Unity.Mathematics;

namespace Game.Script.ECS.Components.Enemy
{
    public struct EnemyMoveData : IComponentData
    {
        public float speed;//速度
        public float separationRadius;//分离检测半径
        public float separationWeight;//分离检测权重
        public float hitRadius;//受击半径
        public float2 centerOffset;
    }
}