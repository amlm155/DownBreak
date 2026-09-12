using System;
using System.Collections.Generic;
using cfg.item;
using MmInventory;
using UnityEngine;

namespace Interaction.Editor
{
    /// <summary>
    /// 表面摆放物品的来源模式
    /// </summary>
    public enum ESurfacePlacerSourceMode
    {
        /// <summary> 指定物品 ID 列表 按权重抽取 </summary>
        ItemIdList = 0,

        /// <summary> 按物品类型与稀有度从物品表随机抽 </summary>
        RandomByType = 1,
    }

    /// <summary>
    /// 生成物的挂载方式
    /// </summary>
    public enum ESurfacePlacerGroupMode
    {
        /// <summary> 目标下新建分组节点 便于整体清理 </summary>
        GroupUnderTarget = 0,

        /// <summary> 场景根节点下新建分组节点 </summary>
        GroupUnderSceneRoot = 1,

        /// <summary> 不建分组 直接挂在目标下 </summary>
        NoGroupUnderTarget = 2,
    }

    /// <summary>
    /// 落位对齐方式
    /// </summary>
    public enum EPlacementAlignMode
    {
        /// <summary> 物品底面(包围盒最低点)贴承托面 视觉与物理都正确 但 Transform 的 y 会比锚点高一个"轴心到底部"的距离</summary>
        BottomToSurface = 0,

        /// <summary> 物品轴心直接落在锚点位置 Transform 数值与锚点完全一致 轴心在模型中间的预制体会半埋</summary>
        PivotToAnchor = 1,
    }

    /// <summary>
    /// 摆放物的运行时行为
    /// </summary>
    public enum EPlacementRuntimeMode
    {
        /// <summary> 保持编辑位置 不随机姿态不下落 刚体运动学 不会被碰撞顶飞(推荐)</summary>
        KeepPlaced = 0,

        /// <summary> 正常世界掉落物 运行时随机姿态自由下落 需要下方有碰撞体托住</summary>
        NormalDrop = 1,
    }

    /// <summary>
    /// 摆放方式 表面采样 / 锚点
    /// </summary>
    public enum EPlacementMode
    {
        /// <summary> 在目标碰撞体表面自动采样摆放 </summary>
        SurfaceSample = 0,

        /// <summary> 按场景里手工摆好的放置锚点摆放 </summary>
        PlacementAnchor = 1,
    }

    /// <summary>
    /// 采样范围 顶面 / 内部 / 两者都采
    /// </summary>
    public enum ESurfacePlacerSampleMode
    {
        /// <summary> 只采顶面等朝上的开放表面 </summary>
        TopOnly = 0,

        /// <summary> 只采内部空间(冰箱隔板 货架层板)</summary>
        InteriorOnly = 1,

        /// <summary> 顶面与内部都采 每件随机挑一种</summary>
        TopAndInterior = 2,
    }

    /// <summary>
    /// ID 列表里的一条物品配置
    /// </summary>
    [Serializable]
    public sealed class SurfacePlacerItemEntry
    {
        /// <summary> 物品表 ID </summary>
        public int itemId = 2001;

        /// <summary> 抽取权重 越大越容易抽到 </summary>
        public int weight = 1;

        /// <summary> 生成的堆叠数量 </summary>
        public int stackCount = 1;
    }

    /// <summary>
    /// 物品表面摆放工具配置
    /// 只服务编辑器摆放 不参与运行时逻辑
    /// </summary>
    [Serializable]
    public sealed class SurfacePlacerSettings
    {
        #region 表面采样

        /// <summary> 摆放方式 </summary>
        public EPlacementMode placementMode = EPlacementMode.SurfaceSample;

        /// <summary> 采样范围 顶面 / 内部 / 两者都采 </summary>
        public ESurfacePlacerSampleMode sampleMode = ESurfacePlacerSampleMode.TopAndInterior;

        /// <summary> 表面层掩码 默认排除 UI / 玩家 / 可交互物层 </summary>
        public int surfaceLayerMask =
            ~((1 << 5) | (1 << 6) | (1 << 7) | (1 << 8) | (1 << 9) | (1 << 10));

