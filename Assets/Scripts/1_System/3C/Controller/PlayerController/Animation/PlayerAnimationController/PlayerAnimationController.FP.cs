using DBWeaponSystem;
using MieMieFrameWork.MMAnimation;
using Interaction.Player;
using UnityEngine;

namespace PlayerControllerSpace
{
    public partial class PlayerAnimationController : IPlayerInteractAnim
    {
        /// <summary>
        /// 如果玩家长时间不动作 就随机播放一个待机动画
        /// </summary>
        public void PlayViewAnimation()
        {
            fpAnimator.Play(PlayerAmHashMap.检视);
        }

        /// <summary>
        /// 按当前FP控制器播放对应攻击动画
        /// </summary>
        public void PlayLightAttack()
        {
            if (fpAnimator.IsAnimationAtTag(HeavyAttack))
                return;
            float speed = PlayerConfig.Instance != null
                ? PlayerConfig.Instance.LightAttackAnimSpeed
                : 1f;
            PlayAttackClip(PlayerAmHashMap.轻攻击, 0.1f, LightAttack, speed);
        }

        /// <summary>
        /// 按当前FP控制器播放对应重攻击动画
        /// </summary>
        public void PlayHeavyAttack()
        {
            if (fpAnimator.IsAnimationAtTag(LightAttack))
                return;

            float speed = PlayerConfig.Instance != null
                ? PlayerConfig.Instance.HeavyAttackAnimSpeed
                : 1f;
            PlayAttackClip(PlayerAmHashMap.重攻击, 0.1f, HeavyAttack, speed);
        }

        /// <summary>
        /// 检查当前Fp的动画控制器是否是传入的
        /// </summary>
        public bool IsFpController(EAnimationModelType animationType)
        {
            if (AnimationAssets.AnimationControllerDict.TryGetValue(animationType, out var controller))
            {
                return fpAnimator.runtimeAnimatorController == controller;
            }
            return false;
        }

        /// <summary>
        /// 检查当前Fp的动画控制器是否为空
        /// </summary>
        public bool IsFpControllerNull() => fpAnimator.runtimeAnimatorController is null;

        /// <summary>
        /// 切换动画模组
        /// </summary>
        public void SwitchModule(EAnimationModelType nextModule)
        {
            SetFpController(nextModule);
            if (fpAnimator == null)
                return;

            fpAnimator.Play(PlayerAmHashMap.取出, 0, 0f);
            fpAnimator.Update(0f);
        }

        /// <summary>
        /// 播放收起动画
        /// </summary>
        public void CloseModule()
        {
            fpAnimator.Play(PlayerAmHashMap.收起);
        }

        /// <summary>
        /// 播放拾取动画
        /// </summary>
        public void PlayPickupAnimation()
        {
            if (IsFpController(EAnimationModelType.None))
                fpAnimator.Play(PlayerAmHashMap.拾取);
        }

        /// <summary>
        /// 切到待机
        /// </summary>
        public void CrossFadeIdle(float fade)
        {
            CrossFadeClip(PlayerAmHashMap.待机, fade);
        }

        /// <summary>
        /// 切换 FP Controller
        /// </summary>
        private void SetFpController(EAnimationModelType animationType)
        {
            if (fpAnimator == null)
                return;

            if (AnimationAssets.AnimationControllerDict.TryGetValue(animationType, out var controller))
            {
                fpAnimator.runtimeAnimatorController = controller;
                return;
            }

            Debug.LogError($"动画控制器 {animationType} 不存在");
        }

     
    }
}
