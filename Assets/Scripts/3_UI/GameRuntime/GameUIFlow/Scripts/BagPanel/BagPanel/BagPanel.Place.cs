using System;
using cfg.item;
using DBGameSystem;
using MieMieFrameWork;
using MiMieEventBus;
using Mm_Budier;
using MmInventory;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// BagPanel 家具菜单放置 关背包进建造预览 放成功扣 1
    /// </summary>
    public partial class BagPanel
    {
        /// <summary> 正在放置的家具实例 ID </summary>
        private string pendingPlaceInstancedItemId;

        /// <summary> 放置成功订阅 </summary>
        private IDisposable cubePlacedDisposable;

        /// <summary> 取消放置订阅 </summary>
        private IDisposable placeCancelledDisposable;

        /// <summary>
        /// 菜单放置家具 关掉背包进入建造射线预览
        /// </summary>
        private bool OnPlaceFromMenu(ItemView itemView)
        {
            if (itemView == null || itemView.ItemData == null)
                return false;

            int itemTableId = itemView.ItemData.ExcelItemId;
            if (!LubanTables.TryGetItem(itemTableId, out var tableData)
                || tableData.ItemType != EItemType.Furniture)
            {
                TipPanel.Push("无法放置");
                return false;
            }

            var builder = GameHub.Get<IBuilder>();
            if (builder == null || !builder.TryEnterPlaceFromItem(itemTableId))
            {
                Debug.LogWarning($"放置失败 无放置预制体 itemId={itemTableId}");
                TipPanel.Push("无法放置");
                return false;
            }

            BindFurniturePlaceSession(itemView.ItemData.InstancedItemId);
            CloseBagPanel();
            return true;
        }

        /// <summary>
        /// 订阅本次放置会话 放成功扣堆叠
        /// </summary>
        private void BindFurniturePlaceSession(string instancedItemId)
        {
            UnbindFurniturePlaceSession();
            pendingPlaceInstancedItemId = instancedItemId;
            cubePlacedDisposable = MmGlobalEventBus.GlobalBus.Subscribe(
                BuilderEvents.CubePlaced,
                OnFurnitureCubePlaced);
            placeCancelledDisposable = MmGlobalEventBus.GlobalBus.Subscribe(
                BuilderEvents.PlaceCancelled,
                OnFurniturePlaceCancelled);
        }

        /// <summary>
        /// 结束放置会话
        /// </summary>
        private void UnbindFurniturePlaceSession()
        {
            cubePlacedDisposable?.Dispose();
            cubePlacedDisposable = null;
            placeCancelledDisposable?.Dispose();
            placeCancelledDisposable = null;
            pendingPlaceInstancedItemId = null;
        }

        /// <summary>
        /// 放置成功扣 1 没了就退出建造
        /// </summary>
        private void OnFurnitureCubePlaced(CubeInstance cubeInstance)
        {
            if (string.IsNullOrEmpty(pendingPlaceInstancedItemId))
                return;

            if (!TryFindItemViewInBags(pendingPlaceInstancedItemId, out var itemView, out _))
            {
                ExitFurniturePlaceMode();
                return;
            }

            ConsumeOneStack(itemView);
            if (!TryFindItemViewInBags(pendingPlaceInstancedItemId, out _, out _))
                ExitFurniturePlaceMode();
        }

        /// <summary>
        /// 右键取消放置 物品不扣
        /// </summary>
        private void OnFurniturePlaceCancelled()
        {
            UnbindFurniturePlaceSession();
        }

        /// <summary>
        /// 退出建造预览并清会话
        /// </summary>
        private void ExitFurniturePlaceMode()
        {
            GameHub.Get<IBuilder>()?.CancelPlace();
        }
    }
}
