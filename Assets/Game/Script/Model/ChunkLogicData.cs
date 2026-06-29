using UnityEngine;

namespace Game.Script.Model
{
    public class ChunkLogicData
    {
        public Vector2Int ChunkPos;
        public int ChunkSize;
        
        // 存储当前区块每个格子的寻路网格数据
        public byte[,] GridCostData; 
        
    }
}