#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace DBWeaponSystem
{
    /// <summary>
    /// WeaponConfig Odin 编辑器
    /// </summary>
    [CustomEditor(typeof(WeaponConfig))]
    public class WeaponConfigEditor : OdinEditor
    {
    }
}
#endif
