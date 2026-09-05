using System.Collections.Generic;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public struct BuildingGridBox
    {
        /// <summary>
        /// 原点格坐标
        /// </summary>
        public Vector3Int originGridPos;

        /// <summary>
        /// 格尺寸
        /// </summary>
        public Vector3Int gridSize;
    }

    public static class BuildingVoxelGreedyMergeUtility
    {
        /// <summary>
        /// 贪心合并体素
        /// </summary>
        public static List<BuildingGridBox> GreedyMerge(HashSet<Vector3Int> voxelHashList)
        {
            var mergedBoxList = new List<BuildingGridBox>();
            if (voxelHashList == null || voxelHashList.Count == 0)
                return mergedBoxList;

            var visitedHashList = new HashSet<Vector3Int>();
            var sortedVoxelList = new List<Vector3Int>(voxelHashList);
            sortedVoxelList.Sort(CompareVoxel);

            foreach (var voxel in sortedVoxelList)
            {
                if (visitedHashList.Contains(voxel))
                    continue;

                int width = 1;
                while (CanOccupy(voxelHashList, visitedHashList, voxel, width, 0, 0))
                    width++;

                int depth = 1;
                bool canExtendDepth = true;
                while (canExtendDepth)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!CanOccupy(voxelHashList, visitedHashList, voxel, x, 0, depth))
                        {
                            canExtendDepth = false;
                            break;
                        }
                    }

                    if (canExtendDepth)
                        depth++;
                }

                int height = 1;
                bool canExtendHeight = true;
                while (canExtendHeight)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            if (!CanOccupy(voxelHashList, visitedHashList, voxel, x, height, z))
                            {
                                canExtendHeight = false;
                                break;
                            }
                        }

                        if (!canExtendHeight)
                            break;
                    }

                    if (canExtendHeight)
                        height++;
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int z = 0; z < depth; z++)
                            visitedHashList.Add(voxel + new Vector3Int(x, y, z));
                    }
                }

                mergedBoxList.Add(new BuildingGridBox
                {
                    originGridPos = voxel,
                    gridSize = new Vector3Int(width, height, depth)
                });
            }

            return mergedBoxList;
        }

        /// <summary>
        /// 比较体素排序
        /// </summary>
        private static int CompareVoxel(Vector3Int a, Vector3Int b)
        {
            int compareY = a.y.CompareTo(b.y);
            if (compareY != 0)
                return compareY;

            int compareZ = a.z.CompareTo(b.z);
            if (compareZ != 0)
                return compareZ;

            return a.x.CompareTo(b.x);
        }

        /// <summary>
        /// 判断体素是否可占用
        /// </summary>
        private static bool CanOccupy(
            HashSet<Vector3Int> voxelHashList,
            HashSet<Vector3Int> visitedHashList,
            Vector3Int origin,
            int offsetX,
            int offsetY,
            int offsetZ)
        {
            var voxel = origin + new Vector3Int(offsetX, offsetY, offsetZ);
            return voxelHashList.Contains(voxel) && !visitedHashList.Contains(voxel);
        }
    }
}
