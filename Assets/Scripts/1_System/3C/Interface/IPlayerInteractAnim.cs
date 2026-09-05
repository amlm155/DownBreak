using System;
using cfg.item;
using DBWeaponSystem;

namespace Interaction.Player
{
    /// <summary>
    /// 交互动画接口 用于沟通交互模块与手部动画 方法实现在 PlayerAnimationController 类里
    /// </summary>
    public interface IPlayerInteractAnim
    {
        /// <summary> 正在消耗品演出 </summary>
        bool IsConsuming { get; }

        /// <summary>
        /// 切换动画模组
        /// </summary>
        void SwitchModule(EAnimationModelType nextModule);

        /// <summary>
        /// 播放轻攻击
        /// </summary>
        void PlayLightAttack();

        /// <summary>
        /// 播放重攻击
        /// </summary>
        void PlayHeavyAttack();

        /// <summary>
        /// 播放拾取
        /// </summary>
        void PlayPickupAnimation();

        /// <summary>
        /// 播放检视
        /// </summary>
        void PlayViewAnimation();

        /// <summary>
        /// 切到待机
        /// </summary>
        void CrossFadeIdle(float fade);

        /// <summary>
        /// 播进食 完了调 onCompleted
        /// </summary>
        bool TryPlayEat(FoodOrWater foodTable, Action onCompleted);

        /// <summary>
        /// 播用药 完了调 onCompleted
        /// </summary>
        bool TryPlayMedicine(Medicine medicineTable, Action onCompleted);
    }
}
