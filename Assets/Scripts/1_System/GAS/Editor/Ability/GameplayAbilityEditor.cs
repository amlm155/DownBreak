using GAS.AbilitySystem;
using GAS.Editor.Tag;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace GAS.Editor.Ability
{
    /// <summary>
    /// Ability Inspector 标签数组在 Editor 侧单独绘制
    /// </summary>
    [CustomEditor(typeof(GameplayAbilityData), true)]
    public sealed class GameplayAbilityEditor : OdinEditor
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
            EditorGUILayout.LabelField("标签条件", EditorStyles.boldLabel);
            GasTagEditorGUI.DrawStringArrayLayout(so.FindProperty("activationRequiredTags"), "激活需要标签");
            GasTagEditorGUI.DrawStringArrayLayout(so.FindProperty("activationBlockedTags"), "激活禁止标签");

            so.ApplyModifiedProperties();
        }
    }
}
