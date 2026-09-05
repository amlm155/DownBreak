using cfg.craft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    public class MakeItemShowGroup : MonoBehaviour
    {
        /// <summary> 自身点击按钮 </summary>
        [SerializeField]
        private Button makeItemShowGroupButton;

        /// <summary> 物品图标 </summary>
        [SerializeField]
        private Image itemIcon;

        /// <summary> 物品名 </summary>
        [SerializeField]
        private TextMeshProUGUI itemName;

        /// <summary> 所属制作页 </summary>
        private MakePanel ownerPanel;

        /// <summary> 当前配方 </summary>
        private Recipe recipe;

        /// <summary> 是否已初始化组件 </summary>
        private bool isComponentsInitialized;

        private void Awake()
        {
            InitComponents();
            makeItemShowGroupButton.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            makeItemShowGroupButton.onClick.RemoveListener(OnClicked);
        }

        /// <summary>
        /// 挂到制作页 由 MakePanel 调用
        /// </summary>
        public void Bind(MakePanel panel)
        {
            ownerPanel = panel;
        }

        public Recipe Recipe => recipe;

        /// <summary>
        /// 刷新列表项显示
        /// </summary>
        public void Set(Sprite iconSprite, string name, Recipe recipeData)
        {
            recipe = recipeData;
            itemIcon.sprite = iconSprite;
            itemName.text = name;
        }

        /// <summary>
        /// 查找按钮与图标名字
        /// </summary>
        private void InitComponents()
        {
            if (isComponentsInitialized)
                return;

            if (makeItemShowGroupButton == null)
                makeItemShowGroupButton = GetComponent<Button>();
            if (itemIcon == null)
                itemIcon = transform.Find("ItemIcon").GetComponent<Image>();
            if (itemName == null)
                itemName = transform.Find("ItemName").GetComponent<TextMeshProUGUI>();

            isComponentsInitialized = true;
        }

        /// <summary>
        /// 点中后刷右侧材料行
        /// </summary>
        private void OnClicked()
        {
            ownerPanel.OnShowGroupClicked(this);
        }
    }
}
