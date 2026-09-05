using DBWeaponSystem;
using MieMieFrameWork.Asset;
using MotionCharacterController;
using PlayerControllerSpace.FpMotion;
using UnityEngine;
using UnityEngine.InputSystem;
using MiMieFSM.Unity;
using MiMieFSM.UpdateFsm;
using MieMieFrameWork;
using GAS.Component;
using GAS.Core;
using GAS.Core.GameplayEffect;
using GAS.StateSystem;
using DBGameSystem;
using Interaction;
using Interaction.Player;
using MieMieFrameWork.M_InputSystem;

namespace PlayerControllerSpace
{
    public partial class PlayerController
    {
        #region 配置

        [SerializeField]
        /// <summary> 眼睛挂点 </summary>
        private Transform eyeTransform;

        [SerializeField]
        /// <summary> 手部程序化运动 </summary>
        private FpHandsMotion handsMotion;

        #endregion

        #region 运行时状态

        /// <summary> 水平朝向角 </summary>
        private float yaw;

        /// <summary> 相机俯仰角 </summary>
        private float pitch;

        /// <summary> 侧视角滚转角 </summary>
        private float slantRoll;

        /// <summary> 是否正在下蹲 </summary>
        private bool isCrouching;

        /// <summary> 眼点下蹲混合 0站立 1下蹲 </summary>
        private float crouchEyeBlend;

        /// <summary> 站立胶囊 height与yOffset </summary>
        private Vector2 standingCapsuleData;

        /// <summary> 站立时眼睛挂点的原始本地坐标 </summary>
        private Vector3 standingEyeLocalPosition;

        /// <summary> 这一帧不含呼吸的眼点基准局部位移 </summary>
        private Vector3 eyeBaseLocalPosition;

        /// <summary> 呼吸累积增量 </summary>
        private float breathCacheDelta;

        /// <summary> 起身重叠检测缓冲 </summary>
        private readonly Collider[] crouchOverlapBuffer = new Collider[MccConfig.MAX_COLLISION_OVERLAPS];

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化组件与朝向 
        /// </summary>
        internal void InitComponents()
        {
            motionController = GetComponent<MotionCC>();
            playerCollider = GetComponent<Collider>();
            fsmHost = GetComponent<UpdateFsmHost>();
            playerAmController = GetComponentInChildren<PlayerAnimationController>();
            playerCameraComponent = GetComponentInChildren<PlayerCamera>();
            statController = GetComponent<StatController>();
            geManager = GetComponent<GEManager>();
            if (geManager == null)
                geManager = gameObject.AddComponent<GEManager>();

            abilitySystemMgr = GetComponent<AbilitySystemMgr>();
            if (abilitySystemMgr == null)
                abilitySystemMgr = gameObject.AddComponent<AbilitySystemMgr>();

            survivalEffectController = GetComponent<PlayerSurvivalEffectController>();
            if (survivalEffectController == null)
                survivalEffectController = gameObject.AddComponent<PlayerSurvivalEffectController>();

            InitYawAndPitch();
            CacheStandingCapsule();
            CacheStandingEye();

        }

