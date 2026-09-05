using System.Collections.Generic;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public static class BuildingRoomDoorUtility
    {
        /// <summary>
        /// 收集房间门洞格坐标
        /// </summary>
        public static List<Vector2Int> CollectRoomDoorGridPosList(
            HashSet<Vector2Int> wallGridPosHashList,
            Vector2Int anchorGridPos,
            int widthGridCount,
            int depthGridCount,
            ERoomDoorWallSide doorWallSide,
            int doorOffsetGridCount,
            int doorWidthGridCount)
        {
            TryCollectRoomDoorGridPosList(
                wallGridPosHashList,
                anchorGridPos,
                widthGridCount,
                depthGridCount,
                doorWallSide,
                doorOffsetGridCount,
                doorWidthGridCount,
                out _,
                out List<Vector2Int> doorGridPosList);
            return doorGridPosList;
        }

        /// <summary>
        /// 收集避开墙交叉点的门洞格
        /// </summary>
        public static bool TryCollectRoomDoorGridPosList(
            HashSet<Vector2Int> wallGridPosHashList,
            Vector2Int anchorGridPos,
            int widthGridCount,
            int depthGridCount,
            ERoomDoorWallSide doorWallSide,
            int preferredDoorOffsetGridCount,
            int doorWidthGridCount,
            out int resolvedDoorOffsetGridCount,
            out List<Vector2Int> doorGridPosList)
        {
            resolvedDoorOffsetGridCount = Mathf.Max(0, preferredDoorOffsetGridCount);
            doorGridPosList = new List<Vector2Int>();
            if (wallGridPosHashList == null || wallGridPosHashList.Count == 0)
                return false;

            int wallLengthGridCount = GetWallLengthGridCount(doorWallSide, widthGridCount, depthGridCount);
            int safeDoorWidth = Mathf.Max(1, doorWidthGridCount);
            int maxOffsetGridCount = Mathf.Max(0, wallLengthGridCount - safeDoorWidth);
            int clampedPreferredOffset = Mathf.Clamp(preferredDoorOffsetGridCount, 0, maxOffsetGridCount);

            if (TryCollectAtOffset(clampedPreferredOffset, out List<Vector2Int> preferredDoorGridPosList))
            {
                resolvedDoorOffsetGridCount = clampedPreferredOffset;
                doorGridPosList = preferredDoorGridPosList;
                return true;
            }

            for (int delta = 1; delta <= maxOffsetGridCount; delta++)
            {
                int forwardOffset = clampedPreferredOffset + delta;
                if (forwardOffset <= maxOffsetGridCount
                    && TryCollectAtOffset(forwardOffset, out List<Vector2Int> forwardDoorGridPosList))
                {
                    resolvedDoorOffsetGridCount = forwardOffset;
                    doorGridPosList = forwardDoorGridPosList;
                    return true;
                }

                int backwardOffset = clampedPreferredOffset - delta;
                if (backwardOffset >= 0
                    && TryCollectAtOffset(backwardOffset, out List<Vector2Int> backwardDoorGridPosList))
                {
                    resolvedDoorOffsetGridCount = backwardOffset;
                    doorGridPosList = backwardDoorGridPosList;
                    return true;
                }
            }

            doorGridPosList = new List<Vector2Int>();
            return false;

            bool TryCollectAtOffset(int doorOffsetGridCount, out List<Vector2Int> candidateDoorGridPosList)
            {
                candidateDoorGridPosList = CollectDoorGridPosListAtOffset(
                    wallGridPosHashList,
                    anchorGridPos,
                    widthGridCount,
                    depthGridCount,
                    doorWallSide,
                    doorOffsetGridCount,
                    safeDoorWidth);
                if (candidateDoorGridPosList.Count < safeDoorWidth)
                    return false;

                foreach (var gridPos in candidateDoorGridPosList)
                {
                    if (IsWallIntersectionCell(gridPos, wallGridPosHashList))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// 是否墙交叉点
        /// </summary>
        public static bool IsWallIntersectionCell(Vector2Int gridPos, HashSet<Vector2Int> wallGridPosHashList)
        {
            if (wallGridPosHashList == null || !wallGridPosHashList.Contains(gridPos))
                return false;

            bool hasHorizontalNeighbor = wallGridPosHashList.Contains(new Vector2Int(gridPos.x - 1, gridPos.y))
                || wallGridPosHashList.Contains(new Vector2Int(gridPos.x + 1, gridPos.y));
            bool hasVerticalNeighbor = wallGridPosHashList.Contains(new Vector2Int(gridPos.x, gridPos.y - 1))
                || wallGridPosHashList.Contains(new Vector2Int(gridPos.x, gridPos.y + 1));
            return hasHorizontalNeighbor && hasVerticalNeighbor;
        }

        /// <summary>
        /// 按偏移收集门洞格
        /// </summary>
        private static List<Vector2Int> CollectDoorGridPosListAtOffset(
            HashSet<Vector2Int> wallGridPosHashList,
            Vector2Int anchorGridPos,
            int widthGridCount,
            int depthGridCount,
            ERoomDoorWallSide doorWallSide,
            int doorOffsetGridCount,
            int doorWidthGridCount)
        {
            var doorGridPosList = new List<Vector2Int>();
            if (!TryGetDoorStartGridPos(
                    anchorGridPos,
                    widthGridCount,
                    depthGridCount,
                    doorWallSide,
                    doorOffsetGridCount,
                    out Vector2Int startGridPos))
                return doorGridPosList;

            int safeDoorWidth = Mathf.Max(1, doorWidthGridCount);
            int minX = anchorGridPos.x;
            int maxX = anchorGridPos.x + widthGridCount - 1;
            int minZ = anchorGridPos.y;
            int maxZ = anchorGridPos.y + depthGridCount - 1;

            switch (doorWallSide)
            {
                case ERoomDoorWallSide.Down:
                case ERoomDoorWallSide.Up:
                    for (int index = 0; index < safeDoorWidth; index++)
                    {
                        int x = startGridPos.x + index;
                        if (x > maxX)
                            break;

                        var gridPos = new Vector2Int(x, startGridPos.y);
                        if (wallGridPosHashList.Contains(gridPos))
                            doorGridPosList.Add(gridPos);
                    }

                    break;
                case ERoomDoorWallSide.Left:
                case ERoomDoorWallSide.Right:
                    for (int index = 0; index < safeDoorWidth; index++)
                    {
                        int z = startGridPos.y + index;
                        if (z > maxZ)
                            break;

                        var gridPos = new Vector2Int(startGridPos.x, z);
                        if (wallGridPosHashList.Contains(gridPos))
                            doorGridPosList.Add(gridPos);
                    }

                    break;
            }

            if (doorGridPosList.Count > 0)
                return doorGridPosList;

            return CollectFallbackDoorGridPosList(
                wallGridPosHashList,
                minX,
                maxX,
                minZ,
                maxZ,
                doorWallSide,
                safeDoorWidth);
        }

        /// <summary>
        /// 获取墙面长度
        /// </summary>
        private static int GetWallLengthGridCount(
            ERoomDoorWallSide doorWallSide,
            int widthGridCount,
            int depthGridCount)
        {
            return doorWallSide == ERoomDoorWallSide.Down || doorWallSide == ERoomDoorWallSide.Up
                ? Mathf.Max(1, widthGridCount)
                : Mathf.Max(1, depthGridCount);
        }

        /// <summary>
        /// 获取门洞起点
        /// </summary>
        private static bool TryGetDoorStartGridPos(
            Vector2Int anchorGridPos,
            int widthGridCount,
            int depthGridCount,
            ERoomDoorWallSide doorWallSide,
            int doorOffsetGridCount,
            out Vector2Int startGridPos)
        {
            int minX = anchorGridPos.x;
            int maxX = anchorGridPos.x + widthGridCount - 1;
            int minZ = anchorGridPos.y;
            int maxZ = anchorGridPos.y + depthGridCount - 1;
            int safeOffset = Mathf.Max(0, doorOffsetGridCount);
            startGridPos = anchorGridPos;

            switch (doorWallSide)
            {
                case ERoomDoorWallSide.Down:
                    startGridPos = new Vector2Int(Mathf.Clamp(minX + safeOffset, minX, maxX), minZ);
                    return true;
                case ERoomDoorWallSide.Up:
                    startGridPos = new Vector2Int(Mathf.Clamp(minX + safeOffset, minX, maxX), maxZ);
                    return true;
                case ERoomDoorWallSide.Left:
                    startGridPos = new Vector2Int(minX, Mathf.Clamp(minZ + safeOffset, minZ, maxZ));
                    return true;
                case ERoomDoorWallSide.Right:
                    startGridPos = new Vector2Int(maxX, Mathf.Clamp(minZ + safeOffset, minZ, maxZ));
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 收集备用门洞格
        /// </summary>
        private static List<Vector2Int> CollectFallbackDoorGridPosList(
            HashSet<Vector2Int> wallGridPosHashList,
            int minX,
            int maxX,
            int minZ,
            int maxZ,
            ERoomDoorWallSide doorWallSide,
            int doorWidthGridCount)
        {
            var doorGridPosList = new List<Vector2Int>();
            switch (doorWallSide)
            {
                case ERoomDoorWallSide.Down:
                case ERoomDoorWallSide.Up:
                    int fixedZ = doorWallSide == ERoomDoorWallSide.Down ? minZ : maxZ;
                    for (int x = minX; x <= maxX; x++)
                    {
                        var gridPos = new Vector2Int(x, fixedZ);
                        if (!wallGridPosHashList.Contains(gridPos))
                            continue;

                        doorGridPosList.Add(gridPos);
                        if (doorGridPosList.Count >= doorWidthGridCount)
                            return doorGridPosList;
                    }

                    break;
                case ERoomDoorWallSide.Left:
                case ERoomDoorWallSide.Right:
                    int fixedX = doorWallSide == ERoomDoorWallSide.Left ? minX : maxX;
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        var gridPos = new Vector2Int(fixedX, z);
                        if (!wallGridPosHashList.Contains(gridPos))
                            continue;

                        doorGridPosList.Add(gridPos);
                        if (doorGridPosList.Count >= doorWidthGridCount)
                            return doorGridPosList;
                    }

                    break;
            }

            return doorGridPosList;
        }
    }
}
