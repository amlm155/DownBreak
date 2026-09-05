using DBGameSystem;
using UnityEngine;

namespace Interaction.Player
{
    /// <summary>
    /// 玩家身体接口 用于沟通交互模块与玩家 方法实现在 PlayerController 类里
    /// </summary>
    public interface IPlayerBody : IGameService
    {
        /// <summary> 闲置多久触发检视 </summary>
        float LongTimeToIdle { get; }

        /// <summary> 聚焦交互管理器 </summary>
        IInteraction InteractionManager { get; }

        /// <summary> 攻击射线起点 </summary>
        Transform AttackRayOrigin { get; }

        /// <summary> 交互用动画控制器 </summary>
        IPlayerInteractAnim Anim { get; }

        /// <summary> 播放攻击相机震动 </summary>
        void ShakeCameraForAttack(bool isHeavyAttack);
    }
}
