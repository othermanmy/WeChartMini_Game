using System.Collections.Generic;
using System.Text;
using Boot.Script;
using Cysharp.Threading.Tasks;
using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Coin;
using Game.Script.ECS.Components.Enemy;
using Game.Script.ECS.Components.Pool;
using Game.Script.Model;
using Newtonsoft.Json;
using QFramework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;

namespace Game.Script.Manager
{
    public class EnemySpawnManager : Singleton<EnemySpawnManager>
    {
        
        public const int MaxActiveEnemies = 500;

        private EntityManager entityManager;
        private World world;
        private EnemySpawnConfig spawnConfig;
        private SubScene enemySubScene;

        private EntityQuery singletonQuery;
        // 地图世界尺寸
        private float2 mapWorldSize;

        // 配置表
        private Dictionary<string, EnemyConfig> configMap;

        // 模板注册表
        private readonly Dictionary<string, Entity> poolContainerMap = new();

        // 标记哪些类型已完成池初始化
        private readonly HashSet<string> initializedTypes = new();

        // 波次系统 
        private int currentWaveIndex;
        private int currentSpawnCountPerBatch;
        private float spawnIntervalTimer;
        private float countUpTimer;
        private bool isSpawning;
        private float currentGameTime;
        private Unity.Mathematics.Random spawnRandom;

        private EnemySpawnManager() { }
        
        
        public async UniTask InitAsync(SubScene s)
        {
            world = World.DefaultGameObjectInjectionWorld;
            entityManager = world.EntityManager;
            enemySubScene = s;
            singletonQuery= entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyTemplateRegistry>());
            // 加载 JSON 配置
            var configJson = await YooAssetManager.Instance
                .LoadAssetAsync<TextAsset>(ResPrefix.Data + "EnemyConfigs");
            byte[] bytes = configJson.Asset.bytes;
            var json = Encoding.UTF8.GetString(bytes);
            configMap = JsonConvert.DeserializeObject<Dictionary<string, EnemyConfig>>(json);

            // 加载敌人生成配置文件
            var ea = await YooAssetManager.Instance
                .LoadAssetAsync<EnemySpawnConfig>(ResPrefix.Data + "EnemySpawnConfig");
            spawnConfig = ea.Asset;
            spawnConfig.spawnWaves.Sort((a, b)=>a.beginTime.CompareTo(b.beginTime));
            await LoadEnemyPrefabScene();
        }

        private async UniTask LoadEnemyPrefabScene()
        {
            var sceneRef = enemySubScene.SceneGUID;
          var r=  SceneSystem.LoadSceneAsync(
                world.Unmanaged,
                sceneRef,
                new SceneSystem.LoadParameters
                {
                    AutoLoad = true,
                });

            // 轮询等待 SubScene 加载完成
            while (!SceneSystem.IsSceneLoaded(world.Unmanaged, r))
                await UniTask.Yield();
        }
        
        public void SetMapWorldSize(float2 size)
        {
            mapWorldSize = size;
        }

        /// <summary>
        /// 确保某类型的模板 Entity 已就绪，并预热对象池
        /// 模板 Entity 从 EnemyTemplateRegistry 单例中读取
        /// </summary>
        public async UniTask<bool> EnsureEnemyType(string typeName, int prewarmCount = 50)
        {
            if (initializedTypes.Contains(typeName))
                return true;

            // 等待 EnemyTemplateRegistry 中出现该类型的模板
            var template = await WaitForTemplate(typeName);
            if (template == Entity.Null)
            {
                Debug.LogError($"[EnemySpawnManager] Template not found for: {typeName}");
                return false;
            }

            // 初始化该类型的对象池
            InitPoolForType(typeName, template, prewarmCount);
            initializedTypes.Add(typeName);

            Debug.Log($"[EnemySpawnManager] Type registered & pool ready: {typeName} x {prewarmCount}");
            return true;
        }

        /// <summary>
        /// 轮询等待 EnemyTemplateRegistry 中出现指定 typeName 的模板 Entity
        /// </summary>
        private async UniTask<Entity> WaitForTemplate(string typeName)
        {
            const int maxFrames = 300; // 最多等 5 秒
            for (int frame = 0; frame < maxFrames; frame++)
            {
                if (TryGetTemplate(typeName, out var template))
                    return template;

                await UniTask.Yield();
            }

            return Entity.Null;
        }

