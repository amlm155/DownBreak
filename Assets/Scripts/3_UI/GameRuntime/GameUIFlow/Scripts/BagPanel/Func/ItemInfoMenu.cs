using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MmInventory;

namespace MieMieUIFrameWork.Runtime
{
    public class ItemInfoMenu : MonoBehaviour
    {
        /// <summary> 物品名称文本 </summary>
        private TextMeshProUGUI nameText;

        /// <summary> 信息菜单矩形变换 </summary>
        private RectTransform rectTransform;

        /// <summary> 信息菜单画布组 </summary>
        private CanvasGroup canvasGroup;

        /// <summary> 是否已初始化组件 </summary>
        private bool isComponentsInitialized;

        /// <summary> 是否有物品正在拖拽 </summary>
        private bool isItemDragging;

        private void Awake()
        {
            InitComponents();
        }

        private void OnEnable()
        {
            ItemView.PointerEntered += OnItemPointerEntered;
            ItemView.PointerExited += OnItemPointerExited;
            ItemView.DragBegan += OnItemDragBegan;
            ItemView.DragEnded += OnItemDragEnded;
        }

        private void OnDisable()
        {
            ItemView.PointerEntered -= OnItemPointerEntered;
            ItemView.PointerExited -= OnItemPointerExited;
            ItemView.DragBegan -= OnItemDragBegan;
            ItemView.DragEnded -= OnItemDragEnded;
        }

        /// <summary>
        /// 查找名称文本并默认隐藏
        /// </summary>
        public void InitComponents()
        {
            if (isComponentsInitialized)
                return;

            nameText = transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();
            // 左上角当锚 才能把菜单左上对齐到物品 Pivot
            rectTransform.pivot = new Vector2(0f, 1f);
            canvasGroup = GetComponent<CanvasGroup>();
            GetComponent<Image>().raycastTarget = false;
            nameText.raycastTarget = false;
            Hide();
            isComponentsInitialized = true;
        }

        /// <summary>
        /// 显示物品名称 菜单左上角对齐物品 Pivot
        /// </summary>
        private void Show(ItemView itemView)
        {
            if (isItemDragging)
                return;

            if (!LubanTables.TryGetItem(itemView.ExcelItemId, out var itemTableData))
                return;

            nameText.text = itemTableData.Name;
            rectTransform.position = itemView.ItemRectTransform.position;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        /// <summary>
        /// 隐藏物品信息菜单
        /// </summary>
        public void Hide()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void OnItemPointerEntered(ItemView itemView)
        {
            Show(itemView);
        }

        private void OnItemPointerExited(ItemView itemView)
        {
            Hide();
        }

        private void OnItemDragBegan(ItemView itemView)
        {
            isItemDragging = true;
            Hide();
        }

        private void OnItemDragEnded(ItemView itemView)
        {
            isItemDragging = false;
        }
    }
}
