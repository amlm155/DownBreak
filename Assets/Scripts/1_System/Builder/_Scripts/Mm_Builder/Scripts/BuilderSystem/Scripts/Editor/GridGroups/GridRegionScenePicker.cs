using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    /// <summary>
    /// Scene 视图两点取格 在水平面 Y = originGridPos.y × gridUnitSize 上射线求交
    /// 不依赖 Ground Collider 楼层由 originGridPos.y 或第一次点击的格 Y 决定
    /// </summary>
    internal static class GridRegionScenePicker
    {
        private static BuilderVirtualGridGroup targetGroup;
        private static int gridUnitSize = 1;
        private static int pickStep;
        private static Vector3Int firstGridPos;
        /// <summary>取格射线与之相交的水平面世界 Y</summary>
        private static float pickPlaneY;

        public static bool IsActive => targetGroup != null;

        /// <summary>
        /// 当前是否正在为此分区取格
        /// </summary>
        public static bool IsPickingGroup(BuilderVirtualGridGroup group)
            => IsActive && ReferenceEquals(targetGroup, group);

        /// <summary>
        /// 取格进度提示
        /// </summary>
        public static string PickStepHint
            => pickStep == 0
                ? "等待第一个角… Esc 取消"
                : "等待对角… Esc 取消";

        public static void Begin(BuilderVirtualGridGroup group, int unitSize)
        {
            if (group == null)
                return;

            targetGroup = group;
            gridUnitSize = Mathf.Max(1, unitSize);
            pickStep = 0;
            // 取格前先定好楼层平面 后续第一次点击可能改 pickPlaneY
            pickPlaneY = group.originGridPos.y * gridUnitSize;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            SceneView.RepaintAll();
            RepaintInspectors();
            Debug.Log("[GridGroups] Scene 取格：点击第一格（Esc 取消）");
        }

        public static void Cancel()
        {
            if (!IsActive)
                return;

            targetGroup = null;
            pickStep = 0;
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.RepaintAll();
            RepaintInspectors();
        }

        static void RepaintInspectors()
        {
            EditorApplication.QueuePlayerLoopUpdate();
        }

        private static void OnSceneGUI(SceneView view)
        {
            if (!IsActive)
                return;

            Handles.BeginGUI();
            var rect = new Rect(10f, 10f, 400f, 24f);
            var msg = pickStep == 0
                ? "分区取格 1/2：点击 Scene 设置第一个角（Esc 取消）"
                : "分区取格 2/2：点击 Scene 设置对角（Esc 取消）";
            EditorGUI.HelpBox(rect, msg, MessageType.Info);
            Handles.EndGUI();

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Cancel();
                Event.current.Use();
                return;
            }

            if (pickStep == 1)
            {
                if (TryPickGridPos(Event.current.mousePosition, out var hoverGridPos))
                    DrawPreviewRect(firstGridPos, hoverGridPos);
            }

            if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || Event.current.alt)
                return;

            if (GUIUtility.hotControl != 0)
                return;

            if (!TryPickGridPos(Event.current.mousePosition, out var gridPos))
                return;

            Event.current.Use();

            if (pickStep == 0)
            {
                firstGridPos = gridPos;
                pickPlaneY = gridPos.y * gridUnitSize;
                pickStep = 1;
                RepaintInspectors();
                Debug.Log($"[GridGroups] 第一格 {firstGridPos}，请点击对角");
            }
            else
            {
                VirtualGridGroupEditorUtility.SetFromTwoGridPos(targetGroup, firstGridPos, gridPos);
                Debug.Log($"[GridGroups] 完成 origin={targetGroup.originGridPos} size={targetGroup.gridSize}");
                Cancel();
            }

            view.Repaint();
        }

        private static bool TryPickGridPos(Vector2 guiMouse, out Vector3Int gridPos)
        {
            gridPos = default;
            var ray = HandleUtility.GUIPointToWorldRay(guiMouse);
            var plane = new Plane(Vector3.up, new Vector3(0f, pickPlaneY, 0f));
            if (!plane.Raycast(ray, out var dist))
                return false;

            gridPos = BuilderVirtualGrid.WorldToGridPos(ray.GetPoint(dist), gridUnitSize);
            return true;
        }

        private static void DrawPreviewRect(Vector3Int a, Vector3Int b)
        {
            var min = Vector3Int.Min(a, b);
            var max = Vector3Int.Max(a, b);
            var unit = gridUnitSize;
            var y = min.y * unit + unit * 0.02f;

            Handles.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            var x0 = min.x * unit;
            var x1 = (max.x + 1) * unit;
            var z0 = min.z * unit;
            var z1 = (max.z + 1) * unit;

            Handles.DrawLine(new Vector3(x0, y, z0), new Vector3(x1, y, z0));
            Handles.DrawLine(new Vector3(x1, y, z0), new Vector3(x1, y, z1));
            Handles.DrawLine(new Vector3(x1, y, z1), new Vector3(x0, y, z1));
            Handles.DrawLine(new Vector3(x0, y, z1), new Vector3(x0, y, z0));
        }
    }
}
