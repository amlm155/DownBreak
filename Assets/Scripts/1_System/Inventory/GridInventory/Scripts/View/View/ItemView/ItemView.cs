using System;
using MieMieFrameWork.Asset;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using cfg.item;
using MieMieFrameTools.Archive;
using DBGameSystem;
using Interaction.Player;

namespace MmInventory
{
    /// <summary>
    /// 此脚本挂载于具体物品上 传递鼠标操作事件 
    /// </summary>
    public class ItemView : MonoBehaviour,
                            IPointerEnterHandler,
                            IPointerExitHandler,
                            IPointerDownHandler,
                            IPointerClickHandler,
                            IBeginDragHandler,
                            IDragHandler,
                            IEndDragHandler
    {
        [Header("表现配置")]
        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private Image itemBackground;

        [SerializeField]
        private TextMeshProUGUI nums;

        [SerializeField]
        private TextMeshProUGUI hotKeyTip;

        [SerializeField]
        private RectTransform itemRectTransform;

        [SerializeField]
        private float bkColorAlpha = 0.5f;

        /// <summary> 是否按稀有度刷 Background 色 </summary>
        [SerializeField]
        private bool applyRarityBackgroundColor = true;


        /// <summary> 配置表物品ID </summary>
        [SerializeField]
        [LabelText("配置表物品ID")]
        private int excelItemId;

        /// <summary> 运行时物品数据 </summary>
        public ItemRtData ItemData;

        // 运行时状态
        private GridContainerView ownerContainer;
        private bool isDragging;

        private void Awake()
        {
            InitComponents();
        }

        private void OnEnable()
        {
            ItemWheelSlotStore.OnSlotsChanged += RefreshHotKeyTip;
            RefreshHotKeyTip();
        }

        private void OnDisable()
        {
            ItemWheelSlotStore.OnSlotsChanged -= RefreshHotKeyTip;
        }

        // 属性
        public int ExcelItemId => ItemData != null ? ItemData.ExcelItemId : excelItemId;
        public Image ItemImage => itemImage;
        public Image ItemBackground => itemBackground;
        public RectTransform ItemRectTransform
        {
            get => itemRectTransform;
            set => itemRectTransform = value;
        }

        public GridContainerView OwnerContainer => ownerContainer;

        // TODO: 以后可以使用事件系统来替代这些委托
        /// <summary> 鼠标进入物品 </summary>
        public Action OnMouseEnter;

        /// <summary> 所有物品鼠标进入事件 </summary>
        public static event Action<ItemView> PointerEntered;

        /// <summary> 所有物品鼠标离开事件 </summary>
        public static event Action<ItemView> PointerExited;

        /// <summary> 所有物品开始拖拽事件 </summary>
        public static event Action<ItemView> DragBegan;

        /// <summary> 所有物品结束拖拽事件 </summary>
        public static event Action<ItemView> DragEnded;
        /// <summary> 物品被拿起 </summary>
        public Action OnItemPickUp;
        /// <summary> 物品被放下 </summary>
        public Action OnItemPutDown;

        #region 初始化

        /// <summary>
        /// 绑定背包容器
        /// </summary>
        public void BindOwner(GridContainerView owner)
        {
            ownerContainer = owner;
        }

        /// <summary>
        /// 初始化 场景预先摆好的物品会调用此方法
        /// </summary>
        public void Init()
        {
            if (ItemData is not null) return;

            // 获取配置表物品数据
            var excelItemData = GameHub.Get<IInventory>()?.GetItemData<IItemTableData>(excelItemId);
            ItemData = ItemRtData.ItemTableData2ItemRtData(excelItemData);

            UpdateItemView();
        }

        /// <summary>
        /// 使用运行时数据初始化
        /// 投放物品时会调用此方法
        /// </summary>
        public void InitWithData(ItemRtData itemRtData)
        {
            ItemData = itemRtData;
            excelItemId = itemRtData != null ? itemRtData.ExcelItemId : 0;

            UpdateItemView();
        }


        #endregion

        #region 生命周期

        /// <summary>
        /// 鼠标按下物品
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
        }

