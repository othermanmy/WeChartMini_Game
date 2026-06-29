using Game.Script.ECS.Components.Coin;
using Unity.Entities;
using UnityEngine;

namespace Game.Script.Model.Authoring
{
    public class CoinAuthoring:MonoBehaviour
    {
        
        public float LifeTime=15f;
        public float CollectRadius=1f;
        public float PullSpeed=8f;

        private class CoinBaker : Baker<CoinAuthoring>
        {
            public override void Bake(CoinAuthoring authoring)
            {
                var entity=GetEntity(TransformUsageFlags.Dynamic);
                AddSharedComponent(entity,new CoinConfig
                {
                    LifeTime=authoring.LifeTime,
                    CollectRadius=authoring.CollectRadius,
                    PullSpeed=authoring.PullSpeed
                });
                AddComponent(entity,new CoinTemplate());
                AddComponent(entity,new CoinData());
                AddComponent(entity,new CoinTag());
            }
        }
    }
}