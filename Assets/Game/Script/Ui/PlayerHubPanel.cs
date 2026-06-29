using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Script.Manager;

namespace Game.Script.Ui
{
	public partial class PlayerHubPanel : PanelBase
	{
		private Tween healthFillTween;
		private Tween shieldFillTween;


		public void UpdateHealth(float maxHp,float hp)
		{
			if(healthFillTween !=null&&healthFillTween.IsActive())
				healthFillTween.Kill();
			var precent = hp / maxHp;
			healthText.text = $"{hp:0}/{maxHp:0}";
			healthFillTween= DOTween.To(() => healthBar.fillAmount, 
				x => healthBar.fillAmount = x,
				precent, 0.5f).SetEase(Ease.OutCubic);
		}
		public void UpdateShield(float maxShield,float shield)
		{
			if(shieldFillTween !=null&&shieldFillTween.IsActive())
				shieldFillTween.Kill();
			var precent = shield / maxShield;
			shieldText.text = $"{shield:0}/{maxShield:0}";
			shieldFillTween= DOTween.To(() => shieldBar.fillAmount, 
				x => shieldBar.fillAmount = x,
				precent, 0.5f).SetEase(Ease.OutCubic);
		}

		public void UpdateCoin(int coin)
		{
			coinText.text = coin.ToString();
		}

		public override async UniTask OpenAsync()
		{
			await base.OpenAsync();
			shopButton.onClick.AddListener(OpenShopping);
			settingsButton.onClick.AddListener(OpenSetting);
		}
		public override async UniTask HideAsync()
		{
			shopButton.onClick.RemoveListener(OpenShopping);
			settingsButton.onClick.RemoveListener(OpenSetting);
			await base.HideAsync();
		}
		public override async UniTask CloseAsync()
		{
			shopButton.onClick.RemoveListener(OpenShopping);
			settingsButton.onClick.RemoveListener(OpenSetting);
			await base.CloseAsync();
		}

		public void OpenShopping()
		{
			UiManager.Instance.OpenAsync(nameof(ShoppingPanel),nameof(ShoppingPanel)).Forget();
		}

		public void OpenSetting()
		{
			UiManager.Instance.OpenAsync(nameof(SettingsPanel),nameof(SettingsPanel)).Forget();
		}


		public void TimeCountDown(float time)
		{
			TimeSpan timeSpan = TimeSpan.FromSeconds(time);
			timeText.text = timeSpan.ToString(@"mm\:ss");
		}
	}
}
