using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Script.Model
{
    [CreateAssetMenu(fileName = "MapConfig", menuName = "ScriptableObjects/MapConfig")]
    public class MapConfig:ScriptableObject
    {
        [Header("地图基础设置")]
        [Tooltip("每个区块的大小(瓦片数量)，比如16x16")]
        public int chunkSize = 16;
        [Tooltip("围绕玩家加载的区块半径，1表示3x3区块，2表示5x5区块")]
        public int activeChunkRadius = 1;
        [Header("地图生成设置")]
        public List<TileGenerationRule> tileRules = new();
    }
    [System.Serializable]
    public class TileGenerationRule
    {
        [Header("规则瓦片")] 
        public RuleTile tile;
        [Header("生成的权重")]
        public int terrainWeight=10;

        [Header("生成范围")] 
        public Vector2Int minSize = new(1, 1);
        public Vector2Int maxSize = new(4, 4);
        
        [Header("寻路")]
        [Tooltip("寻路代价，1=正常, 越大=越难走, 255=不可通行")]
        public byte pathfindingCost = 1;
        
        [Header("生成物")]
        public List<PropSpawnRule> propSpawnRules;
    }
    [System.Serializable]
    public class PropSpawnRule
    {
        public string propId; // 道具的唯一ID，方便走对象池
        public string propPrefabPath; 
        
        [Range(0f, 1f)]
        [Tooltip("在该瓦片上生成该道具的概率 (0~1)")]
        public float spawnProbability; 
        [Header("寻路与避障逻辑数据")]
        public bool isObstacle;      // ORCA 是否需要把它当做静态障碍物
        public byte pathfindingCost=10; // 流场寻路代价 
    }
}