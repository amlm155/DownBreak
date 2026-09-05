using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    [CreateAssetMenu(fileName = "PaintedBuildingBrushPreset", menuName = "Mm_ProceduralBuilding/PaintedBuildingBrushPreset")]
    public class PaintedBuildingBrushPreset : SerializedScriptableObject
    {
        /// <summary>
        /// 格子类型
        /// </summary>
        [LabelText("格子类型")]
        public EPaintedBuildingCellType cellType = EPaintedBuildingCellType.Wall;

        /// <summary>
        /// 笔刷显示名称
        /// </summary>
        [LabelText("笔刷显示名称")]
        public string displayName;

        /// <summary>
        /// 是否为该类型主笔刷
        /// </summary>
        [LabelText("主笔刷")]
        public bool isPrimaryPreset = true;

        /// <summary>
        /// 预览颜色
        /// </summary>
        [LabelText("预览颜色")]
        public Color previewColor = Color.red;

        /// <summary>
        /// 默认墙体高度
        /// </summary>
        [LabelText("默认墙体高度")]
        [MinValue(1)]
        public int defaultHeightGridCount = 3;

        /// <summary>
        /// 生成材质
        /// </summary>
        [LabelText("生成材质")]
        public Material material;

        /// <summary>
        /// 挖空填充预制体
        /// </summary>
        [LabelText("挖空填充预制体")]
        [ShowIf(nameof(IsCutoutFillBrush))]
        public GameObject cutoutFillPrefab;

        /// <summary>
        /// 挖空填充Y轴角度
        /// </summary>
        [LabelText("填充Y轴角度")]
        [ShowIf(nameof(IsCutoutFillBrush))]
        public float cutoutFillYRotation;

        private bool IsCutoutFillBrush => cellType == EPaintedBuildingCellType.CutoutFill;

        /// <summary>
        /// 获取安全显示名称
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        /// <summary>
        /// 校验参数
        /// </summary>
        private void OnValidate()
        {
            defaultHeightGridCount = Mathf.Max(1, defaultHeightGridCount);
        }
    }
}
