using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BuilderVirtualGrid : MonoBehaviour
{
    [LabelText("单位虚拟网格大小")] public int gridUnitSize;

    [InfoBox("列表项底部「两点取格」在 Scene 连点两角设 XZ 高度用尺寸 Y Esc 取消", InfoMessageType.None)]
    [ListDrawerSettings(
        DraggableItems = true,
        ShowIndexLabels = false,
        ListElementLabelName = "id")]
    [LabelText("网格组列表")]
    public List<BuilderVirtualGridGroup> gridGroups = new();
    
    [FoldoutGroup("可视化配置")]
    [LabelText("显示虚拟网格高度")]
    public bool showGridHeight = true;
    [FoldoutGroup("可视化配置")]
    [LabelText("高度线颜色"), ShowIf("showGridHeight")]
    public Color yAxisColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [FoldoutGroup("可视化配置")]
    [LabelText("高度标签颜色"), ShowIf("showGridHeight")]
    public Color heightLabelColor = Color.white;
    [FoldoutGroup("可视化配置")]
    [LabelText("高度标签字号"), ShowIf("showGridHeight"), MinValue(8)]
    public int heightLabelFontSize = 16;
    [FoldoutGroup("可视化配置")]
    [LabelText("显示虚拟网格颜色")]
    public bool showGridColor = true;
    [FoldoutGroup("可视化配置")]
    [LabelText("虚拟网格颜色"), ShowIf("showGridColor")]
    public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

    /// <summary>
    /// 世界坐标转网格坐标
    /// </summary>
    public Vector3Int WorldToGrid(Vector3 worldPos) => WorldToGridPos(worldPos, gridUnitSize);

    /// <summary>
    /// 世界坐标转网格坐标
    /// </summary>
    public static Vector3Int WorldToGridPos(Vector3 worldPos, int gridUnitSize)
    {
        var unit = gridUnitSize > 0 ? gridUnitSize : 1;
        return new Vector3Int(
            // 比如 1.7/1 = 1.7 向下取整为1
            Mathf.FloorToInt(worldPos.x / unit),
            Mathf.FloorToInt(worldPos.y / unit),
            Mathf.FloorToInt(worldPos.z / unit));
    }

    /// <summary>
    /// 网格坐标转世界中心
    /// </summary>
    public Vector3 GridToWorldCenter(Vector3Int gridPos)
    {
        float worldX = gridPos.x * gridUnitSize + gridUnitSize / 2f;
        float worldY = gridPos.y * gridUnitSize + gridUnitSize / 2f;
        float worldZ = gridPos.z * gridUnitSize + gridUnitSize / 2f;
        return new Vector3(worldX, worldY, worldZ);
    }

    /// <summary>
    /// 校验分区是否可以放置
    /// </summary>
    public bool ValidVirtualGroup(Vector3Int gridPos)
    {
        if (gridGroups == null || gridGroups.Count == 0)
            return false;

        foreach (var group in gridGroups)
        {
            if (group == null)
                continue;
            if (group.allowPlacement && group.IsInside(gridPos))
                return true;
        }

        return false;
    }

    #region 编辑器可视化
#if UNITY_EDITOR
    public static int EditorGridUnitSize { get; set; } = 1;
#endif

    private void OnValidate()
    {
#if UNITY_EDITOR
        EditorGridUnitSize = Mathf.Max(1, gridUnitSize);
#endif
        if (gridUnitSize <= 0)
        {
            Debug.LogError("[BuilderVirtualGrid] 网格单元大小不能小于等于0 已重置为1", this);
            gridUnitSize = 1;
        }
    }

    private void OnDrawGizmos()
    {
        if (gridUnitSize <= 0 || gridGroups == null)
            return;

        float planeYOffset = gridUnitSize * 0.02f;

        foreach (var group in gridGroups)
        {
            group?.DrawGizmo(
                gridUnitSize, showGridColor, showGridHeight,
                gridColor, yAxisColor, planeYOffset,
                heightLabelColor, heightLabelFontSize);
        }
    }
    #endregion
}
