using System;
using System.Collections.Generic;
using Game.Script.Model;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Script.Manager
{
    /// <summary>
    /// 流场寻路管理者
    /// 职责：
    /// 1. 监听 MapManager 的 Chunk 事件，维护 GridCostData 的统一视图
    /// 2. 监听玩家位置变化，定时触发 BFS 流场重建
    /// 3. 提供 GetFlowDirection() 供敌人查询流场方向
    /// </summary>
    public class PathFindingManager : IDisposable
    {
       
        private MapManager mapManager;
        private MapConfig config;
        
        // 流场网格数据
        private int2 gridMin; // 流场左下角对应的 Chunk 坐标
        private int2 gridMax; // 流场右上角对应的 Chunk 坐标
        private int gridWidth;
        private int gridHeight;
        private float2 gridOrigin; // 流场左下角世界坐标

        //方向数组(大小gridWidth*gridHeight),zero为不可达
        private float2[] flowDirections;
        
        // 距离场数组
        private ushort[] integrationField;

        // rebuild参数
        private float rebuildInterval = 0.4f; // 重建间隔（秒）
        private float lastRebuildTime = -999f;
        private bool dirtyFlag; // Chunk 变化时置 true
        private int2 lastPlayerGridPos; // 上次玩家所在的流场格子坐标，用于判断是否重建
        private float rebuildThreshold = 1f; // 玩家移动超过此距离（格子数）触发重建

        // 缓存
        private Queue<int> bfsQueue = new();
        private int[] neighborOffsets8;
        
        private Dictionary<Vector2Int, ChunkLogicData> chunkDataSnapshot = new();

        
        // 流场格子大小(等于多少个Tile)
        private const float CellSize = 1f;

        public PathFindingManager(MapManager mm)
        {
            // 4 邻域偏移
            // 8 邻域偏移
            neighborOffsets8 = new[] { -1, -1, 0, -1, 1, -1, -1, 0, 1, 0, -1, 1, 0, 1, 1, 1 };
            mapManager = mm;
            config = mm.config;
            
            mapManager.OnChunkGenerated += OnChunkGenerated;
            mapManager.OnChunkDisposed += OnChunkDisposed;
        }
        
        private void OnChunkGenerated(ChunkLogicData chunkData)
        {
            chunkDataSnapshot[chunkData.ChunkPos] = chunkData;
            dirtyFlag = true;
        }

        private void OnChunkDisposed(Vector2Int chunkPos)
        {
            chunkDataSnapshot.Remove(chunkPos);
            dirtyFlag = true;
        }
        

        public void Update(float deltaTime, float3 playerWorldPos)
        {
            float2 playerPos = playerWorldPos.xy;
            
            // 检查是否需要重建流场
            bool shouldRebuild = CheckShouldRebuild(playerPos);

            if (shouldRebuild)
            {
                BuildFlowField(playerPos);
                lastRebuildTime = Time.time;
                dirtyFlag = false;
            }
        }

        /// <summary>
        /// 判断是否需要重建流场
        /// </summary>
        private bool CheckShouldRebuild(float2 playerWorldPos)
        {
            // Chunk 变化时强制重建
            if (dirtyFlag) return true;

            // 计时器间隔检查
            if (Time.time - lastRebuildTime < rebuildInterval)
                return false;

            int2 curGridPos = WorldPosToGridPos(playerWorldPos);
            if (math.abs(curGridPos.x - lastPlayerGridPos.x) >= rebuildThreshold ||
                math.abs(curGridPos.y - lastPlayerGridPos.y) >= rebuildThreshold)
                return true;
            
            return false;
        }
        
        //bfs流场构建
        private void BuildFlowField(float2 playerWorldPos)
        {
            //确定流场覆盖范围（基于所有活跃 Chunk 的 AABB）
            CalculateGridBounds();

            if (gridWidth <= 0 || gridHeight <= 0)
                return;

            // 分配数组
            int totalCells = gridWidth * gridHeight;
            if (flowDirections == null || flowDirections.Length != totalCells)
            {
                flowDirections = new float2[totalCells];
                integrationField = new ushort[totalCells];
            }

            // 计算玩家所在的流场格子坐标
            int2 playerGridPos = WorldPosToGridPos(playerWorldPos);

            // BFS构建距离场
            BuildIntegrationField(playerGridPos);

            //方向场构建
            GenerateFlowField();

            // 更新元数据
            lastPlayerGridPos = new int2(playerGridPos.x, playerGridPos.y);
        }

        /// <summary>
        /// 计算流场网格的边界（基于所有活跃 Chunk）
        /// </summary>
        private void CalculateGridBounds()
        {
            if (chunkDataSnapshot.Count == 0)
            {
                gridWidth = 0;
                gridHeight = 0;
                return;
            }

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            foreach (var kvp in chunkDataSnapshot)
            {
                Vector2Int chunkPos = kvp.Key;
                if (chunkPos.x < minX) minX = chunkPos.x;
                if (chunkPos.y < minY) minY = chunkPos.y;
                if (chunkPos.x > maxX) maxX = chunkPos.x;
                if (chunkPos.y > maxY) maxY = chunkPos.y;
            }

            gridMin = new int2(minX, minY);
            gridMax = new int2(maxX, maxY);

            int chunkSize = config.chunkSize;
            // 流场网格 = ChunkAABB 涵盖的所有格子
            gridWidth = (maxX - minX + 1) * chunkSize;
            gridHeight = (maxY - minY + 1) * chunkSize;
//            Debug.Log($"[PathFindingManager]gridWidth: {gridWidth}, gridHeight: {gridHeight}");
            // 流场左下角世界坐标
            gridOrigin = new float2(minX * chunkSize, minY * chunkSize);
        }

        /// <summary>
        /// BFS 构建 距离场
        /// 从玩家位置开始向外扩散，考虑地形代价
        /// </summary>
        private void BuildIntegrationField(int2 playerGridPos)
        {
            // 初始化距离场为最大值
            Array.Fill(integrationField, ushort.MaxValue);

            // 玩家格子索引
            int playerIdx = playerGridPos.y * gridWidth + playerGridPos.x;
            
            // 边界检查
            if (playerGridPos.x < 0 || playerGridPos.x >= gridWidth ||
                playerGridPos.y < 0 || playerGridPos.y >= gridHeight)
                return;

            // 检查玩家所在格子是否可行走
            byte playerCost = GetCostAtGridPos(playerGridPos);
            if (playerCost >= 255)
                return;

            // BFS 初始化
            bfsQueue.Clear();
            integrationField[playerIdx] = 0;
            bfsQueue.Enqueue(playerIdx);

            while (bfsQueue.Count > 0)
            {
                int curIdx = bfsQueue.Dequeue();
                ushort curDist = integrationField[curIdx];
                int curX = curIdx % gridWidth;
                int curY = curIdx / gridWidth;

                // 遍历 8 邻域
                for (int i = 0; i < 8; i++)
                {
                    int nx = curX + neighborOffsets8[i * 2];
                    int ny = curY + neighborOffsets8[i * 2 + 1];

                    // 边界检查
                    if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                        continue;

                    int nIdx = ny * gridWidth + nx;

                    // 跳过已访问的格子
                    if (integrationField[nIdx] != ushort.MaxValue)
                        continue;

                    // 获取该格子的地形代价
                    byte cost = GetCostAtGridPos(new int2(nx, ny));
                    
                    // 如果不可通行(255)，跳过
                    if (cost >= 255)
                        continue;

                    // 计算新距离: curDist + (1 + cost/25)
                    ushort newDist = (ushort)Mathf.Min(curDist + 1 + cost / 25, ushort.MaxValue - 1);
                    
                    integrationField[nIdx] = newDist;
                    bfsQueue.Enqueue(nIdx);
                }
            }
        }

        /// <summary>
        ///  生成流场方向
        /// </summary>
        private void GenerateFlowField()
        {
            int totalCells = gridWidth * gridHeight;

            for (int idx = 0; idx < totalCells; idx++)
            {
                // 如果该格子不可达，方向设为0
                if (integrationField[idx] == ushort.MaxValue)
                {
                    flowDirections[idx] = float2.zero;
                    continue;
                }

                int curX = idx % gridWidth;
                int curY = idx / gridWidth;
                ushort curDist = integrationField[idx];

                // 在 8 邻域中找距离最小的邻居
                int bestNX = curX;
                int bestNY = curY;
                ushort minDist = curDist;

                for (int i = 0; i < 8; i++)
                {
                    int nx = curX + neighborOffsets8[i * 2];
                    int ny = curY + neighborOffsets8[i * 2 + 1];

                    if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight)
                        continue;

                    int nIdx = ny * gridWidth + nx;
                    ushort nDist = integrationField[nIdx];

                    if (nDist < minDist)
                    {
                        minDist = nDist;
                        bestNX = nx;
                        bestNY = ny;
                    }
                }

                // 计算方向
                float2 dir = new float2(bestNX - curX, bestNY - curY);
                
                // 如果最佳邻居就是自己，指向玩家位置所在格子方向
                if (dir.x == 0 && dir.y == 0)
                {
                    // 指向玩家格子
                    int2 target = lastPlayerGridPos;
                    float dx = target.x - curX;
                    float dy = target.y - curY;
                    float len = math.sqrt(dx * dx + dy * dy);
                    if (len > 0.001f)
                        dir = new float2(dx / len, dy / len);
                    else
                        dir = float2.zero;
                }
                else
                    dir = math.normalize(dir);
                
                flowDirections[idx] = dir;
            }
        }
        

        /// <summary>
        /// 查询某个世界坐标对应的流场方向
        /// 如果流场不存在或该格子不可达，返回指向玩家的 fallback 方向
        /// </summary>
        public float2 GetFlowDirection(float3 worldPos, float2 playerPos)
        {
            if (flowDirections == null || gridWidth <= 0 || gridHeight <= 0)
            {
                // Fallback: 直接指向玩家
                float2 diff = playerPos - worldPos.xy;
                float len = math.length(diff);
                if (len > 0.001f)
                    return diff / len;
                return float2.zero;
            }

            int gx = Mathf.FloorToInt(worldPos.x - gridOrigin.x);
            int gy = Mathf.FloorToInt(worldPos.y - gridOrigin.y);

            // 边界检查
            if (gx < 0 || gx >= gridWidth || gy < 0 || gy >= gridHeight)
            {
                // Fallback
                float2 diff = playerPos - worldPos.xy;
                float len = math.length(diff);
                if (len > 0.001f)
                    return diff / len;
                return float2.zero;
            }

            int idx = gy * gridWidth + gx;
            float2 dir = flowDirections[idx];

            // 如果方向为0（不可达格子），fallback
            if (math.all(dir == 0))
            {
                float2 diff = playerPos - worldPos.xy;
                float len = math.length(diff);
                if (len > 0.001f)
                    return diff / len;
                return float2.zero;
            }

            return dir;
        }
        

        /// <summary>
        /// 世界坐标 -> 流场格子坐标
        /// </summary>
        private int2 WorldPosToGridPos(float2 worldPos)
        {
            int gx = Mathf.FloorToInt(worldPos.x - gridOrigin.x);
            int gy = Mathf.FloorToInt(worldPos.y - gridOrigin.y);
            return new int2(gx, gy);
        }

        /// <summary>
        /// 查询流场格子对应的 GridCostData
        /// </summary>
        private byte GetCostAtGridPos(int2 gridPos)
        {
            int chunkSize = config.chunkSize;

            // 计算 Chunk 坐标
            int chunkX = Mathf.FloorToInt((float)gridPos.x / chunkSize);
            int chunkY = Mathf.FloorToInt((float)gridPos.y / chunkSize);
            Vector2Int chunkPos = new Vector2Int(chunkX, chunkY);

            // 计算在 Chunk 内的局部坐标
            int localX = ((gridPos.x % chunkSize) + chunkSize) % chunkSize;
            int localY = ((gridPos.y % chunkSize) + chunkSize) % chunkSize;

            if (chunkDataSnapshot.TryGetValue(chunkPos, out var chunkData))
                return chunkData.GridCostData[localX, localY];
            // 如果没有该 Chunk 的数据，默认不可通行
            return 255;
        }

        /// <summary>
        /// 全局流场网格的原点
        /// </summary>
        public float2 GetGridOrigin() => gridOrigin;

        /// <summary>
        /// 流场网格尺寸
        /// </summary>
        public int GetGridWidth() => gridWidth;
        public int GetGridHeight() => gridHeight;
        public float GetCellSize() => CellSize;

        /// <summary>
        /// 获取流场方向数组的引用
        /// </summary>
        public float2[] GetFlowDirections() => flowDirections;
        

        public void Dispose()
        {
            if (mapManager != null)
            {
                mapManager.OnChunkGenerated -= OnChunkGenerated;
                mapManager.OnChunkDisposed -= OnChunkDisposed;
            }

            flowDirections = null;
            integrationField = null;
            chunkDataSnapshot.Clear();
            bfsQueue.Clear();
        }
    }
}