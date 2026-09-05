#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace DBWeaponSystem.Editor
{
    /// <summary>
    /// 武器数据 Odin 树形编辑器
    /// </summary>
    public sealed class WeaponDataEditorWindow : OdinMenuEditorWindow
    {
        private const string BaseTitle = "Weapon Data Editor";

        private WeaponConfigEditorPage configPage;
        private WeaponBladeSetupPage bladePage;
        private PrefabInteractSetupPage prefabSetupPage;
        private bool isDirty;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/DownBreak/Weapon Data Editor")]
        private static void Open()
        {
            var window = GetWindow<WeaponDataEditorWindow>();
            window.minSize = new Vector2(900f, 560f);
            window.ClearDirty();
            window.Show();
        }

        /// <summary>
        /// 标记为已修改
        /// </summary>
        public void MarkDirty()
        {
            if (isDirty)
                return;
            isDirty = true;
            UpdateTitle();
        }

        /// <summary>
        /// 清除修改标记
        /// </summary>
        public void ClearDirty()
        {
            if (!isDirty)
                return;
            isDirty = false;
            UpdateTitle();
        }

        /// <summary>
        /// 保存全部
        /// </summary>
        public void SaveAll()
        {
            configPage?.CommitAndSave();
        }

        /// <summary>
        /// 重建目录树
        /// </summary>
        public void RequestTreeRebuild()
        {
            ForceMenuTreeRebuild();
            Repaint();
        }

        protected override void OnImGUI()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown
                && currentEvent.control
                && currentEvent.keyCode == KeyCode.S)
            {
                SaveAll();
                currentEvent.Use();
            }

            base.OnImGUI();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            if (configPage == null)
            {
                configPage = new WeaponConfigEditorPage(this);
                configPage.LoadFromSession();
            }
            else
            {
                configPage.Window = this;
            }

            if (bladePage == null)
            {
                bladePage = new WeaponBladeSetupPage(this);
                bladePage.LoadFromSession();
            }
            else
            {
                bladePage.Window = this;
            }

            if (prefabSetupPage == null)
            {
                prefabSetupPage = new PrefabInteractSetupPage(this);
                prefabSetupPage.LoadFromSession();
            }
            else
            {
                prefabSetupPage.Window = this;
            }

            var tree = new OdinMenuTree(supportsMultiSelect: false)
            {
                Config = { DrawSearchToolbar = true }
            };

            tree.Add("配置页", configPage);
            tree.Add("武器配置页", bladePage);
            tree.Add("预制体交互配置", prefabSetupPage);
            return tree;
        }

        /// <summary>
        /// 更新窗口标题
        /// </summary>
        private void UpdateTitle()
        {
            titleContent = new GUIContent(isDirty ? $"{BaseTitle} *" : BaseTitle);
        }
    }
}
#endif
