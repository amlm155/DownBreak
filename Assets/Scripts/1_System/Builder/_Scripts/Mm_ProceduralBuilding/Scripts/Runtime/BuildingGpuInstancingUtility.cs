using System.Collections.Generic;
using UnityEngine;

namespace Mm_ProceduralBuilding
{
    public class BuildingGpuInstancingResult
    {
        /// <summary>
        /// 成功开启数量
        /// </summary>
        public int EnabledCount => enabledMaterialList.Count;

        /// <summary>
        /// 已开启材质列表
        /// </summary>
        public List<Material> enabledMaterialList = new();

        /// <summary>
        /// 不支持材质名列表
        /// </summary>
        public List<string> unsupportedMaterialNameList = new();
    }

    public static class BuildingGpuInstancingUtility
    {
        /// <summary>
        /// 开启 GPU Instancing
        /// </summary>
        public static BuildingGpuInstancingResult EnableForGeneratedRoot(Transform generatedRoot)
        {
            var result = new BuildingGpuInstancingResult();
            if (generatedRoot == null)
                return result;

            var materialHashList = new HashSet<Material>();
            foreach (var meshRenderer in generatedRoot.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (meshRenderer == null)
                    continue;

                foreach (var material in meshRenderer.sharedMaterials)
                {
                    if (material != null)
                        materialHashList.Add(material);
                }
            }

            foreach (var material in materialHashList)
            {
                if (SupportsGpuInstancing(material))
                {
                    material.enableInstancing = true;
                    result.enabledMaterialList.Add(material);
                    continue;
                }

                result.unsupportedMaterialNameList.Add(material.name);
            }

            return result;
        }

        /// <summary>
        /// 判断材质是否支持 GPU Instancing
        /// </summary>
        public static bool SupportsGpuInstancing(Material material)
        {
            if (material == null || material.shader == null)
                return false;

            string shaderName = material.shader.name;
            if (shaderName.StartsWith("Hidden/"))
                return false;

            if (shaderName.Contains("UI/") || shaderName.Contains("Sprites/"))
                return false;

            if (shaderName.Contains("Unlit/Color") || shaderName == "Unlit/Texture")
                return false;

            return true;
        }
    }
}
