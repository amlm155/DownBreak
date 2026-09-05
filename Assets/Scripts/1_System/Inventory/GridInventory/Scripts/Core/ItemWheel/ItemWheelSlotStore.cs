using System;
using cfg.item;

namespace MmInventory
{
    /// <summary>
    /// 物品轮盘 8 槽运行时绑定 索引 0 对应键 1
    /// </summary>
    public static class ItemWheelSlotStore
    {
        /// <summary> 槽位数 </summary>
        public const int SlotCount = 8;

        /// <summary> 槽位物品 </summary>
        private static readonly ItemRtData[] slotItemList = new ItemRtData[SlotCount];

        /// <summary> 槽位变更 </summary>
        public static event Action OnSlotsChanged;

        /// <summary>
        /// 取槽位物品
        /// </summary>
        public static ItemRtData Get(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                return null;
            return slotItemList[slotIndex];
        }

        /// <summary>
        /// 查找实例所在槽 无则 -1
        /// </summary>
        public static int FindSlotByInstanceId(string instancedItemId)
        {
            if (string.IsNullOrEmpty(instancedItemId))
                return -1;

            for (int i = 0; i < SlotCount; i++)
            {
                if (slotItemList[i] != null
                    && slotItemList[i].InstancedItemId == instancedItemId)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 绑定物品到槽位
        /// 此方法用于物品菜单的热键绑定
        /// </summary>
        public static bool Bind(int slotIndex, ItemRtData itemRtData)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount || itemRtData == null)
                return false;

            // 判断物品是否可绑定到轮盘: 武器 食物水 药品
            if (!ItemTypeUtil.IsWheelBindable(itemRtData.ExcelItemId))
                return false;

            // 查找物品是否已绑定到其他槽位
            int oldSlot = FindSlotByInstanceId(itemRtData.InstancedItemId);
            // 如果物品已绑定到其他槽位 则清空该槽位
            if (oldSlot >= 0 && oldSlot != slotIndex)
                slotItemList[oldSlot] = null;

            // 绑定物品到(新)槽位
            slotItemList[slotIndex] = itemRtData;
            OnSlotsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 清空指定实例所在槽
        /// </summary>
        public static void ClearInstance(string instancedItemId)
        {
            int slotIndex = FindSlotByInstanceId(instancedItemId);
            if (slotIndex < 0)
                return;

            slotItemList[slotIndex] = null;
            OnSlotsChanged?.Invoke();
        }

        /// <summary>
        /// 清空指定槽位上面的物品
        /// </summary>
        public static void ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount)
                return;
            if (slotItemList[slotIndex] == null)
                return;

            slotItemList[slotIndex] = null;
            OnSlotsChanged?.Invoke();
        }

        /// <summary>
        /// 通知槽位展示刷新 堆叠变化但槽引用未变时用
        /// </summary>
        public static void NotifySlotsChanged()
        {
            OnSlotsChanged?.Invoke();
        }
    }
}
