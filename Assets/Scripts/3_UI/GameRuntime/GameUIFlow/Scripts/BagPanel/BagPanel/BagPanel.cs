/// <summary>
/// BagPanel Logic层 - 生命周期与投放区绑定
/// </summary>

using System;
using cfg.item;
using DBWeaponSystem;
using MmUIFrameWork.Core;
using MiMieEventBus;
using MmInventory;
using UnityEngine;
using MieMieFrameWork;
using DBGameSystem;
namespace MieMieUIFrameWork.Runtime
{

    public partial class BagPanel : UIWindowBase
    {
        internal BagPanelGen View { get; private set; }

        /// <summary> 背包是否处于打开显示 </summary>
        public bool IsOpen => View != null && View.isOpen;

        /// <summary> 当前手持武器运行时数据 </summary>
        private ItemRtData equippedWeaponData;

        /// <summary> 模型区 TP 预览 </summary>
        private BagModelPreview modelPreview;

        /// <summary> 主容器与搜刮栏宿主 </summary>
        private BagMainContainerMgr containerHost;

        /// <summary> 堆叠菜单流程 </summary>
        private BagStackFlow stackFlow;

        /// <summary> 物品右键菜单 </summary>
        private ItemMenuPanel itemMenu;

        /// <summary> 手持变化订阅 </summary>
        private IDisposable weaponEquippedDisposable;

        /// <summary> 武器摧毁订阅 </summary>
        private IDisposable weaponBrokenDisposable;

    #region 生命周期

        /// <summary>
        /// 绑定 View 并注册预挂容器
        /// </summary>
        protected override void OnAwake()
        {
            base.OnAwake();
            View = UIContent.GetComponent<BagPanelGen>();
            if (View.ItemInfoMenuItemInfoMenu == null)
                View.ItemInfoMenuItemInfoMenu = UIContent.GetComponentInChildren<ItemInfoMenu>(true);
            View.ItemInfoMenuItemInfoMenu?.InitComponents();

            // 注册背包操作接口到 GameHub
            RegisterBagInteractService();
            // 绑定容器宿主
            BindContainerHost();
            // 绑定堆叠流程
            BindStackFlow();
            // 绑定投放区与菜单丢弃
            BindDropZones();
            // 绑定模型预览
            BindModelPreview();
            // 同步手持 含耐久耗尽摧毁
            BindWeaponEquippedSync();
        }

        protected override void OnShow()
        {
            base.OnShow();
            View.isOpen = true;
            View.ItemInfoMenuItemInfoMenu?.gameObject.SetActive(true);

            SyncEquippedWeaponFromSystem();
            RefreshWeaponIcon();
            containerHost?.RefreshOrder();
            containerHost?.ResumeSearch();
            View.ModelRectModelHotspot?.SetPanelOpen(true);
            modelPreview?.BeginPreview();
            UIHub.Instance.HideWindow<PlayerPanel>();
            CursorController.Unlock();
        }

        protected override void OnHide()
        {
            base.OnHide();
            View.isOpen = false;
            View.ModelRectModelHotspot?.SetPanelOpen(false);
            CursorController.Lock();

            modelPreview?.EndPreview();
            stackFlow?.Hide();
            HideItemMenu();
            View.ItemInfoMenuItemInfoMenu?.Hide();
            containerHost?.PauseSearch();
        }

        /// <summary>
        /// 关闭背包并回到 PlayerPanel
        /// </summary>
        public void CloseBagPanel()
        {
            UIHub.Instance.HideWindow<BagPanel>();
            UIHub.Instance.ShowWindow<PlayerPanel>();
        }

        protected override void OnDestroy()
        {
            containerHost?.HideSearchAndClearActive();
            UnbindWeaponEquippedSync();
            UnbindFurniturePlaceSession();
            UnregisterBagInteractService();
            base.OnDestroy();
            if (modelPreview != null)
            {
                modelPreview.Unbind();
                modelPreview.Release();
                modelPreview = null;
            }
            UnbindDropZones();
            CursorController.Lock();
            View.isOpen = false;
            View = null;
            GridMainContainerManager.ClearActiveContainer();
        }

