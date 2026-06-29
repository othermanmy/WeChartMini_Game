using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Anim;
using Game.Script.ECS.Components.Enemy;
using Game.Script.Manager;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Script.ECS.Systems.Anim
{
    /// <summary>
    /// 渲染桥接系统：将 ECS SpriteRenderData 同步到 SpriteRenderer
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(TransformTweenSystem))]
    [UpdateAfter(typeof(AnimationFSMSystem))]
    public partial struct SpriteRenderBridgeSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = state.GetEntityQuery(
                ComponentType.ReadOnly<SpriteRenderData>(),
                ComponentType.ReadOnly<SpriteAnimationPlay>(),
                ComponentType.ReadOnly<EnemyActiveTag>()
            );
        }

        public void OnUpdate(ref SystemState state)
        {
            var spriteManager = SpriteManager.Instance;
            if (!spriteManager)
                return;

            var entities = _query.ToEntityArray(Allocator.TempJob);
            var renderDataArray = _query.ToComponentDataArray<SpriteRenderData>(Allocator.TempJob);

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                var data = renderDataArray[i];

                if (state.EntityManager.HasComponent<SpriteRenderer>(entity))
                {
                    var sr = state.EntityManager.GetComponentObject<SpriteRenderer>(entity);
                    if (!sr)
                        continue;

                    var spriteName = data.CurrentSpriteName.ToString();
                    if (spriteManager.TryGetSprite(spriteName, out var targetSprite))
                        if (sr.sprite != targetSprite) sr.sprite = targetSprite;
                    
                    // 同步颜色
                    var color = new Color(data.Color.x, data.Color.y, data.Color.z, data.Color.w);
                    if (sr.color != color)
                        sr.color = color;
                }
            }

            entities.Dispose();
            renderDataArray.Dispose();
        }

        public void OnDestroy(ref SystemState state) { }
    }
}
