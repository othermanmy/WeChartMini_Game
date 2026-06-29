using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Script.Model.Ability
{
    public abstract class AbilityBase
    {
        public AbilityConfig Config { get; private set; }
        public bool IsActive { get; private set; }
        public GameObject Owner { get; private set; }

        public virtual void Init(AbilityConfig config, GameObject owner)
        {
            Config = config;
            Owner = owner;
            IsActive = true;
        }

        public abstract void OnAcquire();
        public abstract void OnUpdate(float dt);
        public abstract void OnRemove();

        protected T GetParameter<T>(string key, T defaultValue = default)
        {
            if (Config?.parameters == null) return defaultValue;
            if (!Config.parameters.TryGetValue(key, out var token) || token.Type == JTokenType.Null)
                return defaultValue;
            return token.ToObject<T>();
        }

        protected JObject Params => Config?.parameters;
    }
}