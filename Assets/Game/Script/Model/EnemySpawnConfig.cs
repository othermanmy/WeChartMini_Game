using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Script.Model
{
    /// <summary>
    /// 敌人生成总配置
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "ScriptableObjects/EnemySpawnConfig")]
    public class EnemySpawnConfig : ScriptableObject
    {
        /// <summary>
        /// 地图边缘生成偏移距离
        /// </summary>
        public float mapEdgeSpawnOffset = 1f;

        public float spawnRadius = 10f;

        public List<EnemySpawnWave> spawnWaves;
    }

    [Serializable]
    public class EnemySpawnWave
    {
        [Tooltip("开始时间（分钟）")] [Min(0)]
        public int beginTime;
        /// <summary>
        /// 初始等级
        /// </summary>
        [Tooltip("初始等级")] [Min(1)]
        public int beginLevel;
        /// <summary>
        /// 升级间隔（分钟）
        /// </summary>
        [Tooltip("升级间隔（分钟）")] [Min(1)]
        public int levelUpInterval;
        /// <summary>
        /// 每个间隔生多少级
        /// </summary>
        [Tooltip("每个间隔生多少级")] [Min(0)]
        public int levelUp;
        /// <summary>
        /// 敌人生成间隔
        /// </summary>
        [Tooltip("敌人生成间隔")] [Min(0.5f)]
        public float enemySpawnInterval;
        /// <summary>
        /// 最开始敌人生成的数量
        /// </summary>
        [Tooltip("最开始敌人生成的数量")] [Min(0)]
        public int baseEnemySpawnCount;
        /// <summary>
        /// 每个间隔敌人最多能生成的数量
        /// </summary>
        [Tooltip("每个间隔敌人最多能生成的数量")] [Min(0)]
        public int maxEnemySpawnCount;
        /// <summary>
        /// 敌人间隔生成人数间隔
        /// </summary>
        [Tooltip("敌人间隔生成人数间隔")] [Min(0.5f)]
        public float enemySpawnCountUpInterval;
        /// <summary>
        /// 敌人每个间隔增长的数量
        /// </summary>
        [Tooltip("敌人每个间隔增长的数量")] [Min(0)] 
        public int enemyUpCount;

        public List<EnemySpawnData> enemySpawnDataList;
        
    }
    [Serializable]
    public class EnemySpawnData
    {
        public string enemyKey;
        [Range(0.1f,1f)]
        [Tooltip("敌人生成概率")]
        public float probability;// 敌人生成概率
    }
}