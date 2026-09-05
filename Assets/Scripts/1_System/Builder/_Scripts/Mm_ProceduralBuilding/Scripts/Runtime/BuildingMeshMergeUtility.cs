using System.Collections.Generic;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public static class BuildingMeshMergeUtility
    {
        private const string FloorLayerName = "FloorLayer";
        private const string StructureLayerName = "StructureLayer";

        /// <summary>
        /// 合并生成根节点下的渲染网格
        /// </summary>
        public static int MergeGeneratedRoot(Transform generatedRoot, EBuildingMergeTarget mergeTarget)
        {
            if (generatedRoot == null)
                return 0;

            int mergedLayerCount = 0;
            for (int i = 0; i < generatedRoot.childCount; i++)
            {
                var floorRoot = generatedRoot.GetChild(i);
                if (ShouldMergeFloor(mergeTarget))
                {
                    var floorLayer = floorRoot.Find(FloorLayerName);
                    if (MergeLayer(floorLayer, $"{FloorLayerName}_Merged"))
                        mergedLayerCount++;
                }

                if (ShouldMergeStructure(mergeTarget))
                {
                    var structureLayer = floorRoot.Find(StructureLayerName);
                    if (MergeLayer(structureLayer, $"{StructureLayerName}_Merged"))
                        mergedLayerCount++;
                }
            }

            return mergedLayerCount;
        }

        /// <summary>
        /// 是否合并地面
        /// </summary>
        private static bool ShouldMergeFloor(EBuildingMergeTarget mergeTarget)
        {
            return mergeTarget == EBuildingMergeTarget.All || mergeTarget == EBuildingMergeTarget.Floor;
        }

        /// <summary>
        /// 是否合并结构
        /// </summary>
        private static bool ShouldMergeStructure(EBuildingMergeTarget mergeTarget)
        {
            return mergeTarget == EBuildingMergeTarget.All || mergeTarget == EBuildingMergeTarget.Structure;
        }

        /// <summary>
        /// 合并单层渲染网格
        /// </summary>
        private static bool MergeLayer(Transform layerRoot, string mergedRootName)
        {
            if (layerRoot == null || layerRoot.childCount == 0)
                return false;

            var materialGroupDict = new Dictionary<EntityId, List<CombineInstance>>();
            var materialDict = new Dictionary<EntityId, Material>();
            var childObjectList = new List<GameObject>();

            foreach (Transform child in layerRoot)
            {
                var meshFilter = child.GetComponent<MeshFilter>();
                var meshRenderer = child.GetComponent<MeshRenderer>();
                if (meshFilter == null || meshRenderer == null || meshFilter.sharedMesh == null)
                    continue;

                Material material = meshRenderer.sharedMaterial;
                var materialId = material != null ? material.GetEntityId() : EntityId.None;
                if (!materialGroupDict.TryGetValue(materialId, out var combineList))
                {
                    combineList = new List<CombineInstance>();
                    materialGroupDict.Add(materialId, combineList);
                    materialDict.Add(materialId, material);
                }

                combineList.Add(new CombineInstance
                {
                    mesh = meshFilter.sharedMesh,
                    transform = layerRoot.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix
                });
                childObjectList.Add(child.gameObject);
            }

            if (childObjectList.Count == 0)
                return false;

            foreach (var childObject in childObjectList)
            {
                if (Application.isPlaying)
                    Object.Destroy(childObject);
                else
                    Object.DestroyImmediate(childObject);
            }

            foreach (var pair in materialGroupDict)
            {
                var combineList = pair.Value;
                if (combineList.Count == 0)
                    continue;

                var mergedMesh = new Mesh
                {
                    name = $"{mergedRootName}_{pair.Key}",
                    indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
                };
                mergedMesh.CombineMeshes(combineList.ToArray(), true, true);
                mergedMesh.RecalculateBounds();

                var mergedObject = new GameObject($"{mergedRootName}_{pair.Key}");
                mergedObject.transform.SetParent(layerRoot, false);
                mergedObject.transform.localPosition = Vector3.zero;
                mergedObject.transform.localRotation = Quaternion.identity;
                mergedObject.transform.localScale = Vector3.one;

                var mergedFilter = mergedObject.AddComponent<MeshFilter>();
                mergedFilter.sharedMesh = mergedMesh;

                var mergedRenderer = mergedObject.AddComponent<MeshRenderer>();
                mergedRenderer.sharedMaterial = materialDict[pair.Key];
            }

            return true;
        }
    }
}
