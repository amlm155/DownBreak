/// <summary>
/// BagPanel 装备与武器穿戴
/// </summary>

using System;
using System.Collections.Generic;
using cfg.item;
using DBWeaponSystem;
using MmInventory;
using UnityEngine;
using DBGameSystem;
namespace MieMieUIFrameWork.Runtime
{

    /// <summary>
    /// 装备与武器穿戴
    /// </summary>
    public partial class BagPanel
    {
        #region 拖拽与菜单装备

        /// <summary>
        /// 拖拽松手装备回调
        /// </summary>

        private bool OnEquipCommitFromDrag(ItemView itemView, GridContainerView sourceContainer)
        {
            if (itemView == null || itemView.ItemData == null)
                return false;

            if (!TryEquipFromItemView(itemView, sourceContainer))
                return false;

            sourceContainer.DestroyDraggingItemView(itemView);
            return true;
        }

        /// <summary>
        /// 尝试穿戴拖拽中的装备或武器
        /// </summary>
        private bool TryEquipFromItemView(ItemView itemView, GridContainerView sourceContainer)
        {
            var newItemData = itemView.ItemData;
            var weapon = LubanTables.Tables.TbWeapon.GetOrDefault(newItemData.ExcelItemId);
            if (weapon != null)
                return TryEquipWeaponFromItemView(newItemData, sourceContainer);

            var equipment = LubanTables.Tables.TbEquipment.GetOrDefault(newItemData.ExcelItemId);
            if (equipment == null)
                return false;

            var eSlot = equipment.EquipSlot;
            if (!containerHost.TryGetGroup(eSlot, out var group))
                return false;

            // 空槽直接穿
            if (!HasContainer(eSlot))
            {
                group.WearEquipment(newItemData, equipment);
                group.gameObject.SetActive(true);
                containerHost.RefreshOrder();
                TipPanel.Push($"装备 {equipment.Name}");
                return true;
            }

            var oldItemData = group.EquippedItemData;
            var oldEquipment = group.EquippedEquipment;
            if (oldItemData == null || oldEquipment == null)
            {
                group.WearEquipment(newItemData, equipment);
                containerHost.RefreshOrder();
                TipPanel.Push($"装备 {equipment.Name}");
                return true;
            }

            // 同 ID 比耐久
            if (oldItemData.ExcelItemId == newItemData.ExcelItemId)
                return TryReplaceSameIdEquipment(group, oldItemData, newItemData, equipment, sourceContainer);

            int oldCells = oldEquipment.Capacity.X * oldEquipment.Capacity.Y;
            int newCells = equipment.Capacity.X * equipment.Capacity.Y;

            // 小换大或等大
            if (newCells >= oldCells)
                return TryReplaceLargerOrEqualEquipment(
                    group, oldItemData, newItemData, equipment, sourceContainer);

            // 大换小
            return TryReplaceSmallerEquipment(
                group, oldItemData, oldEquipment, newItemData, equipment, sourceContainer);
        }

        /// <summary>
        /// 装备武器到手持 同 ID 比耐久
        /// </summary>
        private bool TryEquipWeaponFromItemView(ItemRtData newItemData, GridContainerView sourceContainer)
        {
            if (GameHub.Get<IWeaponSystem>() == null)
            {
                Debug.LogWarning("WeaponSystem 不存在");
                return false;
            }

            if (equippedWeaponData != null
                && equippedWeaponData.ExcelItemId == newItemData.ExcelItemId
                && newItemData.CurrDurability <= equippedWeaponData.CurrDurability)
                return false;

            // 必须在 TryEquipWeapon 前缓存旧枪 事件回调会先改写 equippedWeaponData
            var oldWeaponData = equippedWeaponData;
            if (!GameHub.Get<IWeaponSystem>().TryEquipWeapon(newItemData, out _))
                return false;

            equippedWeaponData = newItemData;
            RefreshWeaponIcon();
            if (oldWeaponData != null
                && oldWeaponData.InstancedItemId != newItemData.InstancedItemId)
                TryReturnItemToBags(oldWeaponData, sourceContainer);

            var weaponRow = LubanTables.Tables.TbWeapon.GetOrDefault(newItemData.ExcelItemId);
            TipPanel.Push($"装备 {weaponRow.Name}");
            return true;
        }

