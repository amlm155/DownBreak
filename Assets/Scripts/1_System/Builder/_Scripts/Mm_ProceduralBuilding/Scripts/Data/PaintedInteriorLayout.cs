using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    [CreateAssetMenu(fileName = "PaintedInteriorLayout", menuName = "Mm_ProceduralBuilding/PaintedInteriorLayout")]
    public class PaintedInteriorLayout : SerializedScriptableObject
    {
        /// <summary>
        /// 关联建筑蓝图
        /// </summary>
        [LabelText("关联建筑蓝图")]
        public PaintedBuildingPlan paintedBuildingPlan;

        /// <summary>
        /// 内饰布局数据列表
        /// </summary>
        [LabelText("内饰布局数据列表")]
        public List<InteriorFurniturePlacementData> furniturePlacementDataList = new();

        /// <summary>
        /// 添加内饰布局
        /// </summary>
        public void AddPlacement(InteriorFurniturePlacementData placementData)
        {
            if (placementData == null)
                return;

            furniturePlacementDataList.Add(placementData);
        }

        /// <summary>
        /// 查找占用目标格的布局
        /// </summary>
        public int FindPlacementIndexAt(int floorIndex, Vector2Int gridPos)
        {
            for (int i = furniturePlacementDataList.Count - 1; i >= 0; i--)
            {
                var placementData = furniturePlacementDataList[i];
                if (placementData == null || placementData.floorIndex != floorIndex)
                    continue;

                if (InteriorFurniturePlacementUtility.ContainsGridPos(placementData, gridPos))
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// 移除指定布局
        /// </summary>
        public bool RemovePlacementAt(int index)
        {
            if (index < 0 || index >= furniturePlacementDataList.Count)
                return false;

            furniturePlacementDataList.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 清空楼层布局
        /// </summary>
        public void ClearFloor(int floorIndex)
        {
            for (int i = furniturePlacementDataList.Count - 1; i >= 0; i--)
            {
                var placementData = furniturePlacementDataList[i];
                if (placementData != null && placementData.floorIndex == floorIndex)
                    furniturePlacementDataList.RemoveAt(i);
            }
        }

        /// <summary>
        /// 判断笔刷是否被布局引用
        /// </summary>
        public bool ContainsBrush(InteriorFurnitureBrushPreset brushPreset)
        {
            if (brushPreset == null)
                return false;

            foreach (var placementData in furniturePlacementDataList)
            {
                if (placementData != null && placementData.brushPreset == brushPreset)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 校验数据
        /// </summary>
        private void OnValidate()
        {
            if (furniturePlacementDataList == null)
                furniturePlacementDataList = new List<InteriorFurniturePlacementData>();

            foreach (var placementData in furniturePlacementDataList)
            {
                if (placementData != null)
                    placementData.floorIndex = Mathf.Max(0, placementData.floorIndex);
            }
        }
    }
}
