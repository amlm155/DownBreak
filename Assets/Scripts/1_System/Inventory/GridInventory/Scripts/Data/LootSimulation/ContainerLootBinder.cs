using cfg.loot;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 场景容器投放绑定 打开时按 TbScrapContainer 适配并投放
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridContainerView))]
    public class ContainerLootBinder : MonoBehaviour
    {
        /// <summary> 搜刮容器模板 ID </summary>
        [SerializeField]
        [LabelText("搜刮容器ID")]
        private int scrapContainerId;

        /// <summary> 已搜过标记 </summary>
        [SerializeField]
        [LabelText("已搜过")]
        private bool alreadyLooted;

        /// <summary> 搜刮揭幕 可空 </summary>
        [SerializeField]
        [LabelText("搜刮揭幕")]
        private ContainerLootRevealMask revealMask;

        /// <summary> 绑定的容器视图 </summary>
        private GridContainerView containerView;

        /// <summary> 是否已初始化组件引用 </summary>
        private bool isComponentsInitialized;

        public int ScrapContainerId => scrapContainerId;
        public bool AlreadyLooted => alreadyLooted;
        public GridContainerView ContainerView => containerView;

        private void Awake()
        {
            InitComponents();
        }

        /// <summary>
        /// 容器被打开时调用 首次会适配容量投放并播揭幕
        /// </summary>
        public bool OnContainerOpened()
        {
            if (containerView is null)
                return false;

            if (alreadyLooted)
            {
                revealMask?.HideImmediate();
                return true;
            }

            if (!TryApplyScrapTemplate(out var scrapContainer))
                return false;

            // 已有存档或已有物品视为搜过
            if (containerView.HasSaveFile || HasAnyItem())
            {
                alreadyLooted = true;
                revealMask?.HideImmediate();
                return true;
            }

            containerView.InitCoreGridMatchesView();
            var result = LootRuntime.TryFill(containerView, scrapContainer, false);
            alreadyLooted = true;

            if (containerView.EnablePersistence)
                containerView.TrySaveToDisk();

            TryPlayReveal(result, true);

            Debug.Log(
                $"[{name}] OnContainerOpened scrap={scrapContainerId} empty={result.WasEmptyRoll} " +
                $"candidates={result.CandidateCount} placed={result.PlacedCount} skipped={result.SkippedCount}");
            return true;
        }

        /// <summary>
        /// GM 强制清空并重投 然后走打开揭幕
        /// </summary>
        public LootRuntime.FillResult ForceRefill()
        {
            if (containerView is null)
                return new LootRuntime.FillResult(false, 0, 0, 0);

            if (!TryApplyScrapTemplate(out var scrapContainer))
                return new LootRuntime.FillResult(false, 0, 0, 0);

            alreadyLooted = false;
            revealMask?.HideImmediate();
            containerView.InitCoreGridMatchesView();
            var result = LootRuntime.TryFill(containerView, scrapContainer, true);
            alreadyLooted = true;

            if (containerView.EnablePersistence)
                containerView.TrySaveToDisk();

            TryPlayReveal(result, true);
            return result;
        }

        /// <summary>
        /// 重置已搜过标记 不清空物品
        /// </summary>
        public void ResetLootedFlag()
        {
            alreadyLooted = false;
        }

        /// <summary>
        /// 背包搜刮栏打开 容量外观由外部适配 此处负责投放与揭幕
        /// </summary>
        public bool PlayLootOnOpen(int worldScrapContainerId, bool worldAlreadyLooted)
        {
            scrapContainerId = worldScrapContainerId;
            if (containerView is null)
                return false;

            if (revealMask != null)
                revealMask.enabled = true;

            if (worldAlreadyLooted)
            {
                alreadyLooted = true;
                revealMask?.HideImmediate();
                return true;
            }

            var scrapContainer = LubanTables.Tables.TbScrapContainer.GetOrDefault(scrapContainerId);
            if (scrapContainer is null)
            {
                Debug.LogWarning($"[{name}] TbScrapContainer 无 id={scrapContainerId}", this);
                return false;
            }

            // 搜刮栏是共享槽 每次首次开箱强制清空再投
            alreadyLooted = false;
            revealMask?.HideImmediate();
            containerView.EnsureInventoryService();
            var result = LootRuntime.TryFill(containerView, scrapContainer, true);
            alreadyLooted = true;
            TryPlayReveal(result, true);

            Debug.Log(
                $"[{name}] PlayLootOnOpen scrap={scrapContainerId} empty={result.WasEmptyRoll} " +
                $"candidates={result.CandidateCount} placed={result.PlacedCount} skipped={result.SkippedCount}");
            return true;
        }

        /// <summary>
        /// 读取模板并重建格子容量
        /// </summary>
        private bool TryApplyScrapTemplate(out ScrapContainer scrapContainer)
        {
            scrapContainer = LubanTables.Tables.TbScrapContainer.GetOrDefault(scrapContainerId);
            if (scrapContainer is null)
            {
                Debug.LogWarning($"[{name}] TbScrapContainer 无 id={scrapContainerId}", this);
                return false;
            }

            containerView.RebuildFromCapacity(
                new Vector2Int(scrapContainer.Capacity.X, scrapContainer.Capacity.Y),
                containerView.GridCellSize);
            containerView.EnsureInventoryService();
            return containerView.IsInventoryReady;
        }

        /// <summary>
        /// 尝试播放揭幕
        /// </summary>
        private void TryPlayReveal(LootRuntime.FillResult result, bool forceReveal)
        {
            if (revealMask is null)
                return;

            if (!forceReveal)
            {
                revealMask.HideImmediate();
                return;
            }

            if (!result.WasEmptyRoll && result.CandidateCount <= 0 && result.PlacedCount <= 0)
            {
                revealMask.HideImmediate();
                return;
            }

            revealMask.PlayReveal();
        }

        /// <summary>
        /// 容器内是否已有物品
        /// </summary>
        private bool HasAnyItem()
        {
            var itemViewList = containerView.GetItemViewList();
            return itemViewList != null && itemViewList.Count > 0;
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitComponents()
        {
            if (isComponentsInitialized)
                return;

            if (containerView is null)
                containerView = GetComponent<GridContainerView>();
            if (revealMask is null)
                revealMask = GetComponent<ContainerLootRevealMask>();
            isComponentsInitialized = true;
        }
    }
}
