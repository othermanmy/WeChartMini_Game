using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using QFramework;

namespace Game.Script.Ui
{
	public partial class ControlPanel : PanelBase
	{
		public Action<Vector2> OnMoveChanged;
		public Action<Vector2> OnAttackChanged;
		public Action OnDodgeClicked;

		public override async UniTask OpenAsync()
		{
			await base.OpenAsync();
			attackJoystick.OnValueChanged += AttackChanged;
			joystick.OnValueChanged += MoveChanged;
			dodge.onClick.AddListener(DodgeClicked);
		}

		public override async UniTask HideAsync()
		{
			attackJoystick.OnValueChanged -= AttackChanged;
			joystick.OnValueChanged -= MoveChanged;
			dodge.onClick.RemoveListener(DodgeClicked);
			await base.HideAsync();
		}

		public override async UniTask CloseAsync()
		{
			attackJoystick.OnValueChanged -= AttackChanged;
			joystick.OnValueChanged -= MoveChanged;
			dodge.onClick.RemoveListener(DodgeClicked);
			await base.CloseAsync();
		}

		private void AttackChanged(Vector2 value)=>OnAttackChanged?.Invoke(value);
		private void DodgeClicked()=>OnDodgeClicked?.Invoke();
		
		private void MoveChanged(Vector2 value)=>OnMoveChanged?.Invoke(value);
	}
}
