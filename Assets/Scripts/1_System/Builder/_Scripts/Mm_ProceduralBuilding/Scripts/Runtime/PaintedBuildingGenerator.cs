using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public class PaintedBuildingGenerator : SerializedMonoBehaviour
    {
        private const string GeneratedRootName = "__PaintedBuildingGenerated";
        private const string GeneratedFurnitureRootName = "__PaintedBuildingFurnitureGenerated";

        /// <summary>
        /// 格子公约
        /// </summary>
        [LabelText("格子公约")]
        public BuildingGridConvention convention;

        /// <summary>
        /// 绘制蓝图
        /// </summary>
        [LabelText("绘制蓝图")]
        public PaintedBuildingPlan paintedPlan;

        /// <summary>
        /// 内饰布局
        /// </summary>
        [LabelText("内饰布局")]
        public PaintedInteriorLayout interiorLayout;

        /// <summary>
        /// 笔刷预设列表
        /// </summary>
        [LabelText("笔刷预设列表")]
        public List<PaintedBuildingBrushPreset> brushPresetList = new();

        /// <summary>
        /// 地面材质
        /// </summary>
        [LabelText("地面材质")]
        public Material floorMaterial;

        /// <summary>
        /// 墙体材质
        /// </summary>
        [LabelText("墙体材质")]
        public Material wallMaterial;

        /// <summary>
        /// 生成绘制建筑
        /// </summary>
        [ContextMenu("生成绘制建筑")]
        public void GenerateBuilding()
        {
            if (convention == null || paintedPlan == null)
            {
                Debug.LogWarning("[PaintedBuildingGenerator] 缺少格子公约或绘制蓝图", this);
                return;
            }

            ClearGenerated();

            var root = BuildingPrimitiveFactory.CreateGroup(GeneratedRootName, transform);
            foreach (var floorData in paintedPlan.paintFloorDataList)
            {
                if (floorData == null)
                    continue;

                GenerateFloor(floorData, root);
            }
        }

        /// <summary>
        /// 生成绘制内饰
        /// </summary>
        [ContextMenu("生成绘制内饰")]
        public void GenerateInterior()
        {
            if (convention == null || interiorLayout == null)
            {
                Debug.LogWarning("[PaintedBuildingGenerator] 缺少格子公约或内饰布局", this);
                return;
            }

            PaintedBuildingPlan interiorPlan = paintedPlan != null
                ? paintedPlan
                : interiorLayout.paintedBuildingPlan;
            if (interiorPlan == null)
            {
                Debug.LogWarning("[PaintedBuildingGenerator] 内饰布局缺少关联建筑蓝图", this);
                return;
            }

            ClearInteriorGenerated();
            var furnitureRoot = BuildingPrimitiveFactory.CreateGroup(GeneratedFurnitureRootName, transform);
            foreach (var placementData in interiorLayout.furniturePlacementDataList)
            {
                if (placementData == null
                    || placementData.brushPreset == null
                    || placementData.brushPreset.furniturePrefab == null)
                {
                    continue;
                }

                var brushPreset = placementData.brushPreset;
                var furniturePrefab = brushPreset.furniturePrefab;
                Vector3 furniturePosition = InteriorFurniturePlacementUtility.GetWorldPosition(
                    interiorPlan,
                    convention,
                    placementData);
                Quaternion furnitureRotation = InteriorFurniturePlacementUtility.GetWorldRotation(placementData);
                var furnitureObject = InstantiatePrefab(
                    furniturePrefab,
                    furnitureRoot,
                    furniturePosition,
                    furnitureRotation);
                furnitureObject.name = $"{brushPreset.DisplayName}_{placementData.floorIndex}_{placementData.anchorGridPos.x}_{placementData.anchorGridPos.y}";
            }
        }

        /// <summary>
        /// 实例化内饰预制体
        /// </summary>
        private GameObject InstantiatePrefab(
            GameObject prefab,
            Transform parent,
            Vector3 worldPosition,
            Quaternion worldRotation)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var prefabInstance = UnityEditor.PrefabUtility.InstantiatePrefab(
                    prefab,
                    parent) as GameObject;
                if (prefabInstance != null)
                {
                    prefabInstance.transform.SetPositionAndRotation(worldPosition, worldRotation);
                    return prefabInstance;
                }
            }
