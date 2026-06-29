using Unity.Entities;
using UnityEngine;

namespace Game.Script.Model
{
    //单个关键帧
    public struct SpriteKeyFrame
    {
        public BlobString SpriteName;
        public float Duration;
    }
         
    //动画片段
    public struct SpriteClip
    {
        public BlobString Name;//片段名称
        public BlobArray<SpriteKeyFrame> Frames;//全部帧
        public byte Loop;//0不循环，1循环
    }
    
    public struct SpriteClipBlob
    {
        public BlobArray<SpriteClip> Clips;//全部动画片段
    }
}