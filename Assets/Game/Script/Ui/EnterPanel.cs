using System;
using Cysharp.Threading.Tasks;
using Game.Script.Manager;
using QFramework;

// 1.请在菜单 编辑器扩展/Namespace Settings 里设置命名空间
// 2.命名空间更改后，生成代码之后，需要把逻辑代码文件（非 Designer）的命名空间手动更改
namespace Game.Script.Ui
{
	public partial class EnterPanel :PanelBase
	{
		public override async UniTask OpenAsync()
		{
			await base.OpenAsync();
			exitButton.onClick.AddListener(ExitGame);
			enterButton.onClick.AddListener(EnterGame);
		}

		public override async UniTask HideAsync()
		{
			exitButton.onClick.RemoveListener(ExitGame);
			enterButton.onClick.RemoveListener(EnterGame);
			 await base.HideAsync();
		}

		public override async UniTask CloseAsync()
		{
			exitButton.onClick.RemoveListener(ExitGame);
			enterButton.onClick.RemoveListener(EnterGame);
			await base.CloseAsync();
		}

		private void EnterGame()
		{
		
			//关闭EnterPanel
			UiManager.Instance.HideAsync(nameof(EnterPanel)).Forget();
			//进行一局游戏的初始化
			//TODO:
			GameManager.Instance.BeginGame().Forget();

		}

		private void ExitGame()
		{
			
		}
	}
}
