using System.Collections.Generic;
using DBGameSystem;
using Interaction.Player;
using MmInventory;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 场景储物箱 落地 F 打开 左键走 SimpleDamageable 扣耐久
    /// </summary>
    [RequireComponent(typeof(InteractOutline))]
    public class StorageBoxInteractBehaviour : PlaceAndBreakInteractBehaviour, IWorldContainerContents
    {
        [SerializeField]
        private int storageBoxItemId = 6001;

        /// <summary> 箱子内存物品快照 </summary>
        [SerializeField]
        private List<ItemSaveData> storedItemSaveDataList = new();

        public override int ItemTableId => storageBoxItemId;

        public override bool CanInteract(InteractionContext ctx)
        {
            return true;
        }

        public override void Interact(InteractionContext ctx)
        {
            var bagInteract = GameHub.Get<IUIBagInteract>();
            if (bagInteract == null)
                return;
            bagInteract.TryOpenStorageBox(storageBoxItemId, this);
        }

        public override string GetPromptText()
        {
            return "打开";
        }

        public void ReplaceStoredItems(List<ItemSaveData> itemSaveDataList)
        {
            storedItemSaveDataList = itemSaveDataList ?? new List<ItemSaveData>();
        }

        public List<ItemSaveData> TakeStoredItems()
        {
            var itemSaveDataList = storedItemSaveDataList;
            storedItemSaveDataList = new List<ItemSaveData>();
            return itemSaveDataList;
        }

        public IReadOnlyList<ItemSaveData> PeekStoredItems()
        {
            return storedItemSaveDataList;
        }

        protected override void OnBroken(Vector3 hitPoint)
        {
            base.OnBroken(hitPoint);
            GameHub.Get<IUIBagInteract>()?.TryDropOpenedContainerItems(this, hitPoint);
        }
    }
}
