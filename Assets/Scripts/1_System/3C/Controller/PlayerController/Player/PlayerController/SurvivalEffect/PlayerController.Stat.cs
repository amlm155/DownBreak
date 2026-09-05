using GAS.StateSystem;
using MieMieFrameWork;
using UnityEngine;

namespace PlayerControllerSpace
{
    /// <summary>
    /// 玩家数值调整 只设速率与发起改值 计算由 GAS 完成
    /// 对外通知由 StatController 直接发布 PlayerStatEvents
    /// </summary>
    public partial class PlayerController
    {
        /// <summary>
        /// 是否处于体力消耗后的等待阶段
        /// </summary>
        private bool isPowerReduce;

        /// <summary>
        /// 体力恢复等待计时器 ID
        /// </summary>
        private int powerRecoveryTimerId = -1;

        /// <summary>
        /// 初始化玩家属性速率
        /// </summary>
        internal void InitStatEvents()
        {
            // 水分与饱食恒定自然下降
            statController.SetTickPerSecond(PlayerStatIds.Water, -PlayerRtConfig.AutoReduceWaterSpeed);
            statController.SetTickPerSecond(PlayerStatIds.Food, -PlayerRtConfig.AutoReduceFoodSpeed);
        }

        #region 体力

        /// <summary>
        /// 改变玩家体力
        /// </summary>
        /// <param name="powerDelta">体力变化量</param>
        public void ChangePlayerPowerValue(float powerDelta)
        {
            statController.ChangeValue(PlayerStatIds.Power, powerDelta, true);
        }

        /// <summary>
        /// 更新玩家体力变化速率 冲刺消耗 等待后恢复
        /// </summary>
        public void UpdatePlayerPower()
        {
            var powerStat = statController.GetImStat(PlayerStatIds.Power);
            bool isSprintRequested = IsSprintHeld && IsMoving;

            // 冲刺状态 消耗体力
            if (isSprintRequested)
            {
                // 体力耗尽 停止消耗
                if (powerStat.CurrentValue <= 0f)
                {
                    statController.SetTickPerSecond(PlayerStatIds.Power, 0f);
                    return;
                }

                // 进入体力消耗状态
                BeginPowerReduce();
                // 设置体力每秒消耗速率
                statController.SetTickPerSecond(PlayerStatIds.Power, -PlayerRtConfig.ReducePowerSpeed);
                return;
            }

            // 恢复等待阶段 不消耗不恢复
            if (isPowerReduce)
            {
                statController.SetTickPerSecond(PlayerStatIds.Power, 0f);
                StartPowerRecoveryTimer();
                return;
            }

            // 体力已满 停止恢复
            if (powerStat.CurrentValue >= powerStat.MaxValue)
            {
                statController.SetTickPerSecond(PlayerStatIds.Power, 0f);
                return;
            }

            // 正常恢复体力
            statController.SetTickPerSecond(PlayerStatIds.Power, PlayerRtConfig.AutoRecoveryPowerSpeed);
        }

        /// <summary>
        /// 进入体力消耗状态
        /// </summary>
        private void BeginPowerReduce()
        {
            if (powerRecoveryTimerId != -1)
            {
                ModuleHub.Instance.GetManager<UniTimerManager>().StopTimer(powerRecoveryTimerId);
                powerRecoveryTimerId = -1;
            }

            isPowerReduce = true;
        }

        /// <summary>
        /// 启动体力恢复等待计时器
        /// </summary>
        private void StartPowerRecoveryTimer()
        {
            if (powerRecoveryTimerId != -1)
                return;

            powerRecoveryTimerId = ModuleHub.Instance.GetManager<UniTimerManager>()
                .StartTimer(PlayerRtConfig.RecoveryPowerWaitTime, () =>
                {
                    isPowerReduce = false;
                    powerRecoveryTimerId = -1;
                });
        }

        #endregion

        #region 水和饱食度

        /// <summary>
        /// 改变玩家水分
        /// </summary>
        /// <param name="waterDelta">水分变化量</param>
        public void ChangePlayerWaterValue(float waterDelta)
        {
            statController.ChangeValue(PlayerStatIds.Water, waterDelta, true);
        }

        /// <summary>
        /// 改变玩家饱食度
        /// </summary>
        /// <param name="foodDelta">饱食度变化量</param>
        public void ChangePlayerFoodValue(float foodDelta)
        {
            statController.ChangeValue(PlayerStatIds.Food, foodDelta, true);
        }

        #endregion

        #region 血量

        public bool IsAlive => statController != null
            && statController.GetCurrentValue(PlayerStatIds.Health) > 0f;

        /// <summary>
        /// 应用玩家受到的原始伤害
        /// </summary>
        /// <param name="damage">原始伤害</param>
        /// <param name="hitPoint">受击位置</param>
        public int ApplyDamage(int damage, Vector3 hitPoint)
        {
            if (!IsAlive || damage <= 0)
                return 0;

            /// <summary>
            /// 当前防御倍率
            /// </summary>
            float defenceMultiplier = statController.GetCurrentValue(PlayerStatIds.Defence);

            /// <summary>
            /// 防御倍率换算后的实际伤害
            /// </summary>
            // 防御倍率为 1 表示不改变承伤 数值越高受到的伤害越低
            int appliedDamage = Mathf.Max(1, Mathf.RoundToInt(damage / defenceMultiplier));

            /// <summary>
            /// 受伤前的玩家血量
            /// </summary>
            float healthBefore = statController.GetCurrentValue(PlayerStatIds.Health);
            ChangeHealthValue(-appliedDamage);
            return Mathf.RoundToInt(Mathf.Min(healthBefore, appliedDamage));
        }

        /// <summary>
        /// 改变玩家血量
        /// </summary>
        /// <param name="healthDelta">血量变化量</param>
        public void ChangeHealthValue(float healthDelta)
        {
            var healthStat = statController.GetImStat(PlayerStatIds.Health);
            if (healthStat.CurrentValue <= 0f)
            {
                // TODO:玩家死亡
                Debug.LogWarning("玩家血量不足 死亡");
                return;
            }

            statController.ChangeValue(PlayerStatIds.Health, healthDelta, true);
        }

        #endregion

        #region 理智值

        /// <summary>
        /// 改变玩家理智值
        /// </summary>
        /// <param name="sanDelta">理智变化量</param>
        public void ChangeSanValue(float sanDelta)
        {
            statController.ChangeValue(PlayerStatIds.San, sanDelta, true);
        }

        #endregion
    }
}
