using cfg.item;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品数据接口
    /// 用于定义所有物品都应该存在的基础数据
    /// </summary>
    public interface IItemTableData
    {
        // ID
        public int ExcelItemId { get; }

        // 物品名称
        public string Name { get; }

        // 描述
        public string Description { get; }

        // 图片路径
        public string IconPath { get; }

        // 世界掉落拾取预制体路径 家具为空
        public string WorldPrefabPath { get; }

        // 放置与场景交互预制体路径 非家具为空
        public string PlacePrefabPath { get; }

        // 尺寸
        public Vector2Int DataSize { get; }

        // 类型
        public EItemType ItemType { get; }

        // 稀有度
        public EItemRarity ItemRarity { get; }

        // 堆叠类型
        public EItemStackType ItemStackType { get; }

        // 最大堆叠数量
        public int MaxStackCount { get; }

        // 最大耐久 无耐久为0
        public int MaxDurability { get; }

        // 通用单次损耗 装备无此项返回0 武器由轻重攻击字段决定
        public int DurabilityLoss { get; }
    }
}
