using System.Collections.Generic;
using Game.Script.Architecture;
using Game.Script.Event;
using Game.Script.Model;
using QFramework;

namespace Game.Script.ECS.Components.Coin
{
    public static class CoinPickupBridge
    {
        private static readonly List<int> Pending = new(256);
        private static PlayerModel playerModel;

        /// <summary>由 CoinPickupSystem 每帧拾取时调用</summary>
        public static void Enqueue(int count)
        {
            Pending.Add(count);
        }

        /// <summary>每帧末尾 Flush</summary>
        public static void Flush()
        {
            if (Pending.Count == 0)
                return;

            int total = 0;
            foreach (var c in Pending)
                total += c;

            playerModel ??= GameApp.Interface.GetModel<PlayerModel>();
            playerModel.coin.Value += total;
            Pending.Clear();

            TypeEventSystem.Global.Send(new OnCoinPickupEvent { count = total });
        }

        /// <summary>获取当前金币吸收范围</summary>
        public static float GetAbsorbRange()
        {
            playerModel ??= GameApp.Interface.GetModel<PlayerModel>();
            return playerModel.coinAbsorbRange.Value;
        }
    }
}