        /// <summary>
        /// 初始化属性控制器
        /// </summary>
        private void StartStatController()
        {
            // 属性 SO 走完整路径 不依赖短名别名
            var statDataPathList = new string[]
            {
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Immediate/Health.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Immediate/Food.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Immediate/Water.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Immediate/Power.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Immediate/San.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Immediate/Energy.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Immediate/Pain.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Passive/Attack.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Passive/Defence.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Passive/MoveSpeed.asset",
                "Assets/Arts/InteranlArts/Configs/GASConfig/PlayerState/Passive/JumpSpeed.asset",
            };
            for (int i = 0; i < statDataPathList.Length; i++)
            {
                var statData = MmAssetMgr.LoadAsset<StatData>(statDataPathList[i]);
                if (statData != null)
                    statController.AddStatData(statData);
            }
            statController.Init();

            // 订阅 stat 变化并设置自然衰减速率
            InitStatEvents();
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void StartComponent()
        {
            interactionManager = GameHub.Get<IInteraction>() as InteractionManager;
            inputManager = ModuleHub.Instance.GetManager<InputManager>();
            RegisterPlayerServices();
            // 状态机黑板与切换初始状态
            fsmHost.Machine.Blackboard.SetValue(EBlockBoardParme.PlayerController, this);
            fsmHost.Machine.ChangeState<PlayerIdle>();

            // 初始化玩家动画控制器
            fsmHost.Machine.Blackboard.SetValue(EBlockBoardParme.PlayerAnimator, playerAmController);
        }

        /// <summary>
        /// 初始化玩家交互器
        /// </summary>
        private void StartPlayerInteract()
        {
            if (playerInteract != null)
            {
                Debug.LogWarning("玩家交互器已初始化");
                return;
            }
            playerInteract = new PlayerInteractCore();
            BindWeaponAnimationRequest();
            BindAttackWindowEvents();

            if (PlayerAmController.IsFpControllerNull() ||
                PlayerAmController.IsFpController(EAnimationModelType.None))
            {
                playerInteract.ChangeToNullHand();
            }
        }

        /// <summary>
        /// 初始化yaw和pitch
        /// </summary>
        private void InitYawAndPitch()
        {
            // 用当前朝向初始化 yaw
            Vector3 planarForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (planarForward.sqrMagnitude > 0.0001f)
                yaw = Quaternion.LookRotation(planarForward.normalized, Vector3.up).eulerAngles.y;
            else
                yaw = transform.eulerAngles.y;

            pitch = 0f;
        }

        /// <summary>
        /// 缓存站立胶囊尺寸
        /// </summary>
        private void CacheStandingCapsule()
        {
            var config = motionController.Config;
            // x=height y=yOffset 半径蹲站不变直接读Config
            standingCapsuleData = new Vector2(config.capsuleHeight, config.capsuleYOffset);
        }

        /// <summary>
        /// 缓存站立相机挂点
        /// </summary>
        private void CacheStandingEye()
        {
            if (eyeTransform == null)
                return;
            standingEyeLocalPosition = eyeTransform.localPosition;
            eyeBaseLocalPosition = standingEyeLocalPosition;
        }

        /// <summary>
        /// 更新游戏光标逻辑状态
        /// </summary>
        private void UpdateGameCursor()
        {
            if (inputManager != null && inputManager.IsLookPressed)
                CursorController.Lock();
        }

        #endregion

        #region 移动

        /// <summary>
        /// 应用地面移动
        /// </summary>
        /// <param name="currentVelocity">当前速度</param>
        /// <param name="deltaTime">时间差</param>
        internal void ApplyGroundMove(ref Vector3 currentVelocity, float deltaTime)
        {
            var config = motionController.Config;
            // 实时获取地面法线
            var groundNormal = motionController.GroundingStatus.GroundNormal;
            // 从Input接口获取的输入向量
            var moveInput = motionController.InputDirection;

            // 将当前速度方向时刻保持贴地
            currentVelocity = motionController.GetDirectionTangentToSurface(currentVelocity, groundNormal)
                              * currentVelocity.magnitude;

            // 没有输入目标速度就是 0 有输入则把输入扳到地面切线再乘移速
            Vector3 targetVelocity = Vector3.zero;
            bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
            if (hasMoveInput)
            {
                // Cross(输入, up) 得到输入右侧
                Vector3 inputRight = Vector3.Cross(moveInput, PlayerUp);
                // Cross(地面法线, 右侧) 得到沿坡面的前进方向
                Vector3 reorientedInput = Vector3.Cross(groundNormal, inputRight).normalized
                                          * moveInput.magnitude;
                // 判断跑步 走路 还是下蹲 
                // 基础移速 = 基础移速值 * 配置倍率
                float moveSpeedBase = config.moveSpeed;
                if (inputManager != null && inputManager.IsSprintHeld && StatController.GetCurrentValue(PlayerStatIds.Power) > 0f)
                {
                    moveSpeedBase = config.moveSpeed * PlayerRtConfig.SprintMoveSpeedRate;
                }
                else if (inputManager != null && inputManager.IsCrouchHeld)
                {
                    moveSpeedBase = config.moveSpeed * PlayerRtConfig.CrouchMoveSpeedRate;
                }
                else
                {
                    moveSpeedBase = config.moveSpeed * PlayerRtConfig.NormalMoveSpeedRate;
                }
                // 最终速度 = 输入方向 * 基础移速 * 属性速度
                targetVelocity = reorientedInput
                                 * moveSpeedBase
                                 * StatController.GetCurrentValue(PlayerStatIds.MoveSpeed);
            }

            // 指数平滑追目标 手感由 stableMovementSharpness 控制
            float t = 1f - Mathf.Exp(-config.stableMovementSharpness * deltaTime);
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, t);
        }

