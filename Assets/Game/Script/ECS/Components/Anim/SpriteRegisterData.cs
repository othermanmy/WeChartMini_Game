using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Script.ECS.Components.Anim
{
    public class SpriteRegisterData : IComponentData
    {
        public Dictionary<string, Sprite> SpriteMap = new();
    }
}