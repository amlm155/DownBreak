#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mm_ProceduralBuilding.Editor
{
    public static class BuildingGpuInstancingEditorUtility
    {
        /// <summary>
        /// 开启 GPU Instancing 并提示结果
        /// </summary>
        public static void EnableWithDialog(Transform generatedRoot)
        {
            var result = BuildingGpuInstancingUtility.EnableForGeneratedRoot(generatedRoot);
            foreach (var material in result.enabledMaterialList)
            {
                if (material == null)
                    continue;

                EditorUtility.SetDirty(material);
            }

            PingEnabledMaterials(result);

            if (result.unsupportedMaterialNameList.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "GPU Instancing",
                    BuildSuccessMessage(result),
                    "确定");
                return;
            }

            EditorUtility.DisplayDialog("GPU Instancing", BuildPartialSuccessMessage(result), "确定");
        }

        /// <summary>
        /// Ping 已开启材质
        /// </summary>
        private static void PingEnabledMaterials(BuildingGpuInstancingResult result)
        {
            if (result.enabledMaterialList.Count == 0)
                return;

            var selectionList = new Object[result.enabledMaterialList.Count];
            for (int i = 0; i < result.enabledMaterialList.Count; i++)
                selectionList[i] = result.enabledMaterialList[i];

            Selection.objects = selectionList;

            foreach (var material in result.enabledMaterialList)
            {
                if (material == null)
                    continue;

                EditorGUIUtility.PingObject(material);
                Debug.Log($"[BuildingPainterWindow] 已开启 GPU Instancing 材质 {material.name}", material);
            }
        }

        /// <summary>
        /// 构建成功消息
        /// </summary>
        private static string BuildSuccessMessage(BuildingGpuInstancingResult result)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"已为 {result.EnabledCount} 个材质开启 GPU Instancing");
            messageBuilder.AppendLine("已在 Project 窗口高亮以下材质");
            AppendMaterialNameList(messageBuilder, result.enabledMaterialList);
            return messageBuilder.ToString();
        }

        /// <summary>
        /// 构建部分成功消息
        /// </summary>
        private static string BuildPartialSuccessMessage(BuildingGpuInstancingResult result)
        {
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine($"成功开启 {result.EnabledCount} 个材质");
            messageBuilder.AppendLine("已在 Project 窗口高亮以下材质");
            AppendMaterialNameList(messageBuilder, result.enabledMaterialList);
            messageBuilder.AppendLine();
            messageBuilder.AppendLine("以下材质 Shader 不支持 GPU Instancing");
            foreach (var materialName in result.unsupportedMaterialNameList)
                messageBuilder.AppendLine($"- {materialName}");

            return messageBuilder.ToString();
        }

        /// <summary>
        /// 追加材质名列表
        /// </summary>
        private static void AppendMaterialNameList(StringBuilder messageBuilder, System.Collections.Generic.List<Material> materialList)
        {
            foreach (var material in materialList)
            {
                if (material == null)
                    continue;

                messageBuilder.AppendLine($"- {material.name}");
            }
        }
    }
}
#endif
