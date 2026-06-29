using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using QFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;


namespace Boot.Script
{
    public static class ResPrefix
    {
        public const string Scene= "Scene_";
        public const string Assembly= "Assembly_";
        public const string UiPanel= "UiPanel_";
        public const string Data= "Data_";
        public const string Player= "Player_";
        public const string Props= "Props_";
        public const string Bullets= "Bullets_";
        public const string Image = "Image_";
        public const string Audio= "Audio_";
    }
    
    
    
    public class YooAssetManager:MonoSingleton<YooAssetManager>
    {
        private class RemoteServices : IRemoteService
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }
            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                return new[]
                {
                    $"{_defaultHostServer}/{fileName}",
                    $"{_fallbackHostServer}/{fileName}"
                };
            }
        }
        
        public ResourcePackage Package { get; private set; }
        public async UniTask<bool> InitAsync(EPlayMode mode,string defaultHost, string fallbackHost)
        {
            YooAssets.Initialize();
            Package = YooAssets.CreatePackage("DefaultPackage");
            bool success = true;
            switch (mode)//模式初始话
            {
                case EPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateBuildInvoker.Build("DefaultPackage", (int)EBundleType.VirtualAssetBundle);
                    var packageRoot = buildResult.PackageRootDirectory;
                    var fileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
    
                    var createParameters = new EditorSimulateModeOptions
                    {
                        EditorFileSystemParameters = fileSystemParams
                    };

                    var initOperation =  Package.InitializePackageAsync(createParameters);
                   await initOperation.ToUniTask();
                   if (initOperation.Status != EOperationStatus.Succeeded)
                       success = false;
                }
                    break;
                case EPlayMode.WebPlayMode://微信小游戏模式
                {
                    IRemoteService remoteService=new RemoteServices(defaultHost, fallbackHost);
                    var webServerFileSystemParams = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                    var webNetworkFileSystemParams =
                        FileSystemParameters.CreateDefaultWebNetworkFileSystemParameters(remoteService);
                    var createParameters = new WebPlayModeOptions()
                    {
                        WebServerFileSystemParameters = webServerFileSystemParams,
                        WebNetworkFileSystemParameters = webNetworkFileSystemParams
                    };
                    var initOperation =  Package.InitializePackageAsync(createParameters);
                    await initOperation.ToUniTask();
                    if (initOperation.Status != EOperationStatus.Succeeded)
                        success = false;
                }
                    break;
            }
            return success;
        }
        /// <summary>
        /// 获取资源版本
        /// </summary>
        private async UniTask<string> RequestPackageVersion()
        {
            var operation = Package.RequestPackageVersionAsync();
            await operation.ToUniTask();
            if (operation.Status == EOperationStatus.Succeeded)
            {
                //请求成功
                string packageVersion = operation.PackageVersion;
                Debug.Log($"[YooAsset] 当前最新资源版本为: {packageVersion}");
                return packageVersion;
            }
            //请求失败
            Debug.LogError(operation.Error);
            return null;
        }
        
        public async UniTask<bool> UpdateAndDownLoadResAsync
            (Action<string> infoCallback,Action<float> progressCallback,int downloadingMaxNum=3,int failedTryAgain=3 )
        {
            //获取最新的资源版本
            infoCallback?.Invoke("正在获取最新资源版本...");
            var version = await RequestPackageVersion();
            if(string.IsNullOrEmpty(version))
                return false;
            infoCallback?.Invoke("正在更新资源清单...");
            //更新资源清单
            var manifestOp = Package.LoadPackageManifestAsync(new LoadPackageManifestOptions(version, 120));
            await manifestOp.ToUniTask();
            if(manifestOp.Status!=EOperationStatus.Succeeded)
            {
                Debug.Log($"加载资源清单失败! {manifestOp.Error}");
                return false;
            }
            //验证并下载资源
            infoCallback?.Invoke("正在验证资源并下载...");
            var downloader =Package.CreateResourceDownloader(
                new ResourceDownloaderOptions(downloadingMaxNum, failedTryAgain));
            if (downloader.TotalDownloadCount == 0)
            {
                Debug.Log("无更新内容");
                return true;
            }
            //需要下载的文件总数和总大小
            int totalDownloadCount = downloader.TotalDownloadCount;
            downloader.DownloadProgressChanged += args =>
            {
                float progress =(float)args.CurrentDownloadCount / totalDownloadCount;
                progressCallback?.Invoke(progress);
                infoCallback?.Invoke(string.Format("正在下载资源，已下载{0:0.0}MB/{1:0.0}MB,当前进度{2:0}%"
                    ,args.CurrentDownloadBytes/ 1048576f,args.TotalDownloadBytes/1048576f,progress*100));
            };
            downloader.StartDownload();
            await downloader.ToUniTask();
            if (downloader.Status == EOperationStatus.Succeeded)
            {
                infoCallback?.Invoke("资源下载完成!");
                Debug.Log("资源下载完成!");   
                await UniTask.Delay(1000);
                return true;
            }
            infoCallback?.Invoke("下载资源失败!");
            Debug.LogError($"下载资源失败! {downloader.Error}");
            return false;
        }

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
            //先清理未引用的资源
           await Package.UnloadUnusedAssetsAsync().ToUniTask();
          return Package.LoadSceneAsync(ResPrefix.Scene + location, loadSceneMode, physicsMode, allowSceneActivation);
        }
    }
    
    
}