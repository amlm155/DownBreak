using MiMieEventBus;
using UnityEngine;

namespace Interaction.Combat
{
    /// <summary>
    /// 战斗表现反馈事件 由 System 发布 UI 订阅
    /// </summary>
    public static class CombatFeedbackEvents
    {
        /// <summary>
        /// 伤害跳字 参数 世界坐标 伤害值 是否暴击
        /// </summary>
        public static readonly EventKey<Vector3, long, bool> DamageFloatingText =
            new EventKey<Vector3, long, bool>("Combat.DamageFloatingText");

        /// <summary>
        /// 攻击准心反馈 参数 是否命中目标
        /// </summary>
        public static readonly EventKey<bool> AttackCrosshair =
            new EventKey<bool>("Combat.AttackCrosshair");
    }
}
