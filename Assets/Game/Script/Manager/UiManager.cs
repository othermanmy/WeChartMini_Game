using System;
using System.Collections.Generic;
using Boot.Script;
using Cysharp.Threading.Tasks;
using Game.Script.Ui;
using QFramework;

namespace Game.Script.Manager
{
    public class UiManager:MonoSingleton<UiManager>
    {
        
        private Dictionary<string, PanelBase> activePanels = new();
        private Dictionary<string, UniTaskCompletionSource<PanelBase>> pendingPanels = new();

        public async UniTask<PanelBase> OpenAsync(string defineName, string panelName)
        {
            //查找是否有该panel
            if (!activePanels.TryGetValue(defineName, out var panel))
            {
                //竞争保护
                if (pendingPanels.TryGetValue(defineName, out var pendingSource))
                    panel = await pendingSource.Task;
                else
                {
                    var source = new UniTaskCompletionSource<PanelBase>();
                    pendingPanels.Add(defineName, source);
                    LoadPanelAndSetResultAsync(defineName, panelName, source).Forget();
                    panel = await source.Task;
                }
            }
            else
            {
                if(panel.IsOpen) return panel;
                await panel.OpenAsync();
            }
            return panel;
        }

        private async UniTaskVoid LoadPanelAndSetResultAsync(string defineName, string panelName, UniTaskCompletionSource<PanelBase> source)
        {
            try
            {
                var obj = await YooAssetManager.Instance.InstantiateAsync(ResPrefix.UiPanel + panelName);
                if (!obj)
                {
                    source.TrySetResult(null);
                    return;
                }
                var panel = obj.GetComponent<PanelBase>();
                panel.defineName = defineName;
                activePanels.Add(defineName, panel);
                panel.OnClose += OnPanelClosed;
                await panel.OpenAsync();
                source.TrySetResult(panel);
            }
            catch (Exception ex)
            {
                source.TrySetException(ex);
            }
            finally
            {
                pendingPanels.Remove(defineName);
            }
        }

        public async UniTask<bool> HideAsync(string defineName)
        {
            if (activePanels.TryGetValue(defineName, out var panel))
            {
                if(!panel.IsOpen)return true;
                await panel.HideAsync();
                return true;
            }
            print($"尝试隐藏的面板 {defineName} 尚未创建或已被销毁");
            return false;
        }
        
        
        public async UniTask<bool> CloseAsync(string defineName)
        {
            if (activePanels.TryGetValue(defineName, out var panel))
            {
                await panel.CloseAsync();
                return true;
            }
            print($"尝试关闭的面板 {defineName} 尚未创建或已被销毁");
            return false;
        }

        public bool GetPanel<T>(string defineName, out T panel) where T : PanelBase
        {
            if(!activePanels.TryGetValue(defineName, out var basePanel))
            {
                panel = null;
                return false;
            }
            panel = basePanel as T;
            return panel;
        }
        private void OnPanelClosed(PanelBase panel)
        {
            activePanels.Remove(panel.defineName);
        }
        
        public bool IsOpen(string defineName) => activePanels.ContainsKey(defineName);
    }
}