using System.Collections.Generic;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public static class BuildingPerimeterWallUtility
    {
        private static readonly Vector2Int[] NeighborOffsetList =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        /// <summary>
        /// 计算圈墙格子
        /// </summary>
        public static HashSet<Vector2Int> CalculateWallGridPosHashList(
            HashSet<Vector2Int> floorGridPosHashList,
            int thicknessGridCount,
            EWallExtendDirection extendDirection)
        {
            var wallGridPosHashList = new HashSet<Vector2Int>();
            if (floorGridPosHashList == null || floorGridPosHashList.Count == 0)
                return wallGridPosHashList;

            int safeThickness = Mathf.Max(1, thicknessGridCount);
            var edgeFloorGridPosHashList = CollectEdgeFloorGridPosHashList(floorGridPosHashList);
            foreach (var gridPos in edgeFloorGridPosHashList)
                wallGridPosHashList.Add(gridPos);

            int extraLayerCount = safeThickness - 1;
            for (int layerIndex = 0; layerIndex < extraLayerCount; layerIndex++)
            {
                var nextLayerGridPosHashList = extendDirection == EWallExtendDirection.Outward
                    ? CollectOutwardLayerGridPosHashList(floorGridPosHashList, wallGridPosHashList)
                    : CollectInwardLayerGridPosHashList(floorGridPosHashList, wallGridPosHashList);

                foreach (var gridPos in nextLayerGridPosHashList)
                    wallGridPosHashList.Add(gridPos);
            }

            return wallGridPosHashList;
        }

        /// <summary>
        /// 收集地面边缘格子
        /// </summary>
        private static HashSet<Vector2Int> CollectEdgeFloorGridPosHashList(HashSet<Vector2Int> floorGridPosHashList)
        {
            var edgeFloorGridPosHashList = new HashSet<Vector2Int>();
            foreach (var gridPos in floorGridPosHashList)
            {
                if (!IsEdgeFloorGridPos(floorGridPosHashList, gridPos))
                    continue;

                edgeFloorGridPosHashList.Add(gridPos);
            }

            return edgeFloorGridPosHashList;
        }

        /// <summary>
        /// 是否地面边缘格子
        /// </summary>
        private static bool IsEdgeFloorGridPos(HashSet<Vector2Int> floorGridPosHashList, Vector2Int gridPos)
        {
            foreach (var offset in NeighborOffsetList)
            {
                if (!floorGridPosHashList.Contains(gridPos + offset))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 收集向外延伸层
        /// </summary>
        private static HashSet<Vector2Int> CollectOutwardLayerGridPosHashList(
            HashSet<Vector2Int> floorGridPosHashList,
            HashSet<Vector2Int> wallGridPosHashList)
        {
            var nextLayerGridPosHashList = new HashSet<Vector2Int>();
            foreach (var gridPos in wallGridPosHashList)
            {
                foreach (var offset in NeighborOffsetList)
                {
                    var neighborGridPos = gridPos + offset;
                    if (floorGridPosHashList.Contains(neighborGridPos))
                        continue;

                    if (wallGridPosHashList.Contains(neighborGridPos))
                        continue;

                    nextLayerGridPosHashList.Add(neighborGridPos);
                }
            }

            return nextLayerGridPosHashList;
        }

        /// <summary>
        /// 收集向内延伸层
        /// </summary>
        private static HashSet<Vector2Int> CollectInwardLayerGridPosHashList(
            HashSet<Vector2Int> floorGridPosHashList,
            HashSet<Vector2Int> wallGridPosHashList)
        {
            var nextLayerGridPosHashList = new HashSet<Vector2Int>();
            foreach (var gridPos in wallGridPosHashList)
            {
                foreach (var offset in NeighborOffsetList)
                {
                    var neighborGridPos = gridPos + offset;
                    if (!floorGridPosHashList.Contains(neighborGridPos))
                        continue;

                    if (wallGridPosHashList.Contains(neighborGridPos))
                        continue;

                    nextLayerGridPosHashList.Add(neighborGridPos);
                }
            }

            return nextLayerGridPosHashList;
        }
    }
}
