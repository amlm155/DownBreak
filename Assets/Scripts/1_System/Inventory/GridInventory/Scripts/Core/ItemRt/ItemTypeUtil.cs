using cfg.item;
using DBGameSystem;

namespace MmInventory
{
    /// <summary>
    /// 物品类型解析
    /// </summary>
    public static class ItemTypeUtil
    {
        /// <summary>
        /// 解析物品大类
        /// </summary>
        public static EItemType ResolveItemType(ItemView itemView)
        {
            int excelItemId = itemView.ExcelItemId;
            var tableData = GameHub.Get<IInventory>().GetItemData<IItemTableData>(excelItemId);
            return tableData.ItemType;
        }

        /// <summary>
        /// 武器与装备视为可装备
        /// </summary>
        public static bool IsEquipable(ItemView itemView)
        {
            if (itemView == null)
                return false;

            var eItemType = ResolveItemType(itemView);
            return eItemType == EItemType.Weapon || eItemType == EItemType.Equipment;
        }

        /// <summary>
        /// 可否绑到物品轮盘 武器 食物水 药品
        /// </summary>
        public static bool IsWheelBindable(int excelItemId)
        {
            if (!LubanTables.TryGetItem(excelItemId, out var tableData))
                return false;

            var eItemType = tableData.ItemType;
            return eItemType == EItemType.Weapon
                   || eItemType == EItemType.FoodOrWater
                   || eItemType == EItemType.Medicine;
        }

        /// <summary>
        /// 视图可否绑到轮盘
        /// </summary>
        public static bool IsWheelBindable(ItemView itemView)
        {
            if (itemView == null)
                return false;
            return IsWheelBindable(itemView.ExcelItemId);
        }
    }
}
