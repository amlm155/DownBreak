using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MieMieUIFrameWork.Runtime
{
    public class MakeItemNeedGroup : MonoBehaviour
    {
        /// <summary> 材料图标 </summary>
        [SerializeField]
        private Image icon;

        /// <summary> 材料名 </summary>
        [SerializeField]
        private TextMeshProUGUI nameText;

        /// <summary> 持有与消耗数量 </summary>
        [SerializeField]
        private TextMeshProUGUI numText;

        /// <summary>
        /// 刷新材料行 数量格式为持有/消耗
        /// </summary>
        public void Set(Sprite iconSprite, string itemName, int haveCount, int needCount)
        {
            SetVisible(true);
            icon.sprite = iconSprite;
            nameText.text = itemName;
            SetNum(haveCount, needCount);
        }

        /// <summary>
        /// 控制材料行内容显隐 保留材料行容器
        /// </summary>
        public void SetVisible(bool visible)
        {
            icon.gameObject.SetActive(visible);
            nameText.gameObject.SetActive(visible);
            numText.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 刷新数量 格式 n/m
        /// </summary>
        public void SetNum(int haveCount, int needCount)
        {
            numText.text = $"{haveCount}/{needCount}";
        }
    }
}
