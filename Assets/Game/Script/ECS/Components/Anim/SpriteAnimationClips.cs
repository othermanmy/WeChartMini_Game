using Game.Script.Model;
using Unity.Entities;

namespace Game.Script.ECS.Components.Anim
{
    /// <summary>
    /// 共享动画数据
    /// </summary>
    public struct SpriteAnimationClips : IComponentData
    {
        public BlobAssetReference<SpriteClipBlob> Clips;
    }
}
