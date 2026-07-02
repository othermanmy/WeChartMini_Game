using UnityEngine;
using QFramework;


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
