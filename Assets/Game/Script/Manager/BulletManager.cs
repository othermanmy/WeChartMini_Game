using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Script.Architecture;
using Game.Script.Model.Bullets;
using QFramework;
using Game.Script.ECS.Components;
using Game.Script.Model;
using Game.Script.Model.Bullets.Trait;
using Game.Script.Tool;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Script.Manager
{
    public class BulletManager:AbstractSystem
    {
        public const int max_Bullet_Count = 1000;
        
        private AssetWrapper<GameObject> bulletPrefab;
    
        public List<IBulletTrait> bulletTraits=new();//子弹特性列表

        public event Action<IBulletTrait> onAddTrait;//添加特性事件

        private List<BulletBase> activeBullets=new(max_Bullet_Count);
        //子弹池
        private ObjectPool<BulletBase> bulletPool;

        private World ecsWorld;
        private EntityQuery spatialHashQuery;
        private EntityQuery flowFieldQuery;
        
        private PlayerModel playerModel;
        
        protected override void OnInit()
        {
            Init().Forget();
        }

        private async UniTask Init()
        {
            var i = GameApp.Interface;
            playerModel = i.GetModel<PlayerModel>();
          
            bulletPrefab=await GameAssetManager.Instance.LoadAssetAsync<GameObject>(ResPrefix.Bullets + "NormalBullet");
            bulletPool = new(CreateFunc, OnGet, OnRelease, 
                null, true,10,max_Bullet_Count);

            ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (ecsWorld != null && ecsWorld.IsCreated)
            {
                spatialHashQuery = ecsWorld.EntityManager.CreateEntityQuery(
                    typeof(SpatialHashData));
                flowFieldQuery = ecsWorld.EntityManager.CreateEntityQuery(
                    typeof(FlowFieldData));
            }
            
            AddTrait(new TakeDamageTrait());
            AddTrait(new CollideDestroyTrait());
            AddTrait(new KnockBackTrait());
        }

        private BulletBase CreateFunc()
        {
            var gm=UnityEngine.Object.Instantiate(bulletPrefab.Asset);
            var bb = gm.GetComponent<BulletBase>();
            return bb;
        }

        private void OnGet(BulletBase bullet)
        {
            bullet.gameObject.SetActive(true);
            bullet.ClearAndStopTrail();
            activeBullets.Add(bullet);
        }
        private void OnRelease(BulletBase bullet)
        {
            bullet.ClearAndStopTrail();
            bullet.gameObject.SetActive(false);
            activeBullets.Remove(bullet);
        }

        public void AddTrait(IBulletTrait bulletTrait)
        {
            // 校验是否已存在同一类型的特性
            var traitType = bulletTrait.GetType();
            foreach (var t in bulletTraits)
            {
                if (t.GetType() == traitType)
                    return;
            }
            
            bulletTraits.Add(bulletTrait);
            bulletTraits.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            onAddTrait?.Invoke(bulletTrait);
        }

        public bool RemoveTrait<T>() where T : IBulletTrait
        {
            var targetType = typeof(T);
            for (int i = 0; i < bulletTraits.Count; i++)
                if (bulletTraits[i].GetType() == targetType)
                {
                    var b = bulletTraits[i];
                    bulletTraits.RemoveAt(i);
                    return true;
                }
            
            return false;
        }

        public void FireBullet(Vector2 pos, Vector2 dir)
        {
            var bullet = bulletPool.Get();
            if (!bullet) return; // 数量达到上限
            bullet.Init(playerModel, bulletTraits, pos, dir);
            bullet.trans.position = pos;
            bullet.ResumeTrail();
        }

        public void Update(float dt)
        {
            NativeParallelMultiHashMap<int, EnemyHashEntry> hashGrid = default;
            float cellSize = 0f;
            bool hasECSData = false;

            if (ecsWorld != null && ecsWorld.IsCreated)
            {
                var spatialHashData = spatialHashQuery.GetSingleton<SpatialHashData>();
                if (spatialHashData.IsCreated)
                {
                    hashGrid = spatialHashData.HashGrid;
                    var flowFieldData = flowFieldQuery.GetSingleton<FlowFieldData>();
                    cellSize = flowFieldData.CellSize;
                    hasECSData = true;
                }
            }

            for(int i=activeBullets.Count-1; i>=0; i--)
            {
                foreach (var trait in bulletTraits)
                    trait.OnUpdate(activeBullets[i], dt);

                activeBullets[i].currentLifeTime.Value -= dt;
                if (activeBullets[i].currentLifeTime.Value <= 0)
                {
                   BulletRelease(activeBullets[i]);
                    continue;
                }
                activeBullets[i].position += activeBullets[i].currentSpeed.Value 
                                             * dt * activeBullets[i].direction;
                //分离碰撞检测
                bool shouldDestroy = false;
                if (hasECSData)
                {
                    var bullet = activeBullets[i];
                    
                    int gx = (int)math.floor(bullet.position.x / cellSize);
                    int gy = (int)math.floor(bullet.position.y / cellSize);
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int hash = (int)math.hash(new int2(gx + dx, gy + dy));
                            if (!hashGrid.TryGetFirstValue(hash, out var entry, out var it))
                                continue;
                            do
                            {
                                float distSq = math.distancesq(bullet.position, entry.Position);
                                float hitDist = bullet.currentRadius.Value + entry.Radius;
                                if (distSq < hitDist * hitDist)
                                {
                                    //Debug.Log($"Bullet Hit Entity:{entry.Entity.Index}");
                                    foreach (var trait in bulletTraits)
                                    {
                                        shouldDestroy = trait.OnHit(bullet, entry.Entity, ecsWorld.EntityManager);
                                        if (shouldDestroy) break;
                                    }
                                }

                                if (shouldDestroy) break;
                            } while (hashGrid.TryGetNextValue(out entry, ref it));

                            if (shouldDestroy) break;
                        }

                        if (shouldDestroy) break;
                    }
                }

                if (shouldDestroy)
                {
                    BulletRelease(activeBullets[i]);
                    continue;
                }
                
                activeBullets[i].trans.position = activeBullets[i].position;
            }
            //Debug.Log($"当前子弹数量：{activeBullets.Count}");
        }
        
        private void BulletRelease(BulletBase bullet)
        {
            foreach(var trait in bulletTraits)
                trait.OnRelease(bullet);
            bulletPool.Release(bullet);
        }
    }
}