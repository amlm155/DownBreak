using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 建造预制体的放置锚点模式
    /// </summary>
    public enum EBuilderPlacementAnchorMode
    {
        /// <summary>
        /// 根节点中心对齐网格中心
        /// </summary>
        Center = 0,

        /// <summary>
        /// 根节点底部对齐网格底部
        /// </summary>
        Bottom = 1,

        /// <summary>
        /// 自定义锚点对齐网格底部中心
        /// </summary>
        Custom = 2,
    }

    /// <summary>
    /// 配置建造预制体的放置锚点
    /// </summary>
    [DisallowMultipleComponent]
    public class BuilderPlacementAnchor : MonoBehaviour
    {
        /// <summary>
        /// 放置锚点模式
        /// </summary>
        [LabelText("锚点模式")]
        public EBuilderPlacementAnchorMode anchorMode;

        /// <summary>
        /// 是否允许其他方块在自身上向上叠放
        /// </summary>
        [LabelText("允许叠高")]
        public bool allowVerticalStacking = true;

        /// <summary>
        /// 自定义锚点
        /// </summary>
        [LabelText("自定义锚点"), ShowIf(nameof(IsCustomAnchor))]
        public Transform customAnchor;

        /// <summary>
        /// 当前是否使用自定义锚点
        /// </summary>
        private bool IsCustomAnchor()
        {
            return anchorMode == EBuilderPlacementAnchorMode.Custom;
        }

        /// <summary>
        /// 获取自定义锚点相对预制体根节点的位置
        /// </summary>
        public Vector3 GetCustomAnchorLocalPosition()
        {
            if (customAnchor == null)
                return Vector3.zero;

            return transform.InverseTransformPoint(customAnchor.position);
        }
    }
}
