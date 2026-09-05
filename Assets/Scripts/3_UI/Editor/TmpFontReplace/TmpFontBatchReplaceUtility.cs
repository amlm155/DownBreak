using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DownBreak.UI.Editor
{
    /// <summary>
    /// 单条 TMP 扫描结果
    /// </summary>
    public sealed class TmpFontHit
    {
        /// <summary>
        /// 拖入的根物体 场景对象用
        /// </summary>
        public GameObject Root;

        /// <summary>
        /// 预制体资源路径 场景对象为空
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// 是否为预制体资源
        /// </summary>
        public bool IsPrefabAsset;

        /// <summary>
        /// 相对根的层级路径
        /// </summary>
        public string HierarchyPath;

        /// <summary>
        /// 相对根的兄弟索引路径
        /// </summary>
        public string SiblingPath;

        /// <summary>
        /// 同一节点上的 TMP 序号
        /// </summary>
        public int ComponentIndex;

        /// <summary>
        /// 当前字体
        /// </summary>
        public TMP_FontAsset CurrentFont;

        /// <summary>
        /// 是否位于嵌套预制体实例内
        /// </summary>
        public bool IsNested;

        /// <summary>
        /// 是否参与替换
        /// </summary>
        public bool Selected;
    }

    /// <summary>
    /// 批量替换 TMP 字体的扫描与写入
    /// </summary>
    public static class TmpFontBatchReplaceUtility
    {
        #region 扫描

        /// <summary>
        /// 扫描根物体下全部 TMP_Text
        /// </summary>
        public static void Scan(IList<GameObject> rootList, List<TmpFontHit> hitList)
        {
            hitList.Clear();
            var hitKeyHashList = new HashSet<string>();
            int rootCount = rootList.Count;
            for (int i = 0; i < rootCount; i++)
            {
                GameObject root = rootList[i];
                if (root == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(root);
                bool isPrefabAsset = IsPrefabAssetPath(assetPath);
                if (isPrefabAsset)
                    ScanPrefabAsset(assetPath, hitList, hitKeyHashList);
                else
                    CollectFromRoot(root, string.Empty, false, hitList, hitKeyHashList);
            }
        }

        /// <summary>
        /// 打开预制体内容扫描
        /// </summary>
        private static void ScanPrefabAsset(
            string assetPath,
            List<TmpFontHit> hitList,
            HashSet<string> hitKeyHashList)
        {
            var contents = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                CollectFromRoot(contents, assetPath, true, hitList, hitKeyHashList);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// 从根收集 TMP
        /// </summary>
        private static void CollectFromRoot(
            GameObject root,
            string assetPath,
            bool isPrefabAsset,
            List<TmpFontHit> hitList,
            HashSet<string> hitKeyHashList)
        {
            TMP_Text[] tmpList = root.GetComponentsInChildren<TMP_Text>(true);
            int tmpCount = tmpList.Length;
            for (int i = 0; i < tmpCount; i++)
            {
                var tmp = tmpList[i];
                string siblingPath = GetSiblingPath(tmp.transform, root.transform);
                int componentIndex = GetComponentIndex(tmp);
                string hitKey = BuildHitKey(root, assetPath, siblingPath, componentIndex);
                if (!hitKeyHashList.Add(hitKey))
                    continue;

                // 嵌套实例仍写入当前根 源资源需单独拖入
                var nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(tmp.gameObject);
                bool isNested = nearestRoot != null && nearestRoot != root;

                var hit = new TmpFontHit
                {
                    Root = isPrefabAsset ? null : root,
                    AssetPath = assetPath,
                    IsPrefabAsset = isPrefabAsset,
                    HierarchyPath = GetHierarchyPath(tmp.transform, root.transform),
                    SiblingPath = siblingPath,
                    ComponentIndex = componentIndex,
                    CurrentFont = tmp.font,
                    IsNested = isNested,
                    Selected = true,
                };
                hitList.Add(hit);
            }
        }

        #endregion

        #region 写入

        /// <summary>
        /// 替换已勾选条目
        /// </summary>
        public static int Apply(IList<TmpFontHit> hitList, TMP_FontAsset targetFont)
        {
            int replacedCount = 0;
            var assetHitDict = new Dictionary<string, List<TmpFontHit>>();
            var sceneHitList = new List<TmpFontHit>();
            int hitCount = hitList.Count;
            for (int i = 0; i < hitCount; i++)
            {
                TmpFontHit hit = hitList[i];
                if (!hit.Selected)
                    continue;
                if (hit.CurrentFont == targetFont)
                    continue;

                if (hit.IsPrefabAsset)
                {
                    if (!assetHitDict.TryGetValue(hit.AssetPath, out List<TmpFontHit> groupList))
                    {
                        groupList = new List<TmpFontHit>();
                        assetHitDict.Add(hit.AssetPath, groupList);
                    }

                    groupList.Add(hit);
                }
                else
                {
                    sceneHitList.Add(hit);
                }
            }

            try
            {
                int assetIndex = 0;
                int assetTotal = assetHitDict.Count;
                foreach (var pair in assetHitDict)
                {
                    if (assetTotal > 1)
                        EditorUtility.DisplayProgressBar("批量替换TMP字体", pair.Key, (float)assetIndex / assetTotal);
                    replacedCount += ApplyPrefabAsset(pair.Key, pair.Value, targetFont);
                    assetIndex++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (sceneHitList.Count > 0)
            {
                Undo.SetCurrentGroupName("批量替换TMP字体");
                int undoGroup = Undo.GetCurrentGroup();
                replacedCount += ApplySceneHits(sceneHitList, targetFont);
                Undo.CollapseUndoOperations(undoGroup);
            }

            AssetDatabase.SaveAssets();
            return replacedCount;
        }

        /// <summary>
        /// 写入单个预制体资源
        /// </summary>
        private static int ApplyPrefabAsset(string assetPath, List<TmpFontHit> groupList, TMP_FontAsset targetFont)
        {
            int replacedCount = 0;
            var contents = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                int groupCount = groupList.Count;
                for (int i = 0; i < groupCount; i++)
                {
                    var hit = groupList[i];
                    var tmp = FindTmp(contents, hit);
                    if (tmp == null)
                        continue;
                    if (!ApplyToTmp(tmp, targetFont, false))
                        continue;
                    hit.CurrentFont = targetFont;
                    replacedCount++;
                }

                if (replacedCount > 0)
                    PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return replacedCount;
        }

        /// <summary>
        /// 写入场景中的 TMP
        /// </summary>
        private static int ApplySceneHits(List<TmpFontHit> sceneHitList, TMP_FontAsset targetFont)
        {
            int replacedCount = 0;
            int sceneCount = sceneHitList.Count;
            for (int i = 0; i < sceneCount; i++)
            {
                var hit = sceneHitList[i];
                var tmp = FindTmp(hit.Root, hit);
                if (tmp == null)
                    continue;
                if (!ApplyToTmp(tmp, targetFont, true))
                    continue;
                hit.CurrentFont = targetFont;
                replacedCount++;
            }

            return replacedCount;
        }

        /// <summary>
        /// 写入单个 TMP
        /// </summary>
        private static bool ApplyToTmp(TMP_Text tmp, TMP_FontAsset targetFont, bool recordUndo)
        {
            if (tmp.font == targetFont)
                return false;

            if (recordUndo)
                Undo.RecordObject(tmp, "批量替换TMP字体");

            tmp.font = targetFont;
            tmp.fontSharedMaterial = targetFont.material;
            EditorUtility.SetDirty(tmp);
            if (recordUndo && PrefabUtility.IsPartOfPrefabInstance(tmp))
                PrefabUtility.RecordPrefabInstancePropertyModifications(tmp);

            return true;
        }

        #endregion

        #region 查找与路径

        /// <summary>
        /// 按命中定位 TMP
        /// </summary>
        public static TMP_Text FindTmp(GameObject root, TmpFontHit hit)
        {
            if (root == null)
                return null;

            Transform node = FindBySiblingPath(root.transform, hit.SiblingPath);
            if (node == null)
                return null;

            TMP_Text[] tmpList = node.GetComponents<TMP_Text>();
            if (hit.ComponentIndex < 0 || hit.ComponentIndex >= tmpList.Length)
                return null;

            return tmpList[hit.ComponentIndex];
        }

        /// <summary>
        /// 是否为预制体资源路径
        /// </summary>
        public static bool IsPrefabAssetPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return false;
            return assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 构造去重键
        /// </summary>
        private static string BuildHitKey(GameObject root, string assetPath, string siblingPath, int componentIndex)
        {
            var rootId = root != null ? root.GetEntityId() : EntityId.None;
            return assetPath + "|" + siblingPath + "|" + componentIndex + "|" + rootId;
        }

        /// <summary>
        /// 同一节点上的 TMP 序号
        /// </summary>
        private static int GetComponentIndex(TMP_Text tmp)
        {
            TMP_Text[] tmpList = tmp.GetComponents<TMP_Text>();
            int tmpCount = tmpList.Length;
            for (int i = 0; i < tmpCount; i++)
            {
                if (tmpList[i] == tmp)
                    return i;
            }

            return 0;
        }

        /// <summary>
        /// 相对根的层级路径
        /// </summary>
        private static string GetHierarchyPath(Transform target, Transform root)
        {
            if (target == root)
                return string.Empty;

            var nameList = new List<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                nameList.Add(current.name);
                current = current.parent;
            }

            nameList.Reverse();
            return string.Join("/", nameList);
        }

        /// <summary>
        /// 相对根的兄弟索引路径
        /// </summary>
        private static string GetSiblingPath(Transform target, Transform root)
        {
            if (target == root)
                return string.Empty;

            var indexList = new List<int>();
            Transform current = target;
            while (current != null && current != root)
            {
                indexList.Add(current.GetSiblingIndex());
                current = current.parent;
            }

            indexList.Reverse();
            return string.Join("/", indexList);
        }

        /// <summary>
        /// 按兄弟索引找回节点
        /// </summary>
        private static Transform FindBySiblingPath(Transform root, string siblingPath)
        {
            if (string.IsNullOrEmpty(siblingPath))
                return root;

            Transform current = root;
            string[] partList = siblingPath.Split('/');
            int partCount = partList.Length;
            for (int i = 0; i < partCount; i++)
            {
                int siblingIndex = int.Parse(partList[i]);
                if (siblingIndex < 0 || siblingIndex >= current.childCount)
                    return null;
                current = current.GetChild(siblingIndex);
            }

            return current;
        }

        #endregion
    }
}
