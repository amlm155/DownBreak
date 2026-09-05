using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 格子预览状态
    /// </summary>
    public enum ECellPreviewState
    {
        None,
        Valid,
        Invalid,
    }

    /// <summary>
    /// 格子的UI需要挂载的脚本 
    /// 管理了高亮等功能
    /// </summary>
    public class GridCellView : MonoBehaviour
    {
        [SerializeField]
        private Image backgroundImage;

        /// <summary> 空闲态 Highlight 颜色 一般透明露出 Basemap </summary>
        [BoxGroup("Highlight")]
        [SerializeField]
        private Color defaultColor = new(0f, 0f, 0f, 0f);

        /// <summary> 悬停高亮色 </summary>
        [BoxGroup("Highlight")]
        [SerializeField]
        private Color highLightColor = new(1f, 1f, 1f, 0.35f);

        /// <summary> 可放置预览色 </summary>
        [BoxGroup("Highlight")]
        [SerializeField]
        private Color canPlacePreviewColor = new(0f, 1f, 0f, 0.45f);

        /// <summary> 不可放置预览色 </summary>
        [BoxGroup("Highlight")]
        [SerializeField]
        private Color cannotPlacePreviewColor = new(1f, 0f, 0f, 0.45f);

        private void Awake()
        {
            InitComponents();
            StretchVisualsToCell();
        }

        private void Start()
        {
            StretchVisualsToCell();
            SetBkHighLight(false);
        }

        /// <summary>
        /// 设置背景高亮
        /// </summary>
        public void SetBkHighLight(bool isHighLight)
        {
            // Unity 假 null 需用 == 判断已销毁对象
            if (backgroundImage == null)
                return;

            backgroundImage.color = isHighLight ? highLightColor : defaultColor;
        }

        /// <summary>
        /// 设置拖拽 footprint 预览色
        /// </summary>
        public void SetPreviewState(ECellPreviewState previewState)
        {
            if (backgroundImage == null)
                return;

            backgroundImage.color = previewState switch
            {
                ECellPreviewState.Valid => canPlacePreviewColor,
                ECellPreviewState.Invalid => cannotPlacePreviewColor,
                _ => defaultColor,
            };
        }

        /// <summary>
        /// 子图撑满格子槽位 避免写死 100 在小格子上溢出叠层
        /// </summary>
        public void StretchVisualsToCell()
        {
            StretchChildRect("Basemap");
            StretchChildRect("Highlight");
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void InitComponents()
        {
            backgroundImage = transform.Find("Highlight")?.GetComponent<Image>();
        }

        /// <summary>
        /// 指定子节点改为四周锚点铺满
        /// </summary>
        private void StretchChildRect(string childName)
        {
            var child = transform.Find(childName) as RectTransform;
            if (child == null)
                return;

            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.one;
            child.anchoredPosition = Vector2.zero;
            child.sizeDelta = Vector2.zero;
            child.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
