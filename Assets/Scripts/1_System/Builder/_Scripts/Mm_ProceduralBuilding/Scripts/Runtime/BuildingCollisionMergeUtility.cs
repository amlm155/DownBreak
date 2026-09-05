using System.Collections.Generic;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public static class BuildingCollisionMergeUtility
    {
        private const string CollisionRootName = "Collision";
        private const string FloorCollisionName = "FloorCollision";
        private const string StructureCollisionName = "StructureCollision";
        private const string PlacementBlockerLayerName = "Cube";

        /// <summary>
        /// 从蓝图合并碰撞体
        /// </summary>
        public static int MergeFromPlan(
            Transform generatedRoot,
            PaintedBuildingPlan paintedPlan,
            BuildingGridConvention convention)
        {
            if (generatedRoot == null || paintedPlan == null || convention == null)
                return 0;

            ClearCollisionRoot(generatedRoot);
            RemoveVisualColliders(generatedRoot);

            var collisionRoot = BuildingPrimitiveFactory.CreateGroup(CollisionRootName, generatedRoot);
            int colliderCount = 0;

            foreach (var floorData in paintedPlan.paintFloorDataList)
            {
                if (floorData == null)
                    continue;

                int baseY = paintedPlan.GetFloorBaseY(floorData.floorIndex);
                int globalWallHeightGridCount = paintedPlan.GlobalWallHeightGridCount;
                var floorCollisionRoot = BuildingPrimitiveFactory.CreateGroup(
                    $"Floor_{floorData.floorIndex}",
                    collisionRoot);
                var floorLayerRoot = BuildingPrimitiveFactory.CreateGroup(FloorCollisionName, floorCollisionRoot);
                var structureLayerRoot = BuildingPrimitiveFactory.CreateGroup(StructureCollisionName, floorCollisionRoot);

                colliderCount += CreateMergedBoxes(
                    floorLayerRoot,
                    convention,
                    BuildFloorVoxelHashList(floorData, baseY));
                colliderCount += CreateMergedBoxes(
                    structureLayerRoot,
                    convention,
                    BuildStructureVoxelHashList(floorData, baseY, globalWallHeightGridCount));
            }

            return colliderCount;
        }

        /// <summary>
        /// 清理碰撞根节点
        /// </summary>
        private static void ClearCollisionRoot(Transform generatedRoot)
        {
            var collisionRoot = generatedRoot.Find(CollisionRootName);
            if (collisionRoot == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(collisionRoot.gameObject);
            else
                Object.DestroyImmediate(collisionRoot.gameObject);
        }

        /// <summary>
        /// 移除可视格子碰撞体
        /// </summary>
        private static void RemoveVisualColliders(Transform generatedRoot)
        {
            foreach (var collider in generatedRoot.GetComponentsInChildren<Collider>())
            {
                if (collider == null)
                    continue;

                var layerTransform = collider.transform;
                if (IsUnderCollisionRoot(layerTransform))
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(collider);
                else
                    Object.DestroyImmediate(collider);
            }
        }

        /// <summary>
        /// 是否在碰撞根节点下
        /// </summary>
        private static bool IsUnderCollisionRoot(Transform transform)
        {
            var current = transform;
            while (current != null)
            {
                if (current.name == CollisionRootName)
                    return true;

                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// 构建地面体素集合
        /// </summary>
        private static HashSet<Vector3Int> BuildFloorVoxelHashList(PaintedBuildingFloorData floorData, int baseY)
        {
            var voxelHashList = new HashSet<Vector3Int>();
            foreach (var cellData in floorData.floorCellDataList)
            {
                if (cellData == null)
                    continue;

                voxelHashList.Add(new Vector3Int(cellData.gridPos.x, baseY, cellData.gridPos.y));
            }

            return voxelHashList;
        }

        /// <summary>
        /// 构建结构体素集合
        /// </summary>
        private static HashSet<Vector3Int> BuildStructureVoxelHashList(
            PaintedBuildingFloorData floorData,
            int baseY,
            int globalWallHeightGridCount)
        {
            var voxelHashList = new HashSet<Vector3Int>();
            foreach (var cellData in floorData.structureCellDataList)
            {
                if (cellData == null)
                    continue;

                switch (cellData.cellType)
                {
                    case EPaintedBuildingCellType.Wall:
                        AddSolidColumnVoxelList(
                            voxelHashList,
                            cellData.gridPos,
                            baseY + 1,
                            globalWallHeightGridCount);
                        break;
                    case EPaintedBuildingCellType.Cutout:
                        AddCutoutColumnVoxelList(voxelHashList, cellData, baseY, globalWallHeightGridCount);
                        break;
                }
            }

            return voxelHashList;
        }

        /// <summary>
        /// 添加实心柱体素
        /// </summary>
        private static void AddSolidColumnVoxelList(
            HashSet<Vector3Int> voxelHashList,
            Vector2Int gridPos,
            int originY,
            int heightGridCount)
        {
            for (int y = 0; y < heightGridCount; y++)
                voxelHashList.Add(new Vector3Int(gridPos.x, originY + y, gridPos.y));
        }

        /// <summary>
        /// 添加挖空柱体素
        /// </summary>
        private static void AddCutoutColumnVoxelList(
            HashSet<Vector3Int> voxelHashList,
            PaintedBuildingCellData cellData,
            int baseY,
            int globalWallHeightGridCount)
        {
            int totalHeight = globalWallHeightGridCount;
            int cutoutStart = Mathf.Clamp(cellData.cutoutStartHeightGridCount, 0, totalHeight - 1);
            int cutoutEnd = Mathf.Clamp(cellData.cutoutEndHeightGridCount, cutoutStart + 1, totalHeight);
            int originY = baseY + 1;

            if (cutoutStart > 0)
            {
                AddSolidColumnVoxelList(
                    voxelHashList,
                    cellData.gridPos,
                    originY,
                    cutoutStart);
            }

            int upperHeight = totalHeight - cutoutEnd;
            if (upperHeight > 0)
            {
                AddSolidColumnVoxelList(
                    voxelHashList,
                    cellData.gridPos,
                    originY + cutoutEnd,
                    upperHeight);
            }
        }

        /// <summary>
        /// 创建合并后的碰撞盒
        /// </summary>
        private static int CreateMergedBoxes(
            Transform parent,
            BuildingGridConvention convention,
            HashSet<Vector3Int> voxelHashList)
        {
            var mergedBoxList = BuildingVoxelGreedyMergeUtility.GreedyMerge(voxelHashList);
            int colliderCount = 0;

            foreach (var gridBox in mergedBoxList)
            {
                CreateColliderBox(parent, convention, gridBox);
                colliderCount++;
            }

            return colliderCount;
        }

        /// <summary>
        /// 创建碰撞盒
        /// </summary>
        private static void CreateColliderBox(
            Transform parent,
            BuildingGridConvention convention,
            BuildingGridBox gridBox)
        {
            var colliderObject = new GameObject(
                $"Box_{gridBox.originGridPos.x}_{gridBox.originGridPos.y}_{gridBox.originGridPos.z}");
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.position = convention.GridBoxToWorldCenter(gridBox.originGridPos, gridBox.gridSize);
            colliderObject.transform.rotation = Quaternion.identity;
            colliderObject.transform.localScale = convention.GridSizeToWorldSize(gridBox.gridSize);

            // 只有结构碰撞体阻挡建造 地面碰撞体不参与放置校验
            if (parent.name == StructureCollisionName)
            {
                int placementBlockerLayer = LayerMask.NameToLayer(PlacementBlockerLayerName);
                if (placementBlockerLayer >= 0)
                    colliderObject.layer = placementBlockerLayer;
            }

            colliderObject.AddComponent<BoxCollider>();
        }
    }
}
