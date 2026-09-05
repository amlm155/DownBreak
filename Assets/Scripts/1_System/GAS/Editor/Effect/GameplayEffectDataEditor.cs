using GAS.Core.GameplayEffect;
using GAS.Editor.Tag;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace GAS.Editor.Effect
{
    /// <summary>
    /// GE Inspector 标签数组在 Editor 侧单独绘制
    /// </summary>
    [CustomEditor(typeof(GameplayEffectData))]
    public sealed class GameplayEffectDataEditor : OdinEditor
    {
        /// <summary>
        /// 绘制
        /// </summary>
        public override void OnInspectorGUI()
        {
            SerializedObject so = serializedObject;
            so.Update();

            EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
            GasTagEditorGUI.DrawStringArrayLayout(so.FindProperty("gameplayTags"), "分类标签");
            GasTagEditorGUI.DrawStringArrayLayout(so.FindProperty("requiredNeedTags"), "需要标签");
            GasTagEditorGUI.DrawStringArrayLayout(so.FindProperty("requiredBanTags"), "禁止标签");
            EditorGUILayout.Space(6f);
            so.ApplyModifiedProperties();

            base.OnInspectorGUI();
        }
    }
}
