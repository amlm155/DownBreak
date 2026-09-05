using cfg.item;
using DBGameSystem;
using GAS.StateSystem;
using Interaction.Player;
using MmInventory;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// BagPanel 药品菜单与轮盘用药
    /// </summary>
    public partial class BagPanel
    {
        /// <summary>
        /// 菜单用药 播完后扣 1
        /// </summary>
        private bool OnUseMedicineFromMenu(ItemView itemView)
        {
            if (itemView == null || itemView.ItemData == null)
                return false;

            int itemTableId = itemView.ItemData.ExcelItemId;
            var medicineTable = LubanTables.Tables.TbMedicine.GetOrDefault(itemTableId);
            if (medicineTable == null)
            {
                Debug.LogWarning($"用药失败 非 Medicine id={itemTableId}");
                TipPanel.Push("无法使用");
                return false;
            }

            var anim = GameHub.Get<IPlayerBody>()?.Anim;
            if (anim == null)
            {
                Debug.LogWarning("用药失败 未拿到 IPlayerInteractAnim");
                TipPanel.Push("无法使用");
                return false;
            }

            if (anim.IsConsuming)
            {
                TipPanel.Push("正在使用中");
                return false;
            }

            bool started = anim.TryPlayMedicine(medicineTable, () =>
            {
                GameHub.Get<IPlayerStatus>()?.ApplyMedicineEffects(medicineTable);
                ConsumeOneStack(itemView);
            });
            if (started)
                CloseBagPanel();
            else
                TipPanel.Push("使用失败");

            return started;
        }

        /// <summary>
        /// 轮盘松手用药 按背包内实例扣堆叠
        /// </summary>
        public bool TryUseMedicineFromWheel(ItemRtData itemRtData)
        {
            if (itemRtData == null)
                return false;

            var medicineTable = LubanTables.Tables.TbMedicine.GetOrDefault(itemRtData.ExcelItemId);
            if (medicineTable == null)
            {
                TipPanel.Push("无法使用");
                return false;
            }

            var anim = GameHub.Get<IPlayerBody>()?.Anim;
            if (anim == null)
            {
                Debug.LogWarning("轮盘用药失败 未拿到 IPlayerInteractAnim");
                TipPanel.Push("无法使用");
                return false;
            }

            if (anim.IsConsuming)
            {
                TipPanel.Push("正在使用中");
                return false;
            }

            if (!TryFindItemViewInBags(itemRtData.InstancedItemId, out var itemView, out _))
            {
                Debug.LogWarning($"轮盘用药失败 背包内无实例 {itemRtData.InstancedItemId}");
                TipPanel.Push("背包中找不到该物品");
                return false;
            }

            if (!anim.TryPlayMedicine(medicineTable, () =>
            {
                GameHub.Get<IPlayerStatus>()?.ApplyMedicineEffects(medicineTable);
                ConsumeOneStack(itemView);
            }))
            {
                TipPanel.Push("使用失败");
                return false;
            }

            return true;
        }
    }
}
