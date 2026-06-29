using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Script.Architecture;
using Game.Script.Manager;
using Game.Script.Model;
using Game.Script.Ui;
using QFramework;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Script.Controller
{
    public class ShoppingController:MonoBehaviour,IController
    {
        private ShoppingPanel shoppingPanel;
        private PlayerModel playerModel;
        private AbilityManager  abilityManager;
        private GameObject Player;
        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }

        private void Awake()
        {
            shoppingPanel = GetComponent<ShoppingPanel>();
        }

        private void Start()
        {
            playerModel = this.GetModel<PlayerModel>();
            abilityManager =this.GetSystem<AbilityManager>();
            Player = GameManager.Instance.playerTrans.gameObject;
            shoppingPanel.OnOpen+=RegisterEvent;
            shoppingPanel.OnHide+=UnRegisterEvent;
            shoppingPanel.OnClose+=UnRegisterEvent;
            DoRefreshShop();
        }

        private void RegisterEvent(PanelBase panel)
        {
            shoppingPanel.purchaseButton.onClick.AddListener(OnPurchase);
            shoppingPanel.refreshButton.onClick.AddListener(OnRefreshShop);
            playerModel.Cost.RegisterWithInitValue(CostUpdate);
        }

        private void UnRegisterEvent(PanelBase panel)
        {
            shoppingPanel.purchaseButton.onClick.RemoveListener(OnPurchase);
            shoppingPanel.refreshButton.onClick.RemoveListener(OnRefreshShop);
            playerModel.Cost.UnRegister(CostUpdate);
        }
        
        private void OnPurchase()
        {
            var abilityId = shoppingPanel.CurrentSelectedAbility;
            if (abilityId == 0)
            {
                shoppingPanel.infoText.text = "请先选择一个能力";
                return;
            }

            if (playerModel.coin.Value < playerModel.Cost.Value)
            {
                shoppingPanel.infoText.text = "金币不足";
                return;
            }

            if (abilityManager.AcquireAbility(abilityId, Player))
            {
                playerModel.coin.Value -= playerModel.Cost.Value;
                playerModel.Cost.Value += playerModel.costUp.Value;
                DoRefreshShop();
            }
            else
            {
                shoppingPanel.infoText.text = "已拥有该能力";
            }
        }

        private void OnRefreshShop()
        {
            if (playerModel.coin.Value < playerModel.Cost.Value)
            {
                shoppingPanel.infoText.text = "金币不足";
                return;
            }

            playerModel.coin.Value -= playerModel.Cost.Value;
            DoRefreshShop();
        }

        private void DoRefreshShop()
        {
            var excludeIds = GetOwnedAbilityIds();
            var available = abilityManager.GetAvailableConfigs(excludeIds);

            if (available.Count == 0)
            {
                shoppingPanel.infoText.text = "能力已卖完";
                return;
            }

            Shuffle(available);
            var count = Mathf.Min(playerModel.shopAbilityCount.Value, available.Count);
            shoppingPanel.RefreshAbilityUi(available.GetRange(0, count)).Forget();
        }

        private HashSet<int> GetOwnedAbilityIds()
        {
            var ids = new HashSet<int>();
            var ownerAbilities = abilityManager.GetOwnedAbilities(Player);
            if (ownerAbilities != null)
            {
                foreach (var ab in ownerAbilities)
                    ids.Add(ab.Config.id);
            }
            return ids;
        }

        /// <summary>Fisher-Yates 洗牌算法</summary>
        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void CostUpdate(int cost)
        {
            shoppingPanel.costCoinText.text = cost.ToString();
        }
    }
}