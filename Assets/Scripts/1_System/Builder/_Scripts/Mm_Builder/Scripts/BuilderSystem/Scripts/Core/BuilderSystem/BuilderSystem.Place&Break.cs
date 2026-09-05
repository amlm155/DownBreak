using System.Collections.Generic;
using MiMieEventBus;
using MieMieFrameWork;
using UnityEngine;

namespace Mm_Budier
{
    /// <summary>
    /// 处理放置和破坏方块
    /// </summary>
    public partial class BuilderSystem
    {

        /// <summary>
        /// 处理放置方块
        /// </summary>
        public override void HandlePlace(BuilderPlacementReport placement, CubeData cubeData)
        {
            if (!HandlePlaceValid(placement, cubeData)) return;
            CreatAndPlaceCube(placement, cubeData);
        }

        /// <summary>
        /// 处理破坏方块
        /// </summary>
        public override void HandleBreak(Vector3Int gridPos, CubeData cubeData)
        {
            if (!HandleBreakValid(gridPos, cubeData)) return;
            BreakCube(gridPos);
        }


        /// <summary>
        /// 处理放置校验
        /// </summary>
        /// <param name="placement">放置报告</param>
        /// <param name="cubeData">方块数据</param>
        /// <returns></returns>
        public override bool HandlePlaceValid(BuilderPlacementReport placement, CubeData cubeData)
        {
            // 校验区域放置是否合法
            if (!ValidPlacement(placement))
                return false;

            // 校验外部开发者配置
            if (imBuilder != null)
            {
                if (!imBuilder.CustomPlaceValid(out placement, cubeData))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 处理破坏校验
        /// </summary>
        public override bool HandleBreakValid(Vector3Int gridPos, CubeData cubeData)
        {
            if (!runtimeCubeDataDict.TryGetValue(gridPos, out var instance))
                return false;

            if (imBuilder != null)
            {
                var targetData = instance.data;
                if (!imBuilder.CustomBreakValid(out gridPos, targetData))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 校验区域放置是否合法
        /// </summary>
        private bool ValidPlacement(BuilderPlacementReport placement)
        {
            // 将占格信息写入列表
            placement.FillOccupiedInfoToList(tempOccupiedGridList);

            foreach (var gridPos in tempOccupiedGridList)
            {
                // 校验该格是否在可放置区域内
                if (!virtualGrid.ValidVirtualGroup(gridPos))
                    return false;

                // 校验该格是否被占用
                if (runtimeCubeDataDict.ContainsKey(gridPos))
                    return false;
            }

            if (!ValidVerticalStacking(tempOccupiedGridList))
                return false;

            // 物理碰撞只作为逻辑占格之外的辅助校验
            if (!ValidPlacementCollision(placement))
                return false;

            return true;
        }

        /// <summary>
        /// 校验目标下方的方块是否允许其他方块叠放
        /// </summary>
        private bool ValidVerticalStacking(List<Vector3Int> occupiedGridList)
        {
            int lowestGridY = int.MaxValue;
            for (int i = 0; i < occupiedGridList.Count; i++)
            {
                if (occupiedGridList[i].y < lowestGridY)
                    lowestGridY = occupiedGridList[i].y;
            }

            for (int i = 0; i < occupiedGridList.Count; i++)
            {
                var gridPos = occupiedGridList[i];
                if (gridPos.y != lowestGridY)
                    continue;

                if (!runtimeCubeDataDict.TryGetValue(
                        gridPos + Vector3Int.down,
                        out var supportInstance))
                    continue;

                var supportPrefab = supportInstance.data?.CubePrefab;
                var supportAnchor = supportPrefab == null
                    ? null
                    : supportPrefab.GetComponent<BuilderPlacementAnchor>();
                if (supportAnchor != null && !supportAnchor.allowVerticalStacking)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 校验放置区域是否与阻挡碰撞体重叠
        /// </summary>
        private bool ValidPlacementCollision(BuilderPlacementReport placement)
        {
            if (builderSetting == null || !builderSetting.enablePlacementCollisionCheck)
                return true;

            int blockerLayer = builderSetting.placementBlockerLayer.value;
            if (blockerLayer == 0 || placementColliderBuffer == null)
                return true;

            var bounds = placement.GetBoundsFormGridPos(
                tempOccupiedGridList,
                virtualGrid.gridUnitSize);
            float padding = Mathf.Max(0f, builderSetting.placementCollisionPadding);
            var halfExtents = bounds.extents + Vector3.one * padding;

            int hitCount = Physics.OverlapBoxNonAlloc(
                bounds.center,
                halfExtents,
                placementColliderBuffer,
                Quaternion.identity,
                blockerLayer,
                QueryTriggerInteraction.Ignore);

            return hitCount == 0;
        }


        /// <summary>
        /// 放置方块并写入运行时字典
        /// </summary>
        private void CreatAndPlaceCube(BuilderPlacementReport placement, CubeData cubeData)
        {
            // 将占格信息写入列表
            placement.FillOccupiedInfoToList(tempOccupiedGridList);

            var prefab = cubeData.CubePrefab;
            var unitSize = virtualGrid.gridUnitSize;

            // 计算预制体放置位置
            var worldCenter = GetPlacementWorldPosition(placement, cubeData, tempOccupiedGridList, unitSize);

            // 实例化方块并设置
            var spawnedObj = Instantiate(prefab, worldCenter, placement.CubeWorldRotation, cubeRoot);
            spawnedObj.layer = builderSetting.cubeLayer;

            // 创建运行时数据
            var runtimeData = new CubeInstance(cubeData, spawnedObj, placement);

            // 调用方块行为
            spawnedObj.GetComponent<CubeBehaviour>()?.OnPlaced(runtimeData);

            // 写入运行时字典
            foreach (var gridPos in tempOccupiedGridList)
                runtimeCubeDataDict.Add(gridPos, runtimeData);

            // 调用外部开发者回调
            if (imBuilder != null)
                imBuilder.CustorOnPlaceSucceeded(runtimeData);

            MmGlobalEventBus.GlobalBus.Publish(BuilderEvents.CubePlaced, runtimeData);
        }

        /// <summary>
        /// 根据预制体锚点计算放置位置
        /// </summary>
        private Vector3 GetPlacementWorldPosition(
            BuilderPlacementReport placement,
            CubeData cubeData,
            List<Vector3Int> occupiedGridList,
            float gridUnitSize)
        {
            var prefab = cubeData?.CubePrefab;
            var anchor = prefab == null
                ? null
                : prefab.GetComponent<BuilderPlacementAnchor>();

            if (anchor == null || anchor.anchorMode == EBuilderPlacementAnchorMode.Center)
                return placement.GetWorldCenterFormGridList(occupiedGridList, gridUnitSize);

            var bottomCenter = placement.GetWorldBottomCenterFromGridList(
                occupiedGridList,
                gridUnitSize);
            if (anchor.anchorMode == EBuilderPlacementAnchorMode.Bottom)
                return bottomCenter;

            var localAnchorPosition = anchor.GetCustomAnchorLocalPosition();
            var scaledAnchorPosition = Vector3.Scale(
                localAnchorPosition,
                prefab.transform.localScale);
            return bottomCenter - placement.CubeWorldRotation * scaledAnchorPosition;
        }


        /// <summary>
        /// 破坏方块
        /// </summary>
        private void BreakCube(Vector3Int gridPos)
        {
            // 尝试获取运行时数据
            if (!runtimeCubeDataDict.TryGetValue(gridPos, out var instanceData)) return;

            // 调用方块行为
            instanceData.instantiateCube.GetComponent<CubeBehaviour>()?.OnRemoved();

            // 销毁方块物体
            if (instanceData.instantiateCube != null) Destroy(instanceData.instantiateCube);

            // 单格直接摘掉 多格按占格列表清完
            if (instanceData.data.IsUnit)
            {
                runtimeCubeDataDict.Remove(gridPos);
            }
            else
            {
                var placement = new BuilderPlacementReport(
                    instanceData.originGridPos,
                    instanceData.data.GetCubePrefabSizeInt(),
                    instanceData.rotation);

                placement.FillOccupiedInfoToList(tempOccupiedGridList);

                foreach (var occupiedGridPos in tempOccupiedGridList)
                    runtimeCubeDataDict.Remove(occupiedGridPos);
            }

            if (imBuilder != null)
                imBuilder.CustorOnBreakSucceeded(instanceData);

            MmGlobalEventBus.GlobalBus.Publish(BuilderEvents.CubeBroken, instanceData);
        }

        /// <summary>
        /// 取消当前放置 藏预览退出射线 物品不扣
        /// </summary>
        public void CancelPlace()
        {
            if (stopRayCast)
                return;

            stopRayCast = true;
            activeCubeData = null;
            placeButtonPressed = false;
            breakButtonPressed = false;
            rotateButtonPressed = false;
            HidePreView();
            MmGlobalEventBus.GlobalBus.Publish(BuilderEvents.PlaceCancelled);
        }

    }
}
