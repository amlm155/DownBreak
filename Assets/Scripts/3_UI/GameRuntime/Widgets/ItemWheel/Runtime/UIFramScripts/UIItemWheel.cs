/// <summary>
/// UIItemWheel Logic层 - 长按 T 显示的物品轮盘
/// </summary>

using cfg.item;
using DG.Tweening;
using MieMieFrameWork.Asset;
using MmUIFrameWork.Core;
using MmInventory;
using UnityEngine;
namespace MieMieUIFrameWork.Runtime
{
    
    internal class UIItemWheel : UIWindowBase
    {
        /// <summary> 弹出起始缩放 </summary>
        private const float OpenFromScale = 0.72f;
    
        /// <summary> 弹出时长 </summary>
        private const float OpenDuration = 0.4f;
    
        /// <summary> 关闭外扩倍率 </summary>
        private const float CloseExpandScale = 1.2f;
    
        /// <summary> 关闭外扩时长 </summary>
        private const float CloseExpandDuration = 0.08f;
    
        /// <summary> 关闭缩小时长 </summary>
        private const float CloseShrinkDuration = 0.2f;
    
        internal UIItemWheelGen View { get; private set; }
    
        /// <summary> 轮盘主控 </summary>
        private ItemWheelController wheelController;
    
        /// <summary> 缩放动画根 </summary>
        private Transform animRoot;
    
        /// <summary> 正常显示缩放 </summary>
        private Vector3 shownScale = Vector3.one;
    
        /// <summary> 开合缩放补间 </summary>
        private Tween scaleTween;
    
        /// <summary> 是否已经正式弹出过 </summary>
        private bool hasShown;
    
        /// <summary> 是否正在播关闭动画 </summary>
        private bool isCloseAnimating;
    
        protected override void OnAwake()
        {
            base.OnAwake();
            View = UIContent.GetComponent<UIItemWheelGen>();
            wheelController = View != null ? View.ItemWheelItemWheelController : null;
            if (wheelController == null && UIContent != null)
                wheelController = UIContent.GetComponentInChildren<ItemWheelController>(true);
    
            animRoot = UIContent != null ? UIContent : null;
            if (animRoot != null)
                shownScale = animRoot.localScale;
        }
    
        protected override void OnShow()
        {
            KillScaleTween();
            isCloseAnimating = false;
            hasShown = true;
    
            if (animRoot != null)
                animRoot.localScale = shownScale * OpenFromScale;
    
            base.OnShow();
            CursorController.Unlock();
            RefreshWheelFromSlots();
            ItemWheelSlotStore.OnSlotsChanged += RefreshWheelFromSlots;
    
            PlayOpenScale();
        }
    
        protected override void OnHide()
        {
            // 关闭动画播放中忽略重复 Hide
            if (isCloseAnimating)
                return;
    
            // 先确认当前高亮扇区再清选中
            TryCommitHighlightedItem();
    
            if (wheelController != null)
                wheelController.ClearSelection();
    
            ItemWheelSlotStore.OnSlotsChanged -= RefreshWheelFromSlots;
    
            // 仅回到玩法时重锁 避免盖掉背包/暂停的解锁
            if (ShouldRelockCursor())
                CursorController.Lock();
    
            // 预热或未真正弹出过 直接关
            if (!hasShown || animRoot == null)
            {
                FinishHideInstant();
                return;
            }
    
            PlayCloseScale();
        }
    
        /// <summary>
        /// 弹出缩放
        /// </summary>
        private void PlayOpenScale()
        {
            if (animRoot == null)
                return;
    
            KillScaleTween();
            scaleTween = animRoot
                .DOScale(shownScale, OpenDuration)
                .SetEase(Ease.OutBack, 1.2f)
                .SetUpdate(true);
        }
    
        /// <summary>
        /// 关闭 先微微外扩再缩小消失
        /// </summary>
        private void PlayCloseScale()
        {
            isCloseAnimating = true;
    
            if (UICanvasGroup != null)
            {
                UICanvasGroup.blocksRaycasts = false;
                UICanvasGroup.interactable = false;
            }
    
            KillScaleTween();
            scaleTween = DOTween.Sequence()
                .SetUpdate(true)
                .Append(animRoot.DOScale(shownScale * CloseExpandScale, CloseExpandDuration)
                    .SetEase(Ease.OutQuad))
                .Append(animRoot.DOScale(Vector3.zero, CloseShrinkDuration)
                    .SetEase(Ease.InBack, 1.1f))
                .OnComplete(FinishHideAfterCloseAnim);
        }
    
