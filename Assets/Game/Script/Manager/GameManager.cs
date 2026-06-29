using Boot.Script;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Game.Script.Architecture;
using Game.Script.ECS.Components;
using Game.Script.ECS.Systems;
using Game.Script.Model;
using Game.Script.Ui;
using QFramework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Script.Manager
{
    public class GameManager:MonoSingleton<GameManager>
    {
        public Transform playerTrans { get; private set; }
        [SerializeField]
        private SubScene enemySub;
        
        private MapManager mapManager;
        private PathFindingManager pathFindingManager;
        private BulletManager bulletManager;
        private EnemySpawnManager enemySpawnManager;
        private AbilityManager abilityManager;
        
        private CinemachineVirtualCamera playerCamera;
        public bool IsGameStart { get;private set; }

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            IsPaused = true;
            Time.timeScale = 0;
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            IsPaused = false;
            Time.timeScale = 1;
        }
        
        
        // ECS 系统相关
        private World ecsWorld;
        public Entity wordEntity { get; private set; }//单例

        //一局游戏的总时常（秒）
        public float totalGameTime=10*60;
        
        //游戏计时
        public BindableProperty<float> currentGameTime=new();
        //游戏倒计时
        public BindableProperty<float> currentCountDown=new();
        
        private void Awake()
        {
            //开启enter面板
            UiManager.Instance.OpenAsync(nameof(EnterPanel), nameof(EnterPanel)).Forget();
        }

        private void Start()
        { 
           InitManager().Forget();
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            pathFindingManager?.Dispose();
        }

        private void Update()
        {
            if(!IsGameStart || IsPaused) return;

           
            // 玩家位置
            float3 playerPos = float3.zero;
            if (playerTrans)
                playerPos = playerTrans.position;
            
            // 更新地图
            mapManager?.UpdateMap(playerPos);

            // 更新流场寻路
            pathFindingManager?.Update(Time.deltaTime, playerPos);
            
            // 同步流场数据到 ECS（更新玩家位置）
            if (ecsWorld != null && ecsWorld.IsCreated && ecsWorld.EntityManager.Exists(wordEntity))
            {
                var flowFieldData = ecsWorld.EntityManager.GetComponentData<FlowFieldData>(wordEntity);
                flowFieldData.PlayerPosition = new float2(playerPos.x, playerPos.y);
                ecsWorld.EntityManager.SetComponentData(wordEntity, flowFieldData);
            }
            bulletManager?.Update(Time.deltaTime);
            abilityManager?.Update(Time.deltaTime);
            enemySpawnManager?.Update(currentGameTime.Value,Time.deltaTime);
            CountTime(Time.deltaTime);
        }


        private async UniTask InitManager()
        {
            var t= GameApp.Interface;
            var tilemap=GameObject.FindGameObjectWithTag("TileMap").GetComponent<Tilemap>();
            
            // 加载地图配置
            var mapConfig = await YooAssetManager.Instance.LoadAssetAsync<MapConfig>
                (ResPrefix.Data+"MapConfig");
            mapManager = new MapManager(mapConfig.Asset, tilemap);
            
            // 初始化流场寻路管理器
            pathFindingManager = new PathFindingManager(mapManager);
            
            // 设置 ECS EnemyMoveSystem 的引用
            EnemyMoveSystem.SetPathFindingManager(pathFindingManager);
            
            //子弹管理器
            while (true)
            {
                bulletManager = t.GetSystem<BulletManager>();
                abilityManager=t.GetSystem<AbilityManager>();
                if (bulletManager != null&&abilityManager!=null) break;
                await UniTask.Delay(10, ignoreTimeScale: true);
            }
            //敌人生成管理器
            enemySpawnManager = EnemySpawnManager.Instance;
            await enemySpawnManager.InitAsync(enemySub);

            // 注入地图世界尺寸到 EnemySpawnManager
            var tileSize = mapManager.map.layoutGrid.cellSize;
            int totalChunks = mapConfig.Asset.activeChunkRadius * 2 + 1;
            float worldWidth = totalChunks * mapConfig.Asset.chunkSize * tileSize.x;
            float worldHeight = totalChunks * mapConfig.Asset.chunkSize * tileSize.y;
            enemySpawnManager.SetMapWorldSize(new float2(worldWidth, worldHeight));
            
          
        }

        /// <summary>
        /// 初始化 ECS 流场单例实体
        /// </summary>
        private void InitFlowFieldEntity()
        {
            ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (ecsWorld == null || !ecsWorld.IsCreated)
                return;

            var entityManager = ecsWorld.EntityManager;
            wordEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(wordEntity, new FlowFieldData
            {
                GridOrigin = float2.zero,
                CellSize = 1f,
                GridWidth = 0,
                GridHeight = 0,
                IsValid = 0,
                PlayerPosition = float2.zero
            });
            entityManager.AddComponentData(wordEntity, new PhysicsStep
            {
                SimulationType   = SimulationType.UnityPhysics,
                Gravity          = float3.zero,          
                SolverIterationCount = 4,               
                SynchronizeCollisionWorld = 0 ,      
            });
        }

        public async UniTask BeginGame()
        {
            // 重置 PlayerModel 属性
            GameApp.Interface.GetModel<PlayerModel>().ReSetAllValue();

            // 判空重建 mapManager / pathFindingManager
            if (mapManager == null)
                await InitManager();
            else if (pathFindingManager == null)
            {
                pathFindingManager = new PathFindingManager(mapManager);
                EnemyMoveSystem.SetPathFindingManager(pathFindingManager);
            }

            //计时器
            currentCountDown.Value = totalGameTime;
            if (!playerCamera)
                playerCamera=GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<CinemachineVirtualCamera>();
            await UiManager.Instance.OpenAsync(nameof(ControlPanel), nameof(ControlPanel));
         var panel=await UiManager.Instance.OpenAsync(nameof(PlayerHubPanel), nameof(PlayerHubPanel)) as PlayerHubPanel;
            //注册计时事件  
            if (panel) currentCountDown.RegisterWithInitValue(panel.TimeCountDown).
                UnRegisterWhenGameObjectDestroyed(panel.gameObject);
            // 初始化 ECS 流场单例实体
            InitFlowFieldEntity();
            //加载生成player
            var player=await YooAssetManager.Instance.InstantiateAsync(ResPrefix.Player+"Player");
            if(player)
            {
                playerTrans = player.transform;
            }
            if(playerCamera)
                playerCamera.Follow = playerTrans;
            await enemySpawnManager.BeginSpawn();
            IsGameStart = true;
        }

        public void EndGame()
        {
            UiManager.Instance.CloseAsync(nameof(ControlPanel)).Forget();
            UiManager.Instance.CloseAsync(nameof(PlayerHubPanel)).Forget();
            IsGameStart = false;
            IsPaused = false;
            Time.timeScale = 1;
            
            mapManager?.Dispose();
            mapManager = null;
            pathFindingManager?.Dispose();
            pathFindingManager = null;
            //销毁玩家
            Destroy(playerTrans.gameObject);
            // 销毁 ECS 流场实体
            if (ecsWorld != null && ecsWorld.IsCreated && ecsWorld.EntityManager.Exists(wordEntity))
            {
                ecsWorld.EntityManager.DestroyEntity(wordEntity);
            }
            enemySpawnManager?.EndSpawn();
            playerTrans = null;
            EndCountTime();
        }


        private void CountTime(float dt)
        {
            currentGameTime.Value += dt;
            currentCountDown.Value = totalGameTime - currentGameTime.Value;
            if (currentGameTime.Value >= totalGameTime)
            {
                EndGame();
            }
        }

        private void EndCountTime()
        {
            currentGameTime.Value = 0f;
            currentCountDown.Value = totalGameTime;
        }
    }
}