    #endregion

    #region 初始化收编

        /// <summary>
        /// 订阅手持变化 耐久摧毁时清本地缓存
        /// </summary>
        private void BindWeaponEquippedSync()
        {
            weaponEquippedDisposable = MmGlobalEventBus.GlobalBus.Subscribe(
                WeaponHudEvents.EquippedChanged,
                OnWeaponEquippedChanged);
            weaponBrokenDisposable = MmGlobalEventBus.GlobalBus.Subscribe(
                WeaponHudEvents.Broken,
                OnWeaponBroken);
            SyncEquippedWeaponFromSystem();
        }

        /// <summary>
        /// 取消手持变化订阅
        /// </summary>
        private void UnbindWeaponEquippedSync()
        {
            weaponEquippedDisposable?.Dispose();
            weaponEquippedDisposable = null;
            weaponBrokenDisposable?.Dispose();
            weaponBrokenDisposable = null;
        }

        /// <summary>
        /// 手持变化回调
        /// </summary>
        private void OnWeaponEquippedChanged(ItemRtData itemRtData)
        {
            equippedWeaponData = itemRtData;
            RefreshWeaponIcon();
        }

        /// <summary>
        /// 耐久耗尽 清背包内残留同实例 避免装备回写造成的复制
        /// </summary>
        private void OnWeaponBroken(string instancedItemId)
        {
            if (string.IsNullOrEmpty(instancedItemId))
                return;

            if (equippedWeaponData != null
                && equippedWeaponData.InstancedItemId == instancedItemId)
            {
                equippedWeaponData = null;
                RefreshWeaponIcon();
            }

            if (!TryFindItemViewInBags(instancedItemId, out var itemView, out var container))
                return;

            container.DestroyItemUI(itemView);
        }

        /// <summary>
        /// 从 WeaponSystem 拉一次手持
        /// </summary>
        private void SyncEquippedWeaponFromSystem()
        {
            if (GameHub.Get<IWeaponSystem>() == null)
            {
                equippedWeaponData = null;
                RefreshWeaponIcon();
                return;
            }

            equippedWeaponData = GameHub.Get<IWeaponSystem>().EquippedItemRtData;
            RefreshWeaponIcon();
        }

        /// <summary>
        /// 刷新模型区武器图标
        /// </summary>
        private void RefreshWeaponIcon()
        {
            if (View?.ModelRectModelHotspot == null)
                return;

            View.ModelRectModelHotspot.SetWeaponIcon(equippedWeaponData);
        }

        /// <summary>
        /// 绑定主容器宿主
        /// </summary>
        private void BindContainerHost()
        {
            containerHost = View.MainContainerPartBagMainContainerHost;
            containerHost.InitComponents();
        }

        /// <summary>
        /// 绑定堆叠流程
        /// </summary>
        private void BindStackFlow()
        {
            stackFlow = UIContent.GetComponentInChildren<BagStackFlow>(true);
            stackFlow?.InitComponents();
            stackFlow?.BindSpawnWorldDrop((itemRtData, stackCount) => SpawnWorldDrop(itemRtData, stackCount));
        }

        /// <summary>
        /// 绑定模型区预览组件
        /// </summary>
        private void BindModelPreview()
        {
            var modelHotspot = View.ModelRectModelHotspot;
            if (modelHotspot == null)
                return;

            modelPreview = modelHotspot.GetComponentInChildren<BagModelPreview>(true);
            modelPreview?.InitComponents();
        }

    #endregion

    #region 投放区绑定

