using System.Collections;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 容器存档与 View 重建
    /// </summary>
    public partial class GridContainerView
    {
        /// <summary> 容器存档 ID </summary>
        [SerializeField]
        private int containerId = 1;

        /// <summary> 是否启用自动存读档 </summary>
        [SerializeField]
        private bool enablePersistence = false;

        /// <summary> 容器存档 ID </summary>
        public int ContainerId => containerId;

        /// <summary>
        /// 启动时尝试从存档恢复
        /// </summary>
        private bool TryRestoreFromSaveOnStart()
        {
            if (!enablePersistence || containerId <= 0)
                return false;

            if (!GridInventoryService.HasSaveFile(containerId))
                return false;

            if (!gridInventoryService.TryLoadInventory(containerId))
                return false;

            // 尺寸不一致的旧存档不可用 丢弃后按视图尺寸重新初始化 Core
            if (gridInventoryService.GridSize != gridRowAndCloumns)
            {
                Debug.LogWarning(
                    $"[{name}] 存档网格 {gridInventoryService.GridSize} 与视图配置 {gridRowAndCloumns} 不一致 已忽略并删除旧存档");
                GridInventoryService.TryDeleteSaveFile(containerId);
                gridInventoryService.Init(gridRowAndCloumns);
                return false;
            }

            StartCoroutine(RebuildItemViewsAfterBootstrap());
            return true;
        }

        /// <summary>
        /// 等 Bootstrap 完成后再重建物品 UI
        /// </summary>
        private IEnumerator RebuildItemViewsAfterBootstrap()
        {
            yield return null;
            RebuildItemViewsFromCore();
        }

        /// <summary>
        /// Core 网格与视图尺寸对齐 不一致则清空并重建
        /// </summary>
        public void InitCoreGridMatchesView()
        {
            if (gridInventoryService is null)
                return;

            if (gridInventoryService.GridSize == gridRowAndCloumns)
                return;

            Debug.LogWarning(
                $"[{name}] Core网格 {gridInventoryService.GridSize} 与视图 {gridRowAndCloumns} 不一致 已按视图重建");
            ClearAllItems();
            gridInventoryService.Init(gridRowAndCloumns);
        }

        /// <summary>
        /// 仅清空物品 UI 不改动 Core
        /// </summary>
        private void ClearAllItemViewsOnly()
        {
            // 场景预摆物可能尚未进字典 需按 ItemContent 子节点全清
            var itemViewArray = itemContent.GetComponentsInChildren<ItemView>(true);
            for (int i = 0; i < itemViewArray.Length; i++)
            {
                var itemView = itemViewArray[i];
                if (itemView is null)
                    continue;

                Destroy(itemView.gameObject);
            }

            itemViewDict.Clear();
            itemViewsCache = null;
        }

        /// <summary>
        /// 从 Core 重建全部物品 UI
        /// </summary>
        private void RebuildItemViewsFromCore()
        {
            Canvas.ForceUpdateCanvases();
            ClearAllItemViewsOnly();

            var itemRtDataList = gridInventoryService.GetAllItemRtDataList();
            for (int i = 0; i < itemRtDataList.Count; i++)
            {
                var itemData = itemRtDataList[i];
                var itemView = SpawnItemView(itemData);
                if (itemView is null)
                    continue;

                SyncItemViewPlacement(itemView, itemData);
            }
        }

        /// <summary> 是否启用自动存读档 </summary>
        public bool EnablePersistence => enablePersistence;

        /// <summary>
        /// GM 开关自动存读档 关则退出暂停不会写盘 启动也不会读档
        /// </summary>
        public void SetEnablePersistence(bool enabled)
        {
            enablePersistence = enabled;
        }

        /// <summary> 当前容器是否存在存档文件 </summary>
        public bool HasSaveFile =>
            containerId > 0 && GridInventoryService.HasSaveFile(containerId);

        /// <summary>
        /// 手动写入存档
        /// </summary>
        public bool TrySaveToDisk()
        {
            if (!enablePersistence || containerId <= 0)
                return false;

            if (gridInventoryService is null)
                return false;

            return gridInventoryService.TrySaveInventory(containerId);
        }

        /// <summary>
        /// GM 强制写入存档 忽略 enablePersistence
        /// </summary>
        public bool TrySaveToDiskForce()
        {
            if (containerId <= 0 || gridInventoryService is null)
                return false;

            return gridInventoryService.TrySaveInventory(containerId);
        }

        /// <summary>
        /// 手动读取存档并刷新 UI
        /// </summary>
        public bool TryLoadFromDisk()
        {
            if (containerId <= 0 || gridInventoryService is null)
                return false;

            if (!gridInventoryService.TryLoadInventory(containerId))
                return false;

            if (gridInventoryService.GridSize != gridRowAndCloumns)
            {
                Debug.LogWarning(
                    $"[{name}] 存档网格 {gridInventoryService.GridSize} 与视图配置 {gridRowAndCloumns} 不一致 已取消读取并恢复视图尺寸");
                gridInventoryService.Init(gridRowAndCloumns);
                ClearAllItemViewsOnly();
                return false;
            }

            RebuildItemViewsFromCore();
            return true;
        }

        void OnApplicationQuit()
        {
            TrySaveToDisk();
        }

        void OnApplicationPause(bool pause)
        {
            if (pause)
                TrySaveToDisk();
        }
    }
}
