using System;
using System.Collections.Generic;
using cfg.item;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 拖拽投放区种类
    /// </summary>
    public enum EDropZoneKind
    {
        /// <summary> 丢弃区 </summary>
        Throw = 0,
        /// <summary> 装备区 </summary>
        Equip = 1,
    }

    /// <summary>
    /// 场景内所有背包容器管理器
    /// </summary>
    public static class GridMainContainerManager
    {
        /// <summary> 所有容器 </summary>
        private static readonly List<GridContainerView> containerList = new();

        /// <summary> 玩家侧常驻容器 按互转优先级排序用 </summary>
        private static readonly List<GridContainerView> playerContainerList = new();

        /// <summary> 候选容器缓冲 </summary>
        private static readonly List<GridContainerView> quickTransferCandidateList = new();

        /// <summary> 常驻玩家背包 </summary>
        private static GridContainerView persistentContainer;

        /// <summary> 当前活跃容器 </summary>
        private static GridContainerView activeContainer;

        /// <summary> 丢弃区 </summary>
        private static readonly DropZone throwZone = new();

        /// <summary> 装备区 </summary>
        private static readonly DropZone equipZone = new();

        /// <summary> 所有容器 </summary>
        public static IReadOnlyList<GridContainerView> ContainerList => containerList;

        /// <summary> 常驻容器 </summary>
        public static IReadOnlyList<GridContainerView> PlayerContainerList => playerContainerList;

        /// <summary> 玩家侧常驻容器 </summary>
        public static GridContainerView PersistentContainer => persistentContainer;

        /// <summary> 当前活跃容器 </summary>
        public static GridContainerView ActiveContainer => activeContainer;

        /// <summary>
        /// 投放区运行时状态
        /// </summary>
        private sealed class DropZone
        {
            /// <summary> 检测矩形 </summary>
            public RectTransform Rect;

            /// <summary> 所属画布 </summary>
            public Canvas Canvas;

            /// <summary> 高亮回调 hovered canAccept </summary>
            public Action<bool, bool> HoverHandler;

            /// <summary> 可否接受 空则区内一律可提交 </summary>
            public Func<ItemView, bool> CanAccept;

            /// <summary> 松手提交 </summary>
            public Func<ItemView, GridContainerView, bool> CommitHandler;

            /// <summary> 当前是否悬停 </summary>
            public bool IsHovered;

            /// <summary> 当前是否可接受 </summary>
            public bool CanAcceptNow;
        }

#region 容器注册

        /// <summary>
        /// 注册背包容器
        /// </summary>
        public static void Register(GridContainerView container)
        {
            if (container is null || containerList.Contains(container))
                return;

            containerList.Add(container);

            if (container.ContainerRole == EGridContainerRole.Persistent)
            {
                if (!playerContainerList.Contains(container))
                    playerContainerList.Add(container);
                persistentContainer = container;
            }

            if (container.ContainerRole == EGridContainerRole.Active)
                activeContainer = container;
        }

        /// <summary>
        /// 注销背包容器
        /// </summary>
        public static void Unregister(GridContainerView container)
        {
            if (container is null)
                return;

            containerList.Remove(container);
            playerContainerList.Remove(container);

            if (persistentContainer == container)
                persistentContainer = null;

            if (activeContainer == container)
                activeContainer = null;
        }

        /// <summary>
        /// 设置当前活跃容器
        /// </summary>
        public static void SetActiveContainer(GridContainerView container)
        {
            activeContainer = container;
        }

        /// <summary>
        /// 清除当前活跃容器
        /// </summary>
        public static void ClearActiveContainer()
        {
            activeContainer = null;
        }

#endregion

#region 快捷互转

        /// <summary>
        /// 解析快捷互转目标容器 兼容旧单目标调用
        /// </summary>
        public static bool TryResolveQuickTransferTarget(GridContainerView sourceContainer,
                                                         out GridContainerView targetContainer)
        {
            targetContainer = null;
            if (!TryCollectQuickTransferTargets(sourceContainer, quickTransferCandidateList))
                return false;

            targetContainer = quickTransferCandidateList[0];
            return true;
        }

        /// <summary>
        /// 收集快捷互转候选 活跃侧按优先级枚举序 玩家侧指向活跃容器
        /// </summary>
        public static bool TryCollectQuickTransferTargets(GridContainerView sourceContainer,
                                                          List<GridContainerView> outCandidateList)
        {
            outCandidateList.Clear();
            if (sourceContainer is null)
                return false;

            bool sourceIsActive = sourceContainer.ContainerRole == EGridContainerRole.Active
                                  || sourceContainer == activeContainer;
            bool sourceIsPlayer = sourceContainer.ContainerRole == EGridContainerRole.Persistent
                                  || playerContainerList.Contains(sourceContainer);

            if (sourceIsActive)
            {
                CollectPlayerTargetsSorted(sourceContainer, outCandidateList);
            }
            else if (sourceIsPlayer)
            {
                if (IsTransferReady(activeContainer) && activeContainer != sourceContainer)
                    outCandidateList.Add(activeContainer);
            }
            else
            {
                return false;
            }

            return outCandidateList.Count > 0;
        }

        /// <summary>
        /// 收集玩家侧容器并按 QuickTransferOrder 升序
        /// </summary>
        private static void CollectPlayerTargetsSorted(GridContainerView sourceContainer,
                                                       List<GridContainerView> outCandidateList)
        {
            for (int i = 0; i < playerContainerList.Count; i++)
            {
                var container = playerContainerList[i];
                if (!IsTransferReady(container) || container == sourceContainer)
                    continue;

                outCandidateList.Add(container);
            }

            outCandidateList.Sort(CompareQuickTransferOrder);
        }

        /// <summary>
        /// 优先级越小越靠前
        /// </summary>
        private static int CompareQuickTransferOrder(GridContainerView a, GridContainerView b)
        {
            return a.QuickTransferOrder.CompareTo(b.QuickTransferOrder);
        }

        /// <summary>
        /// 容器是否可作互转目标
        /// </summary>
        private static bool IsTransferReady(GridContainerView container)
        {
            return container != null
                   && container.isActiveAndEnabled
                   && container.IsInventoryReady;
        }

#endregion

#region 容器悬停命中

        /// <summary>
        /// 尝试解析鼠标悬停的背包容器
        /// </summary>
        public static bool TryResolveHoverContainer(Vector2 screenPos,
                                                    out GridContainerView hoverContainer,
                                                    out Vector2Int gridPos,
                                                    out int gridIndex)
        {
            hoverContainer = null;
            gridPos = Vector2Int.zero;
            gridIndex = -1;

            // 遍历所有背包容器 谁响应谁就是悬停的背包容器
            for (int i = containerList.Count - 1; i >= 0; i--)
            {
                var container = containerList[i];
                if (container == null) continue;

                // 判断视窗
                if (!container.PointIsInViewprot(screenPos)) continue;

                // 判断是否是格子内
                if (!container.TryGetMouseInGridInfo(screenPos, out gridPos, out gridIndex)) continue;

                hoverContainer = container;
                return true;
            }
            return false;
        }

#endregion

#region 投放区

        /// <summary>
        /// 注册投放区
        /// </summary>
        public static void RegisterDropZone(EDropZoneKind eZoneKind,
                                            RectTransform zoneRect,
                                            Canvas canvas,
                                            Action<bool, bool> hoverHandler,
                                            Func<ItemView, bool> canAccept,
                                            Func<ItemView, GridContainerView, bool> commitHandler)
        {
            var zone = GetZone(eZoneKind);
            zone.Rect = zoneRect;
            zone.Canvas = canvas;
            zone.HoverHandler = hoverHandler;
            zone.CanAccept = canAccept;
            zone.CommitHandler = commitHandler;
            SetZoneHover(zone, false, false);
        }

        /// <summary>
        /// 注销投放区
        /// </summary>
        public static void UnregisterDropZone(EDropZoneKind eZoneKind, RectTransform zoneRect)
        {
            var zone = GetZone(eZoneKind);
            if (zone.Rect != zoneRect)
                return;

            SetZoneHover(zone, false, false);
            zone.Rect = null;
            zone.Canvas = null;
            zone.HoverHandler = null;
            zone.CanAccept = null;
            zone.CommitHandler = null;
        }

        /// <summary>
        /// 清除投放区高亮
        /// </summary>
        public static void ClearDropZoneHover(EDropZoneKind eZoneKind)
        {
            SetZoneHover(GetZone(eZoneKind), false, false);
        }

        /// <summary>
        /// 按屏幕坐标刷新投放区高亮
        /// </summary>
        public static bool UpdateDropZoneHover(EDropZoneKind eZoneKind,
                                               Vector2 screenPos,
                                               ItemView itemView)
        {
            var zone = GetZone(eZoneKind);
            bool isHovered = IsPointerInZone(zone, screenPos);
            bool canAccept = isHovered && ResolveCanAccept(zone, itemView);
            SetZoneHover(zone, isHovered, canAccept);
            return isHovered;
        }

        /// <summary>
        /// 松手时按最终坐标提交投放
        /// </summary>
        public static bool TryCommitDropZone(EDropZoneKind eZoneKind,
                                             Vector2 screenPos,
                                             ItemView itemView,
                                             GridContainerView sourceContainer)
        {
            var zone = GetZone(eZoneKind);
            bool isHovered = IsPointerInZone(zone, screenPos);
            bool canAccept = ResolveCanAccept(zone, itemView);
            if (!isHovered || !canAccept || zone.CommitHandler == null || itemView == null)
            {
                SetZoneHover(zone, false, false);
                return false;
            }

            bool isCommitted = zone.CommitHandler.Invoke(itemView, sourceContainer);
            SetZoneHover(zone, false, false);
            return isCommitted;
        }

        /// <summary>
        /// 取投放区实例
        /// </summary>
        private static DropZone GetZone(EDropZoneKind eZoneKind)
        {
            return eZoneKind == EDropZoneKind.Throw ? throwZone : equipZone;
        }

        /// <summary>
        /// 解析是否可接受
        /// </summary>
        private static bool ResolveCanAccept(DropZone zone, ItemView itemView)
        {
            if (zone.CanAccept == null)
                return true;

            return zone.CanAccept.Invoke(itemView);
        }

        /// <summary>
        /// 判断屏幕坐标是否位于投放区
        /// </summary>
        private static bool IsPointerInZone(DropZone zone, Vector2 screenPos)
        {
            Camera canvasCamera = zone.Canvas != null
                                  && zone.Canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? zone.Canvas.worldCamera
                : null;

            return zone.Rect != null
                   && zone.Rect.gameObject.activeInHierarchy
                   && RectTransformUtility.RectangleContainsScreenPoint(
                       zone.Rect,
                       screenPos,
                       canvasCamera);
        }

        /// <summary>
        /// 切换投放区高亮
        /// </summary>
        private static void SetZoneHover(DropZone zone, bool isHovered, bool canAccept)
        {
            if (!isHovered)
                canAccept = false;

            if (zone.IsHovered == isHovered && zone.CanAcceptNow == canAccept)
                return;

            zone.IsHovered = isHovered;
            zone.CanAcceptNow = canAccept;
            zone.HoverHandler?.Invoke(isHovered, canAccept);
        }

#endregion
    }
}
