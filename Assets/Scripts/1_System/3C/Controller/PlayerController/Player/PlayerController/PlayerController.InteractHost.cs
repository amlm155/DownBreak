using DBGameSystem;
using DBWeaponSystem;
using Interaction.Player;
using MieMieFrameWork.MMAnimation;
using UnityEngine;

namespace PlayerControllerSpace
{
    /// <summary>
    /// 玩家对外提供输入和身体能力 给交互模块用
    /// </summary>
    public partial class PlayerController : IPlayerInput, IPlayerBody
    {
        /// <summary> 动画开窗事件名 </summary>
        private const string AttackOpenEventName = "AttackOpen";

        /// <summary> 动画关窗事件名 </summary>
        private const string AttackCloseEventName = "AttackClose";

        /// <summary> 闲置多久触发检视 </summary>
        public float LongTimeToIdle => PlayerRtConfig.LongTimeToIdle;

        /// <summary> 交互用动画桥 </summary>
        public IPlayerInteractAnim Anim => playerAmController;

        /// <summary>
        /// 播放攻击相机震动
        /// </summary>
        public void ShakeCameraForAttack(bool isHeavyAttack)
        {
            playerCameraComponent?.ShakeForAttack(isHeavyAttack);
        }

        /// <summary> 攻击射线起点 与交互检测同原点 </summary>
        public Transform AttackRayOrigin => InteractionManager != null
            ? InteractionManager.RayOrigin
            : null;

        /// <summary>
        /// 把输入和身体能力挂到 GameHub
        /// </summary>
        private void RegisterPlayerServices()
        {
            GameHub.Register<IPlayerInput>(this);
            GameHub.Register<IPlayerBody>(this);
        }

        /// <summary>
        /// 从 GameHub 摘掉
        /// </summary>
        private void UnregisterPlayerServices()
        {
            if (ReferenceEquals(GameHub.Get<IPlayerInput>(), this))
                GameHub.Unregister<IPlayerInput>();
            if (ReferenceEquals(GameHub.Get<IPlayerBody>(), this))
                GameHub.Unregister<IPlayerBody>();
        }

        /// <summary>
        /// 订阅武器装备动画请求
        /// </summary>
        private void BindWeaponAnimationRequest()
        {
            if (GameHub.Get<IWeaponSystem>() == null)
                return;
            GameHub.Get<IWeaponSystem>().OnEquippedAnimationRequest
                += playerInteract.SwitchWeaponModule;
        }

        /// <summary>
        /// 绑定 FP 动画开窗到 WeaponScanner
        /// </summary>
        private void BindAttackWindowEvents()
        {
            if (playerAmController == null || playerAmController.FpAnimator == null)
            {
                Debug.LogWarning("FP Animator 为空 无法绑定攻击开窗事件");
                return;
            }

            var receiver = playerAmController.FpAnimator.GetComponent<AnimationReceiver>();
            if (receiver == null)
            {
                Debug.LogWarning("FP Animator 缺少 AnimationReceiver");
                return;
            }

            if (GameHub.Get<IWeaponSystem>() == null)
            {
                Debug.LogWarning("WeaponSystem 未找到 无法绑定攻击开窗");
                return;
            }

            var scanner = GameHub.Get<IWeaponSystem>().Scanner;
            if (scanner == null)
                scanner = (GameHub.Get<IWeaponSystem>() as UnityEngine.MonoBehaviour).GetComponent<WeaponScanner>();

            if (scanner == null)
            {
                Debug.LogWarning("WeaponScanner 未找到 无法绑定攻击开窗");
                return;
            }

            receiver.AddAnimationEvent(AttackOpenEventName, scanner.OpenWindow);
            receiver.AddAnimationEvent(AttackCloseEventName, scanner.CloseWindow);
        }
    }
}
