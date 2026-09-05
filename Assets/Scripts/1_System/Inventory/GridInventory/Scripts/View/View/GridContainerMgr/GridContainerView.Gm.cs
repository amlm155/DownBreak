using System.Collections.Generic;
using cfg.item;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 此脚本放置了GM相关的API 用于编辑器下GM投放物品
    /// </summary>
    public partial class GridContainerView
    {
        #region 生命周期

        void OnEnable()
        {
            GridMainContainerManager.Register(this);
        }

        void OnDisable()
        {
            GridMainContainerManager.Unregister(this);
        }

        #endregion

        #region 物品查询

        /// <summary> 逻辑服务是否已初始化 </summary>
        public bool IsInventoryReady => gridInventoryService is not null;

        /// <summary> 容器显示名 未填则用物体名 </summary>
        public string ContainerName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(containerDisplayName))
                    return containerDisplayName.Trim();
                return gameObject.name;
            }
        }

        /// <summary>
        /// 获取当前容器内所有物品视图
        /// </summary>
        public List<ItemView> GetItemViewList()
        {
            return new List<ItemView>(itemViewDict.Values);
        }

        /// <summary>
        /// 按实例 ID 取物品视图
        /// </summary>
        public bool TryGetItemView(string instancedItemId, out ItemView itemView)
        {
            itemView = null;
            if (string.IsNullOrEmpty(instancedItemId) || itemViewDict == null)
                return false;
            return itemViewDict.TryGetValue(instancedItemId, out itemView);
        }

        #endregion

        #region 物品投放

        /// <summary>
        /// 按表 ID 新建实例后先堆叠再找空位投放
        /// </summary>
        public bool TryPickupPlaceStackThenEmpty(int excelItemId, int stackCount = 1)
        {
            EnsureInventoryService();
            if (gridInventoryService is null)
                return false;

            if (!LubanTables.TryGetItem(excelItemId, out var itemTableData))
                return false;

            int clampedStack = stackCount;
            if (itemTableData.ItemStackType == EItemStackType.NoStackable)
                clampedStack = 1;
            else
                clampedStack = Mathf.Clamp(stackCount, 1, Mathf.Max(1, itemTableData.MaxStackCount));

            var itemRtData = ItemRtData.ItemTableData2ItemRtData(itemTableData, clampedStack);
            return TryInsertExistingItemRtData(itemRtData);
        }

        /// <summary>
        /// 投放到首个可放置空位
        /// </summary>
        public ItemView CreatItemUIAtFirstEmpty(int excelItemId)
        {
            return CreatItemUIAtFirstEmpty(excelItemId, 1);
        }

        /// <summary>
        /// 投放到首个可放置空位 指定堆叠数
        /// </summary>
        public ItemView CreatItemUIAtFirstEmpty(int excelItemId, int stackCount)
        {
            var itemRtData = gridInventoryService.CreatItemAtFirstEmpty(excelItemId, stackCount);
            if (itemRtData is null) return null;

            var itemView = SpawnItemView(itemRtData);
            if (itemView is null)
            {
                gridInventoryService.TryRemoveItem(itemRtData.AnchorPos);
                return null;
            }

            SyncItemViewPlacement(itemView, itemRtData);
            return itemView;
        }

        /// <summary>
        /// 投放到随机可放置空位 指定堆叠数
        /// </summary>
        public ItemView CreatItemUIAtRandomEmpty(int excelItemId, int stackCount)
        {
            var itemRtData = gridInventoryService.CreatItemAtRandomEmpty(excelItemId, stackCount);
            if (itemRtData is null) return null;

            var itemView = SpawnItemView(itemRtData);
            if (itemView is null)
            {
                gridInventoryService.TryRemoveItem(itemRtData.AnchorPos);
                return null;
            }

            SyncItemViewPlacement(itemView, itemRtData);
            return itemView;
        }

        #endregion

        #region 取出与清空

        /// <summary>
        /// 清空容器内全部物品
        /// </summary>
        public void ClearAllItems()
        {
            var itemViewList = GetItemViewList();
            for (int i = 0; i < itemViewList.Count; i++)
                DestroyItemUI(itemViewList[i]);
        }

        /// <summary>
        /// 取出全部物品数据并销毁视图 保留 ItemRtData 实例
        /// excludeInstancedItemId 用于跳过拖拽中的装备本体
        /// </summary>
        public List<ItemRtData> TakeAllItemsOut(string excludeInstancedItemId = null)
        {
            EnsureInventoryService();
            var itemViewList = GetItemViewList();
            var itemDataList = new List<ItemRtData>(itemViewList.Count);
            for (int i = 0; i < itemViewList.Count; i++)
            {
                var itemView = itemViewList[i];
                if (itemView == null || itemView.ItemData == null)
                    continue;

                var itemData = itemView.ItemData;
                if (!string.IsNullOrEmpty(excludeInstancedItemId)
                    && itemData.InstancedItemId == excludeInstancedItemId)
                    continue;

                // 拖拽中已从数据层移除 跳过 由拖拽结束销毁视图
                var removeReport = gridInventoryService.TryRemoveItem(itemData.AnchorPos);
                if (!removeReport.IsSuccess)
                    continue;

                itemViewDict.Remove(itemData.InstancedItemId);
                Destroy(itemView.gameObject);
                itemDataList.Add(itemData);
            }

            return itemDataList;
        }

        /// <summary>
        /// 将已有运行时物品先堆叠再塞入空位
        /// </summary>
        public bool TryInsertExistingItemRtData(ItemRtData itemRtData)
        {
            EnsureInventoryService();
            if (itemRtData == null || gridInventoryService is null)
                return false;

            bool isSuccess = gridInventoryService.TryAddStackThenFirst(
                itemRtData,
                out var placedItem,
                out var stackedTargetList);

            if (!isSuccess)
                return false;

            for (int i = 0; i < stackedTargetList.Count; i++)
            {
                var stackedData = stackedTargetList[i];
                if (itemViewDict.TryGetValue(stackedData.InstancedItemId, out var stackedView))
                    SyncItemViewPlacement(stackedView, stackedData);
            }

            if (placedItem != null)
            {
                var itemView = SpawnItemView(placedItem);
                if (itemView is null)
                {
                    gridInventoryService.TryRemoveItem(placedItem.AnchorPos);
                    return false;
                }

                SyncItemViewPlacement(itemView, placedItem);
            }

            return true;
        }

        #endregion
    }
}
