using System;
using System.Collections.Generic;
using Boot.Script;
using Cysharp.Threading.Tasks;
using Game.Script.Model;
using QFramework;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Tilemaps;

namespace Game.Script.Manager
{
    public class MapManager
    {
        [Header("配置")]
        public MapConfig config;

        private int poolSize;
        private int totalWeight;
        [Header("组件引用")]
        public Tilemap map;
        
        //对象池
        private Dictionary<string,ObjectPool<GameObject>> propPools;
        
        //已经加载的区块坐标
        private HashSet<Vector2Int> activeChunks = new();
        private Dictionary<Vector2Int,List<GameObject>> activeProps = new();
        
        private Vector2Int lastPlayerChunk = new(999999, 999999);
        
        
        //寻路字典
        private Dictionary<Vector2Int, ChunkLogicData> chunkLogicDict = new();
       /// <summary>
       /// 当某个区块逻辑数据生成完毕时触发。流场系统监听此事件更新寻路网格，ORCA系统监听此事件生成静态障碍线段
       /// </summary>
       public event Action<ChunkLogicData> OnChunkGenerated;
       /// <summary>
       /// 当某个区块被卸载时触发。通知其他系统移除对应数据
       /// </summary>
       public event Action<Vector2Int> OnChunkDisposed;
       
       
       //缓存
       List<(int x, int y)> emptyListCache ;
       HashSet<(int x, int y)> emptySetCache ;
       TileBase[] tilesCache ;
       TileGenerationRule[] decidedRulesCache ;
        public MapManager(MapConfig config, Tilemap map,int pSize=100)
        {
            this.config = config;
            this.map = map;
            propPools = new();
            poolSize = pSize;
            foreach (var rule in config.tileRules)
                totalWeight += rule.terrainWeight;
            int totalChunks=config.chunkSize*config.chunkSize;
            emptyListCache = new List<(int x, int y)>(totalChunks);
            emptySetCache = new HashSet<(int x, int y)>();
            tilesCache = new TileBase[totalChunks];
            decidedRulesCache = new TileGenerationRule[totalChunks];
        }

        public void UpdateMap(Vector3 targetPos)
        {
            var curChunk=GetChunkPosFromWorld(targetPos);

            if (curChunk != lastPlayerChunk)
            {
                UpdateVisibleChunks(curChunk).Forget();
                lastPlayerChunk = curChunk;
            }
        }

        private async UniTask UpdateVisibleChunks(Vector2Int centerChunk)
        {
            
            HashSet<Vector2Int> newActiveChunks = new HashSet<Vector2Int>();
            int radius = config.activeChunkRadius;

            // 根据中心坐标，计算周围需要显示的 Chunk 坐标
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int chunkPos = new Vector2Int(centerChunk.x + x, centerChunk.y + y);
                    newActiveChunks.Add(chunkPos);

                    // 如果是新的区块，则生成
                    if (!activeChunks.Contains(chunkPos))
                      await GenerateChunk(chunkPos);
                }
            }
            // 卸载老区块
            List<Vector2Int> chunksToRemove = new List<Vector2Int>();
            foreach (var activeChunk in activeChunks)
                if (!newActiveChunks.Contains(activeChunk))
                    chunksToRemove.Add(activeChunk);

            foreach (var chunk in chunksToRemove)
               await RemoveChunk(chunk);
            
