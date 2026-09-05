using DBGameSystem;
using DownBreak.CraftingRecipeSystem;
using DBWeaponSystem;
using cfg.item;
using MmInventory;
using UnityEngine;

namespace Interaction.Player
{
    /// <summary>
    /// 拾取与搜刮交互 从 GameHub 获取背包与玩家门面
    /// </summary>
    public class PickupInteractModule : IPlayerInteract
    {
        /// <summary> 背包操作 </summary>
        private IUIBagInteract bagInteract;

        /// <summary>
        /// 每帧检测交互键
        /// </summary>
        public void Tick()
        {
            if (BuildInteractModule.IsBuildModeActive())
                return;

            var input = GameHub.Get<IPlayerInput>();
            if (input == null || !input.IsInteractStarted)
                return;

            TryInteractFocus();
        }

        /// <summary>
        /// 对当前聚焦的目标进行搜刮或拾取
        /// </summary>
        public void TryInteractFocus()
        {
            var body = GameHub.Get<IPlayerBody>();
            if (body == null || body.InteractionManager == null)
                return;

            if (body.InteractionManager.CurrentFocus == null)
                return;

            IInteractableInterface currentInteractItem = body.InteractionManager.CurrentFocus;
            if (currentInteractItem is IPressInteractable)
            {
                if (currentInteractItem.CanInteract(default))
                    currentInteractItem.Interact(default);
                return;
            }

            if (currentInteractItem is IWorkbenchInterface workbench)
            {
                var craftingRecipe = GameHub.Get<ICraftingRecipe>();
                craftingRecipe.EnterWorkbench(workbench.WorkbenchLevel);
                return;
            }

            if (currentInteractItem is IPlaceAndBreakInterface)
            {
                if (currentInteractItem.CanInteract(default))
                    currentInteractItem.Interact(default);
                return;
            }

            if (currentInteractItem is IScrapInterface scrapSource)
            {
                bagInteract = GameHub.Get<IUIBagInteract>();
                if (bagInteract == null)
                    return;

                bool isOpened = bagInteract.TryOpenScrapContainer(
                    scrapSource.ScrapContainerId,
                    scrapSource.IsAlreadyLooted,
                    currentInteractItem as UnityEngine.Object);
                if (isOpened)
                    scrapSource.OnScrapOpened();
                return;
            }

            if (currentInteractItem is not IItemInterface itemSource)
                return;

            var weaponSystem = GameHub.Get<IWeaponSystem>();
            bool isWeapon = IsWeapon(itemSource.ItemTableID);
            if (isWeapon
                && weaponSystem != null
                && weaponSystem.EquippedItemTableId <= 0)
            {
                if (!TryEquipWorldWeapon(itemSource, weaponSystem))
                    return;

                body.Anim?.PlayPickupAnimation();
                itemSource.OnPickup();
                return;
            }

            // 已有武器时必须依赖背包暂存替换武器 没有背包则直接结束
            bagInteract = GameHub.Get<IUIBagInteract>();
            if (bagInteract == null)
                return;

            if (isWeapon && !bagInteract.HasContainer(EEquipSlot.Bag))
                return;

            body.Anim?.PlayPickupAnimation();
            bool isPickedUp = bagInteract.TryPickupWorldItem(itemSource);
            if (isPickedUp)
                itemSource.OnPickup();
        }

        /// <summary>
        /// 判断物品是否为武器
        /// </summary>
        private static bool IsWeapon(int itemTableId)
        {
            return LubanTables.TryGetItem(itemTableId, out var itemTableData)
                && itemTableData.ItemType == cfg.item.EItemType.Weapon;
        }

        /// <summary>
        /// 将世界武器转换为运行时数据并直接装备
        /// </summary>
        private static bool TryEquipWorldWeapon(
            IItemInterface itemSource,
            IWeaponSystem weaponSystem)
        {
            if (!TryCreateItemRuntimeData(itemSource, out var itemRtData))
                return false;

            return weaponSystem.TryEquipWeapon(itemRtData, out _);
        }

        /// <summary>
        /// 从世界物体读取实例快照或物品表数据
        /// </summary>
        private static bool TryCreateItemRuntimeData(
            IItemInterface itemSource,
            out ItemRtData itemRtData)
        {
            itemRtData = null;
            if (itemSource == null)
                return false;

            if (itemSource is IItemSaveCarrier carrier && carrier.HasSaveData)
            {
                if (carrier.SaveData == null || carrier.SaveData.excelItemId <= 0)
                    return false;

                itemRtData = ItemRtData.ItemSaveData2ItemRtData(carrier.SaveData);
                return true;
            }

            if (!LubanTables.TryGetItem(itemSource.ItemTableID, out var itemTableData))
                return false;

            itemRtData = ItemRtData.ItemTableData2ItemRtData(itemTableData);
            return true;
        }
    }
}
