using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DownBreak.UI.Editor
{
    /// <summary>
    /// 批量替换预制体下 TMP 字体
    /// </summary>
    public sealed class TmpFontBatchReplaceWindow : EditorWindow
    {
        /// <summary>
        /// 拖入的根物体
        /// </summary>
        private readonly List<GameObject> prefabList = new List<GameObject>();

        /// <summary>
        /// 扫描结果
        /// </summary>
        private readonly List<TmpFontHit> hitList = new List<TmpFontHit>();

        /// <summary>
        /// 目标字体
        /// </summary>
        private TMP_FontAsset targetFont;

        /// <summary>
        /// 仅替换当前为此字体 空则全部
        /// </summary>
        private TMP_FontAsset filterFont;

        /// <summary>
        /// 预制体列表滚动
        /// </summary>
        private Vector2 prefabScrollPos;

        /// <summary>
        /// 扫描结果滚动
        /// </summary>
        private Vector2 hitScrollPos;

        [MenuItem("Tools/DownBreak/批量替换TMP字体")]
        private static void Open()
        {
            var window = GetWindow<TmpFontBatchReplaceWindow>("批量替换TMP字体");
            window.minSize = new Vector2(560f, 520f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.HelpBox(
                "拖入预制体或文件夹 递归扫描全部 TMP_Text（含隐藏与世界空间）\n" +
                "嵌套预制体上的 TMP 会写成当前预制体覆盖 要改共用源请把嵌套预制体也拖进来\n" +
                "写入预制体资源后不能 Ctrl+Z 场景实例可以撤销",
                MessageType.Info);

            DrawDropArea();
            DrawPrefabList();
            DrawReplaceOptions();
            DrawHitList();
        }

        #region 绘制

        /// <summary>
        /// 拖放区
        /// </summary>
        private void DrawDropArea()
        {
            Rect dropRect = GUILayoutUtility.GetRect(0f, 64f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "把预制体或文件夹拖到这里 可多选\n拖入 TMP 字体资源可直接设为目标字体", EditorStyles.helpBox);
            HandleDragAndDrop(dropRect);
        }

        /// <summary>
        /// 预制体列表
        /// </summary>
        private void DrawPrefabList()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"已加入 {CountValidPrefab()} 个根物体", EditorStyles.boldLabel);
                if (GUILayout.Button("扫描 TMP", GUILayout.Width(88f)))
                    ScanAll();
                using (new EditorGUI.DisabledScope(prefabList.Count == 0))
                {
                    if (GUILayout.Button("清空", GUILayout.Width(52f)))
                    {
                        prefabList.Clear();
                        hitList.Clear();
                    }
                }
            }

            prefabScrollPos = EditorGUILayout.BeginScrollView(prefabScrollPos, GUILayout.Height(120f));
            for (int i = 0; i < prefabList.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    prefabList[i] = (GameObject)EditorGUILayout.ObjectField(prefabList[i], typeof(GameObject), true);
                    if (GUILayout.Button("×", GUILayout.Width(24f)))
                    {
                        prefabList.RemoveAt(i);
                        i--;
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            var addPrefab = (GameObject)EditorGUILayout.ObjectField("追加预制体", null, typeof(GameObject), true);
            if (addPrefab != null)
            {
                TryAddPrefab(addPrefab);
                ScanAll();
            }
        }

        /// <summary>
        /// 字体与过滤
        /// </summary>
        private void DrawReplaceOptions()
        {
            EditorGUILayout.Space(4f);
            targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("目标字体", targetFont, typeof(TMP_FontAsset), false);
            filterFont = (TMP_FontAsset)EditorGUILayout.ObjectField("仅替换此字体 空则全部", filterFont, typeof(TMP_FontAsset), false);

            int selectedCount = CountSelected();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"扫描到 {hitList.Count} 个 TMP  已选 {selectedCount}", EditorStyles.boldLabel);
                if (GUILayout.Button("全选", GUILayout.Width(48f)))
                    SetAllSelected(true);
                if (GUILayout.Button("全不选", GUILayout.Width(60f)))
                    SetAllSelected(false);
                if (GUILayout.Button("反选", GUILayout.Width(48f)))
                    InvertSelected();
            }

            using (new EditorGUI.DisabledScope(selectedCount == 0 || targetFont == null))
            {
                if (GUILayout.Button("批量替换已选 TMP 字体", GUILayout.Height(36f)))
                    ReplaceSelected();
            }
        }

        /// <summary>
        /// 扫描结果列表
        /// </summary>
        private void DrawHitList()
        {
            hitScrollPos = EditorGUILayout.BeginScrollView(hitScrollPos);
            int hitCount = hitList.Count;
            for (int i = 0; i < hitCount; i++)
            {
                var hit = hitList[i];
                bool filterBlocked = filterFont != null && hit.CurrentFont != filterFont;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (filterBlocked)
                    {
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.Toggle(false, GUILayout.Width(18f));
                    }
                    else
                    {
                        hit.Selected = EditorGUILayout.Toggle(hit.Selected, GUILayout.Width(18f));
                    }

                    string rootName = hit.IsPrefabAsset
                        ? System.IO.Path.GetFileNameWithoutExtension(hit.AssetPath)
                        : (hit.Root != null ? hit.Root.name : "(丢失)");
                    string pathLabel = string.IsNullOrEmpty(hit.HierarchyPath) ? "(根)" : hit.HierarchyPath;
                    if (hit.IsNested)
                        pathLabel += "  [嵌套]";
                    EditorGUILayout.LabelField(rootName, GUILayout.Width(140f));
                    EditorGUILayout.LabelField(pathLabel);
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(hit.CurrentFont, typeof(TMP_FontAsset), false, GUILayout.Width(160f));
                    }

                    if (GUILayout.Button("定位", GUILayout.Width(44f)))
                        PingHit(hit);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region 交互

        /// <summary>
        /// 处理拖放
        /// </summary>
        private void HandleDragAndDrop(Rect dropRect)
        {
            Event currentEvent = Event.current;
            if (!dropRect.Contains(currentEvent.mousePosition))
                return;

            var eEventType = currentEvent.type;
            if (eEventType != EventType.DragUpdated && eEventType != EventType.DragPerform)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (eEventType == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                bool added = false;
                Object[] objectList = DragAndDrop.objectReferences;
                int objectCount = objectList.Length;
                for (int i = 0; i < objectCount; i++)
                {
                    Object obj = objectList[i];
                    if (obj is TMP_FontAsset fontAsset)
                    {
                        targetFont = fontAsset;
                        continue;
                    }

                    if (TryAddDropped(obj))
                        added = true;
                }

                if (added)
                    ScanAll();
            }

            currentEvent.Use();
        }

        /// <summary>
        /// 加入拖入对象
        /// </summary>
        private bool TryAddDropped(Object obj)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                bool added = false;
                string[] guidList = AssetDatabase.FindAssets("t:Prefab", new[] { assetPath });
                int guidCount = guidList.Length;
                for (int i = 0; i < guidCount; i++)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(guidList[i]);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (TryAddPrefab(prefab))
                        added = true;
                }

                return added;
            }

            var go = obj as GameObject;
            if (go == null)
                return false;

            return TryAddPrefab(go);
        }

        /// <summary>
        /// 加入根物体 已存在则跳过
        /// </summary>
        private bool TryAddPrefab(GameObject go)
        {
            if (go == null)
                return false;

            string addPath = AssetDatabase.GetAssetPath(go);
            if (!string.IsNullOrEmpty(addPath) && !TmpFontBatchReplaceUtility.IsPrefabAssetPath(addPath))
                return false;

            var addId = go.GetEntityId();
            int prefabCount = prefabList.Count;
            for (int i = 0; i < prefabCount; i++)
            {
                var existing = prefabList[i];
                if (existing == null)
                    continue;
                if (existing.GetEntityId() == addId)
                    return false;
                if (!string.IsNullOrEmpty(addPath) && AssetDatabase.GetAssetPath(existing) == addPath)
                    return false;
            }

            prefabList.Add(go);
            return true;
        }

        /// <summary>
        /// 扫描全部根物体
        /// </summary>
        private void ScanAll()
        {
            TmpFontBatchReplaceUtility.Scan(prefabList, hitList);
            if (filterFont != null)
            {
                int hitCount = hitList.Count;
                for (int i = 0; i < hitCount; i++)
                {
                    TmpFontHit hit = hitList[i];
                    hit.Selected = hit.CurrentFont == filterFont;
                }
            }

            Repaint();
        }

        /// <summary>
        /// 执行替换
        /// </summary>
        private void ReplaceSelected()
        {
            if (filterFont != null)
            {
                int hitCount = hitList.Count;
                for (int i = 0; i < hitCount; i++)
                {
                    TmpFontHit hit = hitList[i];
                    if (hit.CurrentFont != filterFont)
                        hit.Selected = false;
                }
            }

            int selectedCount = CountSelected();
            if (selectedCount == 0 || targetFont == null)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "批量替换TMP字体",
                $"将把 {selectedCount} 个 TMP 改为 {targetFont.name}\n预制体资源无法撤销",
                "替换",
                "取消");
            if (!confirmed)
                return;

            int replacedCount = TmpFontBatchReplaceUtility.Apply(hitList, targetFont);
            ScanAll();
            EditorUtility.DisplayDialog(
                "批量替换TMP字体",
                $"已替换 {replacedCount} 个 TMP",
                "确定");
        }

        /// <summary>
        /// 定位命中对象
        /// </summary>
        private void PingHit(TmpFontHit hit)
        {
            if (hit.IsPrefabAsset)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(hit.AssetPath);
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
                return;
            }

            TMP_Text tmp = TmpFontBatchReplaceUtility.FindTmp(hit.Root, hit);
            if (tmp == null)
                return;

            EditorGUIUtility.PingObject(tmp.gameObject);
            Selection.activeGameObject = tmp.gameObject;
        }

        /// <summary>
        /// 有效根数量
        /// </summary>
        private int CountValidPrefab()
        {
            int count = 0;
            int prefabCount = prefabList.Count;
            for (int i = 0; i < prefabCount; i++)
            {
                if (prefabList[i] != null)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 已选数量
        /// </summary>
        private int CountSelected()
        {
            int count = 0;
            int hitCount = hitList.Count;
            for (int i = 0; i < hitCount; i++)
            {
                TmpFontHit hit = hitList[i];
                if (!hit.Selected)
                    continue;
                if (filterFont != null && hit.CurrentFont != filterFont)
                    continue;
                count++;
            }

            return count;
        }

        /// <summary>
        /// 全选或全不选
        /// </summary>
        private void SetAllSelected(bool selected)
        {
            int hitCount = hitList.Count;
            for (int i = 0; i < hitCount; i++)
            {
                TmpFontHit hit = hitList[i];
                if (filterFont != null && hit.CurrentFont != filterFont)
                {
                    hit.Selected = false;
                    continue;
                }

                hit.Selected = selected;
            }
        }

        /// <summary>
        /// 反选
        /// </summary>
        private void InvertSelected()
        {
            int hitCount = hitList.Count;
            for (int i = 0; i < hitCount; i++)
            {
                TmpFontHit hit = hitList[i];
                if (filterFont != null && hit.CurrentFont != filterFont)
                {
                    hit.Selected = false;
                    continue;
                }

                hit.Selected = !hit.Selected;
            }
        }

        #endregion
    }
}
