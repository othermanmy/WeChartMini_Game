using System;
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
        private List<string> metaDataPath = new()
        {
          "Assembly-CSharp",
"Cinemachine",
"DOTween",
"DOTween.Modules",
"HybridCLR.Runtime",
"LitJson",
"Mono.Security",
"mscorlib",
"Newtonsoft.Json",
"QFramework.CoreKit",
"QFramework",
"System.Configuration",
"System.Core",
"System.Data",
"System",
"System.Drawing",
"System.Numerics",
"System.Runtime",
"System.Runtime.Serialization",
"System.Xml",
"System.Xml.Linq",
"UniTask",
"Unity.2D.Animation.Runtime",
"Unity.2D.Tilemap.Extras",
"Unity.Burst",
"Unity.Burst.Unsafe",
"Unity.Collections",
"Unity.Collections.LowLevel.ILSupport",
"Unity.Deformations",
"Unity.Entities",
"Unity.Entities.Graphics",
"Unity.Entities.Hybrid",
"Unity.Entities.Hybrid.HybridComponents",
"Unity.FontABTool",
"Unity.InputSystem",
"Unity.InternalAPIEngineBridge.001",
"Unity.Mathematics",
"Unity.Mathematics.Extensions",
"Unity.Physics",
"Unity.Physics.Hybrid",
"Unity.RenderPipeline.Universal.ShaderLibrary",
"Unity.RenderPipelines.Core.Runtime",
"Unity.RenderPipelines.Universal.Runtime",
"Unity.Scenes",
"Unity.Serialization",
"Unity.TextMeshPro",
"Unity.Timeline",
"Unity.Transforms",
"Unity.Transforms.Hybrid",
"UnityEngine.AIModule",
"UnityEngine.AndroidJNIModule",
"UnityEngine.AnimationModule",
"UnityEngine.AssetBundleModule",
"UnityEngine.AudioModule",
"UnityEngine.ContentLoadModule",
"UnityEngine.CoreModule",
"UnityEngine.DirectorModule",
"UnityEngine",
"UnityEngine.GridModule",
"UnityEngine.IMGUIModule",
"UnityEngine.InputLegacyModule",
"UnityEngine.InputModule",
"UnityEngine.JSONSerializeModule",
"UnityEngine.ParticleSystemModule",
"UnityEngine.Physics2DModule",
"UnityEngine.PhysicsModule",
"UnityEngine.PropertiesModule",
"UnityEngine.SharedInternalsModule",
"UnityEngine.SpriteMaskModule",
"UnityEngine.SpriteShapeModule",
"UnityEngine.SubsystemsModule",
"UnityEngine.TerrainModule",
"UnityEngine.TextCoreFontEngineModule",
"UnityEngine.TextCoreTextEngineModule",
"UnityEngine.TextRenderingModule",
"UnityEngine.TilemapModule",
"UnityEngine.UI",
"UnityEngine.UIElementsModule",
"UnityEngine.UIModule",
"UnityEngine.UnityAnalyticsModule",
"UnityEngine.UnityWebRequestAssetBundleModule",
"UnityEngine.UnityWebRequestModule",
"UnityEngine.VFXModule",
"UnityEngine.VideoModule",
"UnityEngine.VRModule",
"UnityEngine.WebGLModule",
"UnityEngine.XRModule",
"wx-runtime",
"Wx",
"YooAsset",
        };
        
        private List<string> hotUpdateAssembliesPath = new()
        { 
            "netstandard.dll",
           "HotAssembly.dll"
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
           bool initSucceed= await BootAssetManager.Instance.InitAsync(mode, defaultHostServer, fallbackHostServer);
           if (!initSucceed)
           {
               hotUpdatePanel.SetTextInfo("资源系统初始化失败!");
               return;
           }
           //下载资源
           bool downloadSucceed = await BootAssetManager.Instance.UpdateAndDownLoadResAsync(
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
            Assembly hot = null;
#if !UNITY_EDITOR
            //补充元数据
            foreach (var path in metaDataPath)
            {
                var location = ResPrefix.Assembly + path+".dll";
                print($"加载元数据:{location}");
                var handle = await BootAssetManager.
                    Instance.LoadAssetAsync<TextAsset>(location);
                if (handle == null || !handle.Asset)
                {
                    Debug.LogError($"元数据{location}加载失败");
                    continue;
                }
                byte[] bytes = handle.Asset.bytes;
                var ret = RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.Consistent);
                Debug.Log($"补充元数据 {location} 结果码:{ret}");
            }
            //加载热更程序集
            foreach (var path in hotUpdateAssembliesPath)
            {
                var location = ResPrefix.Assembly + path;
                print(path);
                var bytes = await BootAssetManager.Instance.LoadAssetAsync<TextAsset>(location);
                if (bytes != null)
                {
                   hot= Assembly.Load(bytes.Asset.bytes);
                    Debug.Log($"[HybridCLR] 成功加载热更DLL: {path}");
                }
                else
                    Debug.LogError($"[HybridCLR] 找不到热更DLL资源: {path}");
            }
#endif
                ECSHotFixHelper.InitializeDOTSWorld(hot);
            
        }

        private async UniTask ChangeToMainScene()
        {
            var sceneHandle = BootAssetManager.Instance.LoadSceneAsync("GameScene"
                , LoadSceneMode.Single, LocalPhysicsMode.None, true);
            await sceneHandle;
            print("加载完成");
        }
        
        
        
    }
}