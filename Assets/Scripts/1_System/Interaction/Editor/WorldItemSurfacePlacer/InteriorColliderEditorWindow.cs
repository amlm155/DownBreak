using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Interaction.Editor
{
    /// <summary>
    /// 内部碰撞体状态
    /// </summary>
    public enum EInteriorColliderState
    {
        /// <summary> 没有任何碰撞体</summary>
        None = 0,

        /// <summary> 本工具生成的贴合 BoxCollider</summary>
        GeneratedBox = 1,

        /// <summary> 本工具生成的 MeshCollider</summary>
        GeneratedMesh = 2,

        /// <summary> 自带碰撞体(自身或子物体)</summary>
        Existing = 3,
    }

    /// <summary>
    /// 内部碰撞体编辑辅助
    /// 给内腔的网格补碰撞体 让"内部采样"能打到内腔面 也让摆进去的物品有实体托住
    /// 生成的碰撞体都放在名为 InteriorCollider_ 前缀的子节点上 便于整体移除
    /// </summary>
    public static class InteriorColliderEditorUtility
    {
        /// <summary> 生成的碰撞体节点前缀 </summary>
        public const string GeneratedColliderPrefix = "InteriorCollider_";

        /// <summary> 贴合盒子节点名 </summary>
        private const string BoxNodeName = GeneratedColliderPrefix + "Box";

        /// <summary> 网格碰撞体节点名 </summary>
        private const string MeshNodeName = GeneratedColliderPrefix + "Mesh";

        /// <summary>
        /// 收集目标下全部渲染器(含未激活)
        /// </summary>
        public static List<Renderer> CollectRenderers(GameObject root)
        {
            var resultList = new List<Renderer>();
            if (root == null)
                return resultList;

            var rendererList = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rendererList.Length; i++)
            {
                var renderer = rendererList[i];
                if (renderer == null)
                    continue;

                // 粒子/拖尾/线这类不参与表面采样
                if (renderer is ParticleSystemRenderer
                    || renderer is TrailRenderer
                    || renderer is LineRenderer)
                {
                    continue;
                }

                resultList.Add(renderer);
            }

            return resultList;
        }

        /// <summary>
        /// 取渲染器当前碰撞体状态
        /// </summary>
        public static EInteriorColliderState GetState(Renderer renderer)
        {
            if (renderer == null)
                return EInteriorColliderState.None;

            bool hasExisting = false;
            var colliderList = renderer.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliderList.Length; i++)
            {
                var collider = colliderList[i];
                if (collider == null)
                    continue;

                bool isGenerated = IsGeneratedCollider(collider, out bool isMesh);
                if (!isGenerated)
                {
                    hasExisting = true;
                    continue;
                }

                if (isMesh && IsOnNode(collider, MeshNodeName))
                    return EInteriorColliderState.GeneratedMesh;

                if (!isMesh && IsOnNode(collider, BoxNodeName))
                    return EInteriorColliderState.GeneratedBox;
            }

            return hasExisting ? EInteriorColliderState.Existing : EInteriorColliderState.None;
        }

        /// <summary>
        /// 生成或更新贴合的 BoxCollider
        /// </summary>
        public static bool EnsureFittedBoxCollider(Renderer renderer)
        {
            if (renderer == null)
                return false;

            var node = EnsureNode(renderer, BoxNodeName);
            if (node == null)
                return false;

            Bounds localBounds = renderer.localBounds;
            node.transform.localPosition = localBounds.center;
            node.transform.localRotation = Quaternion.identity;
            node.transform.localScale = Vector3.one;

            var boxCollider = node.GetComponent<BoxCollider>();
            if (boxCollider == null)
                boxCollider = Undo.AddComponent<BoxCollider>(node);

            if (boxCollider == null)
                return false;

            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(
                Mathf.Max(0.005f, localBounds.size.x),
                Mathf.Max(0.005f, localBounds.size.y),
                Mathf.Max(0.005f, localBounds.size.z));
            boxCollider.isTrigger = false;

            EditorUtility.SetDirty(boxCollider);
            return true;
        }

        /// <summary>
        /// 生成或更新 MeshCollider 需要 MeshFilter
        /// </summary>
        public static bool EnsureMeshCollider(Renderer renderer)
        {
            if (renderer == null)
                return false;

            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return false;

            var node = EnsureNode(renderer, MeshNodeName);
            if (node == null)
                return false;

            node.transform.localPosition = Vector3.zero;
            node.transform.localRotation = Quaternion.identity;
            node.transform.localScale = Vector3.one;

            var meshCollider = node.GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = Undo.AddComponent<MeshCollider>(node);

            if (meshCollider == null)
                return false;

            // 非 Convex 也能被射线命中 只有参与刚体碰撞才必须 Convex
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = false;
            meshCollider.isTrigger = false;

            EditorUtility.SetDirty(meshCollider);
            return true;
        }

        /// <summary>
        /// 移除本工具为该渲染器生成的碰撞体
        /// </summary>
        public static bool RemoveGeneratedColliders(Renderer renderer)
        {
            if (renderer == null)
                return false;

            bool removed = false;
            removed |= RemoveNode(renderer, BoxNodeName);
            removed |= RemoveNode(renderer, MeshNodeName);
            return removed;
        }

        /// <summary>
        /// 给目标下所有缺碰撞体的渲染器批量生成
        /// </summary>
        /// <param name="useMesh">优先用 MeshCollider 失败时退回贴合盒</param>
        public static int EnsureAll(GameObject root, bool useMesh)
        {
            var rendererList = CollectRenderers(root);
            int count = 0;
            for (int i = 0; i < rendererList.Count; i++)
            {
                var renderer = rendererList[i];
                if (GetState(renderer) != EInteriorColliderState.None)
                    continue;

                bool isDone = useMesh && EnsureMeshCollider(renderer);
                if (!isDone)
                    isDone = EnsureFittedBoxCollider(renderer);

                if (isDone)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 移除目标下全部由本工具生成的碰撞体
        /// </summary>
        public static int RemoveAll(GameObject root)
        {
            var rendererList = CollectRenderers(root);
            int count = 0;
            for (int i = 0; i < rendererList.Count; i++)
            {
                if (RemoveGeneratedColliders(rendererList[i]))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 取或建生成碰撞体用的子节点
        /// </summary>
        private static GameObject EnsureNode(Renderer renderer, string nodeName)
        {
            var nodeTransform = renderer.transform.Find(nodeName);
            if (nodeTransform != null)
                return nodeTransform.gameObject;

            var node = new GameObject(nodeName);
            Undo.RegisterCreatedObjectUndo(node, "生成内腔碰撞体");
            node.transform.SetParent(renderer.transform, false);
            return node;
        }

        /// <summary>
        /// 移除指定子节点
        /// </summary>
        private static bool RemoveNode(Renderer renderer, string nodeName)
        {
            var nodeTransform = renderer.transform.Find(nodeName);
            if (nodeTransform == null)
                return false;

            Undo.DestroyObjectImmediate(nodeTransform.gameObject);
            return true;
        }

        /// <summary>
        /// 是否本工具生成的碰撞体
        /// </summary>
        private static bool IsGeneratedCollider(Collider collider, out bool isMesh)
        {
            isMesh = collider is MeshCollider;
            return collider != null
                && (IsOnNode(collider, BoxNodeName) || IsOnNode(collider, MeshNodeName));
        }

        /// <summary>
        /// 碰撞体是否挂在指定名字的节点上
        /// </summary>
        private static bool IsOnNode(Collider collider, string nodeName)
        {
            return collider != null
                && collider.transform != null
                && collider.transform.name == nodeName;
        }
    }

    /// <summary>
    /// 内部碰撞体编辑器
    /// 逐个网格查看/生成/移除内腔碰撞体 供"内部采样"与"物品托底"使用
    /// </summary>
    public sealed class InteriorColliderEditorWindow : EditorWindow
    {
        private const string WindowTitle = "内部碰撞体编辑器";
        private const string MenuPath = "Tools/DownBreak/" + WindowTitle;

        [SerializeField]
        private GameObject targetObject;

        [SerializeField]
        private bool preferMeshCollider = true;

        [SerializeField]
        private Vector2 scrollPosition;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<InteriorColliderEditorWindow>(false, WindowTitle, true);
            window.minSize = new Vector2(460f, 520f);
            if (Selection.activeGameObject != null)
                window.targetObject = Selection.activeGameObject;
            window.Show();
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.HelpBox(
                "给场景物体的内腔网格补碰撞体。用途：\n" +
                "1) 让「物品表面摆放」工具的内部采样能打到内腔层板/内壁；\n" +
                "2) 让摆进去的物品有实体托住，运行时不会掉落或穿出。\n" +
                "生成的碰撞体放在 InteriorCollider_ 前缀的子节点上，可随时整体移除。",
                MessageType.None);

            DrawTargetSection();
            DrawBatchSection();
            DrawRendererList();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 目标与统计
        /// </summary>
        private void DrawTargetSection()
        {
            EditorGUILayout.LabelField("目标物体", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                targetObject = (GameObject)EditorGUILayout.ObjectField(
                    targetObject,
                    typeof(GameObject),
                    true);

                if (GUILayout.Button("取当前选中", GUILayout.Width(84f)))
                    targetObject = Selection.activeGameObject;
            }

            if (targetObject == null)
            {
                EditorGUILayout.HelpBox("请指定要编辑内腔碰撞体的场景物体", MessageType.Info);
                return;
            }

            if (!targetObject.scene.IsValid())
            {
                EditorGUILayout.HelpBox("选中的是 Project 里的资源，请从 Hierarchy 里选场景实例", MessageType.Error);
                return;
            }

            var rendererList = InteriorColliderEditorUtility.CollectRenderers(targetObject);
            int noneCount = 0;
            for (int i = 0; i < rendererList.Count; i++)
            {
                if (InteriorColliderEditorUtility.GetState(rendererList[i]) == EInteriorColliderState.None)
                    noneCount++;
            }

            EditorGUILayout.LabelField($"子网格 {rendererList.Count} 个，其中无碰撞体 {noneCount} 个");
        }

        /// <summary>
        /// 批量操作
        /// </summary>
        private void DrawBatchSection()
        {
            if (targetObject == null || !targetObject.scene.IsValid())
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("批量", EditorStyles.boldLabel);
            preferMeshCollider = EditorGUILayout.Toggle("优先用 MeshCollider", preferMeshCollider);
            EditorGUILayout.LabelField("（没有 MeshFilter 的网格会自动退回贴合 BoxCollider）", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("为缺失的网格生成碰撞体", GUILayout.Height(24f)))
                {
                    int count = InteriorColliderEditorUtility.EnsureAll(targetObject, preferMeshCollider);
                    Debug.Log($"[{WindowTitle}] 已为 {count} 个网格生成碰撞体");
                }

                if (GUILayout.Button("移除本工具生成的碰撞体", GUILayout.Height(24f)))
                {
                    int count = InteriorColliderEditorUtility.RemoveAll(targetObject);
                    Debug.Log($"[{WindowTitle}] 已移除 {count} 个网格上的生成碰撞体");
                }
            }
        }

        /// <summary>
        /// 逐网格列表
        /// </summary>
        private void DrawRendererList()
        {
            if (targetObject == null || !targetObject.scene.IsValid())
                return;

            var rendererList = InteriorColliderEditorUtility.CollectRenderers(targetObject);
            if (rendererList.Count == 0)
            {
                EditorGUILayout.HelpBox("目标下没有可用网格", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("网格明细", EditorStyles.boldLabel);

            for (int i = 0; i < rendererList.Count; i++)
            {
                var renderer = rendererList[i];
                if (renderer == null)
                    continue;

                var state = InteriorColliderEditorUtility.GetState(renderer);
                int depth = ResolveDepth(renderer.transform, targetObject.transform);
                string indent = new string(' ', Mathf.Clamp(depth, 0, 6) * 2);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        $"{indent}{renderer.name}  [{ResolveStateText(state)}]",
                        GUILayout.MinWidth(180f));

                    if (GUILayout.Button("贴合盒", GUILayout.Width(56f)))
                    {
                        InteriorColliderEditorUtility.EnsureFittedBoxCollider(renderer);
                        GUI.FocusControl(null);
                    }

                    using (new EditorGUI.DisabledScope(renderer.GetComponent<MeshFilter>() == null))
                    {
                        if (GUILayout.Button("网格", GUILayout.Width(46f)))
                        {
                            InteriorColliderEditorUtility.EnsureMeshCollider(renderer);
                            GUI.FocusControl(null);
                        }
                    }

                    using (new EditorGUI.DisabledScope(state != EInteriorColliderState.GeneratedBox
                                                        && state != EInteriorColliderState.GeneratedMesh))
                    {
                        if (GUILayout.Button("移除", GUILayout.Width(46f)))
                        {
                            InteriorColliderEditorUtility.RemoveGeneratedColliders(renderer);
                            GUI.FocusControl(null);
                        }
                    }

                    if (GUILayout.Button("选中", GUILayout.Width(46f)))
                    {
                        Selection.activeGameObject = renderer.gameObject;
                        EditorGUIUtility.PingObject(renderer.gameObject);
                    }
                }
            }
        }

        /// <summary>
        /// 相对目标的层级深度
        /// </summary>
        private static int ResolveDepth(Transform current, Transform root)
        {
            int depth = 0;
            var node = current;
            while (node != null && node != root && depth < 32)
            {
                depth++;
                node = node.parent;
            }

            return depth;
        }

        /// <summary>
        /// 状态文案
        /// </summary>
        private static string ResolveStateText(EInteriorColliderState state)
        {
            switch (state)
            {
                case EInteriorColliderState.GeneratedBox:
                    return "已生成 贴合盒";
                case EInteriorColliderState.GeneratedMesh:
                    return "已生成 网格";
                case EInteriorColliderState.Existing:
                    return "已有碰撞体";
                default:
                    return "无碰撞体";
            }
        }
    }
}
