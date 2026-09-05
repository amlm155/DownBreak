using System;
using DBGameSystem;
using MmInventory;
using UnityEngine;

namespace DBWeaponSystem
{
    /// <summary>
    /// 武器系统接口 用于沟通玩法层与武器能力 方法实现在 WeaponSystem 类里
    /// </summary>
    public interface IWeaponSystem : IGameService
    {
        /// <summary> 当前手持物品表 ID 0 表示空手 </summary>
        int EquippedItemTableId { get; }

        /// <summary> 当前手持运行时物品 </summary>
        ItemRtData EquippedItemRtData { get; }

        /// <summary> 当前武器表配置 空手为 null </summary>
        WeaponConfig WeaponConfig { get; }

        /// <summary> 近战扫描器 </summary>
        WeaponScanner Scanner { get; }

        /// <summary> 装备成功后请求切动画模组 由 3C 宿主订阅 </summary>
        Action<EAnimationModelType> OnEquippedAnimationRequest { get; set; }

        /// <summary>
        /// 按运行时物品装备武器 保留耐久与实例
        /// </summary>
        bool TryEquipWeapon(ItemRtData itemRtData, out ItemRtData oldItemRtData);

        /// <summary>
        /// 读当前武器表攻击力 空手返回 false
        /// </summary>
        bool TryGetAttackValue(out int attackValue);

        /// <summary>
        /// 扣当前手持耐久 必须传入当前攻击损耗
        /// </summary>
        bool TryApplyDurabilityLoss(int loss);

        /// <summary>
        /// 当前手持耐久
        /// </summary>
        bool TryGetDurability(out int curr, out int max);

        /// <summary>
        /// 清空手持武器视觉 返回原表 ID
        /// </summary>
        int ClearWeapon();

        /// <summary>
        /// 重挂当前武器本地变换 切模组后调用
        /// </summary>
        void RefreshCurrentWeaponPose();

        /// <summary>
        /// 临时显隐当前手持武器视觉 不卸装
        /// </summary>
        void SetEquippedWeaponVisible(bool visible);

        /// <summary>
        /// 判断指定实例是否为当前手持物
        /// </summary>
        bool IsEquippedWeapon(GameObject weaponObject);
    }
}