        /// <summary>
        /// 轮盘悬停切换手持武器 按实例切换不比耐久
        /// </summary>
        public bool TryEquipWeaponFromWheel(ItemRtData newItemData)
        {
            if (newItemData == null)
                return false;

            if (GameHub.Get<IWeaponSystem>() == null)
                return false;

            if (equippedWeaponData != null
                && equippedWeaponData.InstancedItemId == newItemData.InstancedItemId)
                return true;

            var weapon = LubanTables.Tables.TbWeapon.GetOrDefault(newItemData.ExcelItemId);
            if (weapon == null)
                return false;

            if (!TryFindItemViewInBags(newItemData.InstancedItemId, out var itemView, out var container))
                return false;

            // 必须在 TryEquipWeapon 前缓存旧枪 事件回调会先改写 equippedWeaponData
            var oldWeaponData = equippedWeaponData;
            if (!GameHub.Get<IWeaponSystem>().TryEquipWeapon(newItemData, out _))
                return false;

            container.DestroyItemUI(itemView);

            equippedWeaponData = newItemData;
            RefreshWeaponIcon();
            if (oldWeaponData != null
                && oldWeaponData.InstancedItemId != newItemData.InstancedItemId)
                TryReturnItemToBags(oldWeaponData, container);

            return true;
        }

