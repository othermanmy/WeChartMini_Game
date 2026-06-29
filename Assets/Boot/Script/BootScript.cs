using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace Boot.Script
{
    public class BootScript : MonoBehaviour
    {
        [SerializeField]
        string defaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
        [SerializeField]
        string fallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";
        [SerializeField] 
        private EPlayMode mode=EPlayMode.EditorSimulateMode;

        private HotUpdatePanel hotUpdatePanel;

        public List<string> hotUpdateAssembliesPath = new()
        {
           "HotAssembly.dll.bytes",
        };
        private void Awake()
        {
            hotUpdatePanel = GetComponent<HotUpdatePanel>();
        }

        private void Start()
        {
            HotUpdate().Forget();
        }

        private async UniTask HotUpdate()
        {
            //初始化
            hotUpdatePanel.SetTextInfo("正在初始化资源系统...");
           bool initSucceed= await YooAssetManager.Instance.InitAsync(mode, defaultHostServer, fallbackHostServer);
           if (!initSucceed)
           {
               hotUpdatePanel.SetTextInfo("资源系统初始化失败!");
               return;
           }
           //下载资源
           bool downloadSucceed = await YooAssetManager.Instance.UpdateAndDownLoadResAsync(
               hotUpdatePanel.SetTextInfo,
               hotUpdatePanel.SetProcess);
           if (!downloadSucceed) return;
           
           hotUpdatePanel.SetProcess(1f);
           hotUpdatePanel.SetTextInfo("资源已就绪，正在引导进入游戏...");
           //进行脚本热更新
           await LoadDlls(); 
           await ChangeToMainScene();
        }

        private async UniTask LoadDlls()
        {
#if UNITY_EDITOR
            return;         
#endif
            foreach (var path in hotUpdateAssembliesPath)
            {
                var aw =await YooAssetManager.Instance.LoadAssetAsync<TextAsset>(ResPrefix.Assembly + path);
                if (aw!=null)
                {
                    Assembly.Load(aw.Asset.bytes);
                    Debug.Log($"[HybridCLR] 成功加载热更DLL: {path}");
                }
                else
                    Debug.LogError($"[HybridCLR] 找不到热更DLL资源: {path}");
            }
        }

        private async UniTask ChangeToMainScene()
        {
            var sceneHandle = YooAssetManager.Instance.LoadSceneAsync("GameScene"
                , LoadSceneMode.Single, LocalPhysicsMode.None, true);
            await sceneHandle;
        }
    }
}
