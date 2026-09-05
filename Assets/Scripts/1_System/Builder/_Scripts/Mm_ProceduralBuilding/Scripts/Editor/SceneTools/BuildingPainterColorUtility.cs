#if UNITY_EDITOR
using UnityEngine;

namespace Mm_ProceduralBuilding.Editor
{
    public static class BuildingPainterColorUtility
    {
        /// <summary>
        /// 获取格子颜色
        /// </summary>
        public static Color GetCellColor(EPaintedBuildingCellType cellType)
        {
            switch (cellType)
            {
                case EPaintedBuildingCellType.Floor:
                    return new Color(0.55f, 0.55f, 0.55f, 1f);
                case EPaintedBuildingCellType.Wall:
                    return new Color(0.9f, 0.15f, 0.15f, 1f);
                case EPaintedBuildingCellType.Cutout:
                    return new Color(1f, 0.75f, 0.1f, 1f);
                case EPaintedBuildingCellType.CutoutFill:
                    return new Color(0.2f, 0.75f, 1f, 1f);
                case EPaintedBuildingCellType.Erase:
                    return new Color(0.85f, 0.85f, 0.85f, 1f);
                case EPaintedBuildingCellType.Room:
                    return new Color(0.45f, 0.35f, 0.9f, 1f);
                default:
                    return new Color(1f, 1f, 1f, 0.5f);
            }
        }
    }
}
#endif
