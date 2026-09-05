using System;
using MmInventory;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// 堆叠菜单流程 丢弃数量与拆分 世界掉落由外部回调
    /// </summary>
    public class BagStackFlow : MonoBehaviour
    {
        /// <summary> 数量菜单 </summary>
        private StackMenu stackMenu;

        /// <summary> 世界掉落回调 </summary>
        private Func<ItemRtData, int, bool> spawnWorldDropHandler;

        /// <summary> 待丢弃物品视图 </summary>
        private ItemView pendingThrowItemView;

        /// <summary> 待丢弃来源容器 </summary>
        private GridContainerView pendingThrowSourceContainer;

        /// <summary> 待丢弃原锚点 </summary>
        private Vector2Int pendingThrowAnchorPos;

        /// <summary> 待丢弃原朝向 </summary>
        private bool pendingThrowIsRotated;

        /// <summary> 是否已初始化 </summary>
        private bool isInited;

        /// <summary>
        /// 取本地 StackMenu
        /// </summary>
        public void InitComponents()
        {
            if (isInited)
                return;

            stackMenu = GetComponentInChildren<StackMenu>(true);
            stackMenu?.InitComponents();
            isInited = true;
        }

        /// <summary>
        /// 注入世界掉落实现
        /// </summary>
        public void BindSpawnWorldDrop(Func<ItemRtData, int, bool> spawnWorldDrop)
        {
            spawnWorldDropHandler = spawnWorldDrop;
        }

        /// <summary>
        /// 拖拽松手丢弃 堆叠大于 1 先弹数量菜单
        /// </summary>
        public bool TryThrowFromDrag(ItemView itemView, GridContainerView sourceContainer)
        {
            if (itemView == null || itemView.ItemData == null || sourceContainer == null)
                return false;

            int stackCount = itemView.ItemData.CurrStackCount;
            if (stackCount <= 1)
                return CommitThrowAmount(itemView, sourceContainer, stackCount, true);

            OpenThrowStackMenu(itemView, sourceContainer);
            return true;
        }

        /// <summary>
        /// 菜单拆分入口
        /// </summary>
        public bool TrySplitFromMenu(ItemView itemView)
        {
            if (itemView == null || itemView.ItemData == null)
                return false;

            int stackCount = itemView.ItemData.CurrStackCount;
            if (stackCount <= 1)
                return false;

            if (stackMenu == null)
                return false;

            int maxSplit = stackCount - 1;
            int defaultSplit = Mathf.Max(1, stackCount / 2);
            stackMenu.Show(
                StackMenu.EStackMenuMode.Split,
                1,
                maxSplit,
                defaultSplit,
                amount => CommitSplitAmount(itemView, amount),
                null);
            return true;
        }

        /// <summary>
        /// 关闭堆叠菜单并处理未完成的丢弃
        /// </summary>
        public void Hide()
        {
            if (stackMenu != null && stackMenu.IsOpen)
                stackMenu.Hide();

            if (pendingThrowItemView != null)
                OnThrowStackCancelled();
        }

        /// <summary>
        /// 打开丢弃数量菜单 拖拽物先藏起
        /// </summary>
        private void OpenThrowStackMenu(ItemView itemView, GridContainerView sourceContainer)
        {
            if (stackMenu == null)
            {
                CommitThrowAmount(itemView, sourceContainer, itemView.ItemData.CurrStackCount, true);
                return;
            }

            pendingThrowItemView = itemView;
            pendingThrowSourceContainer = sourceContainer;
            // EndDrag 即将 Clear 会话 这里先记下原位
            if (!sourceContainer.TryGetDragStartRestore(out pendingThrowAnchorPos, out pendingThrowIsRotated))
            {
                pendingThrowAnchorPos = itemView.ItemData.AnchorPos;
                pendingThrowIsRotated = itemView.ItemData.IsRotated;
            }

            itemView.gameObject.SetActive(false);

            int stackCount = itemView.ItemData.CurrStackCount;
            stackMenu.Show(
                StackMenu.EStackMenuMode.Throw,
                1,
                stackCount,
                stackCount,
                OnThrowStackConfirmed,
                OnThrowStackCancelled);
        }

        /// <summary>
        /// 丢弃菜单确认
        /// </summary>
        private void OnThrowStackConfirmed(int amount)
        {
            var itemView = pendingThrowItemView;
            var sourceContainer = pendingThrowSourceContainer;
            var anchorPos = pendingThrowAnchorPos;
            var isRotated = pendingThrowIsRotated;
            ClearPendingThrow();
            if (itemView == null || sourceContainer == null)
                return;

            CommitThrowAmount(itemView, sourceContainer, amount, true, anchorPos, isRotated);
        }

        /// <summary>
        /// 丢弃菜单取消 物品塞回原格
        /// </summary>
        private void OnThrowStackCancelled()
        {
            var itemView = pendingThrowItemView;
            var sourceContainer = pendingThrowSourceContainer;
            var anchorPos = pendingThrowAnchorPos;
            var isRotated = pendingThrowIsRotated;
            ClearPendingThrow();
            if (itemView == null || sourceContainer == null)
                return;

            if (!sourceContainer.TryReinsertDraggingItemView(itemView, anchorPos, isRotated))
            {
                Debug.LogWarning("取消丢弃后无法放回背包 物品已销毁");
                sourceContainer.DestroyDraggingItemView(itemView);
            }
        }

        /// <summary>
        /// 按数量执行丢弃
        /// </summary>
        private bool CommitThrowAmount(ItemView itemView,
                                       GridContainerView sourceContainer,
                                       int amount,
                                       bool destroyOrReinsert)
        {
            Vector2Int anchorPos = itemView != null && itemView.ItemData != null
                ? itemView.ItemData.AnchorPos
                : Vector2Int.zero;
            bool isRotated = itemView != null && itemView.ItemData != null
                && itemView.ItemData.IsRotated;
            return CommitThrowAmount(
                itemView, sourceContainer, amount, destroyOrReinsert, anchorPos, isRotated);
        }

        /// <summary>
        /// 按数量执行丢弃 可指定回格锚点与朝向
        /// </summary>
        private bool CommitThrowAmount(ItemView itemView,
                                       GridContainerView sourceContainer,
                                       int amount,
                                       bool destroyOrReinsert,
                                       Vector2Int reinsertAnchorPos,
                                       bool reinsertIsRotated)
        {
            if (itemView == null || itemView.ItemData == null || sourceContainer == null)
                return false;

            int stackCount = itemView.ItemData.CurrStackCount;
            int throwCount = Mathf.Clamp(amount, 1, stackCount);
            if (spawnWorldDropHandler == null
                || !spawnWorldDropHandler(itemView.ItemData, throwCount))
            {
                if (destroyOrReinsert)
                    sourceContainer.TryReinsertDraggingItemView(
                        itemView, reinsertAnchorPos, reinsertIsRotated);
                return false;
            }

            if (throwCount >= stackCount)
            {
                sourceContainer.DestroyDraggingItemView(itemView);
                return true;
            }

            itemView.ItemData.CurrStackCount = stackCount - throwCount;
            return sourceContainer.TryReinsertDraggingItemView(
                itemView, reinsertAnchorPos, reinsertIsRotated);
        }

        /// <summary>
        /// 按数量执行拆分
        /// </summary>
        private void CommitSplitAmount(ItemView itemView, int amount)
        {
            if (itemView == null || itemView.OwnerContainer == null)
                return;

            if (!itemView.OwnerContainer.TrySplitItemView(itemView, amount, out _))
                Debug.LogWarning("拆分失败 可能没有空位");
        }

        /// <summary>
        /// 清空待丢弃缓存
        /// </summary>
        private void ClearPendingThrow()
        {
            pendingThrowItemView = null;
            pendingThrowSourceContainer = null;
            pendingThrowAnchorPos = Vector2Int.zero;
            pendingThrowIsRotated = false;
        }
    }
}
