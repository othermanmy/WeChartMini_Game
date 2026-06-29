using System;
using Newtonsoft.Json.Linq;

namespace Game.Script.Model
{
    [Serializable]
    public class AbilityConfig
    {
        public int id;//id
        public int priority;//优先级
        public string name;//名称
        public string iconPath;//图标路径
        public string description;//描述
        public string className;//类名
        public JObject parameters;//参数
        public AbilityType abilityType;//能力类型
        public string panelName;//面板名称
    }
    public enum AbilityType
    {
        Passive,//被动
        Active//主动
    }
}