using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Script.Tool;
using QFramework;
using UnityEngine;
using UnityEngine.U2D;

namespace Game.Script.Manager
{
    public class SpriteManager : MonoSingleton<SpriteManager>
    {
        private readonly Dictionary<string, Sprite> spriteMap = new();
        private readonly Dictionary<string, AssetWrapper<SpriteAtlas>> atlasCache = new();
     
        public void RegisterSprites(Dictionary<string, Sprite> map)
        {
            foreach (var kv in map)
                spriteMap[kv.Key] = kv.Value;
        }

        public bool TryGetSprite(string n, out Sprite sprite)
        {
            return spriteMap.TryGetValue(n, out sprite);
        }

        public bool TryGetSpriteAtlas(string n, out SpriteAtlas atlas)
        {
            if (atlasCache.TryGetValue(n, out var wrapper))
            {
                atlas = wrapper.Asset;
                return true;
            }
            atlas = null;
            return false;
        }

        public async UniTask LoadAtlasAsync(string atlasPath)
        {
            if (string.IsNullOrEmpty(atlasPath)) return;
            if (atlasCache.ContainsKey(atlasPath)) return;

            var wrapper = await GameAssetManager.Instance.LoadAssetAsync<SpriteAtlas>(
               ResPrefix.Image+atlasPath);
            if (!wrapper?.Asset) return;

            atlasCache[atlasPath] = wrapper;
        }
        

        public void Clear()
        {
            spriteMap.Clear();

            foreach (var wrapper in atlasCache.Values)
                wrapper.Dispose();
            atlasCache.Clear();
        }

        protected override void OnDestroy()
        {
            Clear();
            base.OnDestroy();
        }
    }
}