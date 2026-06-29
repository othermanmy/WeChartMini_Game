using Unity.Entities;

namespace Game.Script.ECS.Components.Coin
{
    /// <summary>
    /// 金币全局配置
    /// </summary>
    public struct CoinConfig:ISharedComponentData
    {
        /// <summary>金币存活时间（秒）</summary>
        public float LifeTime;
        /// <summary>实际拾取到玩家的距离</summary>
        public float CollectRadius;
        /// <summary>磁吸移动速度</summary>
        public float PullSpeed;
    }
}