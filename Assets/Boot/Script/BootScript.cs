using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using HybridCLR;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace Boot.Script
{
    public class BootScript : MonoBehaviour
    {
        [SerializeField]
        string defaultHostServer = "https://796f-youaresurvivor-d3gsh9e8db842a0f0-1379114111.tcb.qcloud.la/Remote";
        [SerializeField]
        string fallbackHostServer = "https://796f-youaresurvivor-d3gsh9e8db842a0f0-1379114111.tcb.qcloud.la/Remote";
        [SerializeField] 
        private EPlayMode mode=EPlayMode.EditorSimulateMode;

        private HotUpdatePanel hotUpdatePanel;

        public List<string> metaDataPath = new()
        {
            "mscorlib",
            "System",
            "System.Core",
            "Assembly-CSharp",
            "UniTask",
            "Newtonsoft.Json",
            "LitJson",
            "DOTween",
            "DOTween.Modules",
            "QFramework",
            "QFramework.CoreKit",
            "Boot.Runtime",
            "Cinemachine"
        };
        
        
        public List<string> hotUpdateAssembliesPath = new()
        {
           "HotAssembly.dll",
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
            //补充元数据
            foreach (var path in metaDataPath)
            {
                var location = ResPrefix.Assembly + path+".dll";
                print($"加载元数据:{location}");
                var handle = await YooAssetManager.
                    Instance.LoadAssetAsync<TextAsset>(location);
                if (handle == null || !handle.Asset)
                {
                    Debug.LogError($"元数据{location}加载失败");
                    continue;
                }
                byte[] bytes = handle.Asset.bytes;
                var ret = RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
                Debug.Log($"补充元数据 {location} 结果码:{ret}");
            }
            
            
            
            //加载热更程序集
            foreach (var path in hotUpdateAssembliesPath)
            {
                var location = ResPrefix.Assembly + path;
                print(path);
                var bytes = await YooAssetManager.Instance.LoadAssetAsync<TextAsset>(location);
                if (bytes != null)
                {
                    Assembly.Load(bytes.Asset.bytes);
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
