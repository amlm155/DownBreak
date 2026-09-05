using DBGameSystem;

namespace Interaction.Player
{
    /// <summary>
    /// 玩家输入接口 用于沟通交互模块与玩家 方法实现在 PlayerController 类里
    /// </summary>
    public interface IPlayerInput : IGameService
    {
        /// <summary> 交互键按住 </summary>
        bool IsInteractHeld { get; }

        /// <summary> 交互键按下瞬间 </summary>
        bool IsInteractStarted { get; }

        /// <summary> 左键攻击按住 </summary>
        bool IsLeftAttackHeld { get; }

        /// <summary> 右键攻击按住 </summary>
        bool IsRightAttackHeld { get; }

        /// <summary> 左键攻击按下瞬间 </summary>
        bool IsLeftAttackPressed { get; }

        /// <summary> 右键攻击按下瞬间 </summary>
        bool IsRightAttackPressed { get; }

        /// <summary> 建造与背包旋转按下瞬间 </summary>
        bool IsRotatePressed { get; }

        /// <summary> 手电开关按下瞬间 </summary>
        bool IsFlashlightPressed { get; }

        /// <summary>
        /// 是否有玩家操作输入
        /// </summary>
        bool HasPlayerAction();
    }
}
