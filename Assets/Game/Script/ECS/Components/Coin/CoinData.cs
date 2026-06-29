using Unity.Entities;

namespace Game.Script.ECS.Components.Coin
{
    /// <summary>
    /// 金币数据组件：数量、存活时间、磁吸状态
    /// </summary>
    public struct CoinData : IComponentData
    {
        public int count;
        public float timer;
        public bool isBeingPulled;
    }
}