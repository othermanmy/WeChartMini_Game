using System;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Script.Ui
{
	public partial class AbilityUi : ViewController
	{
		public int AbilityId { get; protected set; }
		public int AbilityIndex { get; protected set; }
		
		public void Init(int abilityId,int index,Sprite sprite,
			ToggleGroup group,Action<bool> onSelected)
		{
			AbilityId = abilityId;
			AbilityIndex = index;
			icon.sprite = sprite;
			select.group = group;
			select.onValueChanged.RemoveAllListeners();
			select.onValueChanged.AddListener(onSelected.Invoke);
		}
		
	
	}
}
