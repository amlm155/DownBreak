using GAS.StateSystem;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace GAS.Editor.Stat
{
    /// <summary>
    /// Odin 下 GasStat 字符串绘制
    /// </summary>
    public sealed class GasStatStringOdinDrawer : OdinAttributeDrawer<GasStatAttribute, string>
    {
        /// <summary>
        /// 绘制
        /// </summary>
        protected override void DrawPropertyLayout(GUIContent label)
        {
            GasStatAttribute attr = Attribute;
            string current = ValueEntry.SmartValue;
            string next = GasStatDrawHelper.DrawStringPopupLayout(
                label?.text,
                current,
                attr.PassiveOnly,
                attr.ImmediateOnly);

            if (next != current)
                ValueEntry.SmartValue = next;
        }
    }
}
