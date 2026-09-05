using DBWeaponSystem;
using Interaction.Combat;
using MieMieFrameWork;
using MiMieEventBus;
using MmInventory;
using UnityEngine;
using DBGameSystem;
using GAS.StateSystem;

namespace Interaction.Player
{
    /// <summary>
    /// 攻击交互 播动画 由动画开窗驱动 WeaponScanner 命中后结算
    /// </summary>
    public class AttackInteractModule : IPlayerInteract
    {
        /// <summary> 重击伤害倍率 </summary>
        private const float HeavyDamageRate = 1.5f;

        /// <summary> 空手攻击力 </summary>
        private const int FistAttackValue = 5;

        /// <summary> 近战扫描器 </summary>
        private WeaponScanner scanner;

        /// <summary> 是否已订阅扫描命中 </summary>
        private bool isScannerBound;

        /// <summary> 本段攻击是否重击 </summary>
        private bool isHeavyAttack;

        /// <summary> 本窗口是否已扣过耐久 </summary>
        private bool hasAppliedDurabilityLossThisWindow;

        /// <summary>
        /// 轻攻击 只播动画 开窗由动画事件触发
        /// </summary>
        public void LightHandAttack()
        {
            EnsureScannerBound();
            isHeavyAttack = false;
            hasAppliedDurabilityLossThisWindow = false;
            GameHub.Get<IPlayerBody>()?.Anim?.PlayLightAttack();
        }

        /// <summary>
        /// 重攻击 只播动画 开窗由动画事件触发
        /// </summary>
        public void HeavyHandAttack()
        {
            EnsureScannerBound();
            isHeavyAttack = true;
            hasAppliedDurabilityLossThisWindow = false;
            GameHub.Get<IPlayerBody>()?.Anim?.PlayHeavyAttack();
        }

        /// <summary>
        /// 每帧检测攻击输入
        /// </summary>
        public void Tick()
        {
            if (BuildInteractModule.IsBuildModeActive())
                return;

            EnsureScannerBound();

            var input = GameHub.Get<IPlayerInput>();
            if (input == null)
                return;

            if (input.IsLeftAttackPressed)
                LightHandAttack();

            if (input.IsRightAttackPressed)
                HeavyHandAttack();
        }

        /// <summary>
        /// 确保 WeaponScanner 命中回调已绑定 幂等懒绑定
        /// </summary>
        private void EnsureScannerBound()
        {
            if (isScannerBound)
                return;

            if (GameHub.Get<IWeaponSystem>() == null)
                return;

            scanner = GameHub.Get<IWeaponSystem>().Scanner;
            if (scanner == null)
                return;

            scanner.OnHit += OnScannerHit;
            scanner.OnWindowClosed += OnScannerWindowClosed;
            isScannerBound = true;
        }

        /// <summary>
        /// 扫描命中结算
        /// </summary>
        private void OnScannerHit(WeaponScanHit scanHit)
        {
            if (scanHit.Collider == null)
                return;

            var damageable = scanHit.Collider.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive)
                return;

            // 搜刮容器没搜过直接不结算伤害
            var scrapSource = scanHit.Collider.GetComponentInParent<IScrapInterface>();
            if (scrapSource != null && !scrapSource.IsAlreadyLooted)
                return;

            int damage = ResolveAttackDamage(isHeavyAttack);
            if (damage <= 0)
                return;

            int appliedDamage = damageable.ApplyDamage(damage, scanHit.Point);
            if (appliedDamage <= 0)
                return;

            GameHub.Get<IPlayerBody>()?.ShakeCameraForAttack(isHeavyAttack);

            // 持武时本窗口首次有效命中按轻/重攻击扣耐久
            if (!hasAppliedDurabilityLossThisWindow
                && GameHub.Get<IWeaponSystem>() != null
                && GameHub.Get<IWeaponSystem>().TryApplyDurabilityLoss(
                    ResolveDurabilityLoss(isHeavyAttack)))
            {
                hasAppliedDurabilityLossThisWindow = true;
            }

            MmGlobalEventBus.GlobalBus.Publish(
                CombatFeedbackEvents.DamageFloatingText,
                scanHit.Point,
                (long)appliedDamage,
                false);
            MmGlobalEventBus.GlobalBus.Publish(CombatFeedbackEvents.AttackCrosshair, true);
        }

        /// <summary>
        /// 攻击窗口关闭 本窗口零命中时补发挥空反馈
        /// </summary>
        private void OnScannerWindowClosed(bool anyHit)
        {
            hasAppliedDurabilityLossThisWindow = false;

            if (anyHit)
                return;

            MmGlobalEventBus.GlobalBus.Publish(CombatFeedbackEvents.AttackCrosshair, false);
        }

        /// <summary>
        /// 解析本次攻击力 空手用模块常量 持武读 WeaponSystem
        /// </summary>
        private int ResolveAttackDamage(bool isHeavy)
        {
            int attackValue = FistAttackValue;
            if (GameHub.Get<IWeaponSystem>() != null
                && GameHub.Get<IWeaponSystem>().TryGetAttackValue(out int weaponAttack))
            {
                attackValue = weaponAttack;
            }

            if (isHeavy)
                attackValue = Mathf.RoundToInt(attackValue * HeavyDamageRate);

            attackValue = Mathf.RoundToInt(
                attackValue * GameHub.Get<IPlayerStatus>().GetCurrentValue(PlayerStatIds.Attack));

            return attackValue;
        }

        /// <summary>
        /// 解析本次耐久消耗 仅使用武器表轻重攻击字段
        /// </summary>
        private int ResolveDurabilityLoss(bool isHeavy)
        {
            if (GameHub.Get<IWeaponSystem>() == null)
                return 0;

            int itemTableId = GameHub.Get<IWeaponSystem>().EquippedItemTableId;
            if (itemTableId <= 0)
                return 0;

            LubanTables.EnsureLoaded();
            var weaponRow = LubanTables.Tables.TbWeapon.GetOrDefault(itemTableId);
            if (weaponRow == null)
                return 0;

            return isHeavy
                ? weaponRow.HeavyDurabilityLoss
                : weaponRow.LightDurabilityLoss;
        }
    }
}
