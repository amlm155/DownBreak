/// <summary>
/// BagPanel 入格移除与丢弃
/// </summary>

using System;
using cfg.item;
using Cysharp.Threading.Tasks;
using Interaction;
using MieMieFrameWork.Asset;
using MmInventory;
using UnityEngine;
namespace MieMieUIFrameWork.Runtime
{

    public partial class BagPanel
    {
        #region 物品入格

        /// <summary>
        /// 将已有运行时实例放入激活容器
        /// 按装备槽枚举顺序 先堆叠再空位
        /// </summary>
        public bool TryPlaceExistingItemToContainer(ItemRtData itemRtData,
                                            out IItemTableData itemTableData)
        {
            itemTableData = null;
            if (itemRtData == null)
                return false;

            if (!LubanTables.TryGetItem(itemRtData.ExcelItemId, out itemTableData))
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

                int tipCount = itemRtData.CurrStackCount;
                if (!gridView.TryInsertExistingItemRtData(itemRtData))
                    continue;

                TipPanel.Push($"{itemTableData.Name} x{tipCount}");
                return true;
            }

            return false;
        }
        #endregion

        #region 物品移除


        /// <summary>
        /// 按表 ID 从激活容器移除物品
        /// </summary>

        public void RemoveItemFromContainer(int itemTableID, int stackCount = 1)
        {
            int remainCount = stackCount;
            var eSlotList = (EEquipSlot[])Enum.GetValues(typeof(EEquipSlot));
            for (int i = 0; i < eSlotList.Length && remainCount > 0; i++)
            {
                var eSlot = eSlotList[i];
                if (!HasContainer(eSlot))
                    continue;

                var gridView = containerHost.GetGridView(eSlot);
                if (gridView == null)
                    continue;

                remainCount = RemoveMatchedItems(gridView, itemTableID, remainCount);
            }
        }

        /// <summary>
        /// 从单容器按表 ID 扣减并销毁视图
        /// </summary>
        private int RemoveMatchedItems(GridContainerView gridView, int itemTableID, int remainCount)
        {
            var itemViewList = gridView.GetItemViewList();
            for (int i = itemViewList.Count - 1; i >= 0 && remainCount > 0; i--)
            {
                var itemView = itemViewList[i];
                if (itemView == null || itemView.ItemData == null)
                    continue;
                if (itemView.ItemData.ExcelItemId != itemTableID)
                    continue;

                int takeCount = Mathf.Min(remainCount, itemView.ItemData.CurrStackCount);
                if (takeCount >= itemView.ItemData.CurrStackCount)
                {
                    gridView.DestroyItemUI(itemView);
                    remainCount -= takeCount;
                }
                else
                {
                    itemView.ItemData.CurrStackCount -= takeCount;
                    remainCount -= takeCount;
                }
            }

            return remainCount;
        }
        #endregion

        #region 丢弃与世界掉落


        /// <summary>
        /// 菜单丢弃 数据与 UI 一并移除
        /// </summary>

        private bool OnThrowFromMenu(ItemView itemView)
        {
            if (itemView == null || itemView.ItemData == null)
                return false;

            // 生成世界掉落物
            if (!SpawnWorldDrop(itemView.ItemData, itemView.ItemData.CurrStackCount))
                return false;

            // 移除物品UI
            itemView.OwnerContainer.DestroyItemUI(itemView);
            return true;
        }

        /// <summary>
        /// 按运行时实例生成世界掉落物 保留耐久与实例 ID
        /// </summary>
        private bool SpawnWorldDrop(ItemRtData itemRtData, int stackCount)
        {
            if (itemRtData == null)
                return false;

            int dropCount = Mathf.Clamp(stackCount, 1, itemRtData.CurrStackCount);
            ItemRtData dropRtData = dropCount >= itemRtData.CurrStackCount
                ? itemRtData
                : itemRtData.Clone(dropCount);

            var saveData = ItemSaveData.ItemRtToItemSaveData(dropRtData);
            saveData.hasStackCount = dropCount;
            bool isSuccess = SpawnWorldDrop(saveData);
            // 整实例离包时清轮盘绑定
            if (isSuccess && dropCount >= itemRtData.CurrStackCount)
                ItemWheelSlotStore.ClearInstance(itemRtData.InstancedItemId);
            return isSuccess;
        }

