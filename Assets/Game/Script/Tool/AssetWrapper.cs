using System;
using YooAsset;

namespace Game.Script.Tool
{
    public class AssetWrapper<T>:IDisposable where T:UnityEngine.Object
    {
        public T Asset { get; private set; }
        private AssetHandle handle;
        
        public AssetWrapper(AssetHandle handle)
        {
            this.handle = handle;
            Asset = handle.AssetObject as T;
        }

        ~AssetWrapper()
        {
            Release();
        }
        
        public void Dispose()
        {
            
            Release();
            GC.SuppressFinalize(this);
        }
        private void Release()
        {
            if (handle != null)
            {
                handle.Release();
                handle = null;
            }
        }
    }
}