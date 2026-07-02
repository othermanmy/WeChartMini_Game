using System;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace Game.Script.Tool
{
    public static class ResPrefix
    {
        public const string Scene= "Scene_";
        public const string Assembly= "Assembly_";
        public const string UiPanel= "UiPanel_";
        public const string Data= "Data_";
        public const string Player= "Player_";
        public const string Props= "Props_";
        public const string Enemy = "Enemy_";
        public const string Bullets= "Bullets_";
        public const string Image = "Image_";
        public const string Audio= "Audio_";
    }
    
   
    public class GameAssetManager:MonoSingleton<GameAssetManager>
    {
        private ResourcePackage Package => YooAssets.GetPackage("DefaultPackage");

        public async UniTask<AssetWrapper<T>> LoadAssetAsync<T>(string path,uint priority=0) where T:UnityEngine.Object
        {
            var handle= Package?.LoadAssetAsync<T>(path, priority);
            await handle.ToUniTask();
            if (handle == null)return null;
            if (handle.Status == EOperationStatus.Succeeded)
                return new AssetWrapper<T>(handle);
            Debug.LogError($"加载资源失败! {handle.Error}");
            return null;
        }
        public async UniTask<AssetWrapper<UnityEngine.Object>> LoadAssetAsync(string path,Type type,uint priority=0)
        {
            var handle= Package?.LoadAssetAsync(path, type, priority);
            await handle.ToUniTask();
            if (handle == null)return null;
            if (handle.Status == EOperationStatus.Succeeded)
                return new AssetWrapper<UnityEngine.Object>(handle);
            Debug.LogError($"加载资源失败! {handle.Error}");
            return null;
        }
        public async UniTask<GameObject> InstantiateAsync(string path, Transform parent = null)
        {
            var handle = Package?.LoadAssetAsync<GameObject>(path);
            await handle.ToUniTask();
            if (handle == null)return null;
            if (handle.Status == EOperationStatus.Succeeded)
            {
                var prefab = handle.AssetObject as GameObject;
                var instance =Instantiate(prefab, parent);
                // 绑定生命周期
                instance.AddComponent<AutoReleaseHandle>().Init(handle);
                return instance;
            }
            handle.Release();
            return null;
        }
        
        public async UniTask<SceneHandle> LoadSceneAsync(string location, LoadSceneMode loadSceneMode,
            LocalPhysicsMode physicsMode,bool allowSceneActivation)
        {
           await Package.UnloadUnusedAssetsAsync().ToUniTask();
          return Package.LoadSceneAsync(ResPrefix.Scene + location, loadSceneMode, physicsMode, allowSceneActivation);
        }
    }
}
