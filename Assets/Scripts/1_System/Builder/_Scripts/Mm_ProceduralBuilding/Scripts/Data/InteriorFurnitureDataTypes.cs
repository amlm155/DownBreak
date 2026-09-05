using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public enum EInteriorFurnitureCategory
    {
        [LabelText("床")]
        Bed,
        [LabelText("桌子")]
        Table,
        [LabelText("椅子")]
        Chair,
        [LabelText("柜子")]
        Cabinet,
        [LabelText("箱子")]
        Chest
    }

    public enum EInteriorFurnitureRotation
    {
        [LabelText("0 度")]
        Deg0,
        [LabelText("90 度")]
        Deg90,
        [LabelText("180 度")]
        Deg180,
        [LabelText("270 度")]
        Deg270
    }

    [CreateAssetMenu(fileName = "InteriorFurnitureBrushPreset", menuName = "Mm_ProceduralBuilding/InteriorFurnitureBrushPreset")]
    public class InteriorFurnitureBrushPreset : SerializedScriptableObject
    {
        /// <summary>
        /// 内饰大类
        /// </summary>
        [LabelText("内饰大类")]
        public EInteriorFurnitureCategory category;

        /// <summary>
        /// 显示名称
        /// </summary>
        [LabelText("显示名称")]
        public string displayName = "新内饰";

        /// <summary>
        /// 家具预制体
        /// </summary>
        [LabelText("家具预制体")]
        public GameObject furniturePrefab;

        /// <summary>
        /// 占用宽度格数
        /// </summary>
        [LabelText("占用宽度")]
        [MinValue(1)]
        public int footprintWidthGridCount = 1;

        /// <summary>
        /// 占用深度格数
        /// </summary>
        [LabelText("占用深度")]
        [MinValue(1)]
        public int footprintDepthGridCount = 1;

        /// <summary>
        /// 占用高度格数
        /// </summary>
        [LabelText("占用高度")]
        [MinValue(1)]
        public int heightGridCount = 1;

        /// <summary>
        /// 预览颜色
        /// </summary>
        [LabelText("预览颜色")]
        public Color previewColor = new Color(0.2f, 0.75f, 1f, 1f);

        /// <summary>
        /// 默认朝向
        /// </summary>
        [LabelText("默认朝向")]
        public EInteriorFurnitureRotation defaultRotation;

        /// <summary>
        /// 预制体正面校正
        /// </summary>
        [LabelText("预制体正面校正")]
        public EInteriorFurnitureRotation prefabRotationOffset;

        /// <summary>
        /// 获取安全显示名称
        /// </summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        /// <summary>
        /// 获取安全占用尺寸
        /// </summary>
        public Vector2Int FootprintGridSize => new Vector2Int(
            Mathf.Max(1, footprintWidthGridCount),
            Mathf.Max(1, footprintDepthGridCount));

        /// <summary>
        /// 获取安全占用高度
        /// </summary>
        public int HeightGridCount => Mathf.Max(1, heightGridCount);

        /// <summary>
        /// 校验参数
        /// </summary>
        private void OnValidate()
        {
            footprintWidthGridCount = Mathf.Max(1, footprintWidthGridCount);
            footprintDepthGridCount = Mathf.Max(1, footprintDepthGridCount);
            heightGridCount = Mathf.Max(1, heightGridCount);
        }
    }

    [Serializable]
    public class InteriorFurniturePlacementData
    {
        /// <summary>
        /// 内饰笔刷
        /// </summary>
        [LabelText("内饰笔刷")]
        public InteriorFurnitureBrushPreset brushPreset;

        /// <summary>
        /// 楼层索引
        /// </summary>
        [LabelText("楼层索引")]
        [MinValue(0)]
        public int floorIndex;

        /// <summary>
        /// 放置起始格坐标
        /// </summary>
        [LabelText("起始格坐标")]
        public Vector2Int anchorGridPos;

        /// <summary>
        /// 放置朝向
        /// </summary>
        [LabelText("放置朝向")]
        public EInteriorFurnitureRotation rotation;

        /// <summary>
        /// 预制体模型校正快照
        /// </summary>
        [LabelText("模型校正快照")]
        public EInteriorFurnitureRotation prefabRotationOffset;

        /// <summary>
        /// 是否锁定布局
        /// </summary>
        [LabelText("锁定布局")]
        public bool locked;
    }
}
