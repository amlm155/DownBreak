using System;
using cfg.item;
using DBGameSystem;
using DBWeaponSystem;
using MieMieFrameWork;
using MieMieFrameWork.Asset;
using MieMieFrameTools.Archive;
using MiMieEventBus;
using MmInventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace MieMieUIFrameWork.Runtime
{
    
    /// <summary>
    /// 模型区多热区表现 区内才显示热区 拖拽按子 Rect 高亮 双击卸装由 BagPanel 回调
    /// </summary>
    public class ModelHotspot : MonoBehaviour
    {
        /// <summary> 单热区运行时数据 </summary>
        private struct HotspotEntry
        {
            /// <summary> 热区矩形 </summary>
            public RectTransform Rect;
    
            /// <summary> 热区图片 </summary>
            public Image Image;
    
            /// <summary> 热区标签 </summary>
            public TextMeshProUGUI Label;
    
            /// <summary> 对应装备槽 </summary>
            public EEquipSlot Slot;
    
            /// <summary> 原始颜色 </summary>
            public Color OriginColor;
    
            /// <summary> 原始透明度 </summary>
            public float OriginAlpha;
    
            /// <summary> 标签原始颜色 </summary>
            public Color OriginLabelColor;
    
            /// <summary> 标签原始透明度 </summary>
            public float OriginLabelAlpha;
        }
    
        /// <summary> 头部热区 </summary>
        [SerializeField]
        private RectTransform headRect;

        /// <summary> 武器热区 </summary>
        [SerializeField]
        private RectTransform weaponRect;

        /// <summary> 武器图标 </summary>
        [SerializeField]
        private Image weaponIcon;

        /// <summary> 武器热区耐久图片 </summary>
        [SerializeField]
        private Image weaponDurabilityImage;

        /// <summary> 武器耐久事件订阅 </summary>
        private IDisposable weaponDurabilityDisposable;

        /// <summary> 背包热区 </summary>
        [SerializeField]
        private RectTransform bagRect;

        /// <summary> 衣服热区 </summary>
        [SerializeField]
        private RectTransform clothRect;

        /// <summary> 裤子热区 </summary>
        [SerializeField]
        private RectTransform pantsRect;

        /// <summary> 热区列表 </summary>
        private HotspotEntry[] hotspotList;

        /// <summary> 所属画布 </summary>
        private Canvas canvas;

        /// <summary> 可装备高亮色 </summary>
        private Color successHighlightColor = Color.green;

        /// <summary> 不可装备高亮色 </summary>
        private Color failHighlightColor = Color.red;

        /// <summary> 高亮透明度 </summary>
        private float highlightAlpha = 0.5f;

        /// <summary> 是否已初始化 </summary>
        private bool isInited;

        /// <summary> 指针是否在模型区内 </summary>
        private bool pointerInModel;

        /// <summary> 拖拽是否悬停在装备区内 </summary>
        private bool isHovering;

        /// <summary> 当前物品是否可装备 </summary>
        private bool canEquipNow;

        /// <summary> 当前高亮热区下标 </summary>
        private int activeHotspotIndex = -1;

        /// <summary> 双击热区卸装回调 </summary>
        public System.Action<EEquipSlot> OnDoubleClickUnequip;

        /// <summary> 装备检测矩形 整块 ModelRect </summary>
        public RectTransform ZoneRect => transform as RectTransform;

        /// <summary> 装备拖拽检测矩形 覆盖 ModelRect 与同级 WeaponRect </summary>
        public RectTransform EquipZoneRect => transform.parent as RectTransform ?? ZoneRect;

    #region 绑定与高亮

        /// <summary>
        /// 由 BagPanel 在面板就绪后调用一次
        /// </summary>
        public void InitComponents()
        {
            if (isInited)
                return;

            canvas = GetComponentInParent<Canvas>();

            if (weaponRect == null)
                weaponRect = transform.parent?.Find("WeaponRect") as RectTransform;
            if (weaponDurabilityImage == null && weaponRect != null)
                weaponDurabilityImage = weaponRect.GetComponent<Image>();
            if (weaponIcon == null && weaponRect != null)
                weaponIcon = weaponRect.Find("WeaponIcon")?.GetComponent<Image>();
            if (weaponIcon != null)
                weaponIcon.raycastTarget = false;
            ClearWeaponIcon();
            BindWeaponDurabilityEvent();

            // RawImage 与背景不拦截射线 热区自己接
            var previewRawImage = GetComponentInChildren<RawImage>(true);
            if (previewRawImage != null)
                previewRawImage.raycastTarget = false;

            var bkTransform = transform.Find("BkImage");
            if (bkTransform != null)
            {
                var bkImage = bkTransform.GetComponent<Image>();
                if (bkImage != null)
                    bkImage.raycastTarget = false;
            }

            hotspotList = new[]
            {
                CreateEntry(headRect, EEquipSlot.Head),
                CreateEntry(weaponRect, EEquipSlot.Hand),
                CreateEntry(bagRect, EEquipSlot.Bag),
                CreateEntry(clothRect, EEquipSlot.Torso),
                CreateEntry(pantsRect, EEquipSlot.Legs),
            };

            for (int i = 0; i < hotspotList.Length; i++)
                BindHotspotClick(hotspotList[i]);

            // 初始隐藏 等指针进入模型区再显示
            HideAllHotspots();
            isInited = true;
            enabled = false;
        }

        /// <summary>
        /// 绑定武器耐久变化并同步当前值
        /// </summary>
        private void BindWeaponDurabilityEvent()
        {
            weaponDurabilityDisposable = MmGlobalEventBus.GlobalBus.Subscribe(
                WeaponHudEvents.DurabilityChanged,
                OnWeaponDurabilityChanged);

            if (GameHub.Get<IWeaponSystem>() != null
                && GameHub.Get<IWeaponSystem>().TryGetDurability(out int current, out int max))
            {
                OnWeaponDurabilityChanged(current, max);
            }
        }

        /// <summary>
        /// 释放武器耐久事件订阅
        /// </summary>
        private void OnDestroy()
        {
            weaponDurabilityDisposable?.Dispose();
            weaponDurabilityDisposable = null;
        }

        /// <summary>
        /// 根据当前耐久刷新武器图标填充比例
        /// </summary>
        private void OnWeaponDurabilityChanged(int current, int max)
        {
            if (weaponDurabilityImage == null)
                return;

            weaponDurabilityImage.fillAmount = max > 0
                ? Mathf.Clamp01((float)current / max)
                : 1f;
        }

        /// <summary>
        /// 面板打开时启用热区检测
        /// </summary>
        public void SetPanelOpen(bool isOpen)
        {
            if (!isInited)
                return;

            enabled = isOpen;
            if (!isOpen)
            {
                pointerInModel = false;
                activeHotspotIndex = -1;
                isHovering = false;
                HideAllHotspots();
            }
        }

        /// <summary>
        /// 刷新当前武器图标
        /// </summary>
        public void SetWeaponIcon(ItemRtData itemRtData)
        {
            if (weaponIcon == null)
                return;
            if (itemRtData == null || !LubanTables.TryGetItem(itemRtData.ExcelItemId, out var itemTableData))
            {
                ClearWeaponIcon();
                return;
            }

            weaponIcon.sprite = MmAssetMgr.LoadAsset<Sprite>(itemTableData.IconPath);
            SetWeaponIconAlpha(weaponIcon.sprite != null ? 1f : 0f);
        }

        /// <summary>
        /// 清除当前武器图标
        /// </summary>
        public void ClearWeaponIcon()
        {
            if (weaponIcon == null)
                return;

            weaponIcon.sprite = null;
            SetWeaponIconAlpha(0f);
        }

        /// <summary>
        /// 设置武器图标透明度
        /// </summary>
        private void SetWeaponIconAlpha(float alpha)
        {
            Color iconColor = weaponIcon.color;
            iconColor.a = alpha;
            weaponIcon.color = iconColor;
        }

        /// <summary>
        /// 切换装备区高亮 区内按指针命中子热区
        /// </summary>
        public void SetHighlight(bool isHovered, bool canEquip)
        {
            isHovering = isHovered;
            canEquipNow = canEquip;
    
            if (!isHovered)
            {
                activeHotspotIndex = -1;
                if (pointerInModel)
                    ShowAllHotspotsIdle();
                else
                    HideAllHotspots();
                return;
            }
    
            // 进入悬停立刻刷一次 避免等下一帧 LateUpdate
            RefreshActiveHotspot(forceRefresh: true);
        }
    
        /// <summary>
        /// 当前指针下热区是否接受该物品 投放区 CanAccept 用
        /// </summary>
        public bool CanAcceptAtPointer(ItemView itemView)
        {
            if (!isInited || itemView == null)
                return false;
    
            var mouse = Mouse.current;
            if (mouse == null)
                return false;
    
            int hotspotIndex = ResolveHotspotIndex(mouse.position.ReadValue());
            if (hotspotIndex < 0)
                return false;
    
            return DoesItemMatchSlot(itemView, hotspotList[hotspotIndex].Slot);
        }
    
        private void LateUpdate()
        {
            if (!isInited)
                return;
    
            bool inModel = IsPointerInModel();
            if (inModel != pointerInModel)
            {
                pointerInModel = inModel;
                if (!inModel)
                {
                    activeHotspotIndex = -1;
                    HideAllHotspots();
                }
                else if (!isHovering)
                {
                    ShowAllHotspotsIdle();
                }   
            }
    
            if (isHovering)
                RefreshActiveHotspot(forceRefresh: false);
        }
    
    #endregion
    
    #region 双击卸装
    
        /// <summary>
        /// 热区点击 仅响应左键双击
        /// </summary>
        internal void HandleHotspotClick(EEquipSlot eSlot, PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;
    
            if (eventData.clickCount != 2)
                return;
    
            OnDoubleClickUnequip?.Invoke(eSlot);
        }
    
    #endregion
    
    #region 热区工具
    
        /// <summary>
        /// 按指针刷新当前热区高亮
        /// </summary>
        private void RefreshActiveHotspot(bool forceRefresh)
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;
    
            Vector2 screenPos = mouse.position.ReadValue();
            int hotspotIndex = ResolveHotspotIndex(screenPos);
            if (!forceRefresh && hotspotIndex == activeHotspotIndex)
                return;
    
            ShowAllHotspotsIdle();
            activeHotspotIndex = hotspotIndex;
            if (hotspotIndex < 0)
                return;
    
            Color targetColor = canEquipNow ? successHighlightColor : failHighlightColor;
            var entry = hotspotList[hotspotIndex];
            ColorTools.ImageToColor(entry.Image, targetColor, highlightAlpha);
        }
    
        /// <summary>
        /// 指针是否在模型 Rect 内
        /// </summary>
        private bool IsPointerInModel()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return false;
    
            Vector2 screenPos = mouse.position.ReadValue();
            return RectTransformUtility.RectangleContainsScreenPoint(
                ZoneRect,
                screenPos,
                GetCanvasCamera());
        }
    
        /// <summary>
        /// 解析指针下的热区下标
        /// </summary>
        private int ResolveHotspotIndex(Vector2 screenPos)
        {
            Camera canvasCamera = GetCanvasCamera();
            for (int i = 0; i < hotspotList.Length; i++)
            {
                var entry = hotspotList[i];
                if (entry.Rect == null || !entry.Rect.gameObject.activeInHierarchy)
                    continue;
    
                if (RectTransformUtility.RectangleContainsScreenPoint(entry.Rect, screenPos, canvasCamera))
                    return i;
            }
    
            return -1;
        }
    
        /// <summary>
        /// 取画布相机 Overlay 则空
        /// </summary>
        private Camera GetCanvasCamera()
        {
            return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }
    
        /// <summary>
        /// 模型区内闲置显示 还原预制体原始色
        /// </summary>
        private void ShowAllHotspotsIdle()
        {
            if (hotspotList == null)
                return;
    
            for (int i = 0; i < hotspotList.Length; i++)
            {
                var entry = hotspotList[i];
                if (entry.Image == null)
                    continue;

                entry.Image.raycastTarget = true;
                ColorTools.ImageToColor(entry.Image, entry.OriginColor, entry.OriginAlpha);
                ColorTools.TmpToColor(entry.Label, entry.OriginLabelColor, entry.OriginLabelAlpha);
            }
        }
    
        /// <summary>
        /// 模型区外隐藏热区
        /// </summary>
        private void HideAllHotspots()
        {
            if (hotspotList == null)
                return;
    
            for (int i = 0; i < hotspotList.Length; i++)
            {
                var entry = hotspotList[i];
                if (entry.Image == null)
                    continue;

                entry.Image.raycastTarget = entry.Slot == EEquipSlot.Hand;
                if (entry.Slot == EEquipSlot.Hand)
                {
                    ColorTools.ImageToColor(entry.Image, entry.OriginColor, entry.OriginAlpha);
                    continue;
                }

                ColorTools.ImageToColor(entry.Image, entry.OriginColor, 0f);
                ColorTools.TmpToColor(entry.Label, entry.OriginLabelColor, 0f);
            }
        }
    
        /// <summary>
        /// 挂接热区点击接收
        /// </summary>
        private void BindHotspotClick(HotspotEntry entry)
        {
            if (entry.Rect == null)
                return;

            var click = entry.Rect.GetComponent<ModelHotspotClick>();
            if (click == null)
                click = entry.Rect.gameObject.AddComponent<ModelHotspotClick>();
    
            click.Bind(this, entry.Slot);
        }
    
        /// <summary>
        /// 物品是否匹配热区槽位 武器对 Hand 装备对表内 EquipSlot
        /// </summary>
        private static bool DoesItemMatchSlot(ItemView itemView, EEquipSlot eSlot)
        {
            if (itemView.ItemData == null)
                return false;
    
            int excelItemId = itemView.ExcelItemId;
            var weapon = LubanTables.Tables.TbWeapon.GetOrDefault(excelItemId);
            if (weapon != null)
                return eSlot == EEquipSlot.Hand;
    
            var equipment = LubanTables.Tables.TbEquipment.GetOrDefault(excelItemId);
            if (equipment == null)
                return false;
    
            return equipment.EquipSlot == eSlot;
        }
    
        /// <summary>
        /// 从 Rect 构建热区条目
        /// </summary>
        private static HotspotEntry CreateEntry(RectTransform rect, EEquipSlot eSlot)
        {
            if (rect == null)
            {
                return new HotspotEntry
                {
                    Rect = null,
                    Image = null,
                    Label = null,
                    Slot = eSlot,
                };
            }

            var image = rect.GetComponent<Image>();
            var label = rect.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.raycastTarget = false;
    
            return new HotspotEntry
            {
                Rect = rect,
                Image = image,
                Label = label,
                Slot = eSlot,
                OriginColor = image.color,
                OriginAlpha = image.color.a,
                OriginLabelColor = label != null ? label.color : Color.white,
                OriginLabelAlpha = label != null ? label.color.a : 1f,
            };
        }
    
    #endregion
    }
    
}