        #endregion

        #region 跳跃与重力

        /// <summary>
        /// 应用跳跃冲力
        /// </summary>
        /// <param name="currentVelocity">当前速度</param>
        internal void ApplyJumpImpulse(ref Vector3 currentVelocity)
        {
            // 强制离地
            motionController.ForceUnground();
            // 应用跳跃冲力
            currentVelocity += PlayerUp * mccConfig.jumpSpeed
                               * StatController.GetCurrentValue(PlayerStatIds.JumpSpeed)
                               // 清掉旧竖直速度
                               - Vector3.Project(currentVelocity, PlayerUp);
            // 消费跳跃请求
            motionController.ConsumeJumpRequest();
        }

        /// <summary>
        /// 应用重力
        /// </summary>
        /// <param name="currentVelocity"></param>
        /// <param name="deltaTime"></param>
        internal void ApplyGravity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 实时获取角色up朝向 因为上斜坡时up的值会减少
            currentVelocity += PlayerUp * motionController.Config.gravity * deltaTime;
            currentVelocity *= 1f / (1f + motionController.Config.airDrag * deltaTime);
        }

        #endregion

        #region 视角

        /// <summary>
        /// 冲刺 FOV 插值 放在视觉帧 不跟物理移动绑死
        /// </summary>
        internal void UpdateSprintFov()
        {
            if (playerCameraComponent == null)
                return;

            bool isSprinting = IsSprintHeld
                && IsMoving
                && StatController.GetCurrentValue(PlayerStatIds.Power) > 0f;
            float targetFov = isSprinting
                ? PlayerRtConfig.NormalFov + PlayerRtConfig.SprintFovIncrement
                : PlayerRtConfig.NormalFov;
            playerCameraComponent.ChangeFov(targetFov, Time.deltaTime, PlayerRtConfig.SprintFovLerpSpeed);
        }

        /// <summary>
        /// 渲染帧处理鼠标视角 立刻刷新画面并同步马达
        /// </summary>
        internal void ApplyLookRotation()
        {
            Vector2 look = Vector2.zero;
            if (CursorController.IsLocked && inputManager != null)
                look = inputManager.LookInput;

            float sensitivity = PlayerRtConfig.LookSensitivity;
            Vector2 verticalLimit = PlayerRtConfig.VerticalLookLimit;
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, verticalLimit.x, verticalLimit.y);

