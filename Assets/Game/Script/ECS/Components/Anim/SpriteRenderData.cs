using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Script.ECS.Components.Anim
{
    
    public struct SpriteRenderData : IComponentData
    {
        /// <summary>
        /// 当前显示精灵名称
        /// </summary>
        public FixedString64Bytes CurrentSpriteName;

        /// <summary>
        /// 颜色 (RGBA)
        /// </summary>
        public float4 Color;
    }
}
