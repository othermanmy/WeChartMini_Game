using System;
using System.Collections.Generic;
using Game.Script.ECS.Components.Enemy;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

namespace Game.Script.Model.Authoring
{
    public class EnemyPrefabRefsAuthoring : MonoBehaviour
    {
        [Serializable]
        public struct EnemyPrefabEntry
        {
            public string typeName;
            public GameObject prefab;
        }

        public List<EnemyPrefabEntry> entries = new();

        private class Baker : Baker<EnemyPrefabRefsAuthoring>
        {
            public override void Bake(EnemyPrefabRefsAuthoring authoring)
            {
                foreach (var entry in authoring.entries)
                {
                    if (!entry.prefab)
                    {
                        Debug.LogError($"[EnemyPrefabRefsAuthoring] Entry '{entry.typeName}' has null prefab — skipped.");
                        continue;
                    }

                    // 创建标记实体，挂 EntityPrefabReference
                    var markerEntity = CreateAdditionalEntity(TransformUsageFlags.None);

                    AddComponent(markerEntity, new EnemyTypeName { Value = entry.typeName });
                    
#if UNITY_EDITOR
                    var assetPath = UnityEditor.AssetDatabase.GetAssetPath(entry.prefab);
                    var guid = UnityEditor.AssetDatabase.GUIDFromAssetPath(assetPath);
                    var prefabRef = new EntityPrefabReference(guid);
#else
                    var prefabRef = new EntityPrefabReference(new Unity.Entities.Hash128());
#endif
                    AddComponent(markerEntity, new EnemyPrefabRef { Value = prefabRef });
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 自动修正空 typeName
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (string.IsNullOrWhiteSpace(e.typeName) && e.prefab)
                {
                    e.typeName = e.prefab.name;
                    entries[i] = e;
                }
            }
        }
#endif
    }
}