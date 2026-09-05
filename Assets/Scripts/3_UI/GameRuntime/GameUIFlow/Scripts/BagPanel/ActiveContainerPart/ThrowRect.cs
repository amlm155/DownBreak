using MieMieFrameTools.Archive;
using MmInventory;
using UnityEngine;
using UnityEngine.UI;
namespace MieMieUIFrameWork.Runtime
{
    
    /// <summary>
    /// 丢弃区表现 高亮由外部直接调用 绑定由 BagPanel 触发
    /// </summary>
    public class ThrowRect : MonoBehaviour
    {
        /// <summary> 丢弃区图片 </summary>
        private Image throwImage;
    
        /// <summary> 原始透明度 </summary>
        private float originAlpha = 1f;
    
        /// <summary> 高亮透明度 </summary>
        private float highlightAlpha = 0.1f;
    
        /// <summary> 高亮颜色 </summary>
        private Color highlightColor = Color.red;
    
        /// <summary> 原始颜色 </summary>
        private Color originColor;
    
        /// <summary> 是否已绑定 </summary>
        private bool isBound;
    
        /// <summary> 丢弃检测矩形 </summary>
        public RectTransform ZoneRect => transform as RectTransform;
    
        /// <summary>
        /// 由 BagPanel 在面板就绪后调用
        /// </summary>
        public void BindView()
        {
            if (isBound)
                return;
    
            throwImage = GetComponent<Image>();
            originColor = throwImage.color;
            originAlpha = throwImage.color.a;
            isBound = true;
        }
    
        /// <summary>
        /// 切换丢弃区高亮
        /// </summary>
        public void SetHighlight(bool isHovered, bool canAccept)
        {
            Color targetColor = isHovered ? highlightColor : originColor;
            float targetAlpha = isHovered ? highlightAlpha : originAlpha;
            ColorTools.ImageToColor(throwImage, targetColor, targetAlpha);
        }
    }
    
}