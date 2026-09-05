using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public static class BuildingRoomGridDoorUtility
    {
        /// <summary>
        /// 解析阵列房间门参数
        /// </summary>
        public static void ResolveGridRoomDoor(
            ERoomGridDoorMode doorMode,
            int rowIndex,
            int columnIndex,
            int rowCount,
            int columnCount,
            int roomWidthGridCount,
            int roomDepthGridCount,
            ERoomDoorWallSide baseDoorWallSide,
            int baseDoorOffsetGridCount,
            int roomDoorWidthGridCount,
            int doorRandomSeed,
            out ERoomDoorWallSide doorWallSide,
            out int doorOffsetGridCount)
        {
            doorWallSide = baseDoorWallSide;
            doorOffsetGridCount = Mathf.Max(0, baseDoorOffsetGridCount);

            switch (doorMode)
            {
                case ERoomGridDoorMode.Same:
                    return;
                case ERoomGridDoorMode.Symmetric:
                    ResolveSymmetricGridRoomDoor(
                        rowIndex,
                        columnIndex,
                        rowCount,
                        columnCount,
                        roomWidthGridCount,
                        roomDepthGridCount,
                        baseDoorWallSide,
                        baseDoorOffsetGridCount,
                        roomDoorWidthGridCount,
                        out doorWallSide,
                        out doorOffsetGridCount);
                    return;
                case ERoomGridDoorMode.Random:
                    ResolveRandomGridRoomDoor(
                        rowIndex,
                        columnIndex,
                        roomWidthGridCount,
                        roomDepthGridCount,
                        roomDoorWidthGridCount,
                        doorRandomSeed,
                        out doorWallSide,
                        out doorOffsetGridCount);
                    return;
            }
        }

        /// <summary>
        /// 解析对称门参数
        /// </summary>
        private static void ResolveSymmetricGridRoomDoor(
            int rowIndex,
            int columnIndex,
            int rowCount,
            int columnCount,
            int roomWidthGridCount,
            int roomDepthGridCount,
            ERoomDoorWallSide baseDoorWallSide,
            int baseDoorOffsetGridCount,
            int roomDoorWidthGridCount,
            out ERoomDoorWallSide doorWallSide,
            out int doorOffsetGridCount)
        {
            bool mirrorColumn = columnCount > 1 && columnIndex > (columnCount - 1) / 2;
            bool mirrorRow = rowCount > 1 && rowIndex > (rowCount - 1) / 2;
            doorWallSide = baseDoorWallSide;

            if (mirrorColumn)
                doorWallSide = MirrorHorizontal(doorWallSide);

            if (mirrorRow)
                doorWallSide = MirrorVertical(doorWallSide);

            doorOffsetGridCount = baseDoorOffsetGridCount;
            if (IsHorizontalWall(doorWallSide) && mirrorColumn)
            {
                doorOffsetGridCount = MirrorOffsetOnWall(
                    baseDoorOffsetGridCount,
                    roomWidthGridCount,
                    roomDoorWidthGridCount);
                return;
            }

            if (IsVerticalWall(doorWallSide) && mirrorRow)
            {
                doorOffsetGridCount = MirrorOffsetOnWall(
                    baseDoorOffsetGridCount,
                    roomDepthGridCount,
                    roomDoorWidthGridCount);
            }
        }

        /// <summary>
        /// 解析随机门参数
        /// </summary>
        private static void ResolveRandomGridRoomDoor(
            int rowIndex,
            int columnIndex,
            int roomWidthGridCount,
            int roomDepthGridCount,
            int roomDoorWidthGridCount,
            int doorRandomSeed,
            out ERoomDoorWallSide doorWallSide,
            out int doorOffsetGridCount)
        {
            var random = new System.Random(doorRandomSeed + rowIndex * 997 + columnIndex * 37);
            doorWallSide = (ERoomDoorWallSide)random.Next(0, 4);
            int wallLength = GetWallLengthGridCount(doorWallSide, roomWidthGridCount, roomDepthGridCount);
            int safeDoorWidth = Mathf.Max(1, roomDoorWidthGridCount);
            int maxOffset = Mathf.Max(0, wallLength - safeDoorWidth);
            doorOffsetGridCount = maxOffset > 0 ? random.Next(0, maxOffset + 1) : 0;
        }

        /// <summary>
        /// 水平镜像门方向
        /// </summary>
        private static ERoomDoorWallSide MirrorHorizontal(ERoomDoorWallSide doorWallSide)
        {
            switch (doorWallSide)
            {
                case ERoomDoorWallSide.Left:
                    return ERoomDoorWallSide.Right;
                case ERoomDoorWallSide.Right:
                    return ERoomDoorWallSide.Left;
                default:
                    return doorWallSide;
            }
        }

        /// <summary>
        /// 垂直镜像门方向
        /// </summary>
        private static ERoomDoorWallSide MirrorVertical(ERoomDoorWallSide doorWallSide)
        {
            switch (doorWallSide)
            {
                case ERoomDoorWallSide.Down:
                    return ERoomDoorWallSide.Up;
                case ERoomDoorWallSide.Up:
                    return ERoomDoorWallSide.Down;
                default:
                    return doorWallSide;
            }
        }

        /// <summary>
        /// 是否水平墙
        /// </summary>
        private static bool IsHorizontalWall(ERoomDoorWallSide doorWallSide)
        {
            return doorWallSide == ERoomDoorWallSide.Down || doorWallSide == ERoomDoorWallSide.Up;
        }

        /// <summary>
        /// 是否垂直墙
        /// </summary>
        private static bool IsVerticalWall(ERoomDoorWallSide doorWallSide)
        {
            return doorWallSide == ERoomDoorWallSide.Left || doorWallSide == ERoomDoorWallSide.Right;
        }

        /// <summary>
        /// 镜像墙上偏移
        /// </summary>
        private static int MirrorOffsetOnWall(
            int doorOffsetGridCount,
            int wallLengthGridCount,
            int roomDoorWidthGridCount)
        {
            int safeWallLength = Mathf.Max(1, wallLengthGridCount);
            int safeDoorWidth = Mathf.Max(1, roomDoorWidthGridCount);
            int safeOffset = Mathf.Clamp(doorOffsetGridCount, 0, safeWallLength - safeDoorWidth);
            return Mathf.Max(0, safeWallLength - safeOffset - safeDoorWidth);
        }

        /// <summary>
        /// 获取墙面长度
        /// </summary>
        private static int GetWallLengthGridCount(
            ERoomDoorWallSide doorWallSide,
            int roomWidthGridCount,
            int roomDepthGridCount)
        {
            return IsHorizontalWall(doorWallSide)
                ? Mathf.Max(1, roomWidthGridCount)
                : Mathf.Max(1, roomDepthGridCount);
        }
    }
}
