using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Script.Manager;
using UnityEngine;
using QFramework;
using TMPro;
using UnityEngine.Pool;
using Random = UnityEngine.Random;


namespace Game.Script.Ui
{
	public partial class DamageDumpPanel : PanelBase
	{
		
		private ObjectPool<TMP_Text>  textPool;
		private Camera mainCamera;
		[SerializeField]
		private int maxSize = 20;
		[SerializeField] 
		private int initSize = 5;
		[SerializeField]
		private float lifeTime = 1f;
		[SerializeField]
		private float maxLeaveDistance = 0.5f;

		private RectTransform selfRect;
		private void InitPool()
		{
			textPool = new ObjectPool<TMP_Text>(() =>
			{
				var gm=Instantiate(dumpTextTemplate,transform);
				return gm;
			}, (text) =>
			{
				text.alpha = 1f;
			}, (text) =>
			{
				text.text = "";
				text.alpha = 0f;
			}, (text) =>
			{
				Destroy(text.gameObject);
			},defaultCapacity:initSize,maxSize:maxSize);
		}


		protected override async UniTask Init()
		{
			mainCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
			selfRect=GetComponent<RectTransform>();
			InitPool();
			await base.Init();
		}


		private async UniTask Dump(Vector2 worldPos,float damage,Color color)
		{
			var screenPos=RectTransformUtility.WorldToScreenPoint(mainCamera,worldPos);
			var text=textPool.Get();
			RectTransformUtility.ScreenPointToLocalPointInRectangle(selfRect,screenPos, null, out var localPos);
			text.rectTransform.anchoredPosition = localPos;
			text.text = damage.ToString("F0");
			text.color = color;
	        //随机方向c
			Vector2 dir=new Vector2(Random.Range(-1f, 1f),Random.Range(-1f, 1f)).normalized;
			float leaveDistance=Random.Range(0.1f, maxLeaveDistance);
			//DoTween进行位移和透明度的降低
			var moveTween 
				= text.rectTransform.
					DOAnchorPos(localPos + dir * leaveDistance, lifeTime)
					.SetEase(Ease.OutQuad);
			var fadeTween = text.DOFade(0f, lifeTime)
				.SetEase(Ease.InQuad);
			var tcs=new UniTaskCompletionSource();
			int completedCount = 0;
			Action de=()=>{
				completedCount++;
				if (completedCount >= 2) tcs.TrySetResult();
			};
			moveTween.onComplete +=()=> de.Invoke();
			fadeTween.onComplete+=()=>de.Invoke();
			await tcs.Task;
			textPool.Release(text);
		}


		public static async UniTask DumpAsync(Vector2 worldPos,float damage,Color color)
		{
			var panel= await UiManager.Instance.OpenAsync(nameof(DamageDumpPanel), nameof(DamageDumpPanel)) as DamageDumpPanel;
			if(!panel)return;
			await panel.Dump(worldPos,damage,color);
		}
	}
}
