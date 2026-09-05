using System.Collections.Generic;
using cfg.item;
using Interaction;
using MmInventory;
using UnityEngine;

namespace MieMieUIFrameWork.Runtime
{
    /// <summary>
    /// 主容器与搜刮栏Mgr 负责槽位注册 显隐 排序
    /// </summary>
    public class BagMainContainerMgr : MonoBehaviour
    {
        [SerializeField]
        private RectTransform contentRect;

        /// <summary> 手部容器 </summary>
        [SerializeField] 
        private GridContainerGroup handsGroup;

        /// <summary> 背包容器 </summary>
        [SerializeField]
        private GridContainerGroup bagGroup;

        /// <summary> 衣服容器 </summary>
        [SerializeField]
        private GridContainerGroup clothGroup;

        /// <summary> 裤子容器 </summary>
        [SerializeField]
        private GridContainerGroup pantsGroup;

        /// <summary> 头部容器 </summary>
        [SerializeField]
        private GridContainerGroup headGroup;

        /// <summary> 搜刮栏容器组 </summary>
        [SerializeField]
        private GridContainerGroup searchGroup;

        /// <summary> 槽位到容器组 </summary>
        private readonly Dictionary<EEquipSlot, GridContainerGroup> groupDict = new();

        /// <summary> 是否已初始化 </summary>
        private bool isInited;

        /// <summary> 当前搜刮栏绑定的世界容器 </summary>
        private Object currentSearchOwner;

        /// <summary>
        /// 由 BagPanel 就绪后调用一次
        /// </summary>
        public void InitComponents()
        {
            if (isInited)
                return;

            RegisterContainer(EEquipSlot.Hand, handsGroup);
            RegisterContainer(EEquipSlot.Bag, bagGroup);
            RegisterContainer(EEquipSlot.Torso, clothGroup);
            RegisterContainer(EEquipSlot.Legs, pantsGroup);
            RegisterContainer(EEquipSlot.Head, headGroup);
            InitSearchGridContainerGroup();

            isInited = true;
        }

        /// <summary>
        /// 按装备槽枚举重排 Content 下容器
        /// </summary>
        public void RefreshOrder()
        {
            if (contentRect == null)
                return;

            var orderedList = new List<GridContainerGroup>(
                contentRect.GetComponentsInChildren<GridContainerGroup>());
            orderedList.Sort((a, b) =>
                ((int)a.EquipSlot).CompareTo((int)b.EquipSlot));

            for (int i = 0; i < orderedList.Count; i++)
                orderedList[i].transform.SetSiblingIndex(i);
        }

        /// <summary>
        /// 显示并按装备数据适配容器
        /// </summary>
        public void AddContainer(EEquipSlot eSlot, Equipment equipment)
        {
            if (!groupDict.TryGetValue(eSlot, out var group))
            {
                Debug.LogWarning($"未注册装备槽容器 eSlot={eSlot}");
                return;
            }

            ItemRtData equippedItem = null;
            if (LubanTables.TryGetItem(equipment.Id, out var tableData))
                equippedItem = ItemRtData.ItemTableData2ItemRtData(tableData);

            group.WearEquipment(equippedItem, equipment);
            group.gameObject.SetActive(true);
            RefreshOrder();
        }

        /// <summary>
        /// 槽位容器是否已激活装备
        /// </summary>
        public bool HasContainer(EEquipSlot eSlot)
        {
            return groupDict.TryGetValue(eSlot, out var group)
                   && group != null
                   && group.gameObject.activeSelf;
        }

        /// <summary>
        /// 隐藏容器
        /// </summary>
        public void RemoveContainer(EEquipSlot eSlot)
        {
            if (!groupDict.TryGetValue(eSlot, out var group))
                return;

            group.ClearEquippedRecord();
            group.gameObject.SetActive(false);
            RefreshOrder();
        }

        /// <summary>
        /// 取槽位容器组
        /// </summary>
        public bool TryGetGroup(EEquipSlot eSlot, out GridContainerGroup group)
        {
            return groupDict.TryGetValue(eSlot, out group) && group != null;
        }

        /// <summary>
        /// 取槽位网格视图
        /// </summary>
        public GridContainerView GetGridView(EEquipSlot eSlot)
        {
            // 获取容器组
            if (!groupDict.TryGetValue(eSlot, out var group) || group == null)
                // 如果容器组不存在，则返回空
                return null;
            return group.GridView;
        }

