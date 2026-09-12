using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Interaction.Editor
{
    /// <summary>
    /// 放置锚点编辑器辅助 创建锚点 与 生成/移除承托碰撞体
    /// </summary>
    public static class ItemPlacementAnchorEditorUtility
    {
        /// <summary> 锚点默认名字 </summary>
        private const string AnchorObjectName = "ItemAnchor";

        /// <summary>
        /// 在选中物体下创建放置锚点
        /// </summary>
        [MenuItem("GameObject/DownBreak/添加物品放置锚点", false, 31)]
        private static void CreateAnchorFromSelection()
        {
            var anchor = CreateAnchor(Selection.activeGameObject);
            if (anchor == null)
                return;

            Selection.activeGameObject = anchor.gameObject;
            EditorGUIUtility.PingObject(anchor.gameObject);
        }

        /// <summary>
        /// 创建放置锚点 父物体为空则建在场景根
        /// </summary>
        public static ItemPlacementAnchor CreateAnchor(GameObject parent)
        {
            var anchorObject = new GameObject(AnchorObjectName);
            Undo.RegisterCreatedObjectUndo(anchorObject, "创建物品放置锚点");

            if (parent != null)
            {
                anchorObject.transform.SetParent(parent.transform, false);
                anchorObject.transform.rotation = parent.transform.rotation;
                anchorObject.transform.position = ResolveDefaultAnchorPosition(parent);
            }

            var anchor = anchorObject.AddComponent<ItemPlacementAnchor>();
            if (anchor == null)
                return null;

            EditorUtility.SetDirty(anchor);
            return anchor;
        }

        /// <summary>
        /// 没有手动拖过时 默认放到父物体碰撞体的顶面中心
        /// </summary>
        private static Vector3 ResolveDefaultAnchorPosition(GameObject parent)
        {
            var collider = parent.GetComponentInChildren<Collider>();
            if (collider == null)
                return parent.transform.position;

            var bounds = collider.bounds;
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        /// <summary>
        /// 生成或更新承托碰撞体 薄板顶面与锚点面重合
        /// </summary>
        public static bool EnsureSupportCollider(ItemPlacementAnchor anchor)
        {
            if (anchor == null)
                return false;

            var supportTransform = anchor.transform.Find(ItemPlacementAnchor.SupportColliderName);
            GameObject supportObject;
            if (supportTransform == null)
            {
                supportObject = new GameObject(ItemPlacementAnchor.SupportColliderName);
                Undo.RegisterCreatedObjectUndo(supportObject, "生成承托碰撞体");
                supportObject.transform.SetParent(anchor.transform, false);
            }
            else
            {
                supportObject = supportTransform.gameObject;
            }

            // 承托台挂在平面下方 顶面与锚点平面重合
            float thickness = anchor.SupportThickness;
            supportObject.transform.localPosition = Vector3.down * (thickness * 0.5f);
            supportObject.transform.localRotation = Quaternion.identity;
            supportObject.transform.localScale = Vector3.one;

            var boxCollider = supportObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
                boxCollider = Undo.AddComponent<BoxCollider>(supportObject);

            if (boxCollider == null)
                return false;

            Vector2 areaSizeXZ = anchor.AreaSizeXZ;
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(areaSizeXZ.x, thickness, areaSizeXZ.y);
            boxCollider.isTrigger = false;

            EditorUtility.SetDirty(boxCollider);
            EditorUtility.SetDirty(supportObject);
            return true;
        }

        /// <summary>
        /// 移除承托碰撞体节点
        /// </summary>
        public static bool RemoveSupportCollider(ItemPlacementAnchor anchor)
        {
            if (anchor == null)
                return false;

            var supportTransform = anchor.transform.Find(ItemPlacementAnchor.SupportColliderName);
            if (supportTransform == null)
                return false;

            Undo.DestroyObjectImmediate(supportTransform.gameObject);
            return true;
        }

        /// <summary>
        /// 给目标下全部锚点生成承托碰撞体 返回处理数量
        /// </summary>
        public static int EnsureAllSupportColliders(GameObject root)
        {
            if (root == null)
                return 0;

            var anchorList = root.GetComponentsInChildren<ItemPlacementAnchor>(true);
            int count = 0;
            for (int i = 0; i < anchorList.Length; i++)
            {
                var anchor = anchorList[i];
                if (anchor == null || !anchor.UseAsSupportSurface)
                    continue;

                if (EnsureSupportCollider(anchor))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 收集目标下的全部锚点
        /// </summary>
        public static List<ItemPlacementAnchor> CollectAnchors(GameObject root)
        {
            var resultList = new List<ItemPlacementAnchor>();
            if (root == null)
                return resultList;

            var anchorList = root.GetComponentsInChildren<ItemPlacementAnchor>(true);
            for (int i = 0; i < anchorList.Length; i++)
            {
                if (anchorList[i] != null && anchorList[i].gameObject.activeInHierarchy)
                    resultList.Add(anchorList[i]);
            }

            return resultList;
        }
    }

    /// <summary>
    /// 放置锚点检视面板 附承托碰撞体按钮
    /// </summary>
    [CustomEditor(typeof(ItemPlacementAnchor))]
    public sealed class ItemPlacementAnchorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var anchor = target as ItemPlacementAnchor;
            if (anchor == null)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("承托面", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!anchor.UseAsSupportSurface))
                {
                    if (GUILayout.Button("生成/更新承托碰撞体"))
                        ItemPlacementAnchorEditorUtility.EnsureSupportCollider(anchor);
                }

                if (GUILayout.Button("移除承托碰撞体"))
                    ItemPlacementAnchorEditorUtility.RemoveSupportCollider(anchor);
            }

            EditorGUILayout.HelpBox(
                "承托碰撞体是一块薄 BoxCollider 顶面与锚点面重合 用来托住摆上去的物品。\n" +
                "不想加碰撞体时 可在工具里勾选「静态摆放」把物品冻结在原地。",
                MessageType.None);
        }
    }
}
