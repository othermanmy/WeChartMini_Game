using System;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Game.Script.Model
{
    [Serializable]
    public class EnemyInitData
    {
        public float baseSpeed;
        public float separationRadius;//分离半径
        public float separationWeight;//分离权重
        public float hitRadius;//受击半径
        public float2 centerOffset;//碰撞中心偏移
    }
}