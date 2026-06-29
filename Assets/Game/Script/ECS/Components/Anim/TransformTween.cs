using Unity.Entities;
using Unity.Mathematics;

namespace Game.Script.ECS.Components.Anim
{
    /// <summary>
    /// Transform 缓动动画组件
    /// </summary>
    public struct TransformTween : IComponentData
    {
        /// <summary>
        /// 动画总时长（秒）
        /// </summary>
        public float Duration;

        /// <summary>
        /// 已过时间（秒）
        /// </summary>
        public float Elapsed;

        // 位置 
        public float3 StartPos;
        public float3 EndPos;

        // 缩放 
        public float StartScale;
        public float EndScale;

        // 颜色
        public float4 StartColor;
        public float4 EndColor;

        //  控制 
        /// <summary>
        /// 缓动类型：0=Linear, 1=EaseIn, 2=EaseOut, 3=EaseInOut
        /// </summary>
        public byte EaseType;

        /// <summary>
        /// 完成后是否自动移除组件（1=是, 0=否）
        /// </summary>
        public byte AutoDestroy;
    }
}