        /// <summary>
        /// 在已穿戴容器中按实例查找 ItemView
        /// </summary>
        private bool TryFindItemViewInBags(string instancedItemId,
                                           out ItemView itemView,
                                           out GridContainerView container)
        {
            itemView = null;
            container = null;
            if (string.IsNullOrEmpty(instancedItemId))
                return false;

            var eSlotList = (EEquipSlot[])Enum.GetValues(typeof(EEquipSlot));
            for (int i = 0; i < eSlotList.Length; i++)
            {
                var eSlot = eSlotList[i];
                if (!HasContainer(eSlot))
                    continue;

                var gridView = containerHost.GetGridView(eSlot);
                if (gridView == null)
                    continue;

                if (!gridView.TryGetItemView(instancedItemId, out var foundView))
                    continue;

                itemView = foundView;
                container = gridView;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 菜单装备 数据与 UI 一并移除
        /// </summary>
        private bool OnEquipFromMenu(ItemView itemView)
        {
            if (itemView == null || itemView.ItemData == null || itemView.OwnerContainer == null)
                return false;

            if (!TryEquipFromItemView(itemView, itemView.OwnerContainer))
                return false;

            itemView.OwnerContainer.DestroyItemUI(itemView);
            return true;
        }
        #endregion

        #region 卸装


        /// <summary>
        /// 双击热区卸装 无装备则无响应
        /// </summary>

        private void TryUnequipFromHotspot(EEquipSlot eSlot)
        {
            if (eSlot == EEquipSlot.Hand)
            {
                TryUnequipWeapon();
                return;
            }

            TryUnequipEquipment(eSlot);
        }

        /// <summary>
        /// 卸下手持武器并回包
        /// </summary>
        private bool TryUnequipWeapon()
        {
            if (equippedWeaponData == null)
                return false;

            var oldWeaponData = equippedWeaponData;
            equippedWeaponData = null;
            RefreshWeaponIcon();

            if (GameHub.Get<IWeaponSystem>() != null)
                GameHub.Get<IWeaponSystem>().ClearWeapon();

            TryReturnItemToBags(oldWeaponData, null);

            var weaponRow = LubanTables.Tables.TbWeapon.GetOrDefault(oldWeaponData.ExcelItemId);
            TipPanel.Push($"卸下 {(weaponRow != null ? weaponRow.Name : "武器")}");
            return true;
        }

        /// <summary>
        /// 卸下槽位装备 内容物与本体一并回包
        /// </summary>
        private bool TryUnequipEquipment(EEquipSlot eSlot)
        {
            if (!HasContainer(eSlot))
                return false;

            if (!containerHost.TryGetGroup(eSlot, out var group))
                return false;

            var oldItemData = group.EquippedItemData;
            var oldEquipment = group.EquippedEquipment;
            if (oldItemData == null)
                return false;

            var gridView = group.GridView;
            var contentList = gridView != null
                ? gridView.TakeAllItemsOut()
                : null;

            // 先关槽 避免内容物回填进正卸的容器
            group.ClearEquippedRecord();
            group.gameObject.SetActive(false);
            containerHost.RefreshOrder();

            if (contentList != null)
            {
                for (int i = 0; i < contentList.Count; i++)
                    TryReturnItemToBags(contentList[i], null);
            }

            TryReturnItemToBags(oldItemData, null);
            TipPanel.Push($"卸下 {(oldEquipment != null ? oldEquipment.Name : "装备")}");
            return true;
        }
        #endregion

        #region 换装策略


        /// <summary>
        /// 同 ID 换耐久更高者
        /// </summary>

        private bool TryReplaceSameIdEquipment(GridContainerGroup group,
                                               ItemRtData oldItemData,
                                               ItemRtData newItemData,
                                               Equipment equipment,
                                               GridContainerView sourceContainer)
        {
            if (newItemData.CurrDurability <= oldItemData.CurrDurability)
                return false;

            group.WearEquipment(newItemData, equipment, false);
            TryReturnItemToBags(oldItemData, sourceContainer);
            TipPanel.Push($"更换 {equipment.Name}");
            return true;
        }

        /// <summary>
        /// 小容器换大容器 先填空位 剩余丢弃
        /// </summary>
        private bool TryReplaceLargerOrEqualEquipment(GridContainerGroup group,
                                                      ItemRtData oldItemData,
                                                      ItemRtData newItemData,
                                                      Equipment equipment,
                                                      GridContainerView sourceContainer)
        {
            var gridView = group.GridView;
            // 跳过正在拖的新装备 避免被当内容物塞回格子
            var contentList = gridView.TakeAllItemsOut(newItemData.InstancedItemId);
            group.WearEquipment(newItemData, equipment);
            ReinsertContents(gridView, contentList, true);
            TryReturnItemToBags(oldItemData, sourceContainer);
            containerHost.RefreshOrder();
            TipPanel.Push($"装备 {equipment.Name}");
            return true;
        }

        /// <summary>
        /// 大容器换小容器 装得下才换 保证内容物不丢
        /// </summary>
        private bool TryReplaceSmallerEquipment(GridContainerGroup group,
                                                ItemRtData oldItemData,
                                                Equipment oldEquipment,
                                                ItemRtData newItemData,
                                                Equipment equipment,
                                                GridContainerView sourceContainer)
        {
            var gridView = group.GridView;
            var contentList = gridView.TakeAllItemsOut(newItemData.InstancedItemId);
            var newSize = new Vector2Int(equipment.Capacity.X, equipment.Capacity.Y);

            if (!CanFitAllItems(contentList, newSize))
            {
                group.WearEquipment(oldItemData, oldEquipment);
                ReinsertContents(gridView, contentList, false);
                return false;
            }

            group.WearEquipment(newItemData, equipment);
            ReinsertContents(gridView, contentList, false);
            TryReturnItemToBags(oldItemData, sourceContainer);
            containerHost.RefreshOrder();
            TipPanel.Push($"装备 {equipment.Name}");
            return true;
        }
        #endregion

        #region 内容物回填与兜底


        /// <summary>
        /// 模拟内容物是否能全部放入目标容量
        /// </summary>

        private static bool CanFitAllItems(List<ItemRtData> itemDataList, Vector2Int gridSize)
        {
            if (itemDataList == null || itemDataList.Count == 0)
                return true;

            var simState = new InventoryState(gridSize);
            var sortedList = new List<ItemRtData>(itemDataList);
            sortedList.Sort(CompareItemAreaDesc);

            for (int i = 0; i < sortedList.Count; i++)
            {
                var clone = sortedList[i].Clone(sortedList[i].CurrStackCount);
                if (!simState.SetAtFirstWithRotate(clone, out _))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 面积大者优先
        /// </summary>
        private static int CompareItemAreaDesc(ItemRtData a, ItemRtData b)
        {
            int areaA = a.DataSize.x * a.DataSize.y;
            int areaB = b.DataSize.x * b.DataSize.y;
            return areaB.CompareTo(areaA);
        }

        /// <summary>
        /// 内容物塞回容器 dropOverflow 为真时塞不下则世界丢弃
        /// </summary>
        private void ReinsertContents(GridContainerView gridView,
                                      List<ItemRtData> contentList,
                                      bool dropOverflow)
        {
            if (contentList == null || contentList.Count == 0)
                return;

            contentList.Sort(CompareItemAreaDesc);
            for (int i = 0; i < contentList.Count; i++)
            {
                var itemData = contentList[i];
                if (gridView.TryInsertExistingItemRtData(itemData))
                    continue;

                if (dropOverflow)
                    SpawnWorldDrop(itemData, itemData.CurrStackCount);
                else
                    Debug.LogWarning($"内容物回填失败 id={itemData.ExcelItemId}");
            }
        }

        /// <summary>
        /// 换下的装备回背包 优先源容器
        /// </summary>
        private void TryReturnItemToBags(ItemRtData itemData, GridContainerView preferContainer)
        {
            if (itemData == null)
                return;

            if (preferContainer != null
                && preferContainer.IsInventoryReady
                && preferContainer.TryInsertExistingItemRtData(itemData))
                return;

            var eSlotList = (EEquipSlot[])Enum.GetValues(typeof(EEquipSlot));
            for (int i = 0; i < eSlotList.Length; i++)
            {
                var eSlot = eSlotList[i];
                if (!HasContainer(eSlot))
                    continue;

                var gridView = containerHost.GetGridView(eSlot);
                if (gridView == null || gridView == preferContainer)
                    continue;

                if (gridView.TryInsertExistingItemRtData(itemData))
                    return;
            }

            // 兜底丢地上 避免装备实例蒸发
            SpawnWorldDrop(itemData, itemData.CurrStackCount);
            Debug.LogWarning($"换下装备无处可放 已丢弃 id={itemData.ExcelItemId}");
        }
    }

    #endregion

}
