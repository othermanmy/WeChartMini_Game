using UnityEngine;
using QFramework;

// 1.请在菜单 编辑器扩展/Namespace Settings 里设置命名空间
// 2.命名空间更改后，生成代码之后，需要把逻辑代码文件（非 Designer）的命名空间手动更改
namespace Boot.Script
{
	public partial class HotUpdatePanel : ViewController
	{
		/// <summary>
		/// 进度为0-1
		/// </summary>
		public void SetProcess(float process)
		=> Process.fillAmount = process;
		
		
		public void SetTextInfo(string text)
		=>ProcessViewText.text = text;
		
	}
}
