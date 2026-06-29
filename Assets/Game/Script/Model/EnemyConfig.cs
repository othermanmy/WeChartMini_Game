using System;

namespace Game.Script.Model
{
    [Serializable]
    public class EnemyConfig
    {
        public string location;//定位,预制体名称/敌人名称

        public float baseHealth;
        public float baseDamage;
        
        public float levelUpHealth;//生命成长
        public float levelUpDamage;//伤害成长

        public int minFallCoin;//最少掉落的金币数量
        public int maxFallCoin;//最多掉落的金币数量
    }
}