using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public enum EPaintedBuildingCellType
    {
        [LabelText("地面")]
        Floor,
        [LabelText("墙体")]
        Wall,
        [LabelText("挖空")]
        Cutout,
        [LabelText("擦除")]
        Erase,
        [LabelText("房间")]
        Room,
        [LabelText("挖空填充物")]
        CutoutFill
    }

    public enum EWallExtendDirection
    {
        [LabelText("向外延伸")]
        Outward,
        [LabelText("向内延伸")]
        Inward
    }

    public enum ERoomDoorWallSide
    {
        [LabelText("下")]
        Down,
        [LabelText("上")]
        Up,
        [LabelText("左")]
        Left,
        [LabelText("右")]
        Right
    }

    public enum ERoomGridDoorMode
    {
        [LabelText("同向")]
        Same,
        [LabelText("对称")]
        Symmetric,
        [LabelText("随机")]
        Random
    }

    [Serializable]
    public class PaintedBuildingCellData
    {
        /// <summary>
        /// 格子坐标
        /// </summary>
        [LabelText("格子坐标")]
        public Vector2Int gridPos;

        /// <summary>
        /// 格子类型
        /// </summary>
        [LabelText("格子类型")]
        public EPaintedBuildingCellType cellType = EPaintedBuildingCellType.Floor;

        /// <summary>
        /// 使用的建筑笔刷
        /// </summary>
        [LabelText("使用的建筑笔刷")]
        public PaintedBuildingBrushPreset brushPreset;

        /// <summary>
        /// 挖空保留墙体笔刷
        /// </summary>
        [LabelText("挖空保留墙体笔刷")]
        public PaintedBuildingBrushPreset cutoutWallBrushPreset;

        /// <summary>
        /// 墙体高度格数
        /// </summary>
        [LabelText("墙体高度格数")]
        [MinValue(1)]
        public int heightGridCount = 1;

        /// <summary>
        /// 挖空起点高度
        /// </summary>
        [LabelText("挖空起点高度")]
        [MinValue(0)]
        public int cutoutStartHeightGridCount;

        /// <summary>
        /// 挖空终点高度
        /// </summary>
        [LabelText("挖空终点高度")]
        [MinValue(1)]
        public int cutoutEndHeightGridCount = 2;
    }

    [Serializable]
    public class PaintedBuildingFloorData
    {
        /// <summary>
        /// 楼层索引
        /// </summary>
        [LabelText("楼层索引")]
        [MinValue(0)]
        public int floorIndex;

        /// <summary>
        /// 地面格子列表
        /// </summary>
        [LabelText("地面格子列表")]
        public List<PaintedBuildingCellData> floorCellDataList = new();

        /// <summary>
        /// 结构格子列表
        /// </summary>
        [LabelText("结构格子列表")]
        public List<PaintedBuildingCellData> structureCellDataList = new();

        /// <summary>
        /// 查找地面格子
        /// </summary>
        public PaintedBuildingCellData FindFloorCell(Vector2Int gridPos)
        {
            return FindCell(floorCellDataList, gridPos);
        }

        /// <summary>
        /// 查找结构格子
        /// </summary>
        public PaintedBuildingCellData FindStructureCell(Vector2Int gridPos)
        {
            return FindCell(structureCellDataList, gridPos);
        }

        /// <summary>
        /// 移除地面格子
        /// </summary>
        public void RemoveFloorCell(Vector2Int gridPos)
        {
            RemoveCell(floorCellDataList, gridPos);
        }

        /// <summary>
        /// 移除结构格子
        /// </summary>
        public void RemoveStructureCell(Vector2Int gridPos)
        {
            RemoveCell(structureCellDataList, gridPos);
        }

        /// <summary>
        /// 移除顶层格子
        /// </summary>
        public void RemoveTopCell(Vector2Int gridPos)
        {
            if (FindStructureCell(gridPos) != null)
            {
                RemoveStructureCell(gridPos);
                return;
            }

            RemoveFloorCell(gridPos);
        }

        /// <summary>
        /// 查找最近墙体笔刷
        /// </summary>
        public PaintedBuildingBrushPreset FindNearestWallBrushPreset(Vector2Int gridPos)
        {
            PaintedBuildingBrushPreset nearestBrushPreset = null;
            int nearestDistance = int.MaxValue;
            foreach (var cellData in structureCellDataList)
            {
                if (cellData == null
                    || cellData.cellType != EPaintedBuildingCellType.Wall
                    || cellData.brushPreset == null)
                {
                    continue;
                }

                int deltaX = Mathf.Abs(cellData.gridPos.x - gridPos.x);
                int deltaZ = Mathf.Abs(cellData.gridPos.y - gridPos.y);
                if (deltaX > 0 && deltaZ > 0)
                    continue;

                int distance = deltaX + deltaZ;
                if (distance >= nearestDistance)
                    continue;

                nearestDistance = distance;
                nearestBrushPreset = cellData.brushPreset;
            }

            return nearestBrushPreset;
        }

        /// <summary>
        /// 查找格子
        /// </summary>
        private PaintedBuildingCellData FindCell(List<PaintedBuildingCellData> cellDataList, Vector2Int gridPos)
        {
            foreach (var cellData in cellDataList)
            {
                if (cellData != null && cellData.gridPos == gridPos)
                    return cellData;
            }

            return null;
        }

        /// <summary>
        /// 移除格子
        /// </summary>
        private void RemoveCell(List<PaintedBuildingCellData> cellDataList, Vector2Int gridPos)
        {
            for (int i = cellDataList.Count - 1; i >= 0; i--)
            {
                var cellData = cellDataList[i];
                if (cellData != null && cellData.gridPos == gridPos)
                    cellDataList.RemoveAt(i);
            }
        }
    }
}
