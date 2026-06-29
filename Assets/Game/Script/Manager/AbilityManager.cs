using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Boot.Script;
using Cysharp.Threading.Tasks;
using Game.Script.Model;
using Game.Script.Model.Ability;
using Newtonsoft.Json;
using QFramework;
using UnityEngine;

namespace Game.Script.Manager
{
    public class AbilityManager:AbstractSystem
    {  
        private Dictionary<int, AbilityConfig> configs;
        private List<AbilityConfig> configList;
        //能力工厂
        private static Dictionary<string, Func<AbilityBase>> abilityFactories;
        private static bool isFactorInitialized=false;

        private Dictionary<int,List<AbilityBase>> abilities=new ();//key为InstanceID
        protected override void OnInit()
        {
            InitAsync().Forget();
        }
        private async UniTask InitAsync()
        {
            //加载配置文件
            var configAs =await YooAssetManager.Instance.LoadAssetAsync<TextAsset>(ResPrefix.Data + "AbilityConfigs");
            configs=JsonConvert.DeserializeObject<Dictionary<int,AbilityConfig>>(configAs.Asset.text);
            configList=new List<AbilityConfig>(configs.Values);
        }

        public static void BuildFactories()
        {
            if(isFactorInitialized)return;
            if(abilityFactories!=null)return;
            abilityFactories = new Dictionary<string, Func<AbilityBase>>();
            var baseType = typeof(AbilityBase);
            var assembly = baseType.Assembly;
            foreach (var type in assembly.GetTypes())
            {
               
                    if (!type.IsClass || type.IsAbstract) continue;
                    if (!baseType.IsAssignableFrom(type)) continue;
                    if (type.Namespace != "Game.Script.Model.Ability") continue;
                    
                    var newExpr = Expression.New(type);          
                    var lambda = Expression.Lambda<Func<AbilityBase>>(newExpr);
                    var factory = lambda.Compile();
                if(string.IsNullOrEmpty(type.Name))continue;
                    abilityFactories[type.Name] = factory;
            }
            isFactorInitialized=true;
        }

        public void Update(float dt)
        {
            foreach (var list in abilities.Values)
                foreach (var ab in list)
                    if(ab.IsActive)ab.OnUpdate(dt);
        }
        
        

        public List<AbilityConfig> GetAvailableConfigs(HashSet<int> excludeIds)
        {
            if (excludeIds == null || excludeIds.Count == 0)
                return new List<AbilityConfig>(configList);
            var list = new List<AbilityConfig>(configList.Count - excludeIds.Count + 1);
            for (int i = configList.Count - 1; i >= 0; i--)
                if(!excludeIds.Contains(configList[i].id))
                    list.Add(configList[i]);
            return list;
        }

        public bool AcquireAbility(int configId, GameObject owner)
        {
            if (!isFactorInitialized||!owner) return false;
            if (!configs.TryGetValue(configId, out var config)) return false;
            if(!abilityFactories.TryGetValue(config.className,out var factory)) return false;
            var ability = factory();
            ability.Init(config, owner);
            if (!abilities.TryGetValue(owner.GetInstanceID(), out var ownerAbilities))
            {
               
                ownerAbilities = new List<AbilityBase> { ability };
                abilities[owner.GetInstanceID()] = ownerAbilities;
            }
            else
            {
                //查看是否有该能力
                foreach (var a in ownerAbilities)
                    if (a.Config.id == configId)
                        return false;
                ownerAbilities.Add(ability);
                ownerAbilities.Sort((a, b) => a.Config.priority.CompareTo(b.Config.priority));
            }
            ability.OnAcquire();
            return true;
        }

        public List<AbilityBase> GetOwnedAbilities(GameObject owner)
        {
            if (!owner) return null;
            abilities.TryGetValue(owner.GetInstanceID(), out var list);
            return list;
        }

        public void ReleaseAbility(int configId, GameObject owner)
        {
            if(!abilities.TryGetValue(owner.GetInstanceID(),out var ownerAbilities))return;
            for (int i = 0; i < ownerAbilities.Count; i++)
                if(ownerAbilities[i].Config.id == configId)
                {
                    var ability = ownerAbilities[i];
                    ownerAbilities.RemoveAt(i);
                    ability.OnRemove();
                    break;
                }
            
        }
    }
}