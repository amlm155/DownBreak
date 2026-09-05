using cfg.item;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// Luban Item 行到 IItemTableData 的适配
    /// </summary>
    public sealed class LubanItemTableData : IItemTableData
    {
        /// <summary> Luban 原始行 </summary>
        private readonly Item raw;

        /// <summary>
        /// 构造适配器
        /// </summary>
        public LubanItemTableData(Item raw)
        {
            this.raw = raw;
        }

        /// <summary> Luban 原始行 </summary>
        public Item Raw => raw;

        public int ExcelItemId => raw.Id;
        public string Name => raw.Name;
        public string Description => raw.Description;
        public string IconPath => raw.IconPath;
        public string WorldPrefabPath => raw switch
        {
            Equipment data => data.WorldPrefabPath,
            Weapon data => data.WorldPrefabPath,
            FoodOrWater data => data.WorldPrefabPath,
            Medicine data => data.WorldPrefabPath,
            cfg.item.Material data => data.WorldPrefabPath,
            Blueprint data => data.WorldPrefabPath,
            _ => string.Empty
        };
        public string PlacePrefabPath => raw is Furniture furniture ? furniture.PlacePrefabPath : string.Empty;
        public Vector2Int DataSize => new Vector2Int(raw.DataSize.X, raw.DataSize.Y);
        public EItemType ItemType => raw.ItemType;
        public EItemRarity ItemRarity => raw.ItemRarity;
        public EItemStackType ItemStackType => raw.StackType;
        public int MaxStackCount => raw.MaxStackCount;
        public int MaxDurability => raw.MaxDurability;
        public int DurabilityLoss => raw switch
        {
            FoodOrWater data => data.DurabilityLoss,
            Medicine data => data.DurabilityLoss,
            cfg.item.Material data => data.DurabilityLoss,
            Blueprint data => data.DurabilityLoss,
            _ => 0
        };
    }
}
