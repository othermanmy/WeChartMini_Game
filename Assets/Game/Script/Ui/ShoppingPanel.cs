
using System.Collections.Generic;
using Boot.Script;
using Cysharp.Threading.Tasks;
using Game.Script.Manager;
using Game.Script.Model;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Script.Ui
{
	public partial class ShoppingPanel : PanelBase
	{

		private AssetWrapper<GameObject> aUiWrapper;
		private ObjectPool<AbilityUi> abilityUiPool;
		
		private List<AbilityUi> abilityUiList = new();
		
		private const string atlasName="AbilitySprites";
		protected override async UniTask Init()
		{
			await SpriteManager.Instance.LoadAtlasAsync(atlasName);
			aUiWrapper = await YooAssetManager.Instance.
				LoadAssetAsync<GameObject>(ResPrefix.UiPanel + nameof(AbilityUi));
		
			abilityUiPool=new ObjectPool<AbilityUi>(
				() =>
				{
					var go = Instantiate(aUiWrapper.Asset,abilityRoot.transform);
					var abilityUi = go.GetComponent<AbilityUi>();
					return abilityUi;
				},
				abilityUi =>
				{
					abilityUi.gameObject.SetActive(true);
				},
				abilityUi =>
				{
					abilityUi.gameObject.SetActive(false);
				},
				abilityUi =>
				{
					GameObject.Destroy(abilityUi.gameObject);
				});
			
			await base.Init();
		}

		public int CurrentSelectedAbility { get; private set; }

		public async UniTask RefreshAbilityUi(List<AbilityConfig> configs)
		{
			await UniTask.WaitUntil(() => abilityUiPool != null);
			//回收
			foreach (var ui in abilityUiList)
				abilityUiPool.Release(ui);
			abilityUiList.Clear();
			for (int i = 0; i < configs.Count; i++)
			{
				var ui = abilityUiPool.Get();
				if (!SpriteManager.Instance.TryGetSpriteAtlas(atlasName, out var iconAtlas)) continue;
				var i1 = i;
				ui.Init(configs[i].id,i,iconAtlas.GetSprite(configs[i].iconPath),
					abilityRoot, isSelected =>
					{
						if (isSelected)
						{
							CurrentSelectedAbility = configs[i1].id;
							infoText.text = configs[i1].description;
						}
					});
				ui.transform.SetAsLastSibling();
				abilityUiList.Add(ui);
			}
			if(configs.Count>0)
			{
				abilityUiList[0].select.isOn = true;
			}
		}
		
		
		


		public override async UniTask OpenAsync()
		{
			await base.OpenAsync();
			closeButton.onClick.AddListener(Hide);
			UiManager.Instance.HideAsync(nameof(PlayerHubPanel)).Forget();
			GameManager.Instance.PauseGame();
		}

		private void Hide()
		{
			HideAsync().Forget();
		}
		
		public override async  UniTask HideAsync()
		{
			closeButton.onClick.RemoveListener(Hide);
			UiManager.Instance.OpenAsync(nameof(PlayerHubPanel),nameof(PlayerHubPanel)).Forget();
			GameManager.Instance.ResumeGame();
			await base.HideAsync();
		}

		public override async UniTask CloseAsync()
		{
			closeButton.onClick.RemoveListener(Hide);
			UiManager.Instance.OpenAsync(nameof(PlayerHubPanel),nameof(PlayerHubPanel)).Forget();
			GameManager.Instance.ResumeGame();
			await base.CloseAsync();
		}
		
		
		
	}
}
