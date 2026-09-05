using System.Collections.Generic;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public class BuildingSingleRoomConfig
    {
        /// <summary>
        /// 锚点格坐标
        /// </summary>
        public Vector2Int anchorGridPos;

        /// <summary>
        /// 宽度格数
        /// </summary>
        public int widthGridCount = 6;

        /// <summary>
        /// 深度格数
        /// </summary>
        public int depthGridCount = 6;

        /// <summary>
        /// 墙体厚度格数
        /// </summary>
        public int wallThicknessGridCount = 1;

        /// <summary>
        /// 墙体高度格数
        /// </summary>
        public int wallHeightGridCount = 3;

        /// <summary>
        /// 墙体延伸方向
        /// </summary>
        public EWallExtendDirection wallExtendDirection = EWallExtendDirection.Outward;

        /// <summary>
        /// 是否启用门洞
        /// </summary>
        public bool enableDoor = true;

        /// <summary>
        /// 门所在墙面
        /// </summary>
        public ERoomDoorWallSide doorWallSide = ERoomDoorWallSide.Down;

        /// <summary>
        /// 门沿墙偏移格数
        /// </summary>
        public int doorOffsetGridCount = 2;

        /// <summary>
        /// 房间门宽格数
        /// </summary>
        public int roomDoorWidthGridCount = 1;

        /// <summary>
        /// 挖空起点高度
        /// </summary>
        public int cutoutStartHeightGridCount;

        /// <summary>
        /// 挖空终点高度
        /// </summary>
        public int cutoutEndHeightGridCount = 2;

        /// <summary>
        /// 地面笔刷
        /// </summary>
        public PaintedBuildingBrushPreset floorBrushPreset;

        /// <summary>
        /// 墙体笔刷
        /// </summary>
        public PaintedBuildingBrushPreset wallBrushPreset;
    }

    public class BuildingRoomGridConfig
    {
        /// <summary>
        /// 锚点格坐标
        /// </summary>
        public Vector2Int anchorGridPos;

        /// <summary>
        /// 房间宽度格数
        /// </summary>
        public int roomWidthGridCount = 5;

        /// <summary>
        /// 房间深度格数
        /// </summary>
        public int roomDepthGridCount = 5;

        /// <summary>
        /// 行数
        /// </summary>
        public int rowCount = 2;

        /// <summary>
        /// 列数
        /// </summary>
        public int columnCount = 2;

        /// <summary>
        /// 房间横向邻近间隔格数
        /// </summary>
        public int roomAdjacentSpacingGridCount = 1;

        /// <summary>
        /// 走廊宽度格数
        /// </summary>
        public int corridorWidthGridCount = 1;

        /// <summary>
        /// 墙体厚度格数
        /// </summary>
        public int wallThicknessGridCount = 1;

        /// <summary>
        /// 墙体高度格数
        /// </summary>
        public int wallHeightGridCount = 3;

        /// <summary>
        /// 墙体延伸方向
        /// </summary>
        public EWallExtendDirection wallExtendDirection = EWallExtendDirection.Outward;

        /// <summary>
        /// 每个房间是否带门
        /// </summary>
        public bool enableDoorPerRoom = true;

        /// <summary>
        /// 门所在墙面
        /// </summary>
        public ERoomDoorWallSide doorWallSide = ERoomDoorWallSide.Down;

        /// <summary>
        /// 门沿墙偏移格数
        /// </summary>
        public int doorOffsetGridCount = 2;

        /// <summary>
        /// 房间门宽格数
        /// </summary>
        public int roomDoorWidthGridCount = 1;

        /// <summary>
        /// 阵列门模式
        /// </summary>
        public ERoomGridDoorMode gridDoorMode = ERoomGridDoorMode.Same;

        /// <summary>
        /// 阵列门随机种子
        /// </summary>
        public int gridDoorRandomSeed = 12345;

        /// <summary>
        /// 挖空起点高度
        /// </summary>
        public int cutoutStartHeightGridCount;

        /// <summary>
        /// 挖空终点高度
        /// </summary>
        public int cutoutEndHeightGridCount = 2;

        /// <summary>
        /// 地面笔刷
        /// </summary>
        public PaintedBuildingBrushPreset floorBrushPreset;

        /// <summary>
        /// 墙体笔刷
        /// </summary>
        public PaintedBuildingBrushPreset wallBrushPreset;
    }

    public static class BuildingRoomGenerator
    {
        /// <summary>
        /// 生成单个矩形房间
        /// </summary>
        public static int GenerateSingleRoom(PaintedBuildingPlan paintedPlan, int floorIndex, BuildingSingleRoomConfig config)
        {
            if (paintedPlan == null || config == null)
                return 0;

            int safeWidth = Mathf.Max(2, config.widthGridCount);
            int safeDepth = Mathf.Max(2, config.depthGridCount);
            var topRightGridPos = new Vector2Int(
                config.anchorGridPos.x + safeWidth - 1,
                config.anchorGridPos.y + safeDepth - 1);
            paintedPlan.FillFloorRect(
                floorIndex,
                config.anchorGridPos,
                topRightGridPos,
                config.floorBrushPreset);

            var floorGridPosHashList = BuildFloorGridPosHashList(config.anchorGridPos, safeWidth, safeDepth);
            int paintedCellCount = floorGridPosHashList.Count;

            var wallGridPosHashList = BuildingPerimeterWallUtility.CalculateWallGridPosHashList(
                floorGridPosHashList,
                config.wallThicknessGridCount,
                config.wallExtendDirection);
            paintedPlan.SetWallCells(
                floorIndex,
                wallGridPosHashList,
                config.wallHeightGridCount,
                config.wallBrushPreset);
            paintedCellCount += wallGridPosHashList.Count;

            if (!config.enableDoor)
                return paintedCellCount;

            bool hasValidDoor = BuildingRoomDoorUtility.TryCollectRoomDoorGridPosList(
                wallGridPosHashList,
                config.anchorGridPos,
                safeWidth,
                safeDepth,
                config.doorWallSide,
                config.doorOffsetGridCount,
                config.roomDoorWidthGridCount,
                out _,
                out List<Vector2Int> doorGridPosList);
            if (!hasValidDoor || doorGridPosList.Count == 0)
                return paintedCellCount;

            ApplyDoorCells(
                paintedPlan,
                floorIndex,
                config,
                doorGridPosList);

            return paintedCellCount;
        }

        /// <summary>
        /// 应用门洞格
        /// </summary>
        private static void ApplyDoorCells(
            PaintedBuildingPlan paintedPlan,
            int floorIndex,
            BuildingSingleRoomConfig config,
            List<Vector2Int> doorGridPosList)
        {
            foreach (var doorGridPos in doorGridPosList)
            {
                paintedPlan.SetCell(
                    floorIndex,
                    doorGridPos,
                    EPaintedBuildingCellType.Cutout,
                    config.wallHeightGridCount,
                    config.cutoutStartHeightGridCount,
                    config.cutoutEndHeightGridCount,
                    config.wallBrushPreset);
            }
        }

        /// <summary>
        /// 生成房间阵列
        /// </summary>
        public static int GenerateRoomGrid(PaintedBuildingPlan paintedPlan, int floorIndex, BuildingRoomGridConfig config)
        {
            if (paintedPlan == null || config == null)
                return 0;

            int safeRoomWidth = Mathf.Max(2, config.roomWidthGridCount);
            int safeRoomDepth = Mathf.Max(2, config.roomDepthGridCount);
            int safeRowCount = Mathf.Max(1, config.rowCount);
            int safeColumnCount = Mathf.Max(1, config.columnCount);
            int safeAdjacentSpacing = Mathf.Max(0, config.roomAdjacentSpacingGridCount);
            int safeCorridorWidth = Mathf.Max(1, config.corridorWidthGridCount);
            int paintedCellCount = 0;

            for (int rowIndex = 0; rowIndex < safeRowCount; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < safeColumnCount; columnIndex++)
                {
                    var roomAnchorGridPos = new Vector2Int(
                        config.anchorGridPos.x + columnIndex * (safeRoomWidth + safeAdjacentSpacing),
                        config.anchorGridPos.y + rowIndex * (safeRoomDepth + safeCorridorWidth));
                    BuildingRoomGridDoorUtility.ResolveGridRoomDoor(
                        config.gridDoorMode,
                        rowIndex,
                        columnIndex,
                        safeRowCount,
                        safeColumnCount,
                        safeRoomWidth,
                        safeRoomDepth,
                        config.doorWallSide,
                        config.doorOffsetGridCount,
                        config.roomDoorWidthGridCount,
                        config.gridDoorRandomSeed,
                        out ERoomDoorWallSide doorWallSide,
                        out int doorOffsetGridCount);
                    var singleRoomConfig = new BuildingSingleRoomConfig
                    {
                        anchorGridPos = roomAnchorGridPos,
                        widthGridCount = safeRoomWidth,
                        depthGridCount = safeRoomDepth,
                        wallThicknessGridCount = config.wallThicknessGridCount,
                        wallHeightGridCount = config.wallHeightGridCount,
                        wallExtendDirection = config.wallExtendDirection,
                        enableDoor = config.enableDoorPerRoom,
                        doorWallSide = doorWallSide,
                        doorOffsetGridCount = doorOffsetGridCount,
                        roomDoorWidthGridCount = config.roomDoorWidthGridCount,
                        cutoutStartHeightGridCount = config.cutoutStartHeightGridCount,
                        cutoutEndHeightGridCount = config.cutoutEndHeightGridCount,
                        floorBrushPreset = config.floorBrushPreset,
                        wallBrushPreset = config.wallBrushPreset
                    };
                    paintedCellCount += GenerateSingleRoom(paintedPlan, floorIndex, singleRoomConfig);
                }
            }

            return paintedCellCount;
        }

        /// <summary>
        /// 构建地面格集合
        /// </summary>
        private static HashSet<Vector2Int> BuildFloorGridPosHashList(
            Vector2Int anchorGridPos,
            int widthGridCount,
            int depthGridCount)
        {
            var floorGridPosHashList = new HashSet<Vector2Int>();
            int maxX = anchorGridPos.x + widthGridCount - 1;
            int maxZ = anchorGridPos.y + depthGridCount - 1;
            for (int x = anchorGridPos.x; x <= maxX; x++)
            {
                for (int z = anchorGridPos.y; z <= maxZ; z++)
                    floorGridPosHashList.Add(new Vector2Int(x, z));
            }

            return floorGridPosHashList;
        }
    }
}
