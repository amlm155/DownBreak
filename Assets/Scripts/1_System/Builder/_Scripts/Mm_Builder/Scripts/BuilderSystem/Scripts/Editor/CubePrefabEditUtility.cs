using System;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    /// <summary>
    /// 方块预制体在条目页中的读写
    /// </summary>
    internal static class CubePrefabEditUtility
    {
        public struct Snapshot
        {
            public Vector3 LocalScale;
            public Material SharedMaterial;
            public bool HasMeshRenderer;
        }

        /// <summary>
        /// 预制体是否可作为资产编辑
        /// </summary>
        public static bool IsEditablePrefab(GameObject prefab)
        {
            return prefab != null && PrefabUtility.IsPartOfPrefabAsset(prefab);
        }

        /// <summary>
        /// 读取预制体材质与缩放
        /// </summary>
        public static bool TryReadSnapshot(GameObject prefab, out Snapshot snapshot)
        {
            snapshot = default;
            if (!IsEditablePrefab(prefab))
                return false;

            snapshot.LocalScale = prefab.transform.localScale;

            var renderer = FindMeshRenderer(prefab);
            snapshot.HasMeshRenderer = renderer != null;
            snapshot.SharedMaterial = renderer != null ? renderer.sharedMaterial : null;

            return true;
        }

        /// <summary>
        /// 写回 LocalScale 到预制体资产
        /// </summary>
        public static bool TryApplyLocalScale(GameObject prefab, Vector3 localScale)
        {
            return EditPrefab(prefab, "MmBuilder 预制体缩放", root =>
            {
                root.transform.localScale = localScale;
            });
        }

        /// <summary>
        /// 写回 MeshRenderer 材质到预制体资产
        /// </summary>
        public static bool TryApplyMaterial(GameObject prefab, Material material)
        {
            return EditPrefab(prefab, "MmBuilder 预制体材质", root =>
            {
                var renderer = FindMeshRenderer(root);
                if (renderer == null)
                    return;

                renderer.sharedMaterial = material;
            });
        }

        static bool EditPrefab(GameObject prefabAsset, string undoName, Action<GameObject> edit)
        {
            if (!IsEditablePrefab(prefabAsset))
                return false;

            var path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path))
                return false;

            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                var root = scope.prefabContentsRoot;
                if (root == null)
                    return false;

                Undo.RegisterCompleteObjectUndo(prefabAsset, undoName);
                edit(root);
            }

            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();
            return true;
        }

        static MeshRenderer FindMeshRenderer(GameObject root)
        {
            return root.GetComponent<MeshRenderer>() ?? root.GetComponentInChildren<MeshRenderer>(true);
        }
    }
}
