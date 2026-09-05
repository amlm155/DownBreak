using cfg.item;
using DBGameSystem;
using GAS.StateSystem;
using Interaction.Player;
using MmInventory;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// BagPanel 食用菜单与轮盘食用
    /// </summary>
    public partial class BagPanel
    {
        /// <summary>
        /// 菜单食用
        /// </summary>
        private bool OnEatFromMenu(ItemView itemView)
        {
            if (itemView == null || itemView.ItemData == null)
                return false;

            int itemTableId = itemView.ItemData.ExcelItemId;
            var foodTable = LubanTables.Tables.TbFoodOrWater.GetOrDefault(itemTableId);
            if (foodTable == null)
            {
                Debug.LogWarning($"食用失败 非 FoodOrWater id={itemTableId}");
                TipPanel.Push("无法食用");
                return false;
            }

            var anim = GameHub.Get<IPlayerBody>()?.Anim;
            if (anim == null)
            {
                Debug.LogWarning("食用失败 未拿到 IPlayerInteractAnim");
                TipPanel.Push("无法食用");
                return false;
            }

            if (anim.IsConsuming)
            {
                TipPanel.Push("正在使用中");
                return false;
            }

            bool started = anim.TryPlayEat(foodTable, () =>
            {
                GameHub.Get<IPlayerStatus>()?.ApplyFoodOrWaterEffects(foodTable);
                ConsumeOneStack(itemView);
            });
            if (started)
                CloseBagPanel();
            else
                TipPanel.Push("食用失败");

            return started;
        }

        /// <summary>
        /// 轮盘松手食用 按背包内实例扣堆叠
        /// </summary>
        public bool TryEatFoodFromWheel(ItemRtData itemRtData)
        {
            if (itemRtData == null)
                return false;

            var foodTable = LubanTables.Tables.TbFoodOrWater.GetOrDefault(itemRtData.ExcelItemId);
            if (foodTable == null)
            {
                TipPanel.Push("无法食用");
                return false;
            }

            var anim = GameHub.Get<IPlayerBody>()?.Anim;
            if (anim == null)
            {
                Debug.LogWarning("轮盘食用失败 未拿到 IPlayerInteractAnim");
                TipPanel.Push("无法食用");
                return false;
            }

            if (anim.IsConsuming)
            {
                TipPanel.Push("正在使用中");
                return false;
            }

            if (!TryFindItemViewInBags(itemRtData.InstancedItemId, out var itemView, out _))
            {
                Debug.LogWarning($"轮盘食用失败 背包内无实例 {itemRtData.InstancedItemId}");
                TipPanel.Push("背包中找不到该物品");
                return false;
            }

            if (!anim.TryPlayEat(foodTable, () =>
            {
                GameHub.Get<IPlayerStatus>()?.ApplyFoodOrWaterEffects(foodTable);
                ConsumeOneStack(itemView);
            }))
            {
                TipPanel.Push("食用失败");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 消耗后扣 1 堆叠 刷显示 空则清轮盘并销毁
        /// </summary>
        private void ConsumeOneStack(ItemView itemView)
        {
            if (itemView == null || itemView.ItemData == null)
                return;

            var container = itemView.OwnerContainer;
            if (container == null)
                return;

            if (itemView.ItemData.CurrStackCount > 1)
            {
                itemView.ItemData.CurrStackCount -= 1;
                itemView.RefreshStackView();
                ItemWheelSlotStore.NotifySlotsChanged();
                return;
            }

            ItemWheelSlotStore.ClearInstance(itemView.ItemData.InstancedItemId);
            container.DestroyItemUI(itemView);
            TipPanel.Push("你感到舒爽");
        }
    }
}
