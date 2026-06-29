using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Script.ECS.Components.Anim
{
    /// <summary>
    /// 托管组件：持有 Sprite 名称 → Sprite 对象的查找表
    /// 每个使用 SpriteAnimationAuthoring 的 entity 会挂载一个
    /// Key 使用 string 而非 FixedString64Bytes，因为后者无法被托管序列化系统正确反序列化
    /// </summary>
    public class SpriteAtlasAsset : IComponentData
    {
        public Dictionary<string, Sprite> SpriteMap;
    }
}
