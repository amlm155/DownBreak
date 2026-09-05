using System.Collections.Generic;
using UnityEngine;
using Mm_Budier;

namespace Mm_ProceduralBuilding
{
    public static class InteriorFurniturePlacementUtility
    {
        /// <summary>
        /// 获取旋转后的占用尺寸
        /// </summary>
        public static Vector2Int GetRotatedFootprintGridSize(
            InteriorFurnitureBrushPreset brushPreset,
            EInteriorFurnitureRotation rotation)
        {
            if (brushPreset == null)
                return Vector2Int.one;

            Vector2Int footprintGridSize = brushPreset.FootprintGridSize;
            if (rotation == EInteriorFurnitureRotation.Deg90
                || rotation == EInteriorFurnitureRotation.Deg270)
            {
                return new Vector2Int(footprintGridSize.y, footprintGridSize.x);
            }

            return footprintGridSize;
        }

        /// <summary>
        /// 获取局部旋转格坐标
        /// </summary>
        public static Vector2Int GetRotatedLocalGridPos(
            Vector2Int localGridPos,
            Vector2Int originalFootprintGridSize,
            EInteriorFurnitureRotation rotation)
        {
            int widthGridCount = originalFootprintGridSize.x;
            int depthGridCount = originalFootprintGridSize.y;
            switch (rotation)
            {
                case EInteriorFurnitureRotation.Deg90:
                    return new Vector2Int(localGridPos.y, widthGridCount - 1 - localGridPos.x);
                case EInteriorFurnitureRotation.Deg180:
                    return new Vector2Int(widthGridCount - 1 - localGridPos.x, depthGridCount - 1 - localGridPos.y);
                case EInteriorFurnitureRotation.Deg270:
                    return new Vector2Int(depthGridCount - 1 - localGridPos.y, localGridPos.x);
                default:
                    return localGridPos;
            }
        }

        /// <summary>
        /// 获取占用格坐标列表
        /// </summary>
        public static void FillOccupiedGridPosList(
            InteriorFurnitureBrushPreset brushPreset,
            Vector2Int anchorGridPos,
            EInteriorFurnitureRotation rotation,
            List<Vector2Int> outputGridPosList)
        {
            outputGridPosList.Clear();
            if (brushPreset == null)
                return;

            Vector2Int originalFootprintGridSize = brushPreset.FootprintGridSize;
            for (int x = 0; x < originalFootprintGridSize.x; x++)
            {
                for (int z = 0; z < originalFootprintGridSize.y; z++)
                {
                    var localGridPos = GetRotatedLocalGridPos(
                        new Vector2Int(x, z),
                        originalFootprintGridSize,
                        rotation);
                    outputGridPosList.Add(anchorGridPos + localGridPos);
                }
            }
        }

        /// <summary>
        /// 判断布局是否占用目标格
        /// </summary>
        public static bool ContainsGridPos(
            InteriorFurniturePlacementData placementData,
            Vector2Int targetGridPos)
        {
            if (placementData == null || placementData.brushPreset == null)
                return false;

            Vector2Int originalFootprintGridSize = placementData.brushPreset.FootprintGridSize;
            for (int x = 0; x < originalFootprintGridSize.x; x++)
            {
                for (int z = 0; z < originalFootprintGridSize.y; z++)
                {
                    var localGridPos = GetRotatedLocalGridPos(
                        new Vector2Int(x, z),
                        originalFootprintGridSize,
                        placementData.rotation);
                    if (placementData.anchorGridPos + localGridPos == targetGridPos)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取朝向格方向
        /// </summary>
        public static Vector2Int GetForwardGridDirection(EInteriorFurnitureRotation rotation)
        {
            switch (rotation)
            {
                case EInteriorFurnitureRotation.Deg90:
                    return Vector2Int.right;
                case EInteriorFurnitureRotation.Deg180:
                    return Vector2Int.down;
                case EInteriorFurnitureRotation.Deg270:
                    return Vector2Int.left;
                default:
                    return Vector2Int.up;
            }
        }

        /// <summary>
        /// 获取世界旋转
        /// </summary>
        public static Quaternion GetWorldRotation(
            InteriorFurnitureBrushPreset brushPreset,
            EInteriorFurnitureRotation rotation)
        {
            int rotationStep = (int)rotation;
            if (brushPreset != null)
                rotationStep += (int)brushPreset.prefabRotationOffset;

            rotationStep %= 4;
            return Quaternion.Euler(0f, rotationStep * 90f, 0f);
        }

        /// <summary>
        /// 获取已保存布局的世界旋转
        /// </summary>
        public static Quaternion GetWorldRotation(InteriorFurniturePlacementData placementData)
        {
            if (placementData == null)
                return Quaternion.identity;

            int rotationStep = (int)placementData.rotation + (int)placementData.prefabRotationOffset;
            rotationStep %= 4;
            return Quaternion.Euler(0f, rotationStep * 90f, 0f);
        }

        /// <summary>
        /// 获取家具世界位置
        /// </summary>
        public static Vector3 GetWorldPosition(
            PaintedBuildingPlan paintedBuildingPlan,
            BuildingGridConvention convention,
            InteriorFurniturePlacementData placementData)
        {
            if (paintedBuildingPlan == null
                || convention == null
                || placementData == null
                || placementData.brushPreset == null)
            {
                return Vector3.zero;
            }

            var brushPreset = placementData.brushPreset;
            Vector2Int footprintGridSize = GetRotatedFootprintGridSize(brushPreset, placementData.rotation);
            int floorBaseY = paintedBuildingPlan.GetFloorBaseY(placementData.floorIndex) + 1;
            float unitSize = convention.GridUnitSize;
            Vector3 bottomCenter = convention.worldOrigin + new Vector3(
                (placementData.anchorGridPos.x + footprintGridSize.x * 0.5f) * unitSize,
                floorBaseY * unitSize,
                (placementData.anchorGridPos.y + footprintGridSize.y * 0.5f) * unitSize);
            Quaternion worldRotation = GetWorldRotation(placementData);
            var prefab = brushPreset.furniturePrefab;
            var placementAnchor = prefab == null
                ? null
                : prefab.GetComponent<BuilderPlacementAnchor>();

            if (placementAnchor == null || placementAnchor.anchorMode == EBuilderPlacementAnchorMode.Bottom)
                return bottomCenter;

            if (placementAnchor.anchorMode == EBuilderPlacementAnchorMode.Center)
            {
                return bottomCenter + Vector3.up * (brushPreset.HeightGridCount * unitSize * 0.5f);
            }

            var localAnchorPosition = placementAnchor.GetCustomAnchorLocalPosition();
            var scaledAnchorPosition = Vector3.Scale(
                localAnchorPosition,
                prefab.transform.localScale);
            return bottomCenter - worldRotation * scaledAnchorPosition;
        }
    }
}
