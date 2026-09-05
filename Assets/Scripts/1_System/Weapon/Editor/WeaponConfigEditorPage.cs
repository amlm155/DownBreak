#if UNITY_EDITOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace DBWeaponSystem.Editor
{
    /// <summary>
    /// 武器挂点配置页 用 PropertyTree 在窗口内可编辑绘制 SO
    /// </summary>
    public sealed class WeaponConfigEditorPage
    {
        private const string ConfigPathKey = "DBWeapon.WeaponDataEditor.WeaponConfigPath";
        private const string DefaultConfigPath =
            "Assets/Resources/Config/WeaponConfig/KnifeWeaponConfig.asset";

        [HideInInspector]
        public WeaponDataEditorWindow Window;

        /// <summary> 挂点 SO 的 Odin 属性树 </summary>
        private PropertyTree configTree;

        /// <summary> 当前属性树绑定的配置 </summary>
        private WeaponConfig treeTarget;

        public WeaponConfigEditorPage(WeaponDataEditorWindow window)
        {
            Window = window;
        }

        [Title("挂点配置")]
        [Required]
        [OnValueChanged(nameof(OnConfigChanged))]
        [LabelText("Weapon Config")]
        public WeaponConfig WeaponConfig;

        [InfoBox("下方挂点列表可在本窗口直接编辑 改完点保存或 Ctrl+S")]
        [Title("挂点内容")]
        [OnInspectorGUI]
        private void DrawConfigContent()
        {
            if (WeaponConfig == null)
            {
                EditorGUILayout.HelpBox("请先指定 Weapon Config", MessageType.Info);
                return;
            }

            EnsureConfigTree();
            if (configTree == null)
                return;

            // 强制可编辑 避免菜单窗口里 InlineEditor 灰态
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();
            configTree.Draw(false);
            if (EditorGUI.EndChangeCheck())
            {
                configTree.ApplyChanges();
                EditorUtility.SetDirty(WeaponConfig);
                Window?.MarkDirty();
            }

            GUI.enabled = wasEnabled;
        }

        [HorizontalGroup("ConfigOps")]
        [Button("定位默认 KnifeWeaponConfig", ButtonSizes.Medium)]
        private void LocateDefaultConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(DefaultConfigPath);
            if (config == null)
            {
                Debug.LogWarning("未找到 " + DefaultConfigPath);
                return;
            }

            WeaponConfig = config;
            OnConfigChanged();
            Selection.activeObject = config;
            EditorGUIUtility.PingObject(config);
        }

        [HorizontalGroup("ConfigOps")]
        [Button("选中并 Ping", ButtonSizes.Medium)]
        private void PingConfig()
        {
            if (WeaponConfig == null)
            {
                Debug.LogWarning("尚未指定 WeaponConfig");
                return;
            }

            Selection.activeObject = WeaponConfig;
            EditorGUIUtility.PingObject(WeaponConfig);
        }

        [HorizontalGroup("ConfigOps")]
        [Button("刷新挂点列表", ButtonSizes.Medium)]
        private void RefreshEditorLists()
        {
            if (WeaponConfig == null)
                return;

            WeaponConfig.PrepareEditorLists();
            RebuildConfigTree();
            Window?.Repaint();
        }

        [Button("保存配置", ButtonSizes.Medium), GUIColor(0.35f, 0.78f, 0.45f)]
        private void SaveConfig()
        {
            CommitAndSave();
        }

        /// <summary>
        /// 写回分页并保存资源
        /// </summary>
        public void CommitAndSave()
        {
            if (WeaponConfig == null)
                return;

            if (configTree != null)
                configTree.ApplyChanges();

            WeaponConfig.CommitEditorLists();
            EditorUtility.SetDirty(WeaponConfig);
            AssetDatabase.SaveAssets();
            Window?.ClearDirty();
            Debug.Log("WeaponConfig 已保存");
        }

        /// <summary>
        /// 从会话加载
        /// </summary>
        public void LoadFromSession()
        {
            string path = EditorPrefs.GetString(ConfigPathKey, DefaultConfigPath);
            WeaponConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(path);
            if (WeaponConfig == null)
                WeaponConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(DefaultConfigPath);

            WeaponConfig?.PrepareEditorLists();
            RebuildConfigTree();
        }

        private void OnConfigChanged()
        {
            string path = WeaponConfig != null
                ? AssetDatabase.GetAssetPath(WeaponConfig)
                : string.Empty;
            EditorPrefs.SetString(ConfigPathKey, path);
            WeaponConfig?.PrepareEditorLists();
            RebuildConfigTree();
            Window?.MarkDirty();
        }

        private void EnsureConfigTree()
        {
            if (WeaponConfig == null)
            {
                DisposeConfigTree();
                return;
            }

            if (configTree != null && treeTarget == WeaponConfig)
                return;

            RebuildConfigTree();
        }

        private void RebuildConfigTree()
        {
            DisposeConfigTree();
            if (WeaponConfig == null)
                return;

            configTree = PropertyTree.Create(WeaponConfig);
            treeTarget = WeaponConfig;
        }

        private void DisposeConfigTree()
        {
            if (configTree != null)
            {
                configTree.Dispose();
                configTree = null;
            }

            treeTarget = null;
        }
    }
}
#endif