        /// <summary>
        /// 从 EnemyTemplateRegistry 单例中读取模板 Entity
        /// </summary>
        private bool TryGetTemplate(string typeName, out Entity template)
        {
            template = Entity.Null;
            if (world == null || !world.IsCreated) return false;

            
            if (!singletonQuery.TryGetSingleton<EnemyTemplateRegistry>(out var registry))
                return false;

            var key = new FixedString64Bytes(typeName);
            if (registry.TemplateMap.TryGetValue(key, out template))
            {
                if (entityManager.Exists(template))
                    return true;
                Debug.LogWarning($"[EnemySpawnManager] Template Entity for '{typeName}' is invalid, will retry...");
            }

            return false;
        }

        /// <summary>
        /// 为单种敌人类型创建池容器 Entity + 预热 N 个休眠实例
        /// </summary>
        private void InitPoolForType(string typeName, Entity template, int count)
        {
            if (!entityManager.Exists(template))
            {
                Debug.LogError($"[EnemySpawnManager] Template Entity for {typeName} is gone before pool init.");
                return;
            }

            var container = entityManager.CreateEntity();
            entityManager.AddComponentData(container, new EnemyTypeName { Value = typeName });
            entityManager.AddBuffer<PooledEntity>(container);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // 获取金币掉落配置
            EnemyCoinFall coinFall = default;
            if (configMap.TryGetValue(typeName, out var cfg))
            {
                coinFall = new EnemyCoinFall
                {
                    minCoin = cfg.minFallCoin,
                    maxCoin = cfg.maxFallCoin,
                };
            }

            for (int i = 0; i < count; i++)
            {
                var instance = ecb.Instantiate(template);
                ecb.AddComponent(instance, new PoolTag());
                ecb.AddComponent(instance,
                    new PoolContainerRef { Value = container });
                // 休眠状态
                ecb.SetComponent(instance, new EnemyStats { health = 0f, damage = 0f });
                ecb.SetComponent(instance, LocalTransform.FromPosition(
                    new float3(-9999f, -9999f, 0f)));
                // 金币掉落配置
                ecb.SetComponent(instance, coinFall);
                ecb.AppendToBuffer(container, new PooledEntity { Value = instance });
            }

            ecb.Playback(entityManager);
            ecb.Dispose();

            poolContainerMap[typeName] = container;
        }

       

        /// <summary>
        /// 开始敌人生成。预加载所有波次涉及的敌人类型。
        /// </summary>
        public async UniTask BeginSpawn()
        {
            if (!spawnConfig || spawnConfig.spawnWaves == null || spawnConfig.spawnWaves.Count == 0)
            {
                Debug.LogWarning("[EnemySpawnManager] No spawn waves configured.");
                return;
            }

            // 初始化随机数生成器
            spawnRandom = Unity.Mathematics.Random.CreateFromIndex((uint)System.DateTime.Now.Ticks);

            // 收集所有波次中出现的敌人类型，统一预热
            var allTypes = new HashSet<string>();
            foreach (var wave in spawnConfig.spawnWaves)
            {
                if (wave.enemySpawnDataList == null) continue;
                foreach (var data in wave.enemySpawnDataList)
                {
                    if (!string.IsNullOrEmpty(data.enemyKey))
                        allTypes.Add(data.enemyKey);
                }
            }

            var failedTypes = 0;
            foreach (var typeName in allTypes)
            {
                var ok = await EnsureEnemyType(typeName, MaxActiveEnemies / 10);
                if (!ok) failedTypes++;
            }

            if (failedTypes > 0)
            {
                Debug.LogError($"[EnemySpawnManager] {failedTypes} enemy type(s) failed to init. Spawn system aborted.");
                return;
            }

            // 初始化第一波
            currentWaveIndex = 0;
            StartWave(spawnConfig.spawnWaves[0]);

            isSpawning = true;
            Debug.Log("[EnemySpawnManager] Spawn system started.");
        }

        /// <summary>
        /// 停止敌人生成
        /// </summary>
        public void EndSpawn()
        {
            isSpawning = false;
            currentWaveIndex = 0;
            ClearAllEnemies();
        }

