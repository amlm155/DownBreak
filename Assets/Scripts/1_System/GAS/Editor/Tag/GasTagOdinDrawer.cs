using GAS.TagSystem;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace GAS.Editor.Tag
{
    /// <summary>
    /// Odin 仅绘制单个 string 的 GasTag
    /// string[] 请配合 DrawWithUnity 走 Unity PropertyDrawer 避免双绘
    /// </summary>
    public sealed class GasTagStringOdinDrawer : OdinAttributeDrawer<GasTagAttribute, string>
    {
        /// <summary>
        /// 绘制字符串标签
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            string current = ValueEntry.SmartValue;
            string next = GasTagDrawHelper.DrawStringPopupLayout(label?.text, current);
            if (next != current)
                ValueEntry.SmartValue = next;
        }
    }
}
