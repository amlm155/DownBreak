using MieMieFrameWork;
using MieMieFrameWork.M_InputSystem;
using MiMieFSM.Unity;
using MiMieFSM.UpdateFsm;
using MotionCharacterController;
using UnityEngine;
using UnityEngine.InputSystem;
using GAS.StateSystem;
using Interaction;
using Interaction.Combat;
using Interaction.Player;
using DBGameSystem;
using DBWeaponSystem;
using GAS.Component;
using GAS.Core;

namespace PlayerControllerSpace
{
    [RequireComponent(typeof(MotionCC)),
    RequireComponent(typeof(Collider)),
    RequireComponent(typeof(UpdateFsmHost))]
    public partial class PlayerController : MonoBehaviour, IDamageable
    {
        #region 组件
        private MotionCC motionController;
        private Collider playerCollider;
        private UpdateFsmHost fsmHost;
        private PlayerAnimationController playerAmController;
        private PlayerInteractCore playerInteract;
        private PlayerCamera playerCameraComponent;
        private StatController statController;
        private GEManager geManager;
        private AbilitySystemMgr abilitySystemMgr;
        private PlayerSurvivalEffectController survivalEffectController;
        public InteractionManager interactionManager;
        /// <summary> 输入管理器缓存 </summary>
        private InputManager inputManager;
        #endregion
        

        #region 属性

        // 状态
        public bool IsStableOnGround => motionController.GroundingStatus.IsStableOnGround;
        public bool IsJumpRequested => motionController.JumpRequested;
        public bool IsMoving => inputManager != null && inputManager.IsMovePressed;
        public float VerticalSpeed => motionController.VerticalSpeed;
        public bool IsCrouching => isCrouching;
        public bool IsSprintPressed => inputManager != null && inputManager.IsSprintPressed;
        public bool IsSprintHeld => inputManager != null && inputManager.IsSprintHeld;
        public Vector3 PlayerUp => motionController.CharacterUp;

        /// <summary> 背包暂停等 UI 打开时屏蔽攻击交互 </summary>
        public bool IsGameplayActionEnabled => CursorController.IsLocked;
        // 按键
        public bool CrouchHeld => inputManager != null && inputManager.IsCrouchHeld;
        public bool IsInteractHeld => IsGameplayActionEnabled
            && inputManager != null && inputManager.IsInteractHeld;
        public bool IsInteractStarted => IsGameplayActionEnabled
            && inputManager != null && inputManager.IsInteractStarted;
        public bool IsLeftAttackHeld => IsGameplayActionEnabled
            && inputManager != null && inputManager.IsLeftAttackHeld;
        public bool IsRightAttackHeld => IsGameplayActionEnabled
            && inputManager != null && inputManager.IsRightAttackHeld;
        public bool IsLeftAttackPressed => IsGameplayActionEnabled
            && inputManager != null && inputManager.IsLeftAttackPressed;
        public bool IsRightAttackPressed => IsGameplayActionEnabled
            && inputManager != null && inputManager.IsRightAttackPressed;
        public bool IsRotatePressed => inputManager != null && inputManager.IsRotatePressed;
        public bool IsFlashlightPressed => IsGameplayActionEnabled
            && inputManager != null && inputManager.IsFlashlightPressed;

        // 组件

        public MccConfig mccConfig => motionController.Config;
        public PlayerConfig PlayerRtConfig => PlayerConfig.Instance;
        public PlayerAnimationController PlayerAmController => playerAmController;
        public StateMachine Machine => fsmHost.Machine;
        public MotionCC MotionController => motionController;
        public PlayerCamera PlayerCameraComponent => playerCameraComponent;
        public FpMotion.FpHandsMotion HandsMotion => handsMotion;
        public IInteraction InteractionManager => interactionManager;
        public StatController StatController => statController;
        /// <summary> 玩家交互核心 </summary>
        public PlayerInteractCore PlayerInteract => playerInteract;
        #endregion

        #region 生命周期

        private void Awake()
        {
            InitComponents();
        }

        private void Start()
        {
            CursorController.Lock();

            StartStatController();
            StartComponent();
            StartSurvivalEffects();
            StartPlayerInteract();
        }

        /// <summary>
        /// 初始化玩家生存 GE
        /// </summary>
        private void StartSurvivalEffects()
        {
            geManager.SetStatController(statController);
            geManager.SetOwner(abilitySystemMgr);
            survivalEffectController.Init(statController, geManager);
        }

        private void OnDestroy()
        {
            if (GameHub.Get<IWeaponSystem>() != null && playerInteract != null)
                GameHub.Get<IWeaponSystem>().OnEquippedAnimationRequest
                    -= playerInteract.SwitchWeaponModule;
            UnregisterPlayerServices();
        }

        private void Update()
        {
            // UI 打开时仍可移动 但停掉攻击交互与切武器
            if (IsGameplayActionEnabled)
            {
                UpdatePlayerInteract();

                // // 测试:切换武器模组
                // if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
                //     playerInteract.ChangeToKnife();

                // if (Keyboard.current BagPanelGenPartial!= null && Keyboard.current.digit2Key.wasPressedThisFrame)
                //     playerInteract.ChangeToSingleWeapon();

                // if (Keyboard.current != null && Keyboard.current.digit3Key.wasPressedThisFrame)
                //     playerInteract.ChangeToDoubleWeapon();

                // if (Keyboard.current != null && Keyboard.current.digit4Key.wasPressedThisFrame)
                //     playerInteract.ChangeToLantern();
            }

            // 更新玩家体力
            UpdatePlayerPower();
        }

        private void LateUpdate()
        {
            UpdateCrouchEye();
            ApplyLookRotation();
            ApplyEyePitch();
            TickHandsMotion();
            UpdateSprintFov();
        }

        #endregion
    }
}
