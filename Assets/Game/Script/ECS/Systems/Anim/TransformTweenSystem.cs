using Game.Script.ECS.Components.Anim;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Script.ECS.Systems.Anim
{
    /// <summary>
    /// Transform 缓动动画系统（纯 ECS，IJobChunk 并行）
    /// 插值 LocalTransform 的位置/缩放 + SpriteRenderData 颜色
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(SpriteAnimationSystem))]
    public partial struct TransformTweenSystem : ISystem
    {
        private EntityQuery query;
        private ComponentTypeHandle<TransformTween> tweenHandle;
        private ComponentTypeHandle<LocalTransform> transformHandle;
        private ComponentTypeHandle<SpriteRenderData> renderDataHandle;
        private EntityTypeHandle entityHandle;

        public void OnCreate(ref SystemState state)
        {
            query = state.GetEntityQuery(
                ComponentType.ReadWrite<TransformTween>(),
                ComponentType.ReadWrite<LocalTransform>(),
                ComponentType.ReadWrite<SpriteRenderData>()
            );

            tweenHandle = state.GetComponentTypeHandle<TransformTween>();
            transformHandle = state.GetComponentTypeHandle<LocalTransform>();
            renderDataHandle = state.GetComponentTypeHandle<SpriteRenderData>();
            entityHandle = state.GetEntityTypeHandle();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            tweenHandle.Update(ref state);
            transformHandle.Update(ref state);
            renderDataHandle.Update(ref state);
            entityHandle.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbWriter = ecb.AsParallelWriter();

            var job = new TweenJob
            {
                DeltaTime = dt,
                TweenHandle = tweenHandle,
                TransformHandle = transformHandle,
                RenderDataHandle = renderDataHandle,
                EntityHandle = entityHandle,
                Ecb = ecbWriter
            };

            state.Dependency = job.ScheduleParallel(query, state.Dependency);
            state.CompleteDependency();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private struct TweenJob : IJobChunk
        {
            public float DeltaTime;

            public ComponentTypeHandle<TransformTween> TweenHandle;
            public ComponentTypeHandle<LocalTransform> TransformHandle;
            public ComponentTypeHandle<SpriteRenderData> RenderDataHandle;
            [ReadOnly] public EntityTypeHandle EntityHandle;

            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(EntityHandle);
                var tweens = chunk.GetNativeArray(ref TweenHandle);
                var transforms = chunk.GetNativeArray(ref TransformHandle);
                var renderDatas = chunk.GetNativeArray(ref RenderDataHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var tween = tweens[i];
                    
                    if (tween.Duration <= 0)
                    {
                        if (tween.AutoDestroy != 0)
                        {
                            var entity = entities[i];
                            if (tween.AutoDestroy == 1)
                                Ecb.DestroyEntity(unfilteredChunkIndex, entity);
                            else
                                Ecb.RemoveComponent<TransformTween>(unfilteredChunkIndex, entity);
                        }
                        continue;
                    }

                    tween.Elapsed += DeltaTime;
                    float t = math.clamp(tween.Elapsed / tween.Duration, 0f, 1f);
                    float easedT = Ease(t, tween.EaseType);

                    // 直接修改 chunk 内 NativeArray（无需回写）
                    var trans = transforms[i];
                    trans.Position = math.lerp(tween.StartPos, tween.EndPos, easedT);
                    trans.Scale = math.lerp(tween.StartScale, tween.EndScale, easedT);
                    transforms[i] = trans;

                    // 颜色写入 ECS SpriteRenderData
                    var rd = renderDatas[i];
                    rd.Color = math.lerp(tween.StartColor, tween.EndColor, easedT);
                    renderDatas[i] = rd;

                    tweens[i] = tween;

                    // 动画结束 → 移除组件
                    if (tween.Elapsed >= tween.Duration && tween.AutoDestroy != 0)
                    {
                        var entity = entities[i];
                        if (tween.AutoDestroy == 1)
                            Ecb.DestroyEntity(unfilteredChunkIndex, entity);
                        else
                            Ecb.RemoveComponent<TransformTween>(unfilteredChunkIndex, entity);
                    }
                }
            }

            private static float Ease(float t, byte easeType)
            {
                switch (easeType)
                {
                    case 0: return t;                                // Linear
                    case 1: return t * t;                            // EaseIn
                    case 2: return t * (2f - t);                     // EaseOut
                    case 3: return t * t * (3f - 2f * t);            // EaseInOut
                    default: return t;
                }
            }
            
        }

        public void OnDestroy(ref SystemState state) { }
    }
}