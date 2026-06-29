using Boot.Script;
using Cysharp.Threading.Tasks;
using Game.Script.Manager;
using Game.Script.Model;
using QFramework;

namespace Game.Script.Architecture
{
    public class GameApp:Architecture<GameApp>
    {
        protected override void Init()
        {
            LoadPlayerData().Forget();
        }
        
        private async UniTask LoadPlayerData()
        {
            var playerData=await YooAssetManager.Instance.LoadAssetAsync<PlayerInitData>
                (ResPrefix.Data+"PlayerInitData");
            var m=new PlayerModel();
            m.InitData(playerData.Asset);
            RegisterModel(m);
           RegisterSystem(new BulletManager());
           AbilityManager.BuildFactories();
           RegisterSystem(new AbilityManager());
        }
    }
}