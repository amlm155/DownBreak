/// <summary>
/// BagPanel 实现 IUIBagInteract 负责拾取 开槽 搜刮栏
/// </summary>

using cfg.item;
using Cysharp.Threading.Tasks;
using DBGameSystem;
using Interaction;
using Interaction.Player;
using MmInventory;
using MmUIFrameWork.Core;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    public partial class BagPanel : IUIBagInteract
    {
        #region GameHub 注册

        /// <summary>
        /// 预热就绪后注册背包接口
        /// </summary>
        private void RegisterBagInteractService()
        {
            GameHub.Register<IUIBagInteract>(this);
        }

        /// <summary>
        /// 销毁时注销背包接口
        /// </summary>
        private void UnregisterBagInteractService()
        {
            if (ReferenceEquals(GameHub.Get<IUIBagInteract>(), this))
                GameHub.Unregister<IUIBagInteract>();
        }

        #endregion

        #region 拾取入包

        /// <summary>
        /// 给予制作产物 先入包 满了丢到脚下
        /// </summary>
        public bool TryGiveItem(int itemTableId, int stackCount)
        {
            if (!LubanTables.TryGetItem(itemTableId, out var itemTableData))
                return false;

            int clampedStack = stackCount;
            if (itemTableData.ItemStackType == EItemStackType.NoStackable)
                clampedStack = 1;
            else
                clampedStack = Mathf.Clamp(stackCount, 1, Mathf.Max(1, itemTableData.MaxStackCount));

            var itemRtData = ItemRtData.ItemTableData2ItemRtData(itemTableData, clampedStack);
            if (TryPlaceExistingItemToContainer(itemRtData, out _))
                return true;

            return SpawnWorldDrop(itemRtData, itemRtData.CurrStackCount);
        }

        /// <summary>
        /// 在世界坐标生成掉落物 破坏掉落用
        /// </summary>
        public bool TrySpawnWorldItem(int itemTableId, int stackCount, Vector3 worldPosition)
        {
            if (!LubanTables.TryGetItem(itemTableId, out var itemTableData))
                return false;

            int clampedStack = stackCount;
            if (itemTableData.ItemStackType == EItemStackType.NoStackable)
                clampedStack = 1;
            else
                clampedStack = Mathf.Clamp(stackCount, 1, Mathf.Max(1, itemTableData.MaxStackCount));

            var itemRtData = ItemRtData.ItemTableData2ItemRtData(itemTableData, clampedStack);
            var saveData = ItemSaveData.ItemRtToItemSaveData(itemRtData);
            saveData.hasStackCount = itemRtData.CurrStackCount;
            return SpawnWorldDrop(saveData, worldPosition, false, false);
        }

        /// <summary>
        /// 异步在世界坐标生成掉落物 破坏掉落用
        /// </summary>
        public async UniTask<bool> TrySpawnWorldItemAsync(
            int itemTableId,
            int stackCount,
            Vector3 worldPosition)
        {
            if (!LubanTables.TryGetItem(itemTableId, out var itemTableData))
                return false;

            int clampedStack = stackCount;
            if (itemTableData.ItemStackType == EItemStackType.NoStackable)
                clampedStack = 1;
            else
                clampedStack = Mathf.Clamp(stackCount, 1, Mathf.Max(1, itemTableData.MaxStackCount));

            var itemRtData = ItemRtData.ItemTableData2ItemRtData(itemTableData, clampedStack);
            var saveData = ItemSaveData.ItemRtToItemSaveData(itemRtData);
            saveData.hasStackCount = itemRtData.CurrStackCount;
            return await SpawnWorldDropAsync(saveData, worldPosition);
        }

        /// <summary>
        /// 拾取世界物 有快照则还原实例 否则按表 ID 新建
        /// </summary>
        public bool TryPickupWorldItem(IItemInterface itemSource)
        {
            if (itemSource == null)
                return false;

            // 有实例快照 还原后走已有实例入包
            if (itemSource is IItemSaveCarrier carrier && carrier.HasSaveData)
            {
                var saveData = carrier.SaveData;
                if (saveData == null || saveData.excelItemId <= 0)
                    return false;

                var itemRtData = ItemRtData.ItemSaveData2ItemRtData(saveData);
                return TryPickupExistingItem(itemRtData);
            }

            return TryPickupItem(itemSource.ItemTableID);
        }

        /// <summary>
        /// 按已有实例入包 可开装备槽 满包提示
        /// </summary>
        public bool TryPickupExistingItem(ItemRtData itemRtData)
        {
            if (itemRtData == null)
                return false;

            var equipment = LubanTables.Tables.TbEquipment.GetOrDefault(itemRtData.ExcelItemId);
            if (equipment != null)
            {
                var eSlot = equipment.EquipSlot;
                if (!HasContainer(eSlot))
                {
                    AddContainer(eSlot, equipment);
                    return true;
                }
            }

            if (TryPlaceExistingItemToContainer(itemRtData, out _))
                return true;

            TipPanel.Push("背包已满 无法拾取");
            return false;
        }

        /// <summary>
        /// 拾取入包或开槽 按表新建实例
        /// </summary>
        public bool TryPickupItem(int itemTableId)
        {
            if (!LubanTables.TryGetItem(itemTableId, out var itemTableData))
                return false;

            var itemRtData = ItemRtData.ItemTableData2ItemRtData(itemTableData, 1);
            return TryPickupExistingItem(itemRtData);
        }

        /// <summary>
        /// 显示背包并打开搜刮栏
        /// </summary>
        public bool TryOpenScrapContainer(int scrapContainerId, bool alreadyLooted, Object owner = null)
        {
            UIHub.Instance.ShowWindow<BagPanel>();
            return OpenScrapContainer(scrapContainerId, alreadyLooted, owner);
        }

        /// <summary>
        /// 显示背包并打开储物箱栏
        /// </summary>
        public bool TryOpenStorageBox(int storageBoxItemId, Object owner = null)
        {
            UIHub.Instance.ShowWindow<BagPanel>();
            return OpenStorageBox(storageBoxItemId, owner);
        }

        /// <summary>
        /// 容器被打碎时吐出该容器自己的物品 开着从搜刮栏取 关着从容器快照取
        /// </summary>
        public bool TryDropOpenedContainerItems(Object owner, Vector3 worldPosition)
        {
            if (containerHost == null
                || !containerHost.TryTakeOpenedContainerItems(owner, out var itemRtDataList))
            {
                return false;
            }

            bool hasDroppedAny = false;
            for (int i = 0; i < itemRtDataList.Count; i++)
            {
                var itemRtData = itemRtDataList[i];
                if (itemRtData == null)
                    continue;

                var saveData = ItemSaveData.ItemRtToItemSaveData(itemRtData);
                saveData.hasStackCount = itemRtData.CurrStackCount;
                Vector3 dropPos = worldPosition + ResolveContainerBurstOffset(i);
                if (SpawnWorldDrop(saveData, dropPos, false, false))
                    hasDroppedAny = true;
            }

            return hasDroppedAny;
        }

        /// <summary>
        /// 打开物品右键菜单
        /// </summary>
        public void ShowItemMenu(ItemView itemView)
        {
            itemMenu?.Show(itemView);
        }

        /// <summary>
        /// 关闭物品右键菜单
        /// </summary>
        public void HideItemMenu()
        {
            itemMenu?.Hide();
        }

        #endregion

        #region 容器开槽与搜刮栏

        /// <summary>
        /// 显示并按装备数据适配容器 拾取开槽路径
        /// </summary>
        public void AddContainer(EEquipSlot eSlot, Equipment equipment)
        {
            containerHost?.AddContainer(eSlot, equipment);
        }

        /// <summary>
        /// 槽位容器是否已激活
        /// </summary>
        public bool HasContainer(EEquipSlot eSlot)
        {
            return containerHost != null && containerHost.HasContainer(eSlot);
        }

        /// <summary>
        /// 隐藏容器
        /// </summary>
        public void RemoveContainer(EEquipSlot eSlot)
        {
            containerHost?.RemoveContainer(eSlot);
        }

        /// <summary>
        /// 设置搜刮容器外观与容量
        /// </summary>
        public void SetSerachContainer(int scrapContainerId)
        {
            containerHost?.SetSearchContainer(scrapContainerId);
        }

        /// <summary>
        /// 打开搜刮栏 首次投放揭幕 已搜过只显示
        /// </summary>
        public bool OpenScrapContainer(int scrapContainerId, bool alreadyLooted, Object owner = null)
        {
            return containerHost != null
                && containerHost.OpenScrapContainer(scrapContainerId, alreadyLooted, owner);
        }

        /// <summary>
        /// 打开储物箱栏 按表容量建格
        /// </summary>
        public bool OpenStorageBox(int storageBoxItemId, Object owner = null)
        {
            return containerHost != null
                && containerHost.OpenStorageBox(storageBoxItemId, owner);
        }

        /// <summary>
        /// 容器爆出物相对偏移
        /// </summary>
        private static Vector3 ResolveContainerBurstOffset(int index)
        {
            return PlaceAndBreakInteractBehaviour.ResolveBurstOffset(index);
        }

        #endregion
    }
}