        /// <summary>
        /// 关闭动画结束 真正隐藏
        /// </summary>
        private void FinishHideAfterCloseAnim()
        {
            scaleTween = null;
            isCloseAnimating = false;
            hasShown = false;
    
            if (animRoot != null)
                animRoot.localScale = shownScale;
    
            base.OnHide();
        }
    
        /// <summary>
        /// 无动画立刻隐藏
        /// </summary>
        private void FinishHideInstant()
        {
            KillScaleTween();
            isCloseAnimating = false;
            hasShown = false;
    
            if (animRoot != null)
                animRoot.localScale = shownScale;
    
            base.OnHide();
        }
    
        /// <summary>
        /// 杀掉开合缩放补间
        /// </summary>
        private void KillScaleTween()
        {
            if (scaleTween == null)
                return;
    
            scaleTween.Kill();
            scaleTween = null;
        }
    
        /// <summary>
        /// 背包或暂停开着时不重锁光标
        /// </summary>
        private static bool ShouldRelockCursor()
        {
            var bagPanel = UIHub.Instance.GetWindow<BagPanel>();
            if (bagPanel != null && bagPanel.IsOpen)
                return false;
    
            var stopPanel = UIHub.Instance.GetWindow<GameStopPanel>();
            if (stopPanel != null && stopPanel.UIIsShow)
                return false;
    
            return true;
        }
    
        protected override void OnDestroy()
        {
            KillScaleTween();
            ItemWheelSlotStore.OnSlotsChanged -= RefreshWheelFromSlots;
            base.OnDestroy();
        }
    
        /// <summary>
        /// 从槽位仓库刷轮盘数据
        /// </summary>
        private void RefreshWheelFromSlots()
        {
            if (wheelController == null)
                return;
    
            wheelController.SetItems(BuildWheelDataList());
        }
    
        /// <summary>
        /// 松手时按高亮扇区提交 武器切持 食物开吃
        /// </summary>
        private void TryCommitHighlightedItem()
        {
            if (wheelController == null)
                return;

            int sectorIndex = wheelController.CurrentItemIndex;
            if (sectorIndex < 0)
                return;

            var itemRtData = ItemWheelSlotStore.Get(sectorIndex);
            if (itemRtData == null)
                return;

            if (!LubanTables.TryGetItem(itemRtData.ExcelItemId, out var tableData))
                return;

            var bagPanel = UIHub.Instance.GetWindow<BagPanel>();
            if (bagPanel == null)
                return;

            if (tableData.ItemType == EItemType.Weapon)
            {
                bagPanel.TryEquipWeaponFromWheel(itemRtData);
                return;
            }

            if (tableData.ItemType == EItemType.FoodOrWater)
            {
                bagPanel.TryEatFoodFromWheel(itemRtData);
                return;
            }

            if (tableData.ItemType == EItemType.Medicine)
            {
                bagPanel.TryUseMedicineFromWheel(itemRtData);
            }
        }
    
        /// <summary>
        /// 槽位转轮盘展示数据
        /// </summary>
        public static ItemWheelData[] BuildWheelDataList()
        {
            /// 构建轮盘展示数据列表
            var dataList = new ItemWheelData[ItemWheelSlotStore.SlotCount];
            // 遍历槽位仓库 将槽位数据转换为轮盘展示数据
            for (int i = 0; i < ItemWheelSlotStore.SlotCount; i++)
            {
                var itemRtData = ItemWheelSlotStore.Get(i);
                if (itemRtData == null)
                    continue;
    
                // 获取表数据
                if (!LubanTables.TryGetItem(itemRtData.ExcelItemId, out var tableData))
                    continue;
    
                Sprite icon = null;
                // 加载图标
                if (!string.IsNullOrEmpty(tableData.IconPath))
                    icon = MmAssetMgr.LoadAsset<Sprite>(tableData.IconPath);
    
                dataList[i] = new ItemWheelData
                {
                    Icon = icon,
                    Info = tableData.Name,
                    ExcelItemId = itemRtData.ExcelItemId,
                    InstancedItemId = itemRtData.InstancedItemId,
                    ItemType = tableData.ItemType,
                    ItemRtData = itemRtData
                };
            }
    
            return dataList;
        }
    }
    
}