        /// <summary>
        /// 设置搜刮容器外观与容量
        /// </summary>
        public void SetSearchContainer(int scrapContainerId)
        {
            // 先激活再适配 避免未激活时布局读不到真实高度
            searchGroup.gameObject.SetActive(true);
            searchGroup.AdaptContainerGroup(scrapContainerId);

            var searchView = searchGroup.GridView;
            if (searchView != null)
                GridMainContainerManager.SetActiveContainer(searchView);

            RefreshOrder();
        }

        /// <summary>
        /// 打开搜刮栏 首次投放揭幕 已搜过还原该容器自己的物品
        /// </summary>
        public bool OpenScrapContainer(int scrapContainerId, bool alreadyLooted, Object owner = null)
        {
            if (LubanTables.Tables.TbScrapContainer.GetOrDefault(scrapContainerId) == null)
            {
                Debug.LogWarning($"TbScrapContainer 无 id={scrapContainerId}");
                return false;
            }

            if (currentSearchOwner == owner && owner != null)
            {
                ResumeSearch();
                return true;
            }

            GetSearchRevealMask()?.HideImmediate();
            FlushCurrentSearchToOwner();
            SetSearchContainer(scrapContainerId);

            var gridView = searchGroup.GridView;
            if (gridView == null)
                return false;

            if (alreadyLooted)
            {
                RestoreOwnerItems(owner);
                currentSearchOwner = owner;
                return true;
            }

            var lootBinder = gridView.GetComponent<ContainerLootBinder>();
            if (lootBinder != null)
            {
                bool isOpened = lootBinder.PlayLootOnOpen(scrapContainerId, false);
                if (isOpened)
                    currentSearchOwner = owner;
                return isOpened;
            }

            var scrapContainer = LubanTables.Tables.TbScrapContainer.GetOrDefault(scrapContainerId);
            LootRuntime.TryFill(gridView, scrapContainer, true);
            currentSearchOwner = owner;
            return true;
        }

        /// <summary>
        /// 打开玩家储物箱栏 按容量建格并还原该箱子自己的物品
        /// </summary>
        public bool OpenStorageBox(int storageBoxItemId, Object owner = null)
        {
            LubanTables.EnsureLoaded();
            var storageBox = LubanTables.Tables.TbStorageBox.GetOrDefault(storageBoxItemId);
            if (storageBox == null)
            {
                Debug.LogWarning($"TbStorageBox 无 id={storageBoxItemId}");
                return false;
            }

            if (currentSearchOwner == owner && owner != null)
            {
                ResumeSearch();
                return true;
            }

            GetSearchRevealMask()?.HideImmediate();
            FlushCurrentSearchToOwner();
            searchGroup.gameObject.SetActive(true);
            searchGroup.AdaptContainerGroup(storageBox);

            var searchView = searchGroup.GridView;
            if (searchView == null)
                return false;

            RestoreOwnerItems(owner);
            GridMainContainerManager.SetActiveContainer(searchView);
            RefreshOrder();
            currentSearchOwner = owner;
            return true;
        }

        /// <summary>
        /// 取出指定世界容器内全部物品 开着则从搜刮栏取 关着则从容器自己的快照取
        /// </summary>
        public bool TryTakeOpenedContainerItems(Object owner, out List<ItemRtData> itemRtDataList)
        {
            itemRtDataList = null;
            if (owner == null)
                return false;

            if (currentSearchOwner == owner)
            {
                GetSearchRevealMask()?.HideImmediate();
                var searchView = searchGroup != null ? searchGroup.GridView : null;
                if (searchView == null)
                    return false;

                itemRtDataList = searchView.TakeAllItemsOut();
                if (owner is IWorldContainerContents openedContents)
                    openedContents.ReplaceStoredItems(new List<ItemSaveData>());

                searchGroup.gameObject.SetActive(false);
                GridMainContainerManager.ClearActiveContainer();
                currentSearchOwner = null;
                return itemRtDataList != null && itemRtDataList.Count > 0;
            }

            if (owner is not IWorldContainerContents contents)
                return false;

            itemRtDataList = ToItemRtDataList(contents.TakeStoredItems());
            return itemRtDataList != null && itemRtDataList.Count > 0;
        }

        /// <summary>
        /// 暂停当前搜刮 保留物品与揭示进度
        /// </summary>
        public void PauseSearch()
        {
            if (currentSearchOwner == null)
                return;

            GetSearchRevealMask()?.PauseReveal();
            GridMainContainerManager.ClearActiveContainer();
        }

