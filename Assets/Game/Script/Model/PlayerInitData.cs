using UnityEngine;

namespace Game.Script.Model
{
    [CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData")]
    public class PlayerInitData:ScriptableObject
    {  
        [Header("角色属性")]
        public float baseSpeed;
        
        public float baseMaxHp;

        public float baseDamage;

        public float hurtInvincibleTime=0.5f;
        
        public float shootInterval=0.5f;//发射间隔
        
        public float hitRadius=0.5f;//受击半径
        
        public float knockBack=0.5f;//击退距离
        
        [Header("经济")]
        public int coin;

        public int Cost;

        public int CostUp;

        public int shopAbilityCount=3;//商店显示的能力个数

        //金币吸收范围
        public float absorbRange = 0.5f;
        
        [Header("子弹")]
        public float bulletSpeed=1f;
        public float bulletLifeTime=1f;
        public float bulletHitRadius=0.2f;
        
    }
}