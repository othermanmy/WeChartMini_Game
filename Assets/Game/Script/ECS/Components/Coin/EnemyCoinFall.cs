using Unity.Entities;

namespace Game.Script.ECS.Components.Coin
{
    public struct EnemyCoinFall:IComponentData
    {
        public int minCoin;
        public int maxCoin;
        
    }
}