        /// <summary>
        /// 恢复当前搜刮并显示活跃容器
        /// </summary>
        public void ResumeSearch()
        {
            if (currentSearchOwner == null || searchGroup == null)
                return;

            searchGroup.gameObject.SetActive(true);
            var searchView = searchGroup.GridView;
            if (searchView != null)
                GridMainContainerManager.SetActiveContainer(searchView);

            GetSearchRevealMask()?.ResumeReveal();
            RefreshOrder();
        }

        /// <summary>
        /// 关闭搜刮栏并把当前格子写回对应世界容器
        /// </summary>
        public void HideSearchAndClearActive()
        {
            GetSearchRevealMask()?.HideImmediate();
            FlushCurrentSearchToOwner();
            if (searchGroup != null)
                searchGroup.gameObject.SetActive(false);
            GridMainContainerManager.ClearActiveContainer();
            currentSearchOwner = null;
        }

        /// <summary>
        /// 把当前搜刮栏物品写回正在打开的世界容器
        /// </summary>
        private void FlushCurrentSearchToOwner()
        {
            if (currentSearchOwner == null || searchGroup == null)
                return;

            var searchView = searchGroup.GridView;
            if (searchView == null)
            {
                currentSearchOwner = null;
                return;
            }

            var itemRtDataList = searchView.TakeAllItemsOut();
            if (currentSearchOwner is IWorldContainerContents contents)
                contents.ReplaceStoredItems(ToItemSaveDataList(itemRtDataList));

            currentSearchOwner = null;
        }

        /// <summary>
        /// 把世界容器自己的快照灌回搜刮栏
        /// </summary>
        private void RestoreOwnerItems(Object owner)
        {
            var searchView = searchGroup != null ? searchGroup.GridView : null;
            if (searchView == null)
                return;

            searchView.ClearAllItems();
            if (owner is not IWorldContainerContents contents)
                return;

            var itemRtDataList = ToItemRtDataList(contents.PeekStoredItems());
            for (int i = 0; i < itemRtDataList.Count; i++)
            {
                var itemRtData = itemRtDataList[i];
                if (itemRtData == null)
                    continue;
                searchView.TryInsertExistingItemRtData(itemRtData);
            }
        }

        /// <summary>
        /// 运行时物品转存档快照
        /// </summary>
        private static List<ItemSaveData> ToItemSaveDataList(List<ItemRtData> itemRtDataList)
        {
            var itemSaveDataList = new List<ItemSaveData>();
            if (itemRtDataList == null)
                return itemSaveDataList;

            for (int i = 0; i < itemRtDataList.Count; i++)
            {
                var itemRtData = itemRtDataList[i];
                if (itemRtData == null)
                    continue;
                itemSaveDataList.Add(ItemSaveData.ItemRtToItemSaveData(itemRtData));
            }

            return itemSaveDataList;
        }

        /// <summary>
        /// 存档快照转运行时物品
        /// </summary>
        private static List<ItemRtData> ToItemRtDataList(IReadOnlyList<ItemSaveData> itemSaveDataList)
        {
            var itemRtDataList = new List<ItemRtData>();
            if (itemSaveDataList == null)
                return itemRtDataList;

            for (int i = 0; i < itemSaveDataList.Count; i++)
            {
                var itemSaveData = itemSaveDataList[i];
                if (itemSaveData == null || itemSaveData.excelItemId <= 0)
                    continue;
                itemRtDataList.Add(ItemRtData.ItemSaveData2ItemRtData(itemSaveData));
            }

            return itemRtDataList;
        }

        /// <summary>
        /// 初始化搜刮栏角色
        /// </summary>
        private void InitSearchGridContainerGroup()
        {
            if (searchGroup == null)
                return;

            var searchView = searchGroup.GridView;
            if (searchView == null)
                return;

            searchView.SetEnablePersistence(false);
            searchView.SetContainerRole(EGridContainerRole.Active);
        }

        /// <summary>
        /// 获取搜刮栏揭示组件
        /// </summary>
        private ContainerLootRevealMask GetSearchRevealMask()
        {
            return searchGroup != null && searchGroup.GridView != null
                ? searchGroup.GridView.GetComponent<ContainerLootRevealMask>()
                : null;
        }

        /// <summary>
        /// 注册容器组
        /// </summary>
        private void RegisterContainer(EEquipSlot eSlot, GridContainerGroup group)
        {
            group.EquipSlot = eSlot;
            var gridView = group.GridView;
            if (gridView != null)
            {
                // 玩家侧参与快捷互转 优先级跟枚举一致
                gridView.SetContainerRole(EGridContainerRole.Persistent);
                gridView.SetQuickTransferOrder((int)eSlot);
            }

            group.gameObject.SetActive(false);
            groupDict[eSlot] = group;
        }
    }
}
