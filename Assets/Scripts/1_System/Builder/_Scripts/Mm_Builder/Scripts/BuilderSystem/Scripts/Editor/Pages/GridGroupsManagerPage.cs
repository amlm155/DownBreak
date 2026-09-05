using System.Collections.Generic;
using Mm_Budier;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    /// <summary>
    /// Mm Builder 分区管理 编辑分区列表 与场景组件及 grid-groups.json 同步
    /// </summary>
    public class GridGroupsManagerPage
    {
        [TitleGroup("场景目标")]
        [InfoBox("编辑分区后记得「应用到场景」或「保存到文件」。JSON 与方块存档同目录。", InfoMessageType.None)]
        [ShowInInspector, LabelText("虚拟网格组件")]
        [OnValueChanged(nameof(OnTargetChanged))]
        private BuilderVirtualGrid targetGrid;

        [TitleGroup("场景目标")]
        [ShowInInspector, ReadOnly, LabelText("配置文件路径")]
        private string ConfigFilePath => GridGroupsConfigIO.GetFilePath();

        [TitleGroup("分区配置")]
        [InfoBox("列表项底部「两点取格」在 Scene 连点两角设 XZ 高度用尺寸 Y Esc 取消", InfoMessageType.None)]
        [ShowInInspector, LabelText("网格组列表")]
        [ListDrawerSettings(
            DraggableItems = true,
            ShowIndexLabels = false,
            ListElementLabelName = "id")]
        private List<BuilderVirtualGridGroup> groups = new();

        [TitleGroup("操作")]
        [HorizontalGroup("操作/Row1")]
        [Button("从场景读取", ButtonSizes.Medium)]
        private void PullFromScene()
        {
            if (!EnsureTarget())
                return;

            SyncGridUnitSize();
            groups = GridGroupsConfigIO.Capture(targetGrid);
            Debug.Log($"[GridGroups] 已从场景读取 {groups.Count} 个分区");
        }

        [HorizontalGroup("操作/Row1")]
        [Button("应用到场景", ButtonSizes.Medium)]
        private void ApplyToScene()
        {
            if (!EnsureTarget())
                return;

            Undo.RecordObject(targetGrid, "Apply Grid Groups");
            GridGroupsConfigIO.Apply(targetGrid, groups);
            EditorUtility.SetDirty(targetGrid);
            Debug.Log($"[GridGroups] 已应用到场景 {targetGrid.name}（{groups.Count} 个分区）");
        }

        [HorizontalGroup("操作/Row2")]
        [Button("保存到文件", ButtonSizes.Medium)]
        private void SaveToFile()
        {
            if (!GridGroupsConfigIO.TrySave(groups, out var error))
            {
                Debug.LogError($"[GridGroups] 保存失败：{error}");
                return;
            }

            EditorUtility.DisplayDialog("分区配置", $"已保存到：\n{GridGroupsConfigIO.GetFilePath()}", "确定");
        }

        [HorizontalGroup("操作/Row2")]
        [Button("从文件加载", ButtonSizes.Medium)]
        private void LoadFromFile()
        {
            if (!GridGroupsConfigIO.TryLoad(out groups, out var error))
            {
                Debug.LogWarning($"[GridGroups] 加载失败：{error}");
                EditorUtility.DisplayDialog("分区配置", error, "确定");
                return;
            }

            SyncGridUnitSize();
            Debug.Log($"[GridGroups] 已从文件加载 {groups.Count} 个分区");
        }

        [HorizontalGroup("操作/Row2")]
        [Button("打开存档目录", ButtonSizes.Medium)]
        private void OpenSaveFolder() => GridGroupsConfigIO.RevealInExplorer();

        [HorizontalGroup("操作/Row3")]
        [Button("定位场景组件", ButtonSizes.Medium)]
        private void PingTarget()
        {
            if (!EnsureTarget())
                return;

            Selection.activeObject = targetGrid;
            EditorGUIUtility.PingObject(targetGrid);
        }

        public void Refresh()
        {
            if (targetGrid == null)
                targetGrid = GridGroupsConfigIO.FindFirstVirtualGridInActiveScene();

            SyncGridUnitSize();

            if (targetGrid != null && (groups == null || groups.Count == 0))
                PullFromScene();
        }

        private bool EnsureTarget()
        {
            if (targetGrid != null)
                return true;

            targetGrid = GridGroupsConfigIO.FindFirstVirtualGridInActiveScene();
            if (targetGrid != null)
            {
                SyncGridUnitSize();
                return true;
            }

            EditorUtility.DisplayDialog("分区管理", "请先指定场景中的 BuilderVirtualGrid 组件。", "确定");
            return false;
        }

        private void OnTargetChanged()
        {
            SyncGridUnitSize();
            if (targetGrid != null)
                PullFromScene();
        }

        private void SyncGridUnitSize()
        {
            BuilderVirtualGrid.EditorGridUnitSize = targetGrid != null
                ? Mathf.Max(1, targetGrid.gridUnitSize)
                : 1;
        }
    }
}