            // 更新引用
            activeChunks = newActiveChunks;
        }
        private async UniTask GenerateChunk(Vector2Int chunkPos)
        {
            int chunkSize = config.chunkSize;
            BoundsInt bounds = new BoundsInt(chunkPos.x * chunkSize, chunkPos.y * chunkSize, 0, chunkSize, chunkSize, 1);
            Array.Clear(tilesCache, 0, tilesCache.Length);
            Array.Clear(decidedRulesCache, 0, decidedRulesCache.Length);
            ChunkLogicData logicData = new ChunkLogicData
            {
                ChunkPos = chunkPos,
                ChunkSize = config.chunkSize,
                GridCostData = new byte[config.chunkSize, config.chunkSize]
            };
          
            emptyListCache.Clear();
            emptySetCache.Clear();
            //初始化
            for(int y=0; y<chunkSize; y++)
                for (int x = 0; x < chunkSize; x++)
                {
                    emptyListCache.Add((x, y));
                    emptySetCache.Add((x,y));
                }

            while (emptyListCache.Count>0)
            {
                int randIdx=UnityEngine.Random.Range(0, emptyListCache.Count);
                (int startX, int startY) = emptyListCache[randIdx];
                if (!emptySetCache.Contains((startX, startY)))//判断是否已被填充
                {
                    emptyListCache.RemoveAt(randIdx);
                    continue;
                }

                TileGenerationRule rule = GetRandomTileRule();
                if (rule == null)
                {
                    emptyListCache.RemoveAt(randIdx);
                    emptySetCache.Remove((startX, startY));
                    continue;
                }
                
                int w = UnityEngine.Random.Range(rule.minSize.x, rule.maxSize.x + 1);
                int h = UnityEngine.Random.Range(rule.minSize.y, rule.maxSize.y + 1);
                //边界保护
                int maxX=Mathf.Min(startX + w, chunkSize);
                int maxY=Mathf.Min(startY + h, chunkSize);
                //填充
                for (int ty = startY; ty < maxY; ty++)
                {
                    int rowOff = ty * chunkSize;
                    for (int tx = startX; tx < maxX; tx++)
                    {
                        int targetIdx = tx + rowOff;
                        if (decidedRulesCache[targetIdx] == null)
                        {
                            tilesCache[targetIdx] = rule.tile;
                            decidedRulesCache[targetIdx] = rule;
                            // 填充寻路代价
                            logicData.GridCostData[tx, ty] = rule.pathfindingCost;
                            emptySetCache.Remove((tx, ty));
                        }
                    }
                }
            }
            
            #region old

            // for (int y = 0; y < chunkSize; y++)
            // for (int x = 0; x < chunkSize; x++)
            // {
            //     int index = x + y * chunkSize;
            //     //如果已有则跳过
            //     if (decidedRules[index]!=null) continue;
            //
            //     TileGenerationRule selectedRule = GetRandomTileRule();
            //     if (selectedRule == null) continue;
            //
            //     int width = UnityEngine.Random.Range(selectedRule.minSize.x, selectedRule.maxSize.x + 1);
            //     int height = UnityEngine.Random.Range(selectedRule.minSize.y, selectedRule.maxSize.y + 1);
            //     // 根据生成出来的长宽进行连片填充
            //     for (int cy = 0; cy < height; cy++)
            //     {
            //         int targetY = y + cy;
            //         if (targetY >= chunkSize) break; 
            //     
            //         for (int cx = 0; cx < width; cx++)
            //         {
            //             int targetX = x + cx;
            //                 
            //             if (targetX >= chunkSize) break; 
            //         
            //             int targetIdx = targetX + targetY * chunkSize;
            //         
            //             // 不要覆盖已经生成的其他簇
            //             if (decidedRules[targetIdx] == null)
            //             {
            //                 tiles[targetIdx] = selectedRule.tile;
            //                 decidedRules[targetIdx] = selectedRule;
            //             }
            //         }
            //     }
            // }

            #endregion
            chunkLogicDict[chunkPos] = logicData;
            map.SetTilesBlock(bounds, tilesCache);
            //  生成地形道具
           await GeneratePropsBasedOnRules(chunkPos, bounds, decidedRulesCache,logicData);
            OnChunkGenerated?.Invoke(logicData);
        }
      
        /// <summary>
        /// 按权重随机抽取一种地形类型
        /// </summary>
        private TileGenerationRule GetRandomTileRule()
        {
            if (config.tileRules == null || config.tileRules.Count == 0) return null;

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var rule in config.tileRules)
            {
                currentWeight += rule.terrainWeight;
                if (randomValue < currentWeight)
                    return rule;
            }
            return config.tileRules[0];
        }
        /// <summary>
        /// 读取格子自身的地形规则来判定是否生出陷阱/道具
        /// </summary>
        private async UniTask GeneratePropsBasedOnRules(Vector2Int chunkPos, BoundsInt bounds, TileGenerationRule[] decidedRules,ChunkLogicData logicData)
        {
            List<GameObject> chunkProps = new List<GameObject>();
            int index = 0; //decidedRules的索引

            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    TileGenerationRule cellRule = decidedRules[index];
                    index++;

                    if (cellRule == null || cellRule.propSpawnRules == null) continue;
                    
                    // 道具生成
                    foreach (var propRule in cellRule.propSpawnRules)
                        if (UnityEngine.Random.value < propRule.spawnProbability)
                        {
                            Vector3 worldPos = map.CellToWorld(new Vector3Int(x, y, 0));
                            if (!propPools.TryGetValue(propRule.propPrefabPath, out var pool))
                            {
                                //获取预制体
                                var gm = await YooAssetManager.Instance.LoadAssetAsync<GameObject>(ResPrefix.Props +
                                    propRule.propPrefabPath);
                                pool = new ObjectPool<GameObject>(() =>
                                {
                                    var obj = UnityEngine.Object.Instantiate(gm.Asset, worldPos, Quaternion.identity);
                                    return obj;
                                }, obj =>
                                {
                                    obj.SetActive(true);
                                }, obj =>
                                {
                                    obj.SetActive(false);
                                }, UnityEngine.Object.Destroy,true,10,poolSize);
                                //加入字典
                                propPools[propRule.propPrefabPath] = pool;
                            }
                            
                            var prop = pool.Get();
                            prop.transform.position = worldPos;
                            prop.transform.rotation = Quaternion.identity;
                            chunkProps.Add(prop);
                            logicData.GridCostData[x,y]=cellRule.pathfindingCost;
                            // 如果生成了占据格子的物理道具，则将其 cost 设为 255 (不可行走);
                            break; 
                        }
                }
            }
            if (chunkProps.Count > 0)
                activeProps[chunkPos] = chunkProps;
            
        }
    
        /// <summary>
        /// 销毁/隐藏指定区块
        /// </summary>
        private async UniTask RemoveChunk(Vector2Int chunkPos)
        {
            int chunkSize = config.chunkSize;
            BoundsInt bounds = new BoundsInt(chunkPos.x * chunkSize, chunkPos.y * chunkSize, 0, chunkSize, chunkSize, 1);
            
            // 新建一个全为 null 的数组来清除瓦片
            TileBase[] nullTiles = new TileBase[chunkSize * chunkSize];
            map.SetTilesBlock(bounds, nullTiles);

            // 回收该区块的道具到对象池
            if (activeProps.TryGetValue(chunkPos, out var props))
            {
                foreach (var prop in props)
                {
                    int idx = prop.name.IndexOf('(');
                    string res = idx >= 0 ? prop.name.Substring(0, idx) : prop.name;
                    if(propPools.TryGetValue(res, out var pool))
                        pool.Release(prop);
                    else
                        UnityEngine.Object.Destroy(prop);
                }
                activeProps.Remove(chunkPos);
            }
            OnChunkDisposed?.Invoke(chunkPos);
            chunkLogicDict.Remove(chunkPos);
            await UniTask.Yield();
        }

        private Vector2Int GetChunkPosFromWorld(Vector3 worldPos)
        {
            Vector3Int cellPos = map.WorldToCell(worldPos);
            int chunkSize = config.chunkSize;
            
            // 处理坐标偏移防止负数区间的计算误差
            int chunkX = Mathf.FloorToInt((float)cellPos.x / chunkSize);
            int chunkY = Mathf.FloorToInt((float)cellPos.y / chunkSize);

            return new Vector2Int(chunkX, chunkY);
        }

        /// <summary>
        /// 获取指定区块的逻辑数据
        /// </summary>
        public Dictionary<Vector2Int, ChunkLogicData> GetChunkLogicDict()
        {
            return chunkLogicDict;
        }

        /// <summary>
        /// 提供给外部(流场寻路)查询指定真实世界坐标的寻路代价
        /// </summary>
        public byte GetCostAtWorldPosition(Vector3 worldPos)
        {
            Vector3Int cellPos = map.WorldToCell(worldPos);
            
            // 世界坐标 -> Chunk坐标
            int chunkSize = config.chunkSize;
            int chunkX = Mathf.FloorToInt((float)cellPos.x / chunkSize);
            int chunkY = Mathf.FloorToInt((float)cellPos.y / chunkSize);
            Vector2Int chunkPos = new Vector2Int(chunkX, chunkY);

            // 在当前区块内的局部坐标（0 ~ chunkSize-1）
            int localX = cellPos.x - chunkX * chunkSize;
            int localY = cellPos.y - chunkY * chunkSize;

            // 边界安全剪裁
            localX = Mathf.Clamp(localX, 0, chunkSize - 1);
            localY = Mathf.Clamp(localY, 0, chunkSize - 1);

            if (chunkLogicDict.TryGetValue(chunkPos, out var logicData))
            {
                return logicData.GridCostData[localX, localY];
            }

            // 未生成区块默认可通行
            return 1;
        }

        public void Dispose()
        {
                foreach (var kv in activeProps)
                {
                    var props = kv.Value;
                    if (props == null) continue;
                    foreach (var prop in props)
                    {
                        if (!prop) continue;
                        bool returnedToPool = false;
                        foreach (var poolEntry in propPools)
                        {
                            var poolKey = poolEntry.Key;
                            var pool = poolEntry.Value;
                            if (pool == null) continue;
                            if (!string.IsNullOrEmpty(prop.name) && prop.name.IndexOf(poolKey, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                try
                                {
                                    pool.Release(prop);
                                    returnedToPool = true;
                                }
                                catch
                                {
                                    UnityEngine.Object.Destroy(prop);
                                }
                                break;
                            }
                        }
                        if (!returnedToPool)
                            UnityEngine.Object.Destroy(prop);
                    }
                }
                activeProps.Clear();
            if (propPools != null)
            {
                foreach (var poolEntry in propPools)
                {
                    try
                    {
                        poolEntry.Value.Clear(); // 删除池中所有未被取出的实例
                    }
                    catch
                    {
                        // 忽略清理时的异常，继续清理其他池
                    }
                }
                propPools.Clear();
            }
            
            if (chunkLogicDict != null)
            {
                var chunkKeys = new List<Vector2Int>(chunkLogicDict.Keys);
                foreach (var chunkPos in chunkKeys)
                {
                    try
                    {
                        OnChunkDisposed?.Invoke(chunkPos);
                    }
                    catch
                    {
                        // 忽略监听者异常，继续通知其他区块
                    }
                }
                chunkLogicDict.Clear();
            }
            map.ClearAllTiles();
            activeChunks.Clear();
            lastPlayerChunk = new Vector2Int(999999, 999999);
        }

    }
}