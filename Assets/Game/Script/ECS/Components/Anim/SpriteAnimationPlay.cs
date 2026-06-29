using Unity.Collections;
using Unity.Entities;

namespace Game.Script.ECS.Components.Anim
{
    /// <summary>
    /// 帧动画播放状态
    /// </summary>
    public struct SpriteAnimationPlay : IComponentData
    {
        /// <summary>
        /// 当前播放的动画名称（与 SpriteClip.Name 对应）
        /// </summary>
        public FixedString64Bytes ClipName;

        /// <summary>
        /// 当前帧索引
        /// </summary>
        public int FrameIndex;

        /// <summary>
        /// 帧内计时器（秒）
        /// </summary>
        public float Timer;

        /// <summary>
        /// 播放速度倍率
        /// </summary>
        public float Speed;

        /// <summary>
        /// 是否播放中
        /// </summary>
        public bool IsPlaying;

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused;
    }
}