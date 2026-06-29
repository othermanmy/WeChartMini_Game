using Unity.Entities;

namespace Game.Script.ECS.Components.Coin
{
    /// <summary>
    /// 金币池单例：持有模板引用 
    /// </summary>
    public struct CoinPoolSingleton : IComponentData
    {
        public Entity templateEntity;
        public float LifeTime;
        public float CollectRadius;
        public float PullSpeed;
    }
}