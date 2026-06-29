using Unity.Collections;
using Unity.Entities;

namespace Game.Script.ECS.Components.Anim
{
    /// <summary>
    /// 动画状态机状态
    /// </summary>
    public struct AnimState : IComponentData
    {
        /// <summary>
        /// 当前动画名称,与 SpriteClip.Name 对应
        /// </summary>
        public FixedString64Bytes State;
    }
}