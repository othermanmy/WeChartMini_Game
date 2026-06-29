using Cysharp.Threading.Tasks;
using Game.Script.Manager;

namespace Game.Script.Ui
{
	public partial class GameOverPanel : PanelBase
	{
		public override async UniTask OpenAsync()
		{
			reBeginGameButton.onClick.AddListener(NewGame);
			bakeHomeButton.onClick.AddListener(ReturnMainMenu);
			GameManager.Instance.PauseGame();
			await base.OpenAsync();
		}

		public override async UniTask HideAsync()
		{
			reBeginGameButton.onClick.RemoveListener(NewGame);
			bakeHomeButton.onClick.RemoveListener(ReturnMainMenu);
			await base.HideAsync();
		}
		public override async UniTask CloseAsync()
		{
			reBeginGameButton.onClick.RemoveListener(NewGame);
			bakeHomeButton.onClick.RemoveListener(ReturnMainMenu);
			await base.CloseAsync();
		}
		private void ReturnMainMenu()
		{
			ReturnMainMenuAsync().Forget();
		}

		private void NewGame()
		{
			NewGameAsync().Forget();
		}

	


		private async UniTask NewGameAsync()
		{
			GameManager.Instance.EndGame();
			await UiManager.Instance.HideAsync(nameof(GameOverPanel));
			GameManager.Instance.BeginGame().Forget();
		}

		private async UniTask ReturnMainMenuAsync()
		{
			GameManager.Instance.EndGame();
			await UiManager.Instance.HideAsync(nameof(GameOverPanel));
			UiManager.Instance.OpenAsync(nameof(EnterPanel), nameof(EnterPanel)).Forget();
		}
		
	}
}
