using System;
using Game.Script.Architecture;
using Game.Script.Event;
using Game.Script.Manager;
using Game.Script.Model;
using Game.Script.Player;
using Game.Script.Ui;
using QFramework;
using Unity.Entities;
using UnityEngine;

namespace Game.Script.Controller
{
    public class PlayerController:MonoBehaviour,IController
    {
        [SerializeField]
        //面板引用
        private ControlPanel controlPanel;
        private PlayerHubPanel playerHubPanel;
        
        [SerializeField]
        private PlayerModel model;
        private BulletManager bulletManager;
        [SerializeField]
        private Vector2 moveVector;

        [SerializeField] 
        private Vector2 shootVector;
        [SerializeField]
        private float shootCoolDown; 
        
        [Header("组件引用")]
        public Rigidbody2D rb;
        public SpriteRenderer sr;
        public Transform shootPoint;
        public PlayerCollider playerCollider;
        public PlayerFsm playerFsm;

        [Header("音效")] 
        public AudioEntry fireSound;
        public AudioEntry hitSound;
        public AudioEntry walkSound;
        public AudioEntry pickUpSound;
        public AudioSource audioSource;
        
        public IArchitecture GetArchitecture()
        {
            return GameApp.Interface;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr=GetComponent<SpriteRenderer>();
            playerCollider=GetComponent<PlayerCollider>();
            playerFsm=GetComponent<PlayerFsm>();
            audioSource=gameObject.AddComponent<AudioSource>();
            shootPoint=transform.GetChild(0);
        }

        

        private void OnEnable()
        {
            model = this.GetModel<PlayerModel>();
            bulletManager=this.GetSystem<BulletManager>();
            playerCollider.Init(model);
            model.OnHurt += Hurt;
            BindingUi();
            TypeEventSystem.Global.Register<OnPlayerShootEvent>(PlayShootAudio);
            TypeEventSystem.Global.Register<OnPlayerMoveEvent>(PlayMoveAudio);
            TypeEventSystem.Global.Register<OnCoinPickupEvent>(PlayPickupAudio);
        }

        private void OnDisable()
        {
            if(controlPanel)
            {
                controlPanel.OnMoveChanged -= BindInputVector;
                controlPanel.OnAttackChanged -= BindInputVector;    
            }
            model.OnHurt -= Hurt;
            TypeEventSystem.Global.UnRegister<OnPlayerShootEvent>(PlayShootAudio);
            TypeEventSystem.Global.UnRegister<OnPlayerMoveEvent>(PlayMoveAudio);
            TypeEventSystem.Global.UnRegister<OnCoinPickupEvent>(PlayPickupAudio);
        }

        private void Update()
        {
            if(shootCoolDown>0.01f)shootCoolDown-=Time.deltaTime;
            ShootBullet(shootVector);
            
            if(moveVector.x<0)
                sr.flipX=true;
            else if(moveVector.x>0)
                sr.flipX=false;
                
        }

        private void FixedUpdate()
        {
            Move();
        }


        private void Move()
        {
            rb.velocity = moveVector* model.speed.Value;
            if (Vector2.SqrMagnitude(moveVector)>0.1f&&model.speed.Value > 0.1f)
            {
                TypeEventSystem.Global.Send(new OnPlayerMoveEvent
                {
                    moveVector = moveVector,
                    speed = model.speed.Value
                });
            }
        }

        #region 音频

        private void PlayShootAudio(OnPlayerShootEvent e)
        => AudioManager.Instance.PlayShot(fireSound);
        

        private void PlayMoveAudio(OnPlayerMoveEvent e)
        =>AudioManager.Instance.PlaySound(walkSound,audioSource);

        private void PlayPickupAudio(OnCoinPickupEvent e)
        => AudioManager.Instance.PlayShot(pickUpSound);

        #endregion  
    
        private void ShootBullet(Vector2 vector)
        {
            if(vector.magnitude<0.01f||shootCoolDown>0.01f||!model.canFire.Value)return;
            bulletManager.FireBullet(shootPoint.position,vector);
            TypeEventSystem.Global.Send(new OnPlayerShootEvent
            {
                pos = shootPoint.position,
                dir = vector
            });
            shootCoolDown = model.shootInterval.Value;
        }
        
        private void BindShootVector(Vector2 vector2)
        =>shootVector=vector2;
        private void BindInputVector(Vector2 vector)
        => moveVector = vector;
        private void BindingUi()
        {
            //角色控制面板bind
            UiManager.Instance.GetPanel<ControlPanel>(nameof(ControlPanel),out var panel);
            controlPanel = panel;
            if(controlPanel)
            {
                controlPanel.OnMoveChanged += BindInputVector;
                controlPanel.OnAttackChanged += BindShootVector;
            }
            //角色hub面板绑定
            UiManager.Instance.GetPanel<PlayerHubPanel>(nameof(PlayerHubPanel),out var hubPanel);
            playerHubPanel = hubPanel;
            if (playerHubPanel)
            {
                model.hp.RegisterWithInitValue(hp =>
                {
                    playerHubPanel.UpdateHealth(model.maxHp.Value, hp);
                }).UnRegisterWhenGameObjectDestroyed(playerHubPanel);
                model.maxHp.RegisterWithInitValue(maxHp =>
                {
                    playerHubPanel.UpdateHealth(maxHp, model.hp.Value);
                }).UnRegisterWhenGameObjectDestroyed(playerHubPanel);
                model.coin.RegisterWithInitValue(playerHubPanel.UpdateCoin)
                    .UnRegisterWhenGameObjectDestroyed(playerHubPanel);
            }
        }

        private void Hurt(float damage, Entity entity)
        {
            if(model.isInvincible.Value)return;
            model.hp.Value=Mathf.Max(model.hp.Value-damage, 0);
            
            playerFsm.ChangeState(PlayerFsm.State.PlayerHurtState);
        }
        
    }
}