            // 身体只转水平 绕过插值避免画面卡顿
            Quaternion yawRot = Quaternion.AngleAxis(yaw, PlayerUp);
            // 设置身体旋转瞬间 同步MCC的旋转状态
            motionController.SetRotation(yawRot, true);
        }

        /// <summary>
        /// 相机写俯仰与 EQ 侧倾 不叠呼吸 避免镜头晃
        /// </summary>
        internal void ApplyEyePitch()
        {
            if (eyeTransform == null)
                return;

            UpdateSlantRoll();
            eyeTransform.localPosition = eyeBaseLocalPosition;
            eyeTransform.localRotation = Quaternion.Euler(pitch, 0f, slantRoll);
        }

        /// <summary>
        /// 按住 Q/E 插值到配置侧倾角
        /// </summary>
        private void UpdateSlantRoll()
        {
            float targetRoll = 0f;
            if (CursorController.IsLocked)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.qKey.isPressed)
                        targetRoll = PlayerRtConfig.LeftSlantAngle;
                    else if (keyboard.eKey.isPressed)
                        targetRoll = PlayerRtConfig.RightSlantAngle;
                }
            }

            float lerpFactor = 1f - Mathf.Exp(-12f * Time.deltaTime);
            slantRoll = Mathf.Lerp(slantRoll, targetRoll, lerpFactor);
        }

        /// <summary>
        /// 推进 HandsRoot 弹簧手感与武器呼吸 须在视角之后
        /// </summary>
        internal void TickHandsMotion()
        {
            if (handsMotion == null || !PlayerRtConfig.EnableFpHandsMotion)
                return;

            Vector2 look = Vector2.zero;
            if (CursorController.IsLocked && inputManager != null)
                look = inputManager.LookInput;

            float amplitude = PlayerRtConfig.ProceduralBreathAmplitude;
            float frequency = PlayerRtConfig.ProceduralBreathFrequency;
            breathCacheDelta += Time.deltaTime * frequency * Mathf.PI * 2f;
            float s = Mathf.Sin(breathCacheDelta);
            float c = Mathf.Cos(breathCacheDelta);
            // 移动时压低呼吸 避免和走路抢戏
            float weight = IsMoving ? 0.25f : 1f;
            Vector3 breathPos = new Vector3(c * amplitude * 0.35f, s * amplitude, 0f) * weight;
            Vector3 breathEuler = new Vector3(s * amplitude * 20f * weight, 0f, c * amplitude * 10f * weight);

            handsMotion.Tick(look, motionController, isCrouching, Time.deltaTime, breathPos, breathEuler);
        }

        /// <summary>
        /// 模拟帧同步物理朝向到同一套 yaw
        /// </summary>
        /// <param name="currentRotation">当前旋转</param>
        internal void ApplyRotation(ref Quaternion currentRotation)
        {
            currentRotation = Quaternion.AngleAxis(yaw, PlayerUp);
        }

        #endregion

        #region 下蹲

        /// <summary>
        /// 蹲下
        /// </summary>
        internal void Crouch()
        {
            // 已在下蹲则跳过
            if (isCrouching)
                return;

            float radius = motionController.Config.capsuleRadius;
            float crouchHeight = PlayerRtConfig.CrouchHeight;
            // 碰撞体立刻切换 视觉由眼点插值承担过渡
            motionController.SetCapsuleDimensions(radius, crouchHeight, GetCrouchedYOffset());
            isCrouching = true;
        }

        /// <summary>
        /// 站立 头顶有阻挡则保持下蹲
        /// </summary>
        /// <returns>是否成功站立</returns>
        internal bool Stand()
        {
            // 已站立则跳过
            if (!isCrouching)
                return true;

            float radius = motionController.Config.capsuleRadius;
            // 先拉回站立胶囊再做重叠检测
            motionController.SetCapsuleDimensions(radius, standingCapsuleData.x, standingCapsuleData.y);
            int overlapCount = motionController.CharacterCollisionsOverlap(
                motionController.TransientPosition,
                motionController.TransientRotation,
                crouchOverlapBuffer);

            // 头顶或周围有阻挡 缩回下蹲胶囊
            if (overlapCount > 0)
            {
                motionController.SetCapsuleDimensions(radius, PlayerRtConfig.CrouchHeight, GetCrouchedYOffset());
                return false;
            }

            isCrouching = false;
            return true;
        }

        /// <summary>
        /// 下蹲胶囊Y偏移 保脚底高度不变
        /// </summary>
        private float GetCrouchedYOffset()
        {
            float crouchHeight = PlayerRtConfig.CrouchHeight;
            return standingCapsuleData.y - (standingCapsuleData.x - crouchHeight) * 0.5f;
        }

        /// <summary>
        /// 插值眼点高度 做出蹲起过程感
        /// </summary>
        internal void UpdateCrouchEye()
        {
            if (eyeTransform == null)
                return;

            float targetBlend = isCrouching ? 1f : 0f;
            float duration = Mathf.Max(0.01f, PlayerRtConfig.CrouchDuration);
            crouchEyeBlend = Mathf.MoveTowards(crouchEyeBlend, targetBlend, Time.deltaTime / duration);

            Vector3 eyeLocalPos = standingEyeLocalPosition;
            eyeLocalPos.y -= (standingCapsuleData.x - PlayerRtConfig.CrouchHeight) * crouchEyeBlend;
            eyeBaseLocalPosition = eyeLocalPos;
        }

        #endregion

        #region 交互

        /// <summary>
        /// 每帧调度交互管理器
        /// </summary>
        private void UpdatePlayerInteract()
        {
            playerInteract?.Tick();
        }

        /// <summary>
        /// 本帧是否有玩家动作
        /// </summary>
        public bool HasPlayerAction()
        {
            var input = inputManager;
            if (input == null)
                return false;

            return IsMoving
                || input.IsLookPressed
                || input.IsJumpPressed
                || input.IsJumpHeld
                || input.IsCrouchPressed
                || input.IsCrouchHeld
                || input.IsSprintHeld
                || input.IsLeftAttackHeld
                || input.IsRightAttackHeld
                || input.IsInteractStarted
                || input.IsInteractHeld
                || input.IsAnyInput;
        }

        #endregion
    }
}