        /// <summary>
        /// 按快照实例化世界掉落物
        /// </summary>
        private bool SpawnWorldDrop(ItemSaveData saveData)
        {
            Vector3 spawnPos = ResolveDropWorldPos();
            return SpawnWorldDrop(saveData, spawnPos);
        }

        /// <summary>
        /// 按快照实例化世界掉落物 指定世界位置
        /// </summary>
        private bool SpawnWorldDrop(
            ItemSaveData saveData,
            Vector3 spawnPos,
            bool showTip = true,
            bool randomizeRotation = true)
        {
            if (saveData == null || saveData.excelItemId <= 0)
            {
                Debug.LogWarning("丢弃失败 快照无效");
                return false;
            }

            if (!LubanTables.TryGetItem(saveData.excelItemId, out var itemTableData))
            {
                Debug.LogWarning($"丢弃失败 无物品表 id={saveData.excelItemId}");
                return false;
            }

            if (string.IsNullOrEmpty(itemTableData.WorldPrefabPath))
            {
                Debug.LogWarning($"丢弃失败 未配置 world_prefab_path id={saveData.excelItemId}");
                return false;
            }

            var prefab = MmAssetMgr.LoadAsset<GameObject>(itemTableData.WorldPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"丢弃失败 加载预制体失败 path={itemTableData.WorldPrefabPath}");
                return false;
            }

            var go = UnityEngine.Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            ItemPhysicsUtil.PrepareWorldDrop(go, randomizeRotation);

            var carrier = go.GetComponent<IItemSaveCarrier>();
            if (carrier != null)
            {
                carrier.BindSaveData(saveData);
            }
            else
            {
                var itemSource = go.GetComponent<IItemInterface>();
                if (itemSource != null)
                    itemSource.BindItemTableID(saveData.excelItemId);
            }

            if (showTip)
                TipPanel.Push($"丢弃 {itemTableData.Name} x{saveData.hasStackCount}");
            return true;
        }

        /// <summary>
        /// 按快照异步实例化世界掉落物 指定世界位置
        /// </summary>
        private async UniTask<bool> SpawnWorldDropAsync(ItemSaveData saveData, Vector3 spawnPos)
        {
            if (saveData == null || saveData.excelItemId <= 0)
            {
                Debug.LogWarning("异步掉落失败 快照无效");
                return false;
            }

            if (!LubanTables.TryGetItem(saveData.excelItemId, out var itemTableData))
            {
                Debug.LogWarning($"异步掉落失败 无物品表 id={saveData.excelItemId}");
                return false;
            }

            if (string.IsNullOrEmpty(itemTableData.WorldPrefabPath))
            {
                Debug.LogWarning($"异步掉落失败 未配置 world_prefab_path id={saveData.excelItemId}");
                return false;
            }

            GameObject go;
            try
            {
                go = await MmAssetMgr.InstantiateAsync(itemTableData.WorldPrefabPath);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }

            if (go == null)
            {
                Debug.LogWarning($"异步掉落失败 加载预制体失败 path={itemTableData.WorldPrefabPath}");
                return false;
            }

            go.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            ItemPhysicsUtil.PrepareWorldDrop(go, false);

            var carrier = go.GetComponent<IItemSaveCarrier>();
            if (carrier != null)
            {
                carrier.BindSaveData(saveData);
            }
            else
            {
                var itemSource = go.GetComponent<IItemInterface>();
                if (itemSource != null)
                    itemSource.BindItemTableID(saveData.excelItemId);
            }

            return true;
        }

        /// <summary>
        /// 解析掉落点 相机前方地面附近
        /// </summary>
        private static Vector3 ResolveDropWorldPos()
        {
            var cam = Camera.main;
            if (cam == null)
                return Vector3.zero;

            return cam.transform.position + cam.transform.forward * 1f;
        }
    }

    #endregion

}