        /// <summary> 只接受目标自身与子物体的碰撞体 </summary>
        public bool onlyTargetColliders = true;

        /// <summary> 包围盒水平内缩 避免贴边生成后掉落 </summary>
        public float horizontalMargin = 0.05f;

        /// <summary> 生成点之间的最小间距 </summary>
        public float minSpacing = 0.25f;

        /// <summary> 允许的最大表面倾角 超过视为墙面 </summary>
        public float maxSlopeAngle = 25f;

        /// <summary> 射线起点在包围盒顶面之上的抬高 </summary>
        public float rayStartHeight = 2f;

        /// <summary> 射线向下额外延伸的长度 </summary>
        public float rayMaxDistance = 20f;

        /// <summary> 单个点位最多尝试的采样次数 </summary>
        public int sampleAttemptsPerPoint = 40;

        /// <summary> 避开可交互物层上已有的掉落物 </summary>
        public bool avoidExistingItems = true;

        /// <summary> 净空检查 避免物品和台面/侧壁/已摆物品穿模 </summary>
        public bool avoidClipping = true;

        /// <summary> 内部采样起点距包围盒顶面留出的高度 用来跳过外壳顶板 </summary>
        public float interiorTopInset = 0.25f;

        /// <summary> 内部采样起点距包围盒底面的高度 </summary>
        public float interiorBottomInset = 0.03f;

        /// <summary> 内部向下射线长度 </summary>
        public float interiorRayLength = 4f;

        /// <summary> 要求内部采样点头顶有目标自己的几何 才是真内部 </summary>
        public bool requireOverheadCover = false;

        /// <summary> 头顶遮挡检查距离 </summary>
        public float overheadCheckDistance = 4f;

        #endregion

        #region 物品来源

        /// <summary> 来源模式 </summary>
        public ESurfacePlacerSourceMode sourceMode = ESurfacePlacerSourceMode.ItemIdList;

        /// <summary> ID 列表模式的条目 </summary>
        public List<SurfacePlacerItemEntry> itemEntryList = new List<SurfacePlacerItemEntry>();

        /// <summary> 随机模式的物品类型 空表示不限 </summary>
        public List<EItemType> randomTypeList = new List<EItemType>();

        /// <summary> 是否按稀有度过滤 </summary>
        public bool filterByRarity = false;

        /// <summary> 随机模式的稀有度 空表示不限 </summary>
        public List<EItemRarity> randomRarityList = new List<EItemRarity>();

        #endregion

        #region 放置

        /// <summary> 目标生成件数 </summary>
        public int placeCount = 8;

        /// <summary> 挂载方式 </summary>
        public ESurfacePlacerGroupMode groupMode = ESurfacePlacerGroupMode.GroupUnderTarget;

        /// <summary> 生成物相对表面的抬升 避免和表面穿模 </summary>
        public float surfaceOffset = 0.02f;

        /// <summary> 落位对齐方式 底面贴面 / 轴心对齐 </summary>
        public EPlacementAlignMode alignMode = EPlacementAlignMode.BottomToSurface;

        /// <summary> 贴合表面法线 只在斜面上生效 平面等价于不旋转 </summary>
        public bool alignToSurfaceNormal = true;

        /// <summary> 随机水平朝向 </summary>
        public bool randomYaw = true;

        /// <summary> 随机种子 0 表示每次不同 </summary>
        public int randomSeed = 0;

        /// <summary> 堆叠数量大于 1 时写入实例快照 </summary>
        public bool writeSaveDataWhenStacked = true;

        /// <summary> 摆放物运行时行为 </summary>
        public EPlacementRuntimeMode runtimeMode = EPlacementRuntimeMode.KeepPlaced;

        /// <summary> 锚点模式生成前 自动给锚点补承托碰撞体 </summary>
        public bool autoCreateSupportCollider = true;

        #endregion
    }

    /// <summary>
    /// 候选物品池 按配置从物品表筛出可摆放到场景的物品
    /// </summary>
    public sealed class SurfacePlacerItemPool
    {
        /// <summary> 候选物品 </summary>
        private readonly List<IItemTableData> candidateList = new List<IItemTableData>();

        /// <summary> 与候选一一对应的抽取权重 </summary>
        private readonly List<int> weightList = new List<int>();

