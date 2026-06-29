using Game.Script.ECS.Components.Anim;
using Game.Script.Manager;
using Unity.Collections;
using Unity.Entities;

namespace Game.Script.ECS.Systems.Anim
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
   // [UpdateBefore(typeof(SpriteRenderBridgeSystem))]
    public partial struct SpriteRegisterSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = state.GetEntityQuery(typeof(SpriteRegisterData));
        }

        public void OnUpdate(ref SystemState state)
        {
            var spriteManager = SpriteManager.Instance;
            if (!spriteManager)
                return;

            var entities = _query.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0)
            {
                entities.Dispose();
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var entity in entities)
            {
                if (state.EntityManager.HasComponent<SpriteRegisterData>(entity))
                {
                    var data = state.EntityManager.GetComponentObject<SpriteRegisterData>(entity);
                    if (data?.SpriteMap != null)
                    {
                        spriteManager.RegisterSprites(data.SpriteMap);
                    }

                    ecb.RemoveComponent<SpriteRegisterData>(entity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            entities.Dispose();
        }

        public void OnDestroy(ref SystemState state) { }
    }
}