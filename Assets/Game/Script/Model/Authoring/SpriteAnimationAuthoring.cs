using System.Collections.Generic;
using Game.Script.ECS.Components.Anim;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Script.Model.Authoring
{
    public class SpriteAnimationAuthoring : MonoBehaviour
    {
        [Header("动画片段配置")]
        public List<ClipConfig> Clips;
        
        [System.Serializable]
        public struct ClipConfig
        {
            public string Name;
            public Sprite[] Frames;
            public float DefaultFrameDuration;   // 统一帧时长
            public bool Loop;
        }
        

        private class SpriteAnimationBaker : Baker<SpriteAnimationAuthoring> 
        {
            public override void Bake(SpriteAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Clips 未配置时跳过
                if (authoring.Clips == null || authoring.Clips.Count == 0)
                    return;
                
                BlobAssetReference<SpriteClipBlob> blobRef;
                
                var spriteMap = new Dictionary<string, Sprite>();
                
                using (var blobBuilder = new BlobBuilder(Allocator.Temp))
                {
                    var bb = blobBuilder;
                    ref var root = ref blobBuilder.ConstructRoot<SpriteClipBlob>();

                    int clipCount = authoring.Clips.Count;
                    var clipArray = blobBuilder.Allocate(ref root.Clips, clipCount);

                    for (int c = 0; c < clipCount; c++)
                    {
                        ref var clip = ref clipArray[c];
                        var cfg = authoring.Clips[c];

                        bb.AllocateString(ref clip.Name, cfg.Name);
                        clip.Loop = cfg.Loop ? (byte)1 : (byte)0;

                        int frameCount = cfg.Frames.Length;
                        var frameArray = blobBuilder.Allocate(ref clip.Frames, frameCount);

                        for (int f = 0; f < frameCount; f++)
                        {
                            ref var frame = ref frameArray[f];
                            var sprite = cfg.Frames[f];
                            // 在 Blob 中只存 Sprite 名称
                            bb.AllocateString(ref frame.SpriteName, sprite.name);
                            spriteMap[sprite.name] = sprite;
                            frame.Duration = cfg.DefaultFrameDuration;
                        }
                    }
                    blobRef = blobBuilder.CreateBlobAssetReference<SpriteClipBlob>(Allocator.Persistent);
                }
                
                AddComponent(entity, new SpriteAnimationClips
                {
                    Clips = blobRef
                });
                
                AddComponentObject(entity, new SpriteRegisterData
                {
                    SpriteMap = spriteMap
                });

                int totalClipCount = authoring.Clips.Count;
                var firstClipName = totalClipCount > 0 ? authoring.Clips[0].Name : "";
                AddComponent(entity, new SpriteAnimationPlay
                {
                    ClipName = firstClipName,
                    FrameIndex = 0,
                    Timer = 0f,
                    Speed = 1f,
                    IsPlaying = true,
                    IsPaused = false
                });
                
                // 默认取第一个精灵的名称
                FixedString64Bytes defaultSpriteName = "";
                if (totalClipCount > 0 && authoring.Clips[0].Frames.Length > 0)
                    defaultSpriteName = new FixedString64Bytes(authoring.Clips[0].Frames[0].name);

                AddComponent(entity, new SpriteRenderData
                {
                    CurrentSpriteName = defaultSpriteName,
                    Color = new float4(1f, 1f, 1f, 1f)  // 默认白色
                });
                AddComponent(entity, new AnimState { State = firstClipName });

            }
        }
    }
}
