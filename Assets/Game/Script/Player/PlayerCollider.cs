using System;
using Cysharp.Threading.Tasks;
using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Enemy;
using Game.Script.Model;
using Game.Script.Ui;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Script.Player
{
    public class PlayerCollider:MonoBehaviour
    {
        private PlayerModel playerModel;
        private Transform trans;
        private bool isInit;
        [SerializeField] 
        private bool showColliderRange;
        
        public float hitRadius;
        public float2 centerOffset; 
        
        // ECS 碰撞检测
        private World ecsWorld;
        private EntityQuery spatialHashQuery;
        private EntityQuery flowFieldQuery;

     
        public void Init(PlayerModel model)
        {
            if (isInit) return;
            trans = transform;
            playerModel=model;
            hitRadius = model.hitRadius.Value;
            centerOffset = model.centerOffset;
            ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (ecsWorld != null && ecsWorld.IsCreated)
            {
                spatialHashQuery = ecsWorld.EntityManager.CreateEntityQuery(
                    typeof(SpatialHashData));
                flowFieldQuery = ecsWorld.EntityManager.CreateEntityQuery(
                    typeof(FlowFieldData));
            }
            isInit=true;
        }
        
        private void Update()
        {
            if(!isInit)return;
            CheckCollision();
        }

        private void CheckCollision()
        {
            if (ecsWorld == null || !ecsWorld.IsCreated||playerModel.isInvincible.Value)
                return;

            var spatialHashData = spatialHashQuery.GetSingleton<SpatialHashData>();
            if (!spatialHashData.IsCreated)
                return;

            var hashGrid = spatialHashData.HashGrid;
            var flowFieldData = flowFieldQuery.GetSingleton<FlowFieldData>();
            float cellSize = flowFieldData.CellSize;

            float2 playerPos = new float2(trans.position.x + centerOffset.x, 
                                           trans.position.y + centerOffset.y);

            int gx = (int)math.floor(playerPos.x / cellSize);
            int gy = (int)math.floor(playerPos.y / cellSize);

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int hash = (int)math.hash(new int2(gx + dx, gy + dy));
                    if (!hashGrid.TryGetFirstValue(hash, out var entry, out var it))
                        continue;

                    do
                    {
                        float distSq = math.distancesq(playerPos, entry.Position);
                        float hitDist = hitRadius + entry.Radius;
                        if (distSq < hitDist * hitDist)
                        {
                            OnPlayerHit(entry);
                            return;
                        }
                    }
                    while (hashGrid.TryGetNextValue(out entry, ref it));
                }
            
        }

        private void OnPlayerHit(EnemyHashEntry entry)
        {
            var em = ecsWorld.EntityManager;
            if(!em.HasComponent<EnemyStats>(entry.Entity))return;
            var es=em.GetComponentData<EnemyStats>(entry.Entity);
            playerModel.OnHurt?.Invoke(es.damage, entry.Entity);
           DamageDumpPanel.DumpAsync(trans.position, playerModel.currentDamage.Value, Color.red).Forget();
        }
        
        private void OnDrawGizmosSelected()
        {
            if(!showColliderRange)return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position+new Vector3(centerOffset.x,centerOffset.y)
                , hitRadius);
        }
    }
}