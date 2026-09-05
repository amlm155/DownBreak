using UnityEngine;

namespace Interaction.Combat
{
    /// <summary>
    /// 对外提供当前耐久与最大耐久
    /// </summary>
    public interface IDurabilityProvider
    {
        /// <summary>
        /// 尝试获取当前耐久与最大耐久
        /// </summary>
        bool TryGetDurability(out int currentDurability, out int maxDurability);
    }

    /// <summary>
    /// 可受伤目标 玩家 敌人与可破坏物实现
    /// </summary>
    public interface IDamageable
    {
        /// <summary> 存活状态 </summary>
        bool IsAlive { get; }

        /// <summary>
        /// 施加伤害 返回实际扣减值
        /// </summary>
        int ApplyDamage(int damage, Vector3 hitPoint);
    }
}
