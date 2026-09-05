using MiMieEventBus;
using MmInventory;

namespace DBWeaponSystem
{
    /// <summary>
    /// 武器 HUD 同步事件 System 发布 UI 订阅
    /// </summary>
    public static class WeaponHudEvents
    {
        /// <summary>
        /// 手持武器变化 参数 运行时物品 空手为 null
        /// </summary>
        public static readonly EventKey<ItemRtData> EquippedChanged =
            new EventKey<ItemRtData>("Weapon.EquippedChanged");

        /// <summary>
        /// 手持耐久变化 参数 当前耐久 最大耐久
        /// </summary>
        public static readonly EventKey<int, int> DurabilityChanged =
            new EventKey<int, int>("Weapon.DurabilityChanged");

        /// <summary>
        /// 手持武器耐久耗尽摧毁 参数 实例 ID
        /// </summary>
        public static readonly EventKey<string> Broken =
            new EventKey<string>("Weapon.Broken");
    }
}
