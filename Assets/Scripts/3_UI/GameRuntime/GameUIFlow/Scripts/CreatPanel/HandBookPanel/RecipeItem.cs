using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    public class RecipeItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary> 物品图标 </summary>
        private Image itemIcon;

        /// <summary> 未解锁锁 </summary>
        private GameObject itemLock;

        /// <summary> 信息菜单 </summary>
        private HandBookItemInfoMenu infoMenu;

        /// <summary> 物品名 </summary>
        private string itemName;

        /// <summary> 是否已解锁 </summary>
        private bool isUnlocked;

        /// <summary> 是否已初始化组件 </summary>
        private bool isComponentsInitialized;

        /// <summary> 配方项矩形变换 </summary>
        private RectTransform rectTransform;

        /// <summary>
        /// 查找图标和锁
        /// </summary>
        public void InitComponents()
        {
            if (isComponentsInitialized)
                return;

            itemIcon = transform.Find("ItemIcon").GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            itemLock = transform.Find("ItemLock").gameObject;
            itemIcon.raycastTarget = false;
            itemLock.GetComponent<Image>().raycastTarget = false;
            transform.Find("Bk").GetComponent<Image>().raycastTarget = false;
            isComponentsInitialized = true;
        }

        /// <summary>
        /// 挂信息菜单
        /// </summary>
        public void BindInfoMenu(HandBookItemInfoMenu menu)
        {
            infoMenu = menu;
        }

        /// <summary>
        /// 刷新图标与解锁状态
        /// </summary>
        public void Set(Sprite iconSprite, string name, bool unlocked)
        {
            itemName = name;
            isUnlocked = unlocked;
            itemIcon.sprite = iconSprite;
            itemLock.SetActive(!isUnlocked);
        }

        /// <summary>
        /// 指针进入显示名称
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            infoMenu.Show(itemName, isUnlocked, rectTransform);
        }

        /// <summary>
        /// 指针离开隐藏菜单
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            infoMenu.Hide();
        }
    }
}