        /// <summary> 与候选一一对应的堆叠数量 </summary>
        private readonly List<int> stackList = new List<int>();

        /// <summary> 权重总和 </summary>
        private int totalWeight;

        /// <summary> 候选数量 </summary>
        public int Count => candidateList.Count;

        /// <summary> 表里找不到的物品 ID 数量 </summary>
        public int MissingItemCount { get; private set; }

        /// <summary> 没有 world_prefab_path 而跳过的数量 </summary>
        public int NoPrefabCount { get; private set; }

        /// <summary>
        /// 按配置构建候选池
        /// </summary>
        public static SurfacePlacerItemPool Build(SurfacePlacerSettings settings)
        {
            var pool = new SurfacePlacerItemPool();
            if (settings == null)
                return pool;

            if (settings.sourceMode == ESurfacePlacerSourceMode.ItemIdList)
                pool.BuildFromItemIdList(settings);
            else
                pool.BuildFromRandomFilter(settings);

            return pool;
        }

        /// <summary>
        /// 按权重抽一件
        /// </summary>
        public bool TryPick(System.Random random, out IItemTableData item, out int stackCount)
        {
            item = null;
            stackCount = 1;
            if (random == null || candidateList.Count == 0 || totalWeight <= 0)
                return false;

            int roll = random.Next(totalWeight);
            for (int i = 0; i < candidateList.Count; i++)
            {
                roll -= weightList[i];
                if (roll >= 0)
                    continue;

                item = candidateList[i];
                stackCount = stackList[i];
                return true;
            }

            // 兜底取最后一个 理论上不会走到
            item = candidateList[candidateList.Count - 1];
            stackCount = stackList[stackList.Count - 1];
            return true;
        }

        /// <summary>
        /// 写入一条候选
        /// </summary>
        private void Add(IItemTableData item, int weight, int stackCount)
        {
            int safeWeight = Mathf.Max(1, weight);
            candidateList.Add(item);
            weightList.Add(safeWeight);
            stackList.Add(Mathf.Max(1, stackCount));
            totalWeight += safeWeight;
        }

        /// <summary>
        /// ID 列表模式构建
        /// </summary>
        private void BuildFromItemIdList(SurfacePlacerSettings settings)
        {
            var entryList = settings.itemEntryList;
            if (entryList == null)
                return;

            for (int i = 0; i < entryList.Count; i++)
            {
                var entry = entryList[i];
                if (entry == null || entry.itemId <= 0)
                    continue;

                if (!LubanTables.TryGetItem(entry.itemId, out var item))
                {
                    MissingItemCount++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.WorldPrefabPath))
                {
                    NoPrefabCount++;
                    continue;
                }

                Add(item, entry.weight, entry.stackCount);
            }
        }

        /// <summary>
        /// 按类型与稀有度随机模式构建
        /// </summary>
        private void BuildFromRandomFilter(SurfacePlacerSettings settings)
        {
            var itemList = LubanTables.ItemList;
            if (itemList == null)
                return;

            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                if (item == null)
                    continue;

                // 家具与储物箱没有 world_prefab_path 不能作为世界掉落物
                if (string.IsNullOrWhiteSpace(item.WorldPrefabPath))
                {
                    NoPrefabCount++;
                    continue;
                }

                if (!MatchesType(settings, item))
                    continue;

                if (!MatchesRarity(settings, item))
                    continue;

                Add(item, 1, 1);
            }
        }

        /// <summary>
        /// 类型过滤 列表为空表示不限
        /// </summary>
        private static bool MatchesType(SurfacePlacerSettings settings, IItemTableData item)
        {
            var typeList = settings.randomTypeList;
            if (typeList == null || typeList.Count == 0)
                return true;

            return typeList.Contains(item.ItemType);
        }

        /// <summary>
        /// 稀有度过滤
        /// </summary>
        private static bool MatchesRarity(SurfacePlacerSettings settings, IItemTableData item)
        {
            if (!settings.filterByRarity)
                return true;

            var rarityList = settings.randomRarityList;
            if (rarityList == null || rarityList.Count == 0)
                return true;

            return rarityList.Contains(item.ItemRarity);
        }
    }
}
