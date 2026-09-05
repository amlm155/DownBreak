using System;
using System.Collections.Generic;
using cfg.item;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DBGameSystem;
using MieMieFrameWork;
using MieMieFrameWork.M_InputSystem;

// 查看
namespace MmInventory
{
    /// <summary>
    /// 物品右键菜单面板
    /// </summary>
    public class ItemMenuPanel : MonoBehaviour
    {
        private const string USE_BUTTON_EQUIMENTAndWEAPON = "装备";
        private const string USE_BUTTON_FOODWATER = "食用";
        private const string USE_BUTTON_MEDICINE = "使用";
        private const string USE_BUTTON_FURNITURE = "放置";
        private const string CHECK_BUTTON_NAME = "查看";
        private const string SPLIT_BUTTON_NAME = "拆分";
        private const string THROW_BUTTON_NAME = "丢弃";

        /// <summary> 菜单丢弃回调 由 BagPanel 注入 </summary>
        public Func<ItemView, bool> ThrowHandler;

        /// <summary> 菜单装备回调 由 BagPanel 注入 </summary>
        public Func<ItemView, bool> EquipHandler;

        /// <summary> 菜单拆分回调 由 BagPanel 注入 </summary>
        public Func<ItemView, bool> SplitHandler;

        /// <summary> 菜单食用回调 由 BagPanel 注入 </summary>
        public Func<ItemView, bool> EatHandler;

        /// <summary> 菜单用药回调 由 BagPanel 注入 </summary>
        public Func<ItemView, bool> UseMedicineHandler;

        /// <summary> 菜单放置家具回调 由 BagPanel 注入 </summary>
        public Func<ItemView, bool> PlaceHandler;

        /// <summary> 面板 RectTransform </summary>
        private RectTransform panelRectCache;

        /// <summary> 面板 RectTransform </summary>
        private RectTransform PanelRect => panelRectCache ??= transform as RectTransform;

        /// <summary> 所属 Canvas </summary>
        private Canvas rootCanvasCache;

        /// <summary> 所属 Canvas </summary>
        private Canvas RootCanvas => rootCanvasCache ??= GetComponentInParent<Canvas>();

        /// <summary> 射线检测结果缓存 </summary>
        private readonly List<RaycastResult> raycastResultList = new();

        /// <summary> 当前帧是否由 Show 打开 </summary>
        private int showFrame = -1;

        /// <summary> 输入管理器缓存 </summary>
        private InputManager inputManager;

        /// <summary> 使用按钮文案 </summary>
        [SerializeField]
        private TextMeshProUGUI useButtonText;

        /// <summary> 查看按钮文案 </summary>
        [SerializeField]
        private TextMeshProUGUI checkButtonText;

        /// <summary> 拆分按钮文案 </summary>
        [SerializeField]
        private TextMeshProUGUI splitButtonText;

        /// <summary> 丢弃按钮文案 </summary>
        [SerializeField]
        private TextMeshProUGUI throwButtonText;

        /// <summary> 物品名称文案 </summary>
        [SerializeField]
        private TextMeshProUGUI itemNameText;

        /// <summary> 物品描述文案 </summary>
        [SerializeField]
        private TextMeshProUGUI describeText;

        /// <summary> 当前关联物品视图 </summary>
        public ItemView CurrentItemView { get; private set; }

        /// <summary> 是否已绑定按钮 </summary>
        private bool isInited;

        [SerializeField]
        private Button useButton;
        [SerializeField]
        private Button Check;
        [SerializeField]
        private Button Split;
        [SerializeField]
        private Button Throw;

        /// <summary>
        /// 懒获取 InputManager
        /// </summary>
        private InputManager inputMgr
        {
            get
            {
                if (inputManager != null)
                    return inputManager;
                if (ModuleHub.Instance == null)
                    return null;
                inputManager = ModuleHub.Instance.GetManager<InputManager>();
                return inputManager;
            }
        }

#region 生命周期
        /// <summary>
        /// 绑定按钮并默认隐藏 由背包壳调用一次
        /// </summary>
        public void InitComponents()
        {
            if (isInited)
                return;

            BindButtonEvents();
            gameObject.SetActive(false);
            isInited = true;
        }

