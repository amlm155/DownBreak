using UnityEngine;

namespace Mm_Budier.Editor
{
    /// <summary>
    /// Editor 专用：把 Scene 两次点击的格坐标写入分区
    /// 默认只改 XZ 保留已有 originGridPos.y 两点 Y 不同时才重算高度层
    /// </summary>
    internal static class VirtualGridGroupEditorUtility
    {
        public static void SetFromTwoGridPos(BuilderVirtualGridGroup group, Vector3Int gridPosA, Vector3Int gridPosB)
        {
            if (group == null)
                return;

            var min = Vector3Int.Min(gridPosA, gridPosB);
            var max = Vector3Int.Max(gridPosA, gridPosB);

            // XZ 取对角包围盒 Y 先沿用配置里的楼层 避免 Scene 取格误改高度
            group.originGridPos = new Vector3Int(min.x, group.originGridPos.y, min.z);

            // 两点不在同一 Y 层时 才用点击结果覆盖 origin.y 与 gridSize.y
            if (gridPosA.y != gridPosB.y)
            {
                group.originGridPos = new Vector3Int(min.x, min.y, min.z);
                group.gridSize.y = max.y - min.y + 1;
            }

            group.gridSize.x = Mathf.Max(1, max.x - min.x + 1);
            group.gridSize.z = Mathf.Max(1, max.z - min.z + 1);
            group.gridSize.y = Mathf.Max(1, group.gridSize.y);
        }
    }
}
