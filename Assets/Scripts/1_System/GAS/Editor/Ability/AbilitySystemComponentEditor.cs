using GAS.Component;
using GAS.Editor.Tag;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace GAS.Editor.Ability
{
    /// <summary>
    /// ASC Inspector 初始标签在 Editor 侧单独绘制
    /// </summary>
    [CustomEditor(typeof(AbilitySystemMgr))]
    public sealed class AbilitySystemComponentEditor : OdinEditor
    {
        /// <summary>
        /// 绘制
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            SerializedObject so = serializedObject;
            so.Update();
            EditorGUILayout.Space(6f);
            GasTagEditorGUI.DrawStringArrayLayout(so.FindProperty("initialGameplayTags"), "初始标签");
            so.ApplyModifiedProperties();
        }
    }
}
