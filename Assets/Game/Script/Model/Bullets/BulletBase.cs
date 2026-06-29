using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace Game.Script.Model.Bullets
{
    public class BulletBase:MonoBehaviour
    {
        public Transform trans;

        public Vector2 position;

        public Vector2 direction;
        
        public BindableProperty<float> currentLifeTime=new();

        public  BindableProperty<float> currentSpeed=new ();

        /// <summary>
        /// 检查半径
        /// </summary>
        public  BindableProperty<float> currentRadius=new ();

        public  BindableProperty<float> currentDamage=new();
        
        [SerializeField]
        private  bool showCollider;

        private TrailRenderer trailRenderer;
        private float originalTrailWidth;
        
        private void Awake()
        {
            trans = transform;
            trailRenderer = GetComponentInChildren<TrailRenderer>();
            if (trailRenderer)
                originalTrailWidth = trailRenderer.widthMultiplier;
        }

        public void ClearAndStopTrail()
        {
            if (!trailRenderer) return;
            trailRenderer.enabled = false;
            trailRenderer.Clear();
        }

        public void ResumeTrail()
        {
            if (trailRenderer)
                trailRenderer.enabled = true;
        }

        public void SetTrailWidth(float width)
        {
            if (trailRenderer)
                trailRenderer.widthMultiplier = width;
        }

        public float OriginalTrailWidth => originalTrailWidth;

        public void Init(PlayerModel config,List<IBulletTrait> traits, Vector2 pos, Vector2 dir)
        {
            position = pos;
            direction = dir.normalized;
            currentLifeTime.Value = config.bulletLifeTime.Value;
            currentSpeed.Value = config.bulletSpeed.Value;
            currentRadius.Value = config.bulletHitRadius.Value;
            foreach(IBulletTrait trait in traits)
                trait.OnSpawn(this);
        }

        private void OnDrawGizmosSelected()
        {
            if(!showCollider)return;
            var oldColor = Gizmos.color;
            Gizmos.color=Color.green;
            Gizmos.DrawWireSphere(position, currentRadius.Value);
            Gizmos.color = oldColor;
        }
    }
}