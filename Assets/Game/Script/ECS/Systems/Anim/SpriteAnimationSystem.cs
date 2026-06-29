using Game.Script.ECS.Components;
using Game.Script.ECS.Components.Anim;
using Game.Script.ECS.Components.Enemy;
using Game.Script.Model;
using Game.Script.Tool;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace Game.Script.ECS.Systems.Anim
{
    [BurstCompile]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct SpriteAnimationSystem : ISystem
    {
        private const byte LOOP_ENABLED = 1;
        private const byte LOOP_DISABLED = 0;

        private EntityQuery query;
        private ComponentTypeHandle<SpriteAnimationPlay> playHandle;
        [ReadOnly] private ComponentTypeHandle<SpriteAnimationClips> clipsHandle;
        private ComponentTypeHandle<SpriteRenderData> renderDataHandle;
        [ReadOnly] private EntityTypeHandle entityHandle;
        [ReadOnly] private ComponentLookup<AnimationEvent> eventLookup;

        public void OnCreate(ref SystemState state)
        {
            query = state.GetEntityQuery(
                ComponentType.ReadWrite<SpriteAnimationPlay>(),
                ComponentType.ReadOnly<SpriteAnimationClips>(),
                ComponentType.ReadWrite<SpriteRenderData>(),
                ComponentType.ReadOnly<EnemyActiveTag>()
            );

            playHandle = state.GetComponentTypeHandle<SpriteAnimationPlay>();
            clipsHandle = state.GetComponentTypeHandle<SpriteAnimationClips>(true);
            renderDataHandle = state.GetComponentTypeHandle<SpriteRenderData>();
            entityHandle = state.GetEntityTypeHandle();
            eventLookup = state.GetComponentLookup<AnimationEvent>(true);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            playHandle.Update(ref state);
            clipsHandle.Update(ref state);
            renderDataHandle.Update(ref state);
            entityHandle.Update(ref state);
            eventLookup.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallelWriter = ecb.AsParallelWriter();

            var job = new SpriteAnimationJob
            {
                DeltaTime = dt,
                PlayHandle = playHandle,
                ClipsHandle = clipsHandle,
                RenderDataHandle = renderDataHandle,
                EntityHandle = entityHandle,
                EventLookup = eventLookup,
                Ecb = ecbParallelWriter
            };

            state.Dependency = job.ScheduleParallel(query, state.Dependency);
            state.CompleteDependency();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private struct SpriteAnimationJob : IJobChunk
        {
            public float DeltaTime;

            public ComponentTypeHandle<SpriteAnimationPlay> PlayHandle;
            [ReadOnly] public ComponentTypeHandle<SpriteAnimationClips> ClipsHandle;
            public ComponentTypeHandle<SpriteRenderData> RenderDataHandle;
            [ReadOnly] public EntityTypeHandle EntityHandle;

            [ReadOnly] public ComponentLookup<AnimationEvent> EventLookup;
            public EntityCommandBuffer.ParallelWriter Ecb;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities = chunk.GetNativeArray(EntityHandle);
                var plays = chunk.GetNativeArray(ref PlayHandle);
                var clipsArray = chunk.GetNativeArray(ref ClipsHandle);
                var renderData = chunk.GetNativeArray(ref RenderDataHandle);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var play = plays[i];
                    if (!play.IsPlaying || play.IsPaused)
                        continue;

                    var clipsData = clipsArray[i];
                    if (!clipsData.Clips.IsCreated)
                        continue;
                    ref var blob = ref clipsData.Clips.Value;
                    if (blob.Clips.Length == 0)
                        continue;

                    var clipIndex = FindClipIndex(ref blob, play.ClipName);
                    if (clipIndex < 0)
                        continue;

                    ref var clip = ref blob.Clips[clipIndex];
                    if (clip.Frames.Length == 0)
                        continue;

                    // 推进计时器
                    play.Timer += DeltaTime * play.Speed;
                    
                    ref var currentFrame = ref clip.Frames[play.FrameIndex];
                    float duration = currentFrame.Duration;
                    int maxSteps = clip.Frames.Length;

                    for (int step = 0;
                         step < maxSteps && play.Timer >= duration && duration > 0;
                         step++)
                    {
                        play.Timer -= duration;
                        play.FrameIndex++;
                        maxSteps--; // 防止越界

                        if (play.FrameIndex >= clip.Frames.Length)
                        {
                            if (clip.Loop == LOOP_ENABLED)
                            {
                                play.FrameIndex = 0;
                            }
                            else
                            {
                                play.FrameIndex = clip.Frames.Length - 1;
                                play.IsPlaying = false;
                                
                                var entity = entities[i];
                                if (!EventLookup.HasComponent(entity))
                                {
                                    Ecb.AddComponent(unfilteredChunkIndex, entity, new AnimationEvent
                                    {
                                        ClipFinished = true,
                                        FinishedClip = play.ClipName
                                    });
                                }
                                break;
                            }
                        }

                        // 取下一帧时长
                        duration = clip.Frames[play.FrameIndex].Duration;
                    }
                    
                    ref var frame =ref clip.Frames[play.FrameIndex];
                    var currentRenderData = renderData[i];
                    frame.SpriteName.CopyTo(ref currentRenderData.CurrentSpriteName);
                    // 颜色保持原样不变
                    plays[i] = play;
                    renderData[i] = currentRenderData;
                }
            }

            private static int FindClipIndex(ref SpriteClipBlob blob, FixedString64Bytes name)
            {
                for (int i = 0; i < blob.Clips.Length; i++)
                {
                    if (BlobStringTool.BsEqualFs(ref blob.Clips[i].Name, name))
                        return i;
                }
                return -1;
            }
        
        }

        public void OnDestroy(ref SystemState state) { }
    }
}