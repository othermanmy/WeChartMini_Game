using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Game.Script.ECS.Components.Anim
{
    /// <summary>
    /// 动画事件标记
    /// </summary>
    public struct AnimationEvent : IComponentData
    {
        /// <summary>
        /// 非循环动画是否播放完毕
        /// </summary>
        public bool ClipFinished;

        /// <summary>
        /// 刚结束的动画名称
        /// </summary>
        public FixedString64Bytes FinishedClip;
        /// <summary>
        /// 下一个跳转的动画
        /// </summary>
        public FixedString64Bytes NextClip;
    }
}