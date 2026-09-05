using DBGameSystem;
using Cysharp.Threading.Tasks;
using Interaction.Combat;
using Interaction.Player;
using MmInventory;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 可放置可破坏模版交互 落地后左键扣耐久 家具默认不能 F 打开
    /// </summary>
    [RequireComponent(typeof(InteractOutline))]
    [RequireComponent(typeof(DamageableBehaviour))]
    public class PlaceAndBreakInteractBehaviour : InteractableBase, IPlaceAndBreakInterface
    {
        /// <summary> 破坏掉落物生成高度 </summary>
        private const float BreakDropHeightOffset = 0.5f;

        [SerializeField]
        private int itemTableId;

        [SerializeField]
        private bool useItemTableDurability = true;

        /// <summary> 外圈高亮 </summary>
        protected InteractOutline outline;
        /// <summary> 可破坏组件 </summary>
        protected DamageableBehaviour damageableBehaviour;

        public virtual int ItemTableId => itemTableId;

        protected virtual void Awake()
        {
            InitComponents();
            SyncDurabilityFromTable();
            BindDamageable();
        }

        protected virtual void OnDestroy()
        {
            UnbindDamageable();
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        protected virtual void InitComponents()
        {
            outline = GetComponent<InteractOutline>();
            damageableBehaviour = GetComponent<DamageableBehaviour>();
        }

        /// <summary>
        /// 依据物品表同步耐久
        /// </summary>
        protected virtual void SyncDurabilityFromTable()
        {
            if (!useItemTableDurability || ItemTableId <= 0 || damageableBehaviour == null)
                return;

            if (!LubanTables.TryGetItem(ItemTableId, out var itemTableData))
                return;

            if (itemTableData.MaxDurability <= 0)
                return;

            damageableBehaviour.SetDurability(itemTableData.MaxDurability, itemTableData.MaxDurability);
        }

        /// <summary>
        /// 绑定破坏回调
        /// </summary>
        protected virtual void BindDamageable()
        {
            if (damageableBehaviour == null)
                return;

            damageableBehaviour.Died -= OnDamageableDied;
            damageableBehaviour.Died += OnDamageableDied;
        }

        /// <summary>
        /// 解绑破坏回调
        /// </summary>
        protected virtual void UnbindDamageable()
        {
            if (damageableBehaviour == null)
                return;

            damageableBehaviour.Died -= OnDamageableDied;
        }

        /// <summary>
        /// 耐久归零时转发给子类
        /// </summary>
        protected virtual void OnDamageableDied(Vector3 hitPoint)
        {
            OnBroken(hitPoint);
        }

        /// <summary>
        /// 物体被打碎后掉表内破坏材料
        /// </summary>
        protected virtual void OnBroken(Vector3 hitPoint)
        {
            DropBreakMaterialsAsync(hitPoint).Forget();
        }

        /// <summary>
        /// 按家具或储物箱表掉破坏材料
        /// </summary>
        protected async UniTask DropBreakMaterialsAsync(Vector3 hitPoint)
        {
            if (ItemTableId <= 0)
                return;

            cfg.item.Furniture furniture = LubanTables.Tables.TbFurniture.GetOrDefault(ItemTableId);
            if (furniture == null)
                furniture = LubanTables.Tables.TbStorageBox.GetOrDefault(ItemTableId);
            if (furniture == null)
                return;

            var bagInteract = GameHub.Get<IUIBagInteract>();
            if (bagInteract == null)
            {
                Debug.LogWarning($"破坏掉落失败 IUIBagInteract 未注册 id={ItemTableId}", this);
                return;
            }

            var dropList = furniture.BreakDrops;
            for (int i = 0; i < dropList.Count; i++)
            {
                var drop = dropList[i];
                Vector3 dropPos = hitPoint + ResolveBurstOffset(i);
                await bagInteract.TrySpawnWorldItemAsync(drop.ItemId, drop.Count, dropPos);
            }
        }

        /// <summary>
        /// 爆出物相对偏移
        /// </summary>
        public static Vector3 ResolveBurstOffset(int index)
        {
            return Vector3.up * BreakDropHeightOffset;
        }

        public override void OnFocusEnter(InteractionContext ctx)
        {
            outline.Show();
        }

        public override void OnFocusExit(InteractionContext ctx)
        {
            outline.Hide();
        }

        public override string GetPromptText()
        {
            return string.Empty;
        }

        public override bool CanInteract(InteractionContext ctx)
        {
            return false;
        }
    }
}