        /// <summary>
        /// 随背包等父级关闭时清掉当前物品
        /// </summary>
        private void OnDisable()
        {
            CurrentItemView = null;
        }

        /// <summary>
        /// 每帧末尾处理菜单关闭与轮盘绑定
        /// </summary>
        private void LateUpdate()
        {
            if (!gameObject.activeSelf)
                return;

            TryBindWheelHotkeys();

            var input = inputMgr;
            if (input != null && input.IsUiClickPressed)
            {
                // 左键未点在菜单按钮上则关闭
                if (!IsPointerOverMenuButton())
                    Hide();
            }

            if (input != null
                && input.IsUiRightClickPressed
                && showFrame != Time.frameCount)
            {
                // 右键点在空白处关闭 Item 上右键会在同帧 Show
                Hide();
            }
        }

        /// <summary>
        /// 菜单打开时按 1-8 绑定当前物品到轮盘槽
        /// </summary>
        private void TryBindWheelHotkeys()
        {
            if (CurrentItemView == null || CurrentItemView.ItemData == null)
                return;

            if (!ItemTypeUtil.IsWheelBindable(CurrentItemView))
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            for (int slotIndex = 0; slotIndex < ItemWheelSlotStore.SlotCount; slotIndex++)
            {
                var digitKey = ResolveDigitKey(keyboard, slotIndex + 1);
                if (digitKey == null || !digitKey.wasPressedThisFrame)
                    continue;

                if (ItemWheelSlotStore.Bind(slotIndex, CurrentItemView.ItemData))
                    CurrentItemView.RefreshHotKeyTip();
                break;
            }
        }

        /// <summary>
        /// 数字键 1-8
        /// </summary>
        private static UnityEngine.InputSystem.Controls.KeyControl ResolveDigitKey(
            Keyboard keyboard, int digit)
        {
            switch (digit)
            {
                case 1: return keyboard.digit1Key;
                case 2: return keyboard.digit2Key;
                case 3: return keyboard.digit3Key;
                case 4: return keyboard.digit4Key;
                case 5: return keyboard.digit5Key;
                case 6: return keyboard.digit6Key;
                case 7: return keyboard.digit7Key;
                case 8: return keyboard.digit8Key;
                default: return null;
            }
        }


        /// <summary>
        /// 显示菜单 左上角对齐到物品 pivot 溢出时钳回可见区
        /// </summary>
        public void Show(ItemView itemView)
        {
            if (itemView is null)
                return;
            // 获取当前物品视图
            CurrentItemView = itemView;
            // 获取当前帧数
            showFrame = Time.frameCount;
            // 刷新按钮文案
            RefreshButtonLabels(itemView);
            // 刷新物品信息
            RefreshItemInfo(itemView);
            // 显示菜单
            gameObject.SetActive(true);
            // 将菜单左上角对齐到物品 pivot
            AlignTopLeftToItemPivot(itemView);
            // 钳回可见区
            ClampPanelInsideParent();
            // 设置为最后一个子对象
            transform.SetAsLastSibling();
        }

        /// <summary>
        /// 隐藏菜单
        /// </summary>
        public void Hide()
        {
            CurrentItemView = null;
            gameObject.SetActive(false);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 将菜单左上角对齐到物品 RectTransform 的 pivot
        /// </summary>
        private void AlignTopLeftToItemPivot(ItemView itemView)
        {
            if (PanelRect is null || RootCanvas is null)
                return;

            RectTransform parentRect = PanelRect.parent as RectTransform;
            if (parentRect is null)
                return;

            RectTransform itemRect = itemView.ItemRectTransform;
            if (itemRect is null)
                return;

            Camera eventCamera = RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : RootCanvas.worldCamera;

            // 物品 pivot 的世界坐标转屏幕坐标
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, itemRect.position);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
                return;

            // 左上角作为对齐点 避免用面板自身中心 pivot 去贴目标
            PanelRect.pivot = new Vector2(0f, 1f);
            PanelRect.anchoredPosition = localPoint;
        }