        /// <summary>
        /// 注册投放区与菜单动作回调
        /// </summary>
        private void BindDropZones()
        {
            var throwRect = View.ThrowRectThrowRect;
            throwRect.BindView();
            var modelHotspot = View.ModelRectModelHotspot;
            modelHotspot.InitComponents();
            modelHotspot.OnDoubleClickUnequip = TryUnequipFromHotspot;
            var canvas = modelHotspot.GetComponentInParent<Canvas>();

            GridMainContainerManager.RegisterDropZone(
                EDropZoneKind.Throw,
                throwRect.ZoneRect,
                canvas,
                throwRect.SetHighlight,
                null,
                OnThrowCommitFromDrag);

            GridMainContainerManager.RegisterDropZone(
                EDropZoneKind.Equip,
                    modelHotspot.EquipZoneRect,
                canvas,
                modelHotspot.SetHighlight,
                modelHotspot.CanAcceptAtPointer,
                OnEquipCommitFromDrag);

            BindItemMenuHandlers();
        }

        /// <summary>
        /// 注入物品菜单动作到 ItemMenuPanel
        /// </summary>
        private void BindItemMenuHandlers()
        {
            itemMenu = UIContent != null
                ? UIContent.GetComponentInChildren<ItemMenuPanel>(true)
                : null;
            if (itemMenu == null)
                return;

            itemMenu.InitComponents();
            itemMenu.ThrowHandler = OnThrowFromMenu;
            itemMenu.EquipHandler = OnEquipFromMenu;
            itemMenu.SplitHandler = OnSplitFromMenu;
            itemMenu.EatHandler = OnEatFromMenu;
            itemMenu.UseMedicineHandler = OnUseMedicineFromMenu;
            itemMenu.PlaceHandler = OnPlaceFromMenu;
        }

        /// <summary>
        /// 注销投放区与菜单动作回调
        /// </summary>
        private void UnbindDropZones()
        {
            if (View == null)
                return;

            if (View.ThrowRectThrowRect != null)
                GridMainContainerManager.UnregisterDropZone(
                    EDropZoneKind.Throw,
                    View.ThrowRectThrowRect.ZoneRect);

            if (View.ModelRectModelHotspot != null)
            {
                View.ModelRectModelHotspot.OnDoubleClickUnequip = null;
                GridMainContainerManager.UnregisterDropZone(
                    EDropZoneKind.Equip,
                    View.ModelRectModelHotspot.EquipZoneRect);
            }

            UnbindItemMenuHandlers();
        }

        /// <summary>
        /// 清空物品菜单动作注入
        /// </summary>
        private void UnbindItemMenuHandlers()
        {
            if (itemMenu == null)
                return;

            if (itemMenu.ThrowHandler == OnThrowFromMenu)
                itemMenu.ThrowHandler = null;
            if (itemMenu.EquipHandler == OnEquipFromMenu)
                itemMenu.EquipHandler = null;
            if (itemMenu.SplitHandler == OnSplitFromMenu)
                itemMenu.SplitHandler = null;
            if (itemMenu.EatHandler == OnEatFromMenu)
                itemMenu.EatHandler = null;
            if (itemMenu.UseMedicineHandler == OnUseMedicineFromMenu)
                itemMenu.UseMedicineHandler = null;
            if (itemMenu.PlaceHandler == OnPlaceFromMenu)
                itemMenu.PlaceHandler = null;

            itemMenu = null;
        }

    #endregion

    #region 拖拽与菜单转发

        /// <summary>
        /// 拖拽丢弃转发堆叠流程
        /// </summary>
        private bool OnThrowCommitFromDrag(ItemView itemView, GridContainerView sourceContainer)
        {
            return stackFlow != null && stackFlow.TryThrowFromDrag(itemView, sourceContainer);
        }

        /// <summary>
        /// 菜单拆分转发堆叠流程
        /// </summary>
        private bool OnSplitFromMenu(ItemView itemView)
        {
            return stackFlow != null && stackFlow.TrySplitFromMenu(itemView);
        }

    #endregion
    }

}
