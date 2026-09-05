using System.Collections.Generic;
using Interaction.Player;
using UnityEngine;

namespace PlayerControllerSpace
{
    /// <summary>
    /// 玩家动画控制 现阶段专注 FP
    /// </summary>
    public partial class PlayerAnimationController : MonoBehaviour, IPlayerInteractAnim
    {
        private const string LightAttack = "LightAttack";
        private const string HeavyAttack = "HeavyAttack";
        private const string ConsumingTag = "IsConsuming";
        private const float AttackChainNormalizedTime = 0.9f;

        [SerializeField]
        private AnimationModelSoData animationAssets;
        /// <summary> FP Animator </summary>
        [SerializeField]
        private Animator fpAnimator;
        /// <summary> 身体 Animator 暂留给旧跳跃 FSM </summary>
        [SerializeField]
        private Animator bodyAnimator;

        /// <summary> FP Animator </summary>
        public Animator FpAnimator => fpAnimator;

        /// <summary> 身体 Animator 暂留 </summary>
        public Animator BodyAnimator => bodyAnimator;

        public AnimationModelSoData AnimationAssets => animationAssets;

        private void Awake()
        {
            Init();
        }


        #region 通用方法
        /// <summary>
        /// 
        /// </summary>
        private void Init()
        {
            if (fpAnimator == null)
            {
                Transform handsRoot = transform.Find("Model/CameraPos/HandsRoot");
                if (handsRoot == null)
                    handsRoot = GetComponentInChildren<FpMotion.FpHandsMotion>(true)?.transform;
                if (handsRoot != null)
                    fpAnimator = handsRoot.GetComponentInChildren<Animator>(true);
            }
            if (bodyAnimator == null)
            {
                Transform playerModel = transform.Find("PlayerModel");
                if (playerModel == null)
                    playerModel = transform.Find("Model/PlayerModel");
                if (playerModel != null)
                    bodyAnimator = playerModel.GetComponentInChildren<Animator>(true);
            }

            // 构建动画控制器字典
            animationAssets.InitAnimationControllerDict();

            InitEatPerformance();
        }


        /// <summary>
        /// 播放动画片段 无过渡
        /// </summary>
        public void PlayeClip(int stateHash)
        {
            if (fpAnimator == null || stateHash == 0)
                return;

            fpAnimator.Play(stateHash);
        }

        /// <summary>
        /// 动画片段过渡 自定义过渡时间
        /// </summary>
        public void CrossFadeClip(int stateHash, float fade = 0f)
        {
            if (fpAnimator == null)
                return;
            fpAnimator.CrossFadeInFixedTime(stateHash, fade, 0);
        }

        /// <summary>
        /// 攻击播放 起手 CrossFade 同 Tag 未播完不重触发 播完 Play 连段
        /// </summary>
        private void PlayAttackClip(int stateHash, float fade, string attackTag, float animSpeed)
        {
            if (fpAnimator == null || stateHash == 0)
                return;

            if (fpAnimator.IsInTransition(0))
            {
                if (fpAnimator.GetNextAnimatorStateInfo(0).IsTag(attackTag))
                    return;
            }

            AnimatorStateInfo currentInfo = fpAnimator.GetCurrentAnimatorStateInfo(0);
            if (currentInfo.IsTag(attackTag))
            {
                if (currentInfo.normalizedTime < AttackChainNormalizedTime)
                    return;

                ApplyFpAnimSpeed(animSpeed);
                PlayeClip(stateHash);
                return;
            }

            ApplyFpAnimSpeed(animSpeed);
            CrossFadeClip(stateHash, fade);
        }

        /// <summary>
        /// 设置 FP Animator 播放速度
        /// </summary>
        private void ApplyFpAnimSpeed(float speed)
        {
            if (fpAnimator == null)
                return;
            fpAnimator.speed = Mathf.Max(0.01f, speed);
        }

        /// <summary>
        /// 非攻击状态时把速度拉回 1
        /// </summary>
        private void LateUpdate()
        {
            LateUpdateEatFallback();

            if (fpAnimator == null)
                return;

            if (IsFpInAttackState())
                return;

            if (!Mathf.Approximately(fpAnimator.speed, 1f))
                fpAnimator.speed = 1f;
        }

        /// <summary>
        /// 当前或过渡目标是否在轻重攻击 Tag
        /// </summary>
        private bool IsFpInAttackState()
        {
            if (fpAnimator.IsInTransition(0))
            {
                var nextInfo = fpAnimator.GetNextAnimatorStateInfo(0);
                if (nextInfo.IsTag(LightAttack) || nextInfo.IsTag(HeavyAttack))
                    return true;
            }

            var currentInfo = fpAnimator.GetCurrentAnimatorStateInfo(0);
            return currentInfo.IsTag(LightAttack) || currentInfo.IsTag(HeavyAttack);
        }

        /// <summary>
        /// 当前或过渡目标是否在消耗品状态
        /// </summary>
        private bool IsFpInConsumingState()
        {
            if (fpAnimator == null)
                return false;

            if (fpAnimator.IsInTransition(0))
            {
                var nextInfo = fpAnimator.GetNextAnimatorStateInfo(0);
                if (nextInfo.IsTag(ConsumingTag))
                    return true;
            }

            var currentInfo = fpAnimator.GetCurrentAnimatorStateInfo(0);
            return currentInfo.IsTag(ConsumingTag);
        }

        #endregion
    }
}
