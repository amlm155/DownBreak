using System.Collections.Generic;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 一次放置的完整描述
    /// 为什么originGridPos = targetGridPos?因为外部调用传入的就是目标放置格,但是对于物体自己来说originPoint就是起始格 两种视角同一个变量 只是叫法不同
    /// </summary>
    public readonly struct BuilderPlacementReport
    {
        private readonly Vector3Int originGridPos;
        private readonly Vector3Int prefabGridSize;
        private readonly ECubeRotation eRotation;

        public Vector3Int OriginGridPos => originGridPos;
        public Vector3Int PrefabGridSize => prefabGridSize;
        public ECubeRotation ERotation => eRotation;
        public Quaternion CubeWorldRotation => Quaternion.Euler(0f, eRotation == ECubeRotation.Deg90 ? 90f : 0f, 0f);

        public BuilderPlacementReport(Vector3Int originGridPos, Vector3Int gridSize, ECubeRotation rotation)
        {
            this.originGridPos = originGridPos;
            prefabGridSize = gridSize;
            eRotation = rotation;
        }

        /// <summary>
        /// 由目标格 方块数据 旋转构建放置描述
        /// </summary>
        public static void CreateReport(Vector3Int targetGridPos,
                                        CubeData cubeData,
                                        ECubeRotation rotation,
                                        out BuilderPlacementReport placement)
        {
            placement = new BuilderPlacementReport(targetGridPos,
                                                cubeData.GetCubePrefabSizeInt(),
                                                rotation);
        }

        /// <summary>
        /// 克隆一个放置描述
        /// </summary>
        public static BuilderPlacementReport CloneRepoter(BuilderPlacementReport oldReport)
        {
            return new BuilderPlacementReport(oldReport.originGridPos,
                                              oldReport.prefabGridSize,
                                              oldReport.eRotation);
        }

        /// <summary>
        /// 将占格信息写入列表
        /// </summary>
        public void FillOccupiedInfoToList(List<Vector3Int> outputList)
        {
            outputList.Clear();

            for (int x = 0; x < prefabGridSize.x; x++)
            for (int y = 0; y < prefabGridSize.y; y++)
            for (int z = 0; z < prefabGridSize.z; z++)
            {
                var offset = new Vector3Int(x, y, z);
                if (eRotation == ECubeRotation.Deg90)
                    offset = new Vector3Int(offset.z, offset.y, -offset.x);
                outputList.Add(originGridPos + offset);
            }
        }

        /// <summary>
        /// 由占格列表计算方块在世界空间的AABB包围盒
        /// </summary>
        public Bounds GetBoundsFormGridPos(List<Vector3Int> gridList, float gridUnitSize)
        {
            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;

            for (int i = 0; i < gridList.Count; i++)
            {
                var gridPos = gridList[i];
                if (gridPos.x < minX) minX = gridPos.x;
                if (gridPos.y < minY) minY = gridPos.y;
                if (gridPos.z < minZ) minZ = gridPos.z;
                if (gridPos.x > maxX) maxX = gridPos.x;
                if (gridPos.y > maxY) maxY = gridPos.y;
                if (gridPos.z > maxZ) maxZ = gridPos.z;
            }

            var minWorld = new Vector3(minX, minY, minZ) * gridUnitSize;
            var maxWorld = new Vector3(maxX + 1, maxY + 1, maxZ + 1) * gridUnitSize;
            return new Bounds((minWorld + maxWorld) * 0.5f, maxWorld - minWorld);
        }

         /// <summary>
        /// 由占格列表计算方块在世界空间的AABB包围盒
        /// </summary>
        /// <param name="gridList">占格列表</param>
        /// <param name="gridUnitSize">网格单元大小</param>
        /// <returns>世界空间中心点</returns>
        public Vector3 GetWorldCenterFormGridList(List<Vector3Int> gridList, float gridUnitSize)
        {
            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue, maxZ = int.MinValue;

            for (int i = 0; i < gridList.Count; i++)
            {
                var gridPos = gridList[i];
                if (gridPos.x < minX) minX = gridPos.x;
                if (gridPos.y < minY) minY = gridPos.y;
                if (gridPos.z < minZ) minZ = gridPos.z;
                if (gridPos.x > maxX) maxX = gridPos.x;
                if (gridPos.y > maxY) maxY = gridPos.y;
                if (gridPos.z > maxZ) maxZ = gridPos.z;
            }

            var minWorld = new Vector3(minX, minY, minZ) * gridUnitSize;
            var maxWorld = new Vector3(maxX + 1, maxY + 1, maxZ + 1) * gridUnitSize;
            return (minWorld + maxWorld) * 0.5f;
        }

        /// <summary>
        /// 由占格列表计算底部中心点
        /// </summary>
        public Vector3 GetWorldBottomCenterFromGridList(List<Vector3Int> gridList, float gridUnitSize)
        {
            int minX = int.MaxValue, minY = int.MaxValue, minZ = int.MaxValue;
            int maxX = int.MinValue, maxZ = int.MinValue;

            for (int i = 0; i < gridList.Count; i++)
            {
                var gridPos = gridList[i];
                if (gridPos.x < minX) minX = gridPos.x;
                if (gridPos.y < minY) minY = gridPos.y;
                if (gridPos.z < minZ) minZ = gridPos.z;
                if (gridPos.x > maxX) maxX = gridPos.x;
                if (gridPos.z > maxZ) maxZ = gridPos.z;
            }

            float minWorldX = minX * gridUnitSize;
            float minWorldY = minY * gridUnitSize;
            float minWorldZ = minZ * gridUnitSize;
            float maxWorldX = (maxX + 1) * gridUnitSize;
            float maxWorldZ = (maxZ + 1) * gridUnitSize;
            return new Vector3(
                (minWorldX + maxWorldX) * 0.5f,
                minWorldY,
                (minWorldZ + maxWorldZ) * 0.5f);
        }


    }
}
