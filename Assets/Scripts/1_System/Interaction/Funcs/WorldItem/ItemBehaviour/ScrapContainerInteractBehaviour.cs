using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DBGameSystem;
using Interaction.Combat;
using Interaction.Player;
using MmInventory;
using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 场景搜刮容器 未搜过不可拆 搜过之后可砸 物品快照绑在箱子自己身上
    /// </summary>
    [RequireComponent(typeof(InteractOutline))]
    [RequireComponent(typeof(DamageableBehaviour))]
    public class ScrapContainerInteractBehaviour : InteractableBase, IScrapInterface, IWorldContainerContents
    {
        [SerializeField]
        private int scrapContainerId;

        [SerializeField]
        private bool alreadyLooted;

        /// <summary> 已揭示后的容器内存快照 </summary>
        [SerializeField]
        private List<ItemSaveData> storedItemSaveDataList = new();

        /// <summary> 外圈高亮 </summary>
        private InteractOutline outline;

        /// <summary> 可破坏组件 </summary>
        private DamageableBehaviour damageableBehaviour;

        public int ScrapContainerId => scrapContainerId;
        public bool IsAlreadyLooted => alreadyLooted;

        private void Awake()
        {
            InitComponents();
            BindDamageable();
            RefreshBreakableState();
        }

        private void OnDestroy()
        {
            UnbindDamageable();
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitComponents()
        {
            outline = GetComponent<InteractOutline>();
            damageableBehaviour = GetComponent<DamageableBehaviour>();
        }

        public override void OnFocusEnter(InteractionContext ctx)
        {
            outline.Show();
        }

        public override void OnFocusExit(InteractionContext ctx)
        {
            outline.Hide();
        }

        public void OnScrapOpened()
        {
            alreadyLooted = true;
            RefreshBreakableState();
        }

        /// <summary>
        /// 未搜过关伤害 搜过之后才能挨打
        /// </summary>
        private void RefreshBreakableState()
        {
            damageableBehaviour.SetCanTakeDamage(alreadyLooted);
        }

        /// <summary>
        /// 绑定打碎回调
        /// </summary>
        private void BindDamageable()
        {
            if (damageableBehaviour == null)
                return;

            damageableBehaviour.Died -= OnBroken;
            damageableBehaviour.Died += OnBroken;
        }

        /// <summary>
        /// 解绑打碎回调
        /// </summary>
        private void UnbindDamageable()
        {
            if (damageableBehaviour == null)
                return;

            damageableBehaviour.Died -= OnBroken;
        }

        /// <summary>
        /// 打碎后掉出箱内剩余物品和表内破坏材料
        /// </summary>
        private void OnBroken(Vector3 hitPoint)
        {
            var bagInteract = GameHub.Get<IUIBagInteract>();
            bagInteract?.TryDropOpenedContainerItems(this, hitPoint);

            var scrapContainer = LubanTables.Tables.TbScrapContainer.GetOrDefault(scrapContainerId);
            if (scrapContainer == null)
                return;

            var dropList = scrapContainer.BreakDrops;
            SpawnBreakMaterialsAsync(bagInteract, dropList, hitPoint).Forget();
        }

        /// <summary>
        /// 异步生成搜刮容器表内的破坏掉落
        /// </summary>
        private async UniTask SpawnBreakMaterialsAsync(
            IUIBagInteract bagInteract,
            List<cfg.item.ItemCount> dropList,
            Vector3 hitPoint)
        {
            if (bagInteract == null)
            {
                Debug.LogWarning($"破坏掉落失败 IUIBagInteract 未注册 id={scrapContainerId}", this);
                return;
            }

            for (int i = 0; i < dropList.Count; i++)
            {
                var drop = dropList[i];
                Vector3 dropPos = hitPoint + PlaceAndBreakInteractBehaviour.ResolveBurstOffset(i);
                await bagInteract.TrySpawnWorldItemAsync(drop.ItemId, drop.Count, dropPos);
            }
        }

        public override string GetPromptText()
        {
            return "搜索";
        }

        public override bool CanInteract(InteractionContext ctx)
        {
            return true;
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
    }
}
