#if UNITY_EDITOR
using Mm_Budier.Editor;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector 中为每个分区提供 Scene 两点取格入口
/// </summary>
public class VirtualGridGroupDrawer : OdinValueDrawer<BuilderVirtualGridGroup>
{
    const float ActionRowHeight = 22f;

    protected override void DrawPropertyLayout(GUIContent label)
    {
        var group = ValueEntry.SmartValue;
        var isPicking = GridRegionScenePicker.IsPickingGroup(group);

        if (isPicking)
            GUIHelper.RequestRepaint();

        SirenixEditorGUI.BeginBox();
        CallNextDrawer(label);
        DrawScenePickFooter(group, isPicking);
        SirenixEditorGUI.EndBox();
    }

    /// <summary>
    /// 分区项底部 Scene 取格操作区
    /// </summary>
    static void DrawScenePickFooter(BuilderVirtualGridGroup group, bool isPicking)
    {
        EditorGUILayout.Space(4f);

        var footerStyle = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(6, 6, 5, 5),
            margin = new RectOffset(0, 0, 0, 0)
        };

        EditorGUILayout.BeginVertical(footerStyle);
        EditorGUILayout.LabelField("Scene 取格", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        var prevBg = GUI.backgroundColor;
        if (isPicking)
            GUI.backgroundColor = new Color(0.55f, 0.88f, 1f);

        var pickTip = "在 Scene 连续点击两格对角设定 XZ\n高度用「尺寸 Y」\nEsc 取消";
        if (GUILayout.Button(new GUIContent("两点取格", pickTip), GUILayout.Height(ActionRowHeight)))
            GridRegionScenePicker.Begin(group, BuilderVirtualGrid.EditorGridUnitSize);

        if (isPicking)
        {
            if (GUILayout.Button("取消", GUILayout.Width(48f), GUILayout.Height(ActionRowHeight)))
                GridRegionScenePicker.Cancel();
        }

        GUI.backgroundColor = prevBg;
        EditorGUILayout.EndHorizontal();

        if (isPicking)
        {
            EditorGUILayout.LabelField(
                GridRegionScenePicker.PickStepHint,
                EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }
}
#endif
