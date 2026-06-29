using System;
using QFramework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Script.Model
{
    [Serializable]
    public class PlayerModel : AbstractModel
    {
        public PlayerInitData playerInitData { get; private set; }

        public float2 centerOffset;

        [Tooltip("金币")]
        public BindableProperty<int> coin = new();

        /// <summary>
        /// 购买能力需要花费的coin数
        /// </summary>
        public BindableProperty<int> Cost = new();

        /// <summary>
        /// 购买能力后，下一次购买能力需要花费的coin数的增长
        /// </summary>
        public BindableProperty<int> costUp = new();

        public BindableProperty<int> shopAbilityCount = new();
        
        public BindableProperty<float> coinAbsorbRange = new();
        public BindableProperty<float> coinAbsorbRangeExtra  = new();
        public BindableProperty<float> coinAbsorbRangePercent  = new(1);
        [Header("角色属性")]
        [Tooltip("当前血量")]
        public BindableProperty<float> hp = new();

        [Tooltip("最大血量")] public BindableProperty<float> maxHp = new();
        [Tooltip("最大血量-额外值")] public BindableProperty<float> maxHpExtra = new();
        [Tooltip("最大血量-百分比加成(1=100%)")] public BindableProperty<float> maxHpPercent = new(1);

        [Tooltip("当前伤害")] public BindableProperty<float> currentDamage = new();
        public BindableProperty<float> damageExtra = new();
        public BindableProperty<float> damagePercent = new(1);

        [Tooltip("当前速度")] public BindableProperty<float> speed = new();
        public BindableProperty<float> speedExtra = new();
        public BindableProperty<float> speedPercent = new(1);

        [Tooltip("攻击间隔")] public BindableProperty<float> shootInterval = new();
        public BindableProperty<float> shootIntervalExtra = new();
        public BindableProperty<float> shootIntervalPercent = new(1);

        [Tooltip("受击半径")] public BindableProperty<float> hitRadius = new();
        public BindableProperty<float> hitRadiusExtra = new();
        public BindableProperty<float> hitRadiusPercent = new(1);

        [Tooltip("受击无敌时间")] public BindableProperty<float> hurtInvincibleTime = new();
        public BindableProperty<float> hurtInvincibleTimeExtra = new();
        public BindableProperty<float> hurtInvincibleTimePercent = new(1);

        [Tooltip("是否可以开火")] public BindableProperty<bool> canFire = new(true);
        [Tooltip("是否无敌")] public BindableProperty<bool> isInvincible = new();

        [Tooltip("击退距离")] public BindableProperty<float> knockBack = new();
        public BindableProperty<float> knockBackExtra = new();
        public BindableProperty<float> knockBackPercent = new(1);

        [Header("子弹属性")]
        public BindableProperty<float> bulletSpeed = new();
        public BindableProperty<float> bulletSpeedExtra = new();
        public BindableProperty<float> bulletSpeedPercent = new(1);

        public BindableProperty<float> bulletLifeTime = new();
        public BindableProperty<float> bulletLifeTimeExtra = new();
        public BindableProperty<float> bulletLifeTimePercent = new(1);

        public BindableProperty<float> bulletHitRadius = new();
        public BindableProperty<float> bulletHitRadiusExtra = new();
        public BindableProperty<float> bulletHitRadiusPercent = new(1);


        public Action<float, Entity> OnHurt;

        /// <summary>
        /// 重置所有属性至初始值（用于新一局游戏）
        /// </summary>
        public void ReSetAllValue()
        {
            if (playerInitData == null) return;

            // 重置所有 Extra/Percent 加成属性
            maxHpExtra.Value = 0;
            maxHpPercent.Value = 1;
            damageExtra.Value = 0;
            damagePercent.Value = 1;
            speedExtra.Value = 0;
            speedPercent.Value = 1;
            shootIntervalExtra.Value = 0;
            shootIntervalPercent.Value = 1;
            hitRadiusExtra.Value = 0;
            hitRadiusPercent.Value = 1;
            hurtInvincibleTimeExtra.Value = 0;
            hurtInvincibleTimePercent.Value = 1;
            knockBackExtra.Value = 0;
            knockBackPercent.Value = 1;
            bulletSpeedExtra.Value = 0;
            bulletSpeedPercent.Value = 1;
            bulletLifeTimeExtra.Value = 0;
            bulletLifeTimePercent.Value = 1;
            bulletHitRadiusExtra.Value = 0;
            bulletHitRadiusPercent.Value = 1;
            coinAbsorbRangeExtra.Value = 0;
            coinAbsorbRangePercent.Value = 1;

            // 重算基础属性（触发 WireRecalc）
            InitData(playerInitData);

            // 重置状态属性
            hp.Value = playerInitData.baseMaxHp;
            canFire.Value = true;
            isInvincible.Value = false;
        }

        protected override void OnInit()
        {
        }

        public void InitData(PlayerInitData data)
        {
            playerInitData = data;

            hp.Value = data.baseMaxHp;

            // 经济
            coin.Value = data.coin;
            Cost.Value = data.Cost;
            costUp.Value = data.CostUp;
            shopAbilityCount.Value = data.shopAbilityCount;
            
            WireRecalc(() => playerInitData.baseMaxHp, maxHpExtra, maxHpPercent, maxHp);
            WireRecalc(() => playerInitData.baseDamage, damageExtra, damagePercent, currentDamage);
            WireRecalc(() => playerInitData.baseSpeed, speedExtra, speedPercent, speed);
            WireRecalc(() => playerInitData.shootInterval, shootIntervalExtra, shootIntervalPercent, shootInterval);
            WireRecalc(() => playerInitData.hitRadius, hitRadiusExtra, hitRadiusPercent, hitRadius);
            WireRecalc(() => playerInitData.hurtInvincibleTime, hurtInvincibleTimeExtra, hurtInvincibleTimePercent, hurtInvincibleTime);
            WireRecalc(() => playerInitData.knockBack, knockBackExtra, knockBackPercent, knockBack);
            WireRecalc(() => playerInitData.bulletSpeed, bulletSpeedExtra, bulletSpeedPercent, bulletSpeed);
            WireRecalc(() => playerInitData.bulletLifeTime, bulletLifeTimeExtra, bulletLifeTimePercent, bulletLifeTime);
            WireRecalc(() => playerInitData.bulletHitRadius, bulletHitRadiusExtra, bulletHitRadiusPercent, bulletHitRadius);
            WireRecalc(()=>playerInitData.absorbRange, coinAbsorbRangeExtra, coinAbsorbRangePercent, coinAbsorbRange);
        }

        private void WireRecalc(Func<float> baseGetter, BindableProperty<float> extraProp,
            BindableProperty<float> percentProp, BindableProperty<float> finalProp)
        {
            Action<float> recalc = _ => { finalProp.Value = (baseGetter() + extraProp.Value) * percentProp.Value; };
            extraProp.Register(recalc);
            percentProp.Register(recalc);
            // 立即触发一次计算
            recalc(default);
        }
    }
}