using System;
using Cysharp.Threading.Tasks;
using Game.Script.Manager;
using QFramework;
using UnityEngine;
using AudioType = Game.Script.Manager.AudioType;

namespace Game.Script.Ui
{
    public class PanelBase:ViewController
    {
        
        [SerializeField]
        private AudioEntry openSound;
        [SerializeField]
        private AudioEntry closeSound;
        
        private bool isOpen;
        public string defineName;
        /// <summary>
        /// 在Active之后
        /// </summary>
        public Action<PanelBase> OnOpen = null;
        /// <summary>
        /// 在Active之前
        /// </summary>
        public Action<PanelBase> OnHide = null;
        public Action<PanelBase> OnClose = null;
        public Action<PanelBase> OnInit = null;

        protected bool isInit;

        protected virtual async UniTask Init()
        {
            isInit = true;
            OnInit?.Invoke(this);
            await UniTask.Yield();
        }
        
        public bool IsOpen => isOpen;
        
        public virtual async UniTask OpenAsync()
        {
            if(!isInit)
                await Init();
            if(isOpen)return;
            gameObject.SetActive(true);
            OnOpen?.Invoke(this);
            if(openSound != null)
                AudioManager.Instance.PlayShot(openSound);
            isOpen = true;
            await UniTask.Yield();
        }
        
        public virtual async UniTask HideAsync()
        {
            if(!isOpen) return;
            OnHide?.Invoke(this);
            gameObject.SetActive(false);
            if(closeSound != null)
                AudioManager.Instance.PlayShot(closeSound);
            isOpen = false;
            await UniTask.Yield();
        }

        public virtual async UniTask CloseAsync()
        {
            if(!isOpen) return;
            isOpen = false;
            OnClose?.Invoke(this);
            if(closeSound != null)
                AudioManager.Instance.PlayShot(closeSound);
            Destroy(gameObject);
            await UniTask.Yield();
        }
        
        
    }
}