        /// <summary>
        /// 销毁所有敌人实体
        /// </summary>
        private void ClearAllEnemies()
        {
            if (world == null || !world.IsCreated)
            {
                poolContainerMap.Clear();
                initializedTypes.Clear();
                return;
            }

            //收集所有需要销毁的 Entity
            var toDestroy = new NativeList<Entity>(Allocator.Temp);

            foreach (var kvp in poolContainerMap)
            {
                var container = kvp.Value;
                if (entityManager.Exists(container))
                {
                    // 收集容器中的所有休眠实例
                    if (entityManager.HasBuffer<PooledEntity>(container))
                    {
                        var buffer = entityManager.GetBuffer<PooledEntity>(container);
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var entity = buffer[i].Value;
                            if (entityManager.Exists(entity))
                                toDestroy.Add(entity);
                        }
                    }

                    // 收集容器自身
                    toDestroy.Add(container);
                }
            }

            poolContainerMap.Clear();

            //  收集所有场景中活跃的敌人实体
            using var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PoolTag>());
            var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var e in entities)
            {
                if (entityManager.Exists(e))
                    toDestroy.Add(e);
            }
            entities.Dispose();

            // 统一销毁
            foreach (var e in toDestroy)
            {
                if (entityManager.Exists(e))
                    entityManager.DestroyEntity(e);
            }

            toDestroy.Dispose();
            initializedTypes.Clear();
           // Debug.Log("[EnemySpawnManager] All enemies cleared.");
        }

        /// <summary>
        /// 每帧由 GameManager 调用
        /// </summary>
        public void Update(float currentTime, float dt)
        {
            if (!isSpawning) return;
            if (!spawnConfig || spawnConfig.spawnWaves == null || spawnConfig.spawnWaves.Count == 0) return;

            currentGameTime = currentTime;

            // 推进波次：当前波次结束后切换到下一波
            while (currentWaveIndex < spawnConfig.spawnWaves.Count)
            {
                float waveEndTime = GetWaveEndTime();
                if (currentTime < waveEndTime)
                    break;

                currentWaveIndex++;
                if (currentWaveIndex < spawnConfig.spawnWaves.Count)
                    StartWave(spawnConfig.spawnWaves[currentWaveIndex]);
            }

            if (currentWaveIndex >= spawnConfig.spawnWaves.Count)
            {
                isSpawning = false;
                Debug.Log("[EnemySpawnManager] All waves completed.");
                return;
            }

            var currentWave = spawnConfig.spawnWaves[currentWaveIndex];
            
            spawnIntervalTimer += dt;
            if (spawnIntervalTimer >= currentWave.enemySpawnInterval)
            {
                spawnIntervalTimer = 0f;
                SpawnBatch(currentWave);
            }

            // 增加生成数量
            countUpTimer += dt;
            if (countUpTimer >= currentWave.enemySpawnCountUpInterval)
            {
                countUpTimer = 0f;
                currentSpawnCountPerBatch = Mathf.Min(
                    currentSpawnCountPerBatch + currentWave.enemyUpCount,
                    currentWave.maxEnemySpawnCount);
            }
        }

        /// <summary>
        /// 获取当前波次的结束时间
        /// </summary>
        private float GetWaveEndTime()
        {
            int nextIndex = currentWaveIndex + 1;
            if (nextIndex < spawnConfig.spawnWaves.Count)
                return spawnConfig.spawnWaves[nextIndex].beginTime * 60f; // 转为秒
            return float.MaxValue;
        }

        /// <summary>
        /// 切换到新波次
        /// </summary>
        private void StartWave(EnemySpawnWave wave)
        {
            currentSpawnCountPerBatch = wave.baseEnemySpawnCount;
            spawnIntervalTimer = 0f;
            countUpTimer = 0f;
            Debug.Log(
                $"[EnemySpawnManager] Wave {currentWaveIndex} started. Base count: {wave.baseEnemySpawnCount}, Interval: {wave.enemySpawnInterval}s");
        }

        /// <summary>
        /// 批量生成一波敌人
        /// </summary>
        private void SpawnBatch(EnemySpawnWave wave)
        {
            if (wave.enemySpawnDataList == null || wave.enemySpawnDataList.Count == 0) return;

            int activeCount = QueryActiveEnemyCount();
            if (activeCount >= MaxActiveEnemies) return;
            int spawnLimit = Mathf.Min(currentSpawnCountPerBatch, MaxActiveEnemies - activeCount);

            int level = GetCurrentLevel(wave);
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < spawnLimit; i++)
            {
                // 按概率抽取敌人类型
                var enemyData = PickByProbability(wave.enemySpawnDataList, ref spawnRandom);
                if (enemyData == null) continue;

                var typeName = enemyData.enemyKey;
                var pos = GetRandomPositionAroundPlayer(ref spawnRandom);

                // 从池中取
                if (poolContainerMap.TryGetValue(typeName, out var container) &&
                    entityManager.HasBuffer<PooledEntity>(container))
                {
                    var buffer = entityManager.GetBuffer<PooledEntity>(container);
                    if (buffer.Length > 0)
                    {
                        var entity = buffer[buffer.Length - 1].Value;
                        buffer.RemoveAt(buffer.Length - 1);

                        // 激活
                        entityManager.SetComponentData(entity, LocalTransform.FromPosition(
                            new float3(pos.x, pos.y, 0f)));
                        if (!entityManager.HasComponent<EnemyActiveTag>(entity))
                            entityManager.AddComponentData(entity, new EnemyActiveTag());
                        ApplyAttributes(entity, typeName, level);
                    }
                    else
                        // 池空，通过 ECB 创建新实例
                        SpawnNewEnemyViaECB(ecb, typeName, pos, level);
                    
                }
                else
                   SpawnNewEnemyViaECB(ecb, typeName, pos, level);
                
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// 通过 ECB 创建新敌人实例并设置属性
        /// </summary>
        private Entity SpawnNewEnemyViaECB(EntityCommandBuffer ecb, string typeName, float2 position, int level)
        {
            if (!TryGetTemplate(typeName, out var template))
            {
                Debug.LogError($"[EnemySpawnManager] No template for: {typeName}");
                return Entity.Null;
            }

            var entity = ecb.Instantiate(template);
            ecb.SetComponent(entity, LocalTransform.FromPosition(new float3(position.x, position.y, 0f)));
            ecb.AddComponent(entity, new EnemyActiveTag());
            ecb.AddComponent(entity, new PoolTag());

            if (poolContainerMap.TryGetValue(typeName, out var container))
                ecb.AddComponent(entity, new PoolContainerRef { Value = container });

            // 应用属性
            if (configMap.TryGetValue(typeName, out var cfg))
            {
                ecb.SetComponent(entity, new EnemyStats
                {
                    health = cfg.baseHealth + cfg.levelUpHealth * (level - 1),
                    damage = cfg.baseDamage + cfg.levelUpDamage * (level - 1),
                });
                // 金币掉落配置
                ecb.SetComponent(entity, new EnemyCoinFall
                {
                    minCoin = cfg.minFallCoin,
                    maxCoin = cfg.maxFallCoin,
                });
            }

            return entity;
        }

        /// <summary>
        /// 生成一个敌人
        /// </summary>
        public Entity SpawnEnemy(string typeName, float2 position, int level)
        {
            if (QueryActiveEnemyCount() >= MaxActiveEnemies)
            {
               // Debug.LogWarning($"[EnemySpawnManager] MaxActiveEnemies ({MaxActiveEnemies}) reached, cannot spawn {typeName}.");
                return Entity.Null;
            }

            if (!initializedTypes.Contains(typeName))
            {
                Debug.LogError(
                    $"[EnemySpawnManager] Type not initialized: {typeName}. Call EnsureEnemyType first.");
                return Entity.Null;
            }

            Entity entity;

            // 从池中取
            if (poolContainerMap.TryGetValue(typeName, out var container) &&
                entityManager.HasBuffer<PooledEntity>(container))
            {
                var buffer = entityManager.GetBuffer<PooledEntity>(container);
                if (buffer.Length > 0)
                {
                    entity = buffer[buffer.Length - 1].Value;
                    buffer.RemoveAt(buffer.Length - 1);
                    ActivateEnemy(entity, typeName, position, level);
                    return entity;
                }
            }

            // 池空，从 EnemyTemplateRegistry 获取模板 Instantiate 新实例
            if (TryGetTemplate(typeName, out var template))
            {
                entity = entityManager.Instantiate(template);
                entityManager.AddComponentData(entity, new PoolTag());
                if (poolContainerMap.TryGetValue(typeName, out container))
                    entityManager.AddComponentData(entity,
                        new PoolContainerRef { Value = container });
                ActivateEnemy(entity, typeName, position, level);
                return entity;
            }

            Debug.LogError($"[EnemySpawnManager] No template available for: {typeName}");
            return Entity.Null;
        }

        /// <summary>
        /// 按概率随机抽取一个敌人生成数据
        /// </summary>
        private EnemySpawnData PickByProbability(List<EnemySpawnData> list,
            ref Unity.Mathematics.Random random)
        {
            if (list == null || list.Count == 0) return null;

            float totalProb = 0f;
            foreach (var d in list)
                totalProb += d.probability;

            float roll = random.NextFloat(0f, totalProb);
            float cumulative = 0f;
            foreach (var d in list)
            {
                cumulative += d.probability;
                if (roll <= cumulative)
                    return d;
            }

            return list[^1]; // fallback 最后一个
        }

        #region old

        /// <summary>
        /// 在地图边缘随机生成一个位置
        /// </summary>
        private float2 GetRandomEdgePosition(ref Unity.Mathematics.Random random)
        {
            float halfW = mapWorldSize.x * 0.5f;
            float halfH = mapWorldSize.y * 0.5f;
            float offset = spawnConfig ? spawnConfig.mapEdgeSpawnOffset : 1f;

            // 四条边等概率
            int edge = random.NextInt(0, 4);
            return edge switch
            {
                0 => new float2(random.NextFloat(-halfW + offset, halfW - offset), -halfH + offset), // 下
                1 => new float2(random.NextFloat(-halfW + offset, halfW - offset), halfH - offset), // 上
                2 => new float2(-halfW + offset, random.NextFloat(-halfH + offset, halfH - offset)), // 左
                _ => new float2(halfW - offset, random.NextFloat(-halfH + offset, halfH - offset)), // 右
            };
        }


        #endregion
       
        /// <summary>
        /// 在玩家周围 spawnRadius 半径圆边缘随机生成位置
        /// </summary>
        private float2 GetRandomPositionAroundPlayer(ref Unity.Mathematics.Random random)
        {
            float2 playerPos = float2.zero;
            if (GameManager.Instance.playerTrans)
            {
                var p = GameManager.Instance.playerTrans.position;
                playerPos = new float2(p.x, p.y);
            }

            float radius = spawnConfig ? spawnConfig.spawnRadius : 10f;
            float angle = random.NextFloat(0f, math.PI * 2f);
            float2 offset = new float2(math.cos(angle), math.sin(angle)) * radius;
            float2 pos = playerPos + offset;

            // Clamp 到地图范围内
            float halfW = mapWorldSize.x * 0.5f;
            float halfH = mapWorldSize.y * 0.5f;
            float edgeOffset = spawnConfig ? spawnConfig.mapEdgeSpawnOffset : 1f;
            pos.x = math.clamp(pos.x, -halfW + edgeOffset, halfW - edgeOffset);
            pos.y = math.clamp(pos.y, -halfH + edgeOffset, halfH - edgeOffset);

            return pos;
        }

        /// <summary>
        /// 计算当前等级
        /// </summary>
        private int GetCurrentLevel(EnemySpawnWave wave)
        {
            float elapsedInWave = currentGameTime - wave.beginTime * 60f; // 转为秒
            if (elapsedInWave <= 0f) return wave.beginLevel;
            int levelUps = Mathf.FloorToInt(elapsedInWave / (wave.levelUpInterval * 60f));
            return wave.beginLevel + levelUps * wave.levelUp;
        }

        private int QueryActiveEnemyCount()
        {
            using var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<EnemyActiveTag>());
            return query.CalculateEntityCount();
        }

        private void ActivateEnemy(Entity entity, string typeName, float2 position, int level)
        {
            if (!entityManager.HasComponent<EnemyActiveTag>(entity))
                entityManager.AddComponentData(entity, new EnemyActiveTag());

            entityManager.SetComponentData(entity, LocalTransform.FromPosition(
                new float3(position.x, position.y, 0f)));

            ApplyAttributes(entity, typeName, level);
        }

        private void ApplyAttributes(Entity entity, string typeName, int level)
        {
            if (configMap.TryGetValue(typeName, out var config))
            {
                entityManager.SetComponentData(entity, new EnemyStats
                {
                    health = config.baseHealth + config.levelUpHealth * (level - 1),
                    damage = config.baseDamage + config.levelUpDamage * (level - 1),
                });
                // 激活时更新金币掉落配置
                entityManager.SetComponentData(entity, new EnemyCoinFall
                {
                    minCoin = config.minFallCoin,
                    maxCoin = config.maxFallCoin,
                });
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            isSpawning = false;
            initializedTypes.Clear();
            configMap?.Clear();
            poolContainerMap.Clear();
        }
    }
}