#endif

            return Instantiate(
                prefab,
                worldPosition,
                worldRotation,
                parent);
        }

        /// <summary>
        /// 合并渲染网格
        /// </summary>
        public int MergeRenderMeshes(EBuildingMergeTarget mergeTarget)
        {
            var generatedRoot = FindGeneratedRoot();
            if (generatedRoot == null)
            {
                Debug.LogWarning("[PaintedBuildingGenerator] 未找到生成根节点 请先生成建筑", this);
                return 0;
            }

            int mergedLayerCount = BuildingMeshMergeUtility.MergeGeneratedRoot(generatedRoot, mergeTarget);
            if (mergedLayerCount <= 0)
                Debug.LogWarning("[PaintedBuildingGenerator] 没有可合并的渲染网格", this);

            return mergedLayerCount;
        }

        /// <summary>
        /// 合并碰撞体
        /// </summary>
        public int MergeCollisionBoxes()
        {
            if (convention == null || paintedPlan == null)
            {
                Debug.LogWarning("[PaintedBuildingGenerator] 缺少格子公约或绘制蓝图", this);
                return 0;
            }

            var generatedRoot = FindGeneratedRoot();
            if (generatedRoot == null)
            {
                Debug.LogWarning("[PaintedBuildingGenerator] 未找到生成根节点 请先生成建筑", this);
                return 0;
            }

            return BuildingCollisionMergeUtility.MergeFromPlan(generatedRoot, paintedPlan, convention);
        }

        /// <summary>
        /// 开启 GPU Instancing
        /// </summary>
        public BuildingGpuInstancingResult EnableGpuInstancing()
        {
            var generatedRoot = FindGeneratedRoot();
            if (generatedRoot == null)
            {
                Debug.LogWarning("[PaintedBuildingGenerator] 未找到生成根节点 请先生成建筑", this);
                return new BuildingGpuInstancingResult();
            }

            return BuildingGpuInstancingUtility.EnableForGeneratedRoot(generatedRoot);
        }

        /// <summary>
        /// 查找生成根节点
        /// </summary>
        private Transform FindGeneratedRoot()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child != null && child.name == GeneratedRootName)
                    return child;
            }

            return null;
        }

        /// <summary>
        /// 清理生成物
        /// </summary>
        [ContextMenu("清理绘制建筑")]
        public void ClearGenerated()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == null || child.name != GeneratedRootName)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// 清理内饰生成物
        /// </summary>
        [ContextMenu("清理绘制内饰")]
        public void ClearInteriorGenerated()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == null || child.name != GeneratedFurnitureRootName)
                    continue;

                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// 生成单层
        /// </summary>
        private void GenerateFloor(PaintedBuildingFloorData floorData, Transform root)
        {
            int baseY = paintedPlan.GetFloorBaseY(floorData.floorIndex);
            int globalWallHeightGridCount = paintedPlan.GlobalWallHeightGridCount;
            var floorRoot = BuildingPrimitiveFactory.CreateGroup($"Floor_{floorData.floorIndex}", root);
            var floorLayerRoot = BuildingPrimitiveFactory.CreateGroup("FloorLayer", floorRoot);
            var structureLayerRoot = BuildingPrimitiveFactory.CreateGroup("StructureLayer", floorRoot);
            var cutoutFillLayerRoot = BuildingPrimitiveFactory.CreateGroup("CutoutFillLayer", floorRoot);

            foreach (var cellData in floorData.floorCellDataList)
            {
                if (cellData == null)
                    continue;

                GenerateFloorCell(cellData, baseY, floorLayerRoot);
            }

            foreach (var cellData in floorData.structureCellDataList)
            {
                if (cellData == null)
                    continue;

                GenerateStructureCell(
                    cellData,
                    floorData,
                    baseY,
                    globalWallHeightGridCount,
                    structureLayerRoot,
                    cutoutFillLayerRoot);
            }
        }

        /// <summary>
        /// 生成地面格子
        /// </summary>
        private void GenerateFloorCell(PaintedBuildingCellData cellData, int baseY, Transform floorRoot)
        {
            GenerateSolidCell(cellData, baseY, floorRoot, 0, 1, GetMaterial(cellData));
        }

        /// <summary>
        /// 生成结构格子
        /// </summary>
        private void GenerateStructureCell(
            PaintedBuildingCellData cellData,
            PaintedBuildingFloorData floorData,
            int baseY,
            int globalWallHeightGridCount,
            Transform floorRoot,
            Transform cutoutFillLayerRoot)
        {
            switch (cellData.cellType)
            {
                case EPaintedBuildingCellType.Wall:
                    GenerateSolidCell(cellData, baseY, floorRoot, 1, globalWallHeightGridCount, GetMaterial(cellData));
                    break;
                case EPaintedBuildingCellType.Cutout:
                    GenerateCutoutCell(
                        cellData,
                        floorData,
                        baseY,
                        globalWallHeightGridCount,
                        floorRoot,
                        cutoutFillLayerRoot);
                    break;
            }
        }

        /// <summary>
        /// 生成实心格子
        /// </summary>
        private void GenerateSolidCell(
            PaintedBuildingCellData cellData,
            int baseY,
            Transform floorRoot,
            int originYOffset,
            int heightGridCount,
            Material material)
        {
            var originGridPos = new Vector3Int(cellData.gridPos.x, baseY + originYOffset, cellData.gridPos.y);
            var gridSize = new Vector3Int(1, Mathf.Max(1, heightGridCount), 1);
            string objectName = $"{cellData.cellType}_{cellData.gridPos.x}_{cellData.gridPos.y}";
            BuildingPrimitiveFactory.CreateGridCube(objectName, floorRoot, convention, originGridPos, gridSize, material);
        }

        /// <summary>
        /// 生成挖空墙体
        /// </summary>
        private void GenerateCutoutCell(
            PaintedBuildingCellData cellData,
            PaintedBuildingFloorData floorData,
            int baseY,
            int globalWallHeightGridCount,
            Transform floorRoot,
            Transform cutoutFillLayerRoot)
        {
            int totalHeight = globalWallHeightGridCount;
            int cutoutStart = Mathf.Clamp(cellData.cutoutStartHeightGridCount, 0, totalHeight - 1);
            int cutoutEnd = Mathf.Clamp(cellData.cutoutEndHeightGridCount, cutoutStart + 1, totalHeight);
            var material = GetCutoutWallMaterial(cellData, floorData);

            if (cutoutStart > 0)
                GenerateSolidCell(cellData, baseY, floorRoot, 1, cutoutStart, material);

            int upperHeight = totalHeight - cutoutEnd;
            if (upperHeight > 0)
                GenerateSolidCell(cellData, baseY, floorRoot, 1 + cutoutEnd, upperHeight, material);

            GenerateCutoutFill(cellData, baseY, cutoutStart, cutoutEnd, cutoutFillLayerRoot);
        }

        /// <summary>
        /// 生成挖空填充物
        /// </summary>
        private void GenerateCutoutFill(
            PaintedBuildingCellData cellData,
            int baseY,
            int cutoutStart,
            int cutoutEnd,
            Transform cutoutFillLayerRoot)
        {
            if (cellData.brushPreset == null || cellData.brushPreset.cutoutFillPrefab == null)
                return;

            var fillGridSize = new Vector3Int(1, cutoutEnd - cutoutStart, 1);
            var fillOriginGridPos = new Vector3Int(
                cellData.gridPos.x,
                baseY + 1 + cutoutStart,
                cellData.gridPos.y);
            Vector3 fillPosition = convention.GridBoxToWorldCenter(fillOriginGridPos, fillGridSize);
            Quaternion fillRotation = Quaternion.Euler(0f, cellData.brushPreset.cutoutFillYRotation, 0f);
            var fillObject = InstantiatePrefab(
                cellData.brushPreset.cutoutFillPrefab,
                cutoutFillLayerRoot,
                fillPosition,
                fillRotation);
            if (fillObject == null)
                return;

            // 以预制体原始缩放为基准 拉伸到挖空区域
            fillObject.transform.localScale = Vector3.Scale(
                fillObject.transform.localScale,
                convention.GridSizeToWorldSize(fillGridSize));
            fillObject.name = $"填充_{cellData.brushPreset.DisplayName}_{cellData.gridPos.x}_{cellData.gridPos.y}";
        }

        /// <summary>
        /// 获取挖空保留墙体材质
        /// </summary>
        private Material GetCutoutWallMaterial(
            PaintedBuildingCellData cellData,
            PaintedBuildingFloorData floorData)
        {
            if (cellData.cutoutWallBrushPreset != null && cellData.cutoutWallBrushPreset.material != null)
                return cellData.cutoutWallBrushPreset.material;

            var wallBrushPreset = floorData.FindNearestWallBrushPreset(cellData.gridPos);
            if (wallBrushPreset != null && wallBrushPreset.material != null)
                return wallBrushPreset.material;

            return GetMaterial(cellData);
        }

        /// <summary>
        /// 获取材质
        /// </summary>
        private Material GetMaterial(PaintedBuildingCellData cellData)
        {
            if (cellData != null
                && cellData.brushPreset != null
                && cellData.brushPreset.material != null)
            {
                return cellData.brushPreset.material;
            }

            var materialLookupType = GetMaterialLookupType(cellData.cellType);
            foreach (var brushPreset in brushPresetList)
            {
                if (brushPreset == null || brushPreset.material == null)
                    continue;

                if (brushPreset.cellType == materialLookupType)
                    return brushPreset.material;
            }

            return materialLookupType == EPaintedBuildingCellType.Floor ? floorMaterial : wallMaterial;
        }

        /// <summary>
        /// 获取材质查找类型
        /// </summary>
        private static EPaintedBuildingCellType GetMaterialLookupType(EPaintedBuildingCellType cellType)
        {
            if (cellType == EPaintedBuildingCellType.Cutout)
                return EPaintedBuildingCellType.Wall;

            return cellType;
        }
    }
}