        /// <summary>
        /// 菜单越出屏幕可见区时往可见方向推回
        /// </summary>
        private void ClampPanelInsideParent()
        {
            if (PanelRect is null || RootCanvas is null)
                return;

            RectTransform parentRect = PanelRect.parent as RectTransform;
            if (parentRect is null)
                return;

            Canvas.ForceUpdateCanvases();

            // 钳位以 Canvas 可见区为准 父节点 PlayerPart 比菜单还窄 按父节点会把位置锁死
            RectTransform canvasRect = RootCanvas.rootCanvas.transform as RectTransform;
            Camera eventCamera = RootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : RootCanvas.worldCamera;

            Vector3[] canvasWorldCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasWorldCorners);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                RectTransformUtility.WorldToScreenPoint(eventCamera, canvasWorldCorners[0]),
                eventCamera,
                out Vector2 localBottomLeft);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                RectTransformUtility.WorldToScreenPoint(eventCamera, canvasWorldCorners[2]),
                eventCamera,
                out Vector2 localTopRight);

            Vector2 areaMin = Vector2.Min(localBottomLeft, localTopRight);
            Vector2 areaMax = Vector2.Max(localBottomLeft, localTopRight);

            Vector2 panelSize = PanelRect.rect.size;
            Vector2 pivot = PanelRect.pivot;
            Vector2 anchoredPos = PanelRect.anchoredPosition;

            float minX = areaMin.x + panelSize.x * pivot.x;
            float maxX = areaMax.x - panelSize.x * (1f - pivot.x);
            float minY = areaMin.y + panelSize.y * pivot.y;
            float maxY = areaMax.y - panelSize.y * (1f - pivot.y);

            anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
            anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);
            PanelRect.anchoredPosition = anchoredPos;
        }

        /// <summary>
        /// 指针是否点在菜单内 Button 上
        /// </summary>
        private bool IsPointerOverMenuButton()
        {
            if (!TryGetPointerHitGameObject(out GameObject hitObject))
                return false;

            if (!hitObject.transform.IsChildOf(PanelRect))
                return false;

            return hitObject.GetComponentInParent<Button>() is not null;
        }

        /// <summary>
        /// 获取当前指针射线命中的 UI 对象
        /// </summary>
        private bool TryGetPointerHitGameObject(out GameObject hitObject)
        {
            hitObject = null;
            if (EventSystem.current is null)
                return false;

            var pointerData = new PointerEventData(EventSystem.current)
            {
                position = inputMgr != null
                    ? inputMgr.UiPoint
                    : (Mouse.current != null
                        ? Mouse.current.position.ReadValue()
                        : Vector2.zero)
            };

            raycastResultList.Clear();
            EventSystem.current.RaycastAll(pointerData, raycastResultList);
            if (raycastResultList.Count == 0)
                return false;

            hitObject = raycastResultList[0].gameObject;
            return hitObject is not null;
        }

        #endregion


        #region 按钮功能

        /// <summary>
        /// 注册点击并写入通用按钮文案
        /// </summary>
        private void BindButtonEvents()
        {
            useButton.onClick.AddListener(OnUseButtonClick);
            Check.onClick.AddListener(OnCheckButtonClick);
            Split.onClick.AddListener(OnSplitButtonClick);
            Throw.onClick.AddListener(OnThrowButtonClick);

            checkButtonText.text = CHECK_BUTTON_NAME;
            splitButtonText.text = SPLIT_BUTTON_NAME;
            throwButtonText.text = THROW_BUTTON_NAME;
        }

        /// <summary>
        /// 按物品类型刷新 Use 文案
        /// </summary>
        private void RefreshButtonLabels(ItemView itemView)
        {
            var eItemType = ItemTypeUtil.ResolveItemType(itemView);
            useButtonText.text = ResolveUseButtonName(eItemType);
        }

        /// <summary>
        /// 填充物品名称与描述
        /// </summary>
        private void RefreshItemInfo(ItemView itemView)
        {
            int excelItemId = itemView.ExcelItemId;
            var tableData = ResolveTableData(excelItemId);

            itemNameText.text = tableData is null ? string.Empty : tableData.Name;
            describeText.text = tableData is null ? string.Empty : tableData.Description;
        }

        /// <summary>
        /// 解析物品表数据
        /// </summary>
        private static IItemTableData ResolveTableData(int excelItemId)
        {
            if (GameHub.Get<IInventory>() is not null)
            {
                var runtimeData = GameHub.Get<IInventory>().GetItemData<IItemTableData>(excelItemId);
                if (runtimeData is not null)
                    return runtimeData;
            }

            if (LubanTables.TryGetItem(excelItemId, out var itemTableData))
                return itemTableData;

            return null;
        }

        /// <summary>
        /// 按大类返回 Use 按钮文案
        /// </summary>
        private string ResolveUseButtonName(EItemType eItemType)
        {
            switch (eItemType)
            {
                case EItemType.Equipment:
                case EItemType.Weapon:
                    return USE_BUTTON_EQUIMENTAndWEAPON;
                case EItemType.FoodOrWater:
                    return USE_BUTTON_FOODWATER;
                case EItemType.Medicine:
                    return USE_BUTTON_MEDICINE;
                case EItemType.Furniture:
                    return USE_BUTTON_FURNITURE;
                default:
                    return USE_BUTTON_MEDICINE;
            }
        }

        /// <summary>
        /// 使用按钮点击
        /// </summary>
        private void OnUseButtonClick()
        {
            var itemView = CurrentItemView;
            var eItemType = ItemTypeUtil.ResolveItemType(itemView);
            Hide();

            switch (eItemType)
            {
                case EItemType.Equipment:
                case EItemType.Weapon:
                    HandleEquip(itemView);
                    break;
                case EItemType.FoodOrWater:
                    HandleEat(itemView);
                    break;
                case EItemType.Medicine:
                    HandleUseMedicine(itemView);
                    break;
                case EItemType.Furniture:
                    HandlePlace(itemView);
                    break;
                default:
                    HandleUseDefault(itemView);
                    break;
            }
        }

        /// <summary>
        /// 查看按钮点击
        /// </summary>
        private void OnCheckButtonClick()
        {
            var itemView = CurrentItemView;
            Hide();
            HandleCheck(itemView);
        }

        /// <summary>
        /// 拆分按钮点击
        /// </summary>
        private void OnSplitButtonClick()
        {
            var itemView = CurrentItemView;
            Hide();
            HandleSplit(itemView);
        }

        /// <summary>
        /// 丢弃按钮点击
        /// </summary>
        private void OnThrowButtonClick()
        {
            var itemView = CurrentItemView;
            Hide();
            HandleThrow(itemView);
        }

        /// <summary>
        /// 装备武器钩子
        /// </summary>
        private void HandleEquip(ItemView itemView)
        {
            EquipHandler?.Invoke(itemView);
        }

        /// <summary>
        /// 食用钩子
        /// </summary>
        private void HandleEat(ItemView itemView)
        {
            EatHandler?.Invoke(itemView);
        }

        /// <summary>
        /// 使用药品钩子
        /// </summary>
        private void HandleUseMedicine(ItemView itemView)
        {
            UseMedicineHandler?.Invoke(itemView);
        }

        /// <summary>
        /// 放置家具钩子
        /// </summary>
        private void HandlePlace(ItemView itemView)
        {
            PlaceHandler?.Invoke(itemView);
        }

        /// <summary>
        /// 默认使用钩子 材料等
        /// </summary>
        private void HandleUseDefault(ItemView itemView)
        {
        }

        /// <summary>
        /// 查看钩子
        /// </summary>
        private void HandleCheck(ItemView itemView)
        {
        }

        /// <summary>
        /// 拆分钩子
        /// </summary>
        private void HandleSplit(ItemView itemView)
        {
            SplitHandler?.Invoke(itemView);
        }

        /// <summary>
        /// 丢弃钩子
        /// </summary>
        private void HandleThrow(ItemView itemView)
        {
            ThrowHandler?.Invoke(itemView);
        }

        #endregion
    }
}