        /// <summary>
        /// 鼠标点击物品 左键双击快捷互转 右键打开菜单
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging)
                return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                GameHub.Get<IUIBagInteract>()?.ShowItemMenu(this);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (eventData.clickCount != 2)
                return;

            ownerContainer?.TryQuickTransferItem(this);
        }

        /// <summary>
        /// 鼠标进入物品
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            OnMouseEnter?.Invoke();
            PointerEntered?.Invoke(this);
        }

        /// <summary>
        /// 鼠标离开物品
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            PointerExited?.Invoke(this);
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            GameHub.Get<IUIBagInteract>()?.HideItemMenu();
            isDragging = true;
            DragBegan?.Invoke(this);
            ownerContainer?.OnBeginDrag(this, eventData);
            OnItemPickUp?.Invoke();
        }

        /// <summary>
        /// 拖拽中
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            ownerContainer?.OnDragging(eventData);
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            DragEnded?.Invoke(this);
            ownerContainer?.OnEndDrag(eventData);
            OnItemPutDown?.Invoke();
        }

        #endregion

        #region 视图

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitComponents()
        {
            itemRectTransform = transform as RectTransform;
            itemImage = transform.Find("Icon").GetComponent<Image>();
            // 图标区域可以铺满占格 但图像内容必须保持原始宽高比
            itemImage.preserveAspect = true;
            itemBackground = transform.Find("Background").GetComponent<Image>();
            nums = transform.Find("Icon/Nums")?.GetComponent<TextMeshProUGUI>();

            Transform tipTf = transform.Find("Icon/HotKeyTip");
            if (tipTf == null)
                tipTf = transform.Find("Icon/HotKkey");
            hotKeyTip = tipTf != null ? tipTf.GetComponent<TextMeshProUGUI>() : null;
            if (hotKeyTip != null)
                hotKeyTip.gameObject.SetActive(false);
        }

        /// <summary>
        /// 按容器格子尺寸同步根节点宽高 子节点全向拉伸铺满
        /// </summary>
        public void ApplyUiSize(Vector2 uiSize)
        {
            if (itemRectTransform == null)
                itemRectTransform = transform as RectTransform;

            // 中心锚点 保证 sizeDelta 按占格生效 不被拉伸锚点吃掉
            itemRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            itemRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            itemRectTransform.pivot = new Vector2(0.5f, 0.5f);
            itemRectTransform.sizeDelta = uiSize;

            StretchFillChild(itemImage != null ? itemImage.rectTransform : null);
            StretchFillChild(itemBackground != null ? itemBackground.rectTransform : null);
        }

        /// <summary>
        /// 刷新堆叠数量显示
        /// </summary>
        public void RefreshStackView()
        {
            if (nums == null)
                return;

            bool isStackable = ItemData != null
                               && ItemData.ItemStackType == EItemStackType.Stackable;
            nums.gameObject.SetActive(isStackable);
            if (isStackable)
                nums.text = ItemData.CurrStackCount.ToString();
        }

        /// <summary>
        /// 子 Rect 全向拉伸铺满父节点
        /// </summary>
        private static void StretchFillChild(RectTransform childRect)
        {
            if (childRect == null)
            {
                Debug.LogError("尝试拉伸子节点时 子节点为空");
                return;
            }

            childRect.anchorMin = Vector2.zero;
            childRect.anchorMax = Vector2.one;
            childRect.pivot = new Vector2(0.5f, 0.5f);
            childRect.offsetMin = Vector2.zero;
            childRect.offsetMax = Vector2.zero;
            childRect.localScale = Vector3.one;
        }

        private void UpdateItemView()
        {
            UpdateItemIconView();
            UpdateItemBkGroundView();
            RefreshStackView();
            RefreshHotKeyTip();
        }

        /// <summary>
        /// 刷新轮盘快捷键 Tip 的TmpText
        /// </summary>
        public void RefreshHotKeyTip()
        {
            if (hotKeyTip == null)
                return;

            // 判断物品是否可绑定到轮盘: 武器 食物水 药品
            if (ItemData == null || !ItemTypeUtil.IsWheelBindable(ItemData.ExcelItemId))
            {
                hotKeyTip.gameObject.SetActive(false);
                return;
            }

            // 查找物品是否已绑定到其他槽位
            int slotIndex = ItemWheelSlotStore.FindSlotByInstanceId(ItemData.InstancedItemId);
            if (slotIndex < 0)
            {
                hotKeyTip.gameObject.SetActive(false);
                return;
            }

            hotKeyTip.gameObject.SetActive(true);
            hotKeyTip.text = (slotIndex + 1).ToString();
        }

        /// <summary>
        /// 设置是否按稀有度刷 Background 色
        /// </summary>
        public void SetApplyRarityBackgroundColor(bool apply)
        {
            applyRarityBackgroundColor = apply;
        }

        private void UpdateItemBkGroundView()
        {
            if (ItemData is null) return;
            if (itemBackground is null) return;

            if (applyRarityBackgroundColor)
            {
                ColorTools.ImageToColor(
                    itemBackground,
                    ItemRarityColors.GetRgb(ItemData.ItemRarity),
                    bkColorAlpha);
            }

            RefreshDurabilityFill();
        }

        /// <summary>
        /// 按当前耐久刷新 Background Fill 比例向下截到两位小数
        /// </summary>
        public void RefreshDurabilityFill()
        {
            if (itemBackground == null || ItemData == null)
                return;

            int max = ItemData.MaxDurability;
            float ratio = max > 0
                ? (float)ItemData.CurrDurability / max
                : 0f;
            ratio = Mathf.Clamp01(ratio);
            // 0.996 → 0.99 向下截到两位小数
            itemBackground.fillAmount = Mathf.Floor(ratio * 100f) / 100f;
        }

        /// <summary>
        /// 按配置表 IconPath 加载图标
        /// </summary>
        private void UpdateItemIconView()
        {
            if (itemImage is null || ItemData is null)
                return;

            var tableData = ResolveTableData(ItemData.ExcelItemId);
            if (tableData is null || string.IsNullOrEmpty(tableData.IconPath))
                return;

            itemImage.sprite = MmAssetMgr.LoadAsset<Sprite>(tableData.IconPath);
            ColorTools.ImageToColor(itemImage, itemImage.color, 1);
        }

        /// <summary>
        /// 解析物品表数据
        /// </summary>
        private static IItemTableData ResolveTableData(int excelItemId)
        {
            if (GameHub.Get<IInventory>() != null)
            {
                var runtimeData = GameHub.Get<IInventory>().GetItemData<IItemTableData>(excelItemId);
                if (runtimeData != null)
                    return runtimeData;
            }

            if (LubanTables.TryGetItem(excelItemId, out var itemTableData))
                return itemTableData;

            return null;
        }

        #endregion
    }
}
