using UnityEngine;
using YooAsset;

namespace Game.Script.Tool
{
    public class AutoReleaseHandle : MonoBehaviour
    {
        private AssetHandle handle;
    
        public void Init(AssetHandle h)
        {
            handle = h;
        }

        private void OnDestroy()
        {
            handle?.Release();
            handle = null;
        }
    }

}