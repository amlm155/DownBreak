using System;
using UnityEngine;
using cfg.item;

namespace MmInventory
{
    /// <summary>
    /// 运行时物品数据
    /// 外部开发者可以更改此类的字段 但是一般别更改接口的属性 因为可能会影响算法层的使用
    /// </summary>
    public class ItemRtData : IItemRuntime
    {
        /// <summary> 物品实例ID 用于唯一标识一个物品 </summary>
        [SerializeField] private string instancedItemId;

        /// <summary> 配置表中的物品ID </summary>
        [SerializeField] private int excelItemId;

        /// <summary> 物品在背包中的锚点位置 </summary>
        [SerializeField] private Vector2Int anchorPos;

        /// <summary> 物品当前尺寸 </summary>
        [SerializeField] private Vector2Int dataSize;

        /// <summary> 当前堆叠数量 </summary>
        [SerializeField] private int curStackCount;

        /// <summary> 最大堆叠数量 </summary>
        [SerializeField] private int maxStackCount;

        /// <summary> 是否可堆叠 </summary>
        [SerializeField] private EItemStackType itemStackType;

        /// <summary> 稀有度 抽取与存档用 </summary>
        [SerializeField] private EItemRarity itemRarity;

        /// <summary> 注意旋转只有两种情况 0 和 90 </summary>
        [SerializeField] private bool isRotated;

        /// <summary> 当前耐久 </summary>
        [SerializeField] private int currDurability;

        /// <summary> 最大耐久 装备来自表 max_durability </summary>
        [SerializeField] private int maxDurability;

        /// <summary> 默认最大耐久 </summary>
        public const int DefaultMaxDurability = 100;

        public int ExcelItemId => excelItemId;
        public Vector2Int AnchorPos => anchorPos;
        public Vector2Int DataSize => dataSize;
        public bool IsRotated => isRotated;
        public string InstancedItemId => instancedItemId;
        public EItemStackType ItemStackType => itemStackType;
        public int MaxStackCount => maxStackCount;
        public EItemRarity ItemRarity => itemRarity;
        public int CurrDurability => currDurability;
        public int MaxDurability => maxDurability;

        public int CurrStackCount
        {
            get => curStackCount;
            set => SetStackCount(value);
        }

        IItemRuntime IItemRuntime.Clone(int stackCount) => Clone(stackCount);

        /// <summary>
        /// 构造函数
        /// </summary>
        public ItemRtData(int excelItemId,
                          Vector2Int dataSize,
                          int curStackCount,
                          bool isRotated,
                          int maxStackCount = 1,
                          EItemStackType itemStackType = EItemStackType.NoStackable,
                          string instancedItemId = null,
                          EItemRarity itemRarity = EItemRarity.White)
        {
            this.excelItemId = excelItemId;
            this.dataSize = dataSize;
            this.curStackCount = curStackCount;
            this.isRotated = isRotated;
            this.maxStackCount = maxStackCount;
            this.itemStackType = itemStackType;
            this.itemRarity = itemRarity;
            this.instancedItemId = string.IsNullOrEmpty(instancedItemId)
                ? Guid.NewGuid().ToString()
                : instancedItemId;
            this.maxDurability = DefaultMaxDurability;
            this.currDurability = DefaultMaxDurability;
        }

        /// <summary>
        /// 将存档数据转换为运行时数据
        /// </summary>
        public static ItemRtData ItemSaveData2ItemRtData(ItemSaveData save)
        {
            var item = new ItemRtData(
                save.excelItemId,
                save.dataSize,
                save.hasStackCount,
                save.rotated,
                save.maxStackCount,
                save.itemStackType,
                save.instancedItemId,
                save.itemRarity);

            item.SetAnchorPos(save.anchorPos);
            int maxDurability = save.maxDurability > 0 ? save.maxDurability : DefaultMaxDurability;
            int currDurability = save.maxDurability > 0 ? save.currDurability : DefaultMaxDurability;
            item.SetDurability(currDurability, maxDurability);
            return item;
        }

        /// <summary>
        /// 将配置表数据转换为运行时数据
        /// </summary>
        public static ItemRtData ItemTableData2ItemRtData(IItemTableData config,
                                            int curStackCount = 1,
                                            bool isRotated = false)
        {
            var item = new ItemRtData(config.ExcelItemId,
                                    config.DataSize,
                                    curStackCount,
                                    isRotated,
                                    config.MaxStackCount,
                                    config.ItemStackType,
                                    null,
                                    config.ItemRarity);

            // 读表最大耐久 填0则保持默认
            if (config.MaxDurability > 0)
                item.SetDurability(config.MaxDurability, config.MaxDurability);

            return item;
        }

        /// <summary>
        /// 设置物品在背包中的锚点位置 
        /// 此方法用于算法层 不要让View层调用此方法
        /// </summary>
        public void SetAnchorPos(Vector2Int newAnchorPos)
        {
            anchorPos = newAnchorPos;
        }

        /// <summary>
        /// 设置旋转状态
        /// 此方法用于算法层 不要让View层调用此方法
        /// </summary>
        public void SetRotated(bool rotated)
        {
            if (isRotated == rotated) return;

            isRotated = rotated;
            // 掉换xy
            dataSize = new Vector2Int(dataSize.y, dataSize.x);
        }

        /// <summary>
        /// 设置堆叠数量
        /// 此方法用于算法层 不要让View层调用此方法
        /// </summary>
        public void SetStackCount(int count)
        {
            curStackCount = Mathf.Max(0, count);
        }   

        /// <summary>
        /// 设置耐久
        /// </summary>
        public void SetDurability(int curr, int max)
        {
            maxDurability = Mathf.Max(1, max);
            currDurability = Mathf.Clamp(curr, 0, maxDurability);
        }

        /// <summary>
        /// 读取物品通用单次损耗 武器由攻击模块传入轻重攻击损耗
        /// </summary>
        public int GetDurabilityLoss()
        {
            if (!LubanTables.TryGetItem(excelItemId, out var config))
                return 0;
            return config.DurabilityLoss;
        }

        /// <summary>
        /// 扣除耐久 loss小于0时读取物品通用损耗 返回扣后当前值
        /// </summary>
        public int ApplyDurabilityLoss(int loss = -1)
        {
            int applyLoss = loss < 0 ? GetDurabilityLoss() : loss;
            currDurability = Mathf.Max(0, currDurability - applyLoss);
            return currDurability;
        }

        /// <summary>
        /// 拆出新堆实例 分配新的 InstancedItemId
        /// </summary>
        public ItemRtData Clone(int stackCount)
        {
            var clone = new ItemRtData(
                excelItemId,
                dataSize,
                stackCount,
                isRotated,
                maxStackCount,
                itemStackType,
                null,
                itemRarity);
            clone.SetDurability(currDurability, maxDurability);
            return clone;
        }

    }
}
