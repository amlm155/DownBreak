using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Mm_Budier.Editor
{
    /// <summary>
    /// 可旋转的方块预制体 3D 预览
    /// </summary>
    internal static class CubePrefabPreviewUtility
    {
        const float DefaultSize = 220f;
        const float MinSize = 160f;
        const float MaxSize = 320f;

        static PreviewRenderUtility previewUtility;
        static GameObject previewInstance;
        static string sourcePrefabPath;
        static Vector2 orbitAngles = new Vector2(35f, -30f);
        static float zoomDistance = 1.6f;

        const float MinZoomDistance = 0.35f;
        const float MaxZoomDistance = 6f;
        const float BaseDistanceFactor = 2.6f;

        /// <summary>
        /// 绘制可拖拽旋转的预制体预览
        /// </summary>
        public static void Draw(GameObject prefab, float preferredSize = DefaultSize)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("预制体预览", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("按住右键拖动旋转 滚轮缩放", EditorStyles.centeredGreyMiniLabel);

            if (prefab == null)
            {
                EditorGUILayout.HelpBox("指定预制体后显示 3D 预览", MessageType.None);
                return;
            }

            if (!CubePrefabEditUtility.IsEditablePrefab(prefab))
            {
                EditorGUILayout.HelpBox("请指定项目内的预制体资产", MessageType.Warning);
                return;
            }

            var size = Mathf.Clamp(preferredSize, MinSize, MaxSize);
            var viewWidth = EditorGUIUtility.currentViewWidth;
            if (viewWidth > 1f)
                size = Mathf.Min(size, viewWidth - 48f);

            var rect = GUILayoutUtility.GetRect(size, size);
            EditorGUI.DrawRect(rect, new Color(0.14f, 0.14f, 0.14f, 1f));

            EnsurePreview(prefab);
            HandlePreviewInput(rect);

            if (previewInstance != null && Event.current.type == EventType.Repaint)
            {
                DrawInteractivePreview(rect);
            }

            EditorGUILayout.LabelField(prefab.name, EditorStyles.centeredGreyMiniLabel);
        }

        /// <summary>
        /// 预制体被修改后重建预览实例
        /// </summary>
        public static void Invalidate()
        {
            DestroyPreviewInstance();
            sourcePrefabPath = null;
        }

        static void EnsurePreview(GameObject prefab)
        {
            if (prefab == null)
                return;

            var path = AssetDatabase.GetAssetPath(prefab);
            if (previewInstance != null && sourcePrefabPath == path)
                return;

            DestroyPreviewInstance();
            previewUtility ??= CreatePreviewUtility();
            previewInstance = previewUtility.InstantiatePrefabInScene(prefab);
            previewInstance.transform.position = Vector3.zero;
            previewInstance.transform.rotation = Quaternion.identity;
            sourcePrefabPath = path;
            zoomDistance = 1.6f;
        }

        static PreviewRenderUtility CreatePreviewUtility()
        {
            var utility = new PreviewRenderUtility();
            utility.cameraFieldOfView = 28f;
            utility.camera.nearClipPlane = 0.01f;
            utility.camera.farClipPlane = 50f;
            utility.lights[0].intensity = 1.15f;
            utility.lights[0].transform.rotation = Quaternion.Euler(48f, 48f, 0f);
            utility.lights[1].intensity = 0.55f;
            utility.ambientColor = new Color(0.22f, 0.22f, 0.22f, 1f);
            return utility;
        }

        static void HandlePreviewInput(Rect rect)
        {
            var current = Event.current;
            if (!rect.Contains(current.mousePosition))
                return;

            if (current.type == EventType.MouseDrag && current.button == 1)
            {
                orbitAngles.x += current.delta.x;
                orbitAngles.y -= current.delta.y;
                orbitAngles.y = Mathf.Clamp(orbitAngles.y, -85f, 85f);
                current.Use();
                GUIHelper.RequestRepaint();
                return;
            }

            if (current.type == EventType.ScrollWheel)
            {
                // 滚轮向上拉近 向下拉远
                zoomDistance *= 1f - current.delta.y * 0.08f;
                zoomDistance = Mathf.Clamp(zoomDistance, MinZoomDistance, MaxZoomDistance);
                current.Use();
                GUIHelper.RequestRepaint();
            }
        }

        static void DrawInteractivePreview(Rect rect)
        {
            previewUtility.BeginPreview(rect, GUIStyle.none);

            var bounds = CalculateRenderableBounds(previewInstance);
            var center = bounds.center;
            var radius = Mathf.Max(bounds.extents.magnitude, 0.25f);
            var distance = radius * BaseDistanceFactor * zoomDistance;

            var rotation = Quaternion.Euler(orbitAngles.y, orbitAngles.x, 0f);
            var cameraPos = center + rotation * (Vector3.back * distance);
            previewUtility.camera.transform.SetPositionAndRotation(cameraPos, Quaternion.LookRotation(center - cameraPos, Vector3.up));
            previewUtility.camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 3f);
            previewUtility.camera.farClipPlane = distance + radius * 4f;

            // URP 材质需要走 Scriptable Render Pipeline 否则会显示粉色
            previewUtility.Render(allowScriptableRenderPipeline: true);
            var texture = previewUtility.EndPreview();
            if (texture != null)
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
        }

        static Bounds CalculateRenderableBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return bounds;
        }

        static void DestroyPreviewInstance()
        {
            if (previewInstance != null)
            {
                Object.DestroyImmediate(previewInstance);
                previewInstance = null;
            }
        }

        [InitializeOnLoadMethod]
        static void RegisterReloadCleanup()
        {
            AssemblyReloadEvents.beforeAssemblyReload += DisposeShared;
        }

        static void DisposeShared()
        {
            DestroyPreviewInstance();
            sourcePrefabPath = null;

            if (previewUtility == null)
                return;

            previewUtility.Cleanup();
            previewUtility = null;
        }
    }
}
