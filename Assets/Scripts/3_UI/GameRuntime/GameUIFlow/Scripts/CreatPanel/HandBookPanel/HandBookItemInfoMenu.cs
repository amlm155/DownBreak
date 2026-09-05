using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    public class HandBookItemInfoMenu : MonoBehaviour
    {
        /// <summary> 名称文本 </summary>
        private TextMeshProUGUI nameText;

        /// <summary> 信息菜单矩形变换 </summary>
        private RectTransform rectTransform;

        /// <summary> 是否已初始化组件 </summary>
        private bool isComponentsInitialized;

        /// <summary>
        /// 查找名称文本并默认隐藏
        /// </summary>
        public void InitComponents()
        {
            if (isComponentsInitialized)
                return;

            nameText = transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();
            GetComponent<Image>().raycastTarget = false;
            nameText.raycastTarget = false;
            Hide();
            isComponentsInitialized = true;
        }

        /// <summary>
        /// 显示名称 未解锁为未知
        /// </summary>
        public void Show(string itemName, bool isUnlocked, RectTransform targetRectTransform)
        {
            nameText.text = isUnlocked ? itemName : "未知";
            rectTransform.position = targetRectTransform.position;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏信息菜单
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
