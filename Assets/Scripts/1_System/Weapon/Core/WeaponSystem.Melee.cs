using MieMieFrameWork;
using UnityEngine;

namespace DBWeaponSystem
{
    public partial class WeaponSystem
    {
        /// <summary> 近战轨迹扫描 </summary>
        private WeaponScanner weaponScanner;

        [SerializeField]
        /// <summary> 空手刃根 可空 </summary>
        private Transform fistBladeRoot;

        [SerializeField]
        /// <summary> 空手刃尖 可空则回退右手挂点 </summary>
        private Transform fistBladeTip;

        /// <summary> 近战扫描器 </summary>
        public WeaponScanner Scanner => weaponScanner;

        #region 刃点绑定

        /// <summary>
        /// 绑定武器刃根刃尖到扫描器 并灌入该武器扫描参数
        /// </summary>
        private void BindWeaponBlade(GameObject weapon)
        {
            if (weaponScanner == null || weapon == null)
                return;

            var root = FindBladePoint(weapon.transform, "BladeRoot");
            var tip = FindBladePoint(weapon.transform, "BladeTip");
            if (tip == null)
                tip = FindBladePoint(weapon.transform, "HitPoint");

            if (tip == null)
            {
                Debug.LogWarning($"武器无 BladeTip/HitPoint 回退空手挂点 name={weapon.name}", weapon);
                BindFistBlade();
                return;
            }

            weaponScanner.BindBlade(root, tip);
            ApplyEquippedWeaponScanParams();
        }

        /// <summary>
        /// 绑定空手扫描点并恢复默认扫描参数
        /// </summary>
        private void BindFistBlade()
        {
            if (weaponScanner == null)
                return;

            weaponScanner.RestoreDefaultScanParams();

            // 如果空手刃尖存在 则绑定空手刃根刃尖
            if (fistBladeTip != null)
            {
                weaponScanner.BindBlade(fistBladeRoot, fistBladeTip);
                return;
            }

            // 如果空手刃尖不存在 则绑定右手武器挂点
            if (weaponAsyncHandPos != null && weaponAsyncHandPos.RightHandWeaponPos != null)
                weaponScanner.BindHitPoint(weaponAsyncHandPos.RightHandWeaponPos);
        }

        /// <summary>
        /// 按当前装备武器名写入扫描半径与位移余量
        /// </summary>
        private void ApplyEquippedWeaponScanParams()
        {
            if (weaponScanner == null || weaponConfig == null)
                return;

            if (!weaponConfig.TryGetHandPosConfig(equippedWeaponName, out var handPosConfig)
                || handPosConfig == null)
            {
                weaponScanner.RestoreDefaultScanParams();
                return;
            }

            weaponScanner.ApplyScanParams(handPosConfig.ScanRadius, handPosConfig.DistancePadding);
        }

        /// <summary>
        /// 按名查找刃点
        /// </summary>
        private static Transform FindBladePoint(Transform root, string pointName)
        {
            if (root == null || string.IsNullOrEmpty(pointName))
                return null;

            if (root.name == pointName)
                return root;

            return root.FindDeepChild(pointName);
        }

        #endregion
    }
}
