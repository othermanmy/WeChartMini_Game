using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Script.Manager;

namespace Game.Script.Ui
{
	public partial class SettingsPanel : PanelBase
	{
		protected override async UniTask Init()
		{

			await base.Init();
		}


		public override async UniTask OpenAsync()
		{
			await base.OpenAsync();
			reBeginGameButton.onClick.AddListener(NewGame);
			bakeHomeButton.onClick.AddListener(ReturnMainMenu);
			closeButton.onClick.AddListener(ReturnGame);
			voiceToggle.onValueChanged.AddListener(OpenVoice);
			GameManager.Instance.PauseGame();
		}

		public override async UniTask HideAsync()
		{
			reBeginGameButton.onClick.RemoveListener(NewGame);
			bakeHomeButton.onClick.RemoveListener(ReturnMainMenu);
			closeButton.onClick.RemoveListener(ReturnGame);
			voiceToggle.onValueChanged.RemoveListener(OpenVoice);
			GameManager.Instance.ResumeGame();
			await base.HideAsync();
		}
		
		public override async UniTask CloseAsync()
		{
			reBeginGameButton.onClick.RemoveListener(NewGame);
			bakeHomeButton.onClick.RemoveListener(ReturnMainMenu);
			closeButton.onClick.RemoveListener(ReturnGame);
			voiceToggle.onValueChanged.RemoveListener(OpenVoice);
			GameManager.Instance.ResumeGame();
			await base.CloseAsync();
		}

		private void NewGame()
		{
			NewGameAsync().Forget();
		}

		private async UniTask NewGameAsync()
		{
			GameManager.Instance.EndGame();
			await UiManager.Instance.HideAsync(nameof(SettingsPanel));
			GameManager.Instance.BeginGame().Forget();
		}
		private void ReturnMainMenu()
		{
			ReturnMainMenuAsync().Forget();
		}
		private async UniTask ReturnMainMenuAsync()
		{
			GameManager.Instance.EndGame();
			await CloseAsync();
			UiManager.Instance.OpenAsync(nameof(EnterPanel), nameof(EnterPanel)).Forget();
		}

		private void ReturnGame()
		{
		   UiManager.Instance.HideAsync(nameof(SettingsPanel)).Forget();
		}


		private void OpenVoice(bool isOn)
		{
			if (isOn)
			{
				voiceImg_open.gameObject.SetActive(true);
				voiceImg_open.DOFade(1f, 0.5f);
				voiceImg_close.DOFade(0f, 0.5f).onComplete=()=>voiceImg_close.gameObject.SetActive(false);
			}
			else
			{
				voiceImg_close.gameObject.SetActive(true);
				voiceImg_close.DOFade(1f, 0.5f);
				voiceImg_open.DOFade(0f, 0.5f).onComplete=()=>voiceImg_open.gameObject.SetActive(false);
			}
			AudioManager.Instance.SetMasterVolume(isOn ? 1f : 0f);
		}
		
	}
}
