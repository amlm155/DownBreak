using System.Collections.Generic;
using cfg.item;
using cfg.loot;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 容器投放运行时 密度走 TbRarityGradient 容器模板走 TbScrapContainer 消耗物耐久走 TbConsumableDurabilityGradient
    /// </summary>
    public static class LootRuntime
    {
        #region 结果与候选

        /// <summary>
        /// 投放结果
        /// </summary>
        public readonly struct FillResult
        {
            /// <summary> 是否判定为空箱 </summary>
            public readonly bool WasEmptyRoll;

            /// <summary> 候选数量 </summary>
            public readonly int CandidateCount;

            /// <summary> 成功放入数量 </summary>
            public readonly int PlacedCount;

            /// <summary> 因放不下跳过数量 </summary>
            public readonly int SkippedCount;

            public FillResult(bool wasEmptyRoll, int candidateCount, int placedCount, int skippedCount)
            {
                WasEmptyRoll = wasEmptyRoll;
                CandidateCount = candidateCount;
                PlacedCount = placedCount;
                SkippedCount = skippedCount;
            }
        }

        /// <summary>
        /// 一次抽签得到的候选物品
        /// </summary>
        public readonly struct LootCandidate
        {
            /// <summary> 物品 Excel ID </summary>
            public readonly int ExcelItemId;

            /// <summary> 堆叠数量 </summary>
            public readonly int StackCount;

            /// <summary> 占地格数 用于排序 </summary>
            public readonly int CellArea;

            /// <summary> 稀有度 </summary>
            public readonly EItemRarity ItemRarity;

            /// <summary> 抽到的当前耐久 </summary>
            public readonly int CurrDurability;

            public LootCandidate(
                int excelItemId,
                int stackCount,
                int cellArea,
                EItemRarity itemRarity,
                int currDurability)
            {
                ExcelItemId = excelItemId;
                StackCount = stackCount;
                CellArea = cellArea;
                ItemRarity = itemRarity;
                CurrDurability = currDurability;
            }
        }

        #endregion

        #region 抽池与填充

        /// <summary> 总表筛选临时列表 </summary>
        private static readonly List<IItemTableData> tempItemList = new();

        /// <summary>
        /// 按搜刮容器模板模拟候选 不写入容器
        /// </summary>
        public static List<LootCandidate> SimulateCandidates(
            ScrapContainer scrapContainer,
            out bool wasEmptyRoll)
        {
            wasEmptyRoll = false;
            var candidateList = new List<LootCandidate>();
            if (scrapContainer is null)
                return candidateList;

            var gradient = LubanTables.Tables.TbRarityGradient.GetOrDefault(scrapContainer.DefaultGrade);
            if (gradient is null)
                return candidateList;

            // 空箱 Random01 < empty_chance
            if (gradient.EmptyChance > 0f && Random.value < gradient.EmptyChance)
            {
                wasEmptyRoll = true;
                return candidateList;
            }

            int rollCount = ResolveRollCount(scrapContainer);
            if (rollCount <= 0)
                return candidateList;

            for (int i = 0; i < rollCount; i++)
            {
                if (!TryPickCandidate(scrapContainer, gradient, out var candidate))
                    continue;

                candidateList.Add(candidate);
            }

            return candidateList;
        }

        /// <summary>
        /// 按搜刮容器模板向容器填充
        /// </summary>
        public static FillResult TryFill(
            GridContainerView containerView,
            int scrapContainerId,
            bool forceClear)
        {
            if (containerView is null || !containerView.IsInventoryReady)
                return new FillResult(false, 0, 0, 0);

            var scrapContainer = LubanTables.Tables.TbScrapContainer.GetOrDefault(scrapContainerId);
            if (scrapContainer is null)
            {
                Debug.LogWarning($"TbScrapContainer 无 id={scrapContainerId}");
                return new FillResult(false, 0, 0, 0);
            }

            return TryFill(containerView, scrapContainer, forceClear);
        }

        /// <summary>
        /// 按搜刮容器模板向容器填充
        /// </summary>
        public static FillResult TryFill(
            GridContainerView containerView,
            ScrapContainer scrapContainer,
            bool forceClear)
        {
            if (containerView is null || !containerView.IsInventoryReady || scrapContainer is null)
                return new FillResult(false, 0, 0, 0);

            if (forceClear)
                containerView.ClearAllItems();

            var candidateList = SimulateCandidates(scrapContainer, out bool wasEmptyRoll);
            if (wasEmptyRoll || candidateList.Count == 0)
                return new FillResult(wasEmptyRoll, 0, 0, 0);

            candidateList.Sort(CompareCandidateByAreaDesc);

            int placedCount = 0;
            int skippedCount = 0;
            for (int i = 0; i < candidateList.Count; i++)
            {
                var candidate = candidateList[i];
                var itemView = containerView.CreatItemUIAtRandomEmpty(
                    candidate.ExcelItemId,
                    candidate.StackCount);
                if (itemView is null)
                {
                    skippedCount++;
                    continue;
                }

                itemView.ItemData.SetDurability(
                    candidate.CurrDurability,
                    itemView.ItemData.MaxDurability);
                itemView.RefreshDurabilityFill();
                placedCount++;
            }

            return new FillResult(false, candidateList.Count, placedCount, skippedCount);
        }

        #endregion

        #region 加权抽取

        /// <summary> 满档耐久百分比下限 </summary>
        private const int FullPercentMin = 90;

        /// <summary> 满档耐久百分比上限 </summary>
        private const int FullPercentMax = 100;

        /// <summary> 高档耐久百分比下限 </summary>
        private const int HighPercentMin = 70;

        /// <summary> 高档耐久百分比上限 </summary>
        private const int HighPercentMax = 89;

        /// <summary> 中档耐久百分比下限 </summary>
        private const int MidPercentMin = 40;

        /// <summary> 中档耐久百分比上限 </summary>
        private const int MidPercentMax = 69;

        /// <summary> 低档耐久百分比下限 </summary>
        private const int LowPercentMin = 1;

        /// <summary> 低档耐久百分比上限 </summary>
        private const int LowPercentMax = 39;

        /// <summary>
        /// 按表配置抽取件数
        /// </summary>
        private static int ResolveRollCount(ScrapContainer scrapContainer)
        {
            int countMin = Mathf.Max(0, scrapContainer.ItemCountMin);
            int countMax = Mathf.Max(countMin, scrapContainer.ItemCountMax);
            return Random.Range(countMin, countMax + 1);
        }

        /// <summary>
        /// 先抽稀有度再从允许类型池抽物品 无货则降稀有度
        /// </summary>
        private static bool TryPickCandidate(
            ScrapContainer scrapContainer,
            RarityGradient gradient,
            out LootCandidate candidate)
        {
            candidate = default;
            if (!TryPickRarity(gradient, out var eItemRarity))
                return false;

            for (int step = (int)eItemRarity; step >= 0; step--)
            {
                var eTryRarity = (EItemRarity)step;
                if (!TryPickItemFromTable(scrapContainer, eTryRarity, out var tableData))
                    continue;

                int stackCount = ResolveStackCount(tableData);
                int cellArea = Mathf.Max(1, tableData.DataSize.x * tableData.DataSize.y);
                int currDurability = ResolveConsumableDurability(tableData);
                candidate = new LootCandidate(
                    tableData.ExcelItemId,
                    stackCount,
                    cellArea,
                    tableData.ItemRarity,
                    currDurability);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 按梯度 float 权重抽稀有度 P=权重/总和
        /// </summary>
        private static bool TryPickRarity(RarityGradient gradient, out EItemRarity eItemRarity)
        {
            eItemRarity = EItemRarity.White;
            float totalWeight = SumRarityWeights(gradient);
            if (totalWeight <= 0f)
                return false;

            float roll = Random.value * totalWeight;
            float cursor = 0f;

            if (TryAccumulate(gradient.White, EItemRarity.White, ref cursor, roll, out eItemRarity))
                return true;
            if (TryAccumulate(gradient.Green, EItemRarity.Green, ref cursor, roll, out eItemRarity))
                return true;
            if (TryAccumulate(gradient.Blue, EItemRarity.Blue, ref cursor, roll, out eItemRarity))
                return true;
            if (TryAccumulate(gradient.Purple, EItemRarity.Purple, ref cursor, roll, out eItemRarity))
                return true;
            if (TryAccumulate(gradient.Gold, EItemRarity.Gold, ref cursor, roll, out eItemRarity))
                return true;
            if (TryAccumulate(gradient.Red, EItemRarity.Red, ref cursor, roll, out eItemRarity))
                return true;

            eItemRarity = EItemRarity.White;
            return true;
        }

        /// <summary>
        /// 累加权重并判定命中
        /// </summary>
        private static bool TryAccumulate(
            float weight,
            EItemRarity eRarity,
            ref float cursor,
            float roll,
            out EItemRarity eHitRarity)
        {
            eHitRarity = eRarity;
            if (weight <= 0f)
                return false;

            cursor += weight;
            return roll < cursor;
        }

        /// <summary>
        /// 从总表按稀有度与允许大类等权抽取
        /// </summary>
        private static bool TryPickItemFromTable(
            ScrapContainer scrapContainer,
            EItemRarity eItemRarity,
            out IItemTableData tableData)
        {
            tableData = null;
            tempItemList.Clear();

            var itemList = LubanTables.ItemList;
            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                if (item is null)
                    continue;

                if (item.ItemRarity != eItemRarity)
                    continue;

                if (!AllowsItemType(scrapContainer, item.ItemType))
                    continue;

                tempItemList.Add(item);
            }

            if (tempItemList.Count == 0)
                return false;

            int index = Random.Range(0, tempItemList.Count);
            tableData = tempItemList[index];
            return true;
        }

        /// <summary>
        /// 空列表表示不限制大类
        /// </summary>
        private static bool AllowsItemType(ScrapContainer scrapContainer, EItemType eItemType)
        {
            var allowedList = scrapContainer.AllowedItemTypes;
            if (allowedList is null || allowedList.Count == 0)
                return true;

            for (int i = 0; i < allowedList.Count; i++)
            {
                if (allowedList[i] == eItemType)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 消耗物按梯度抽当前耐久 非消耗物或无表则满耐久
        /// </summary>
        private static int ResolveConsumableDurability(IItemTableData tableData)
        {
            int maxDurability = tableData.MaxDurability > 0
                ? tableData.MaxDurability
                : ItemRtData.DefaultMaxDurability;

            if (!IsConsumableItemType(tableData.ItemType))
                return maxDurability;

            var gradient = LubanTables.Tables.TbConsumableDurabilityGradient.GetOrDefault(tableData.ItemType);
            if (gradient is null)
                return maxDurability;

            int percent = RollDurabilityPercent(gradient);
            int currDurability = Mathf.RoundToInt(maxDurability * percent / 100f);
            return Mathf.Clamp(currDurability, 1, maxDurability);
        }

        /// <summary>
        /// 装备武器食物水药品走耐久梯度
        /// </summary>
        private static bool IsConsumableItemType(EItemType eItemType)
        {
            return eItemType == EItemType.Equipment
                   || eItemType == EItemType.Weapon
                   || eItemType == EItemType.FoodOrWater
                   || eItemType == EItemType.Medicine;
        }

        /// <summary>
        /// 按四档权重抽耐久百分比
        /// </summary>
        private static int RollDurabilityPercent(ConsumableDurabilityGradient gradient)
        {
            float totalWeight = Mathf.Max(0f, gradient.FullWeight)
                                + Mathf.Max(0f, gradient.HighWeight)
                                + Mathf.Max(0f, gradient.MidWeight)
                                + Mathf.Max(0f, gradient.LowWeight);
            if (totalWeight <= 0f)
                return FullPercentMax;

            float roll = Random.value * totalWeight;
            float cursor = 0f;
            if (TryHitDurabilityBand(gradient.FullWeight, FullPercentMin, FullPercentMax, ref cursor, roll, out int percent))
                return percent;
            if (TryHitDurabilityBand(gradient.HighWeight, HighPercentMin, HighPercentMax, ref cursor, roll, out percent))
                return percent;
            if (TryHitDurabilityBand(gradient.MidWeight, MidPercentMin, MidPercentMax, ref cursor, roll, out percent))
                return percent;
            if (TryHitDurabilityBand(gradient.LowWeight, LowPercentMin, LowPercentMax, ref cursor, roll, out percent))
                return percent;

            return FullPercentMax;
        }

        /// <summary>
        /// 累加一档权重 命中则在区间内随机百分比
        /// </summary>
        private static bool TryHitDurabilityBand(
            float weight,
            int percentMin,
            int percentMax,
            ref float cursor,
            float roll,
            out int percent)
        {
            percent = percentMax;
            if (weight <= 0f)
                return false;

            cursor += weight;
            if (roll >= cursor)
                return false;

            percent = Random.Range(percentMin, percentMax + 1);
            return true;
        }

        /// <summary>
        /// 解析堆叠数量
        /// </summary>
        private static int ResolveStackCount(IItemTableData tableData)
        {
            if (tableData.ItemStackType == EItemStackType.NoStackable)
                return 1;

            int maxStack = Mathf.Max(1, tableData.MaxStackCount);
            return Random.Range(1, maxStack + 1);
        }

        /// <summary>
        /// 六色权重求和
        /// </summary>
        private static float SumRarityWeights(RarityGradient gradient)
        {
            return Mathf.Max(0f, gradient.White)
                   + Mathf.Max(0f, gradient.Green)
                   + Mathf.Max(0f, gradient.Blue)
                   + Mathf.Max(0f, gradient.Purple)
                   + Mathf.Max(0f, gradient.Gold)
                   + Mathf.Max(0f, gradient.Red);
        }

        /// <summary>
        /// 候选按占地降序
        /// </summary>
        private static int CompareCandidateByAreaDesc(LootCandidate a, LootCandidate b)
        {
            return b.CellArea.CompareTo(a.CellArea);
        }

        #endregion
    }
}
