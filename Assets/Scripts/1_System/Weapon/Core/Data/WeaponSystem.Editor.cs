#if UNITY_EDITOR
using cfg.item;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace DBWeaponSystem
{
    public partial class WeaponSystem
    {
        #region 编辑器工具

        [Button("捕获当前武器本地变换到配置"), PropertyOrder(100)]
        private void CaptureCurrentWeaponPoseToConfig()
        {
            if (currentWeaponGo == null)
            {
                Debug.LogWarning("当前没有武器实例");
                return;
            }

            if (weaponConfig == null)
            {
                Debug.LogWarning("武器配置未设置");
                return;
            }

            if (equippedWeaponName == EWeaponName.None)
            {
                Debug.LogWarning("当前武器挂点名无效");
                return;
            }

            if (!weaponConfig.TryGetHandPosConfig(equippedWeaponName, out var config))
            {
                Debug.LogWarning($"未找到武器 {equippedWeaponName} 的挂点配置");
                return;
            }

            config.CopyFromTransform(currentWeaponGo.transform);
            weaponConfig.SetHandPosConfig(
                equippedWeaponName,
                equippedAnimationType,
                config);
            EditorUtility.SetDirty(weaponConfig);
            AssetDatabase.SaveAssets();
            Debug.Log($"已捕获 {equippedWeaponName} 本地变换到 WeaponConfig");
        }

        #endregion
    }
}
#endif
