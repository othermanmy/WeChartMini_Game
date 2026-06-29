using Game.Script.ECS.Components.Anim;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;

namespace Game.Script.ECS.Systems.Anim
{
    /// <summary>
    /// 动画状态机驱动系统
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(SpriteAnimationSystem))]
    public partial struct AnimationFSMSystem : ISystem
    {
        private EntityQuery stateQuery;
        private EntityQuery eventQuery;

        private ComponentTypeHandle<AnimState> stateHandle;
        private ComponentTypeHandle<SpriteAnimationPlay> playHandle;

        [ReadOnly] private ComponentTypeHandle<AnimationEvent> eventHandle;
        [ReadOnly] private ComponentTypeHandle<SpriteAnimationClips> clipsHandle;
        private ComponentTypeHandle<SpriteAnimationPlay> playForEventHandle;
        [ReadOnly] private EntityTypeHandle entityHandle;
        private ComponentTypeHandle<AnimState> animStateForEventHandle;

        public void OnCreate(ref SystemState state)
        {
            stateQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AnimState>(),
                ComponentType.ReadWrite<SpriteAnimationPlay>()
            );

            eventQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<AnimationEvent>(),
                ComponentType.ReadOnly<SpriteAnimationClips>(),
                ComponentType.ReadWrite<SpriteAnimationPlay>(),
                ComponentType.ReadWrite<AnimState>()
            );

            stateHandle = state.GetComponentTypeHandle<AnimState>(true);
            playHandle = state.GetComponentTypeHandle<SpriteAnimationPlay>();

            eventHandle = state.GetComponentTypeHandle<AnimationEvent>(true);
            clipsHandle = state.GetComponentTypeHandle<SpriteAnimationClips>(true);
            playForEventHandle = state.GetComponentTypeHandle<SpriteAnimationPlay>();
            entityHandle = state.GetEntityTypeHandle();
            animStateForEventHandle = state.GetComponentTypeHandle<AnimState>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            stateHandle.Update(ref state);
            playHandle.Update(ref state);

            eventHandle.Update(ref state);
            clipsHandle.Update(ref state);
            playForEventHandle.Update(ref state);
            entityHandle.Update(ref state);
            animStateForEventHandle.Update(ref state);

            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbParallelWriter = ecb.AsParallelWriter();

            //处理状态变更
            var stateChangeJob = new AnimationStateChangeJob
            {
                StateHandle = stateHandle,
                PlayHandle = playHandle
            };

            // 处理动画完成事件
            var eventJob = new AnimationEventJob
            {
                EventHandle = eventHandle,
                ClipsHandle = clipsHandle,
                PlayHandle = playForEventHandle,
                EntityHandle = entityHandle,
                AnimStateHandle = animStateForEventHandle,
                Ecb = ecbParallelWriter
            };

            var jobHandle = stateChangeJob.ScheduleParallel(stateQuery, state.Dependency);
            jobHandle = eventJob.ScheduleParallel(eventQuery, jobHandle);

            state.Dependency = jobHandle;
            state.CompleteDependency();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state) { }
    }

    [BurstCompile]
    internal struct AnimationStateChangeJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<AnimState> StateHandle;
        public ComponentTypeHandle<SpriteAnimationPlay> PlayHandle;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var states = chunk.GetNativeArray(ref StateHandle);
            var plays = chunk.GetNativeArray(ref PlayHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var newStateName = states[i].State;
                var play = plays[i];

                // 如果状态名变了，切换动画
                if (!play.ClipName.Equals(newStateName))
                {
                    play.ClipName = newStateName;
                    play.FrameIndex = 0;
                    play.Timer = 0f;
                    play.Speed = 1f;
                    play.IsPlaying = true;
                    play.IsPaused = false;

                    plays[i] = play;
                }
            }
        }
    }

    [BurstCompile]
    internal struct AnimationEventJob : IJobChunk
    {
        [ReadOnly] public ComponentTypeHandle<AnimationEvent> EventHandle;
        [ReadOnly] public ComponentTypeHandle<SpriteAnimationClips> ClipsHandle;
        public ComponentTypeHandle<SpriteAnimationPlay> PlayHandle;
        [ReadOnly] public EntityTypeHandle EntityHandle;
        public ComponentTypeHandle<AnimState> AnimStateHandle;

        public EntityCommandBuffer.ParallelWriter Ecb;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var entities = chunk.GetNativeArray(EntityHandle);
            var events = chunk.GetNativeArray(ref EventHandle);
            var clipsArray = chunk.GetNativeArray(ref ClipsHandle);
            var plays = chunk.GetNativeArray(ref PlayHandle);
            var animStates = chunk.GetNativeArray(ref AnimStateHandle);

            for (int i = 0; i < chunk.Count; i++)
            {
                var evt = events[i];
                if (!evt.ClipFinished)
                    continue;

                var clipsData = clipsArray[i];
                if (!clipsData.Clips.IsCreated)
                    continue;

                ref var blob = ref clipsData.Clips.Value;
                if (blob.Clips.Length == 0)
                    continue;

                // 优先使用 NextClip，否则回退到 Clips[0]（默认动画）
                FixedString64Bytes nextName = default;
                if (evt.NextClip.Length > 0)
                {
                    nextName = evt.NextClip;
                }
                else
                {
                    ref var defaultBlobName = ref blob.Clips[0].Name;
                    var copyError = defaultBlobName.CopyTo(ref nextName);
                    if (copyError == ConversionError.Overflow)
                        continue;
                }

                var play = plays[i];
                if (!play.ClipName.Equals(nextName))
                {
                    play.ClipName = nextName;
                    play.FrameIndex = 0;
                    play.Timer = 0f;
                    play.Speed = 1f;
                    play.IsPlaying = true;
                    play.IsPaused = false;
                    plays[i] = play;
                }

                // 同步写回 AnimState，防止 AnimationStateChangeJob 再次覆盖
                var animState = animStates[i];
                animState.State = nextName;
                animStates[i] = animState;

                // 移除事件组件
                Ecb.RemoveComponent<AnimationEvent>(unfilteredChunkIndex, entities[i]);
            }
        }
    }
}