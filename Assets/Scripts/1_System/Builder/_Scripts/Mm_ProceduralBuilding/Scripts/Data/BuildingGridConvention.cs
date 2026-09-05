using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    [CreateAssetMenu(fileName = "BuildingGridConvention", menuName = "Mm_ProceduralBuilding/BuildingGridConvention")]
    public class BuildingGridConvention : SerializedScriptableObject
    {
        /// <summary>
        /// 世界原点
        /// </summary>
        [LabelText("世界原点")]
        public Vector3 worldOrigin = Vector3.zero;

        /// <summary>
        /// 单位格大小
        /// </summary>
        [LabelText("单位格大小")]
        [MinValue(1)]
        public int gridUnitSize = 1;

        /// <summary>
        /// 单层高度格数
        /// </summary>
        [LabelText("单层高度格数")]
        [MinValue(1)]
        public int floorHeightGridCount = 4;

        /// <summary>
        /// 墙体厚度格数
        /// </summary>
        [LabelText("墙体厚度格数")]
        [MinValue(1)]
        public int wallThicknessGridCount = 1;

        /// <summary>
        /// 获取安全单位格大小
        /// </summary>
        public int GridUnitSize => Mathf.Max(1, gridUnitSize);

        /// <summary>
        /// 获取安全单层高度
        /// </summary>
        public int FloorHeightGridCount => Mathf.Max(1, floorHeightGridCount);

        /// <summary>
        /// 获取安全墙体厚度
        /// </summary>
        public int WallThicknessGridCount => Mathf.Max(1, wallThicknessGridCount);

        /// <summary>
        /// 网格坐标转世界中心
        /// </summary>
        public Vector3 GridToWorldCenter(Vector3Int gridPos)
        {
            int unit = GridUnitSize;
            return worldOrigin + new Vector3(
                gridPos.x * unit + unit * 0.5f,
                gridPos.y * unit + unit * 0.5f,
                gridPos.z * unit + unit * 0.5f);
        }

        /// <summary>
        /// 网格盒子转世界中心
        /// </summary>
        public Vector3 GridBoxToWorldCenter(Vector3Int originGridPos, Vector3Int gridSize)
        {
            int unit = GridUnitSize;
            var safeSize = GetSafeGridSize(gridSize);
            return worldOrigin + new Vector3(
                (originGridPos.x + safeSize.x * 0.5f) * unit,
                (originGridPos.y + safeSize.y * 0.5f) * unit,
                (originGridPos.z + safeSize.z * 0.5f) * unit);
        }

        /// <summary>
        /// 网格尺寸转世界尺寸
        /// </summary>
        public Vector3 GridSizeToWorldSize(Vector3Int gridSize)
        {
            int unit = GridUnitSize;
            var safeSize = GetSafeGridSize(gridSize);
            return new Vector3(safeSize.x * unit, safeSize.y * unit, safeSize.z * unit);
        }

        /// <summary>
        /// 获取楼层起点高度
        /// </summary>
        public int GetFloorBaseY(int floorIndex)
        {
            return Mathf.Max(0, floorIndex) * FloorHeightGridCount;
        }

        /// <summary>
        /// 获取安全格尺寸
        /// </summary>
        public static Vector3Int GetSafeGridSize(Vector3Int gridSize)
        {
            return new Vector3Int(
                Mathf.Max(1, gridSize.x),
                Mathf.Max(1, gridSize.y),
                Mathf.Max(1, gridSize.z));
        }

        /// <summary>
        /// 校验参数
        /// </summary>
        private void OnValidate()
        {
            gridUnitSize = Mathf.Max(1, gridUnitSize);
            floorHeightGridCount = Mathf.Max(1, floorHeightGridCount);
            wallThicknessGridCount = Mathf.Max(1, wallThicknessGridCount);
        }
    }
}
