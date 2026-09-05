/// <summary>
/// PlayerPanel 生存数值条 准心 残血与理智 HUD
/// </summary>

using System;
using System.Collections.Generic;
using MiMieEventBus;
using GAS.StateSystem;
using Interaction.Combat;
using UnityEngine;
using MieMieFrameWork;
namespace MieMieUIFrameWork.Runtime
{
    public partial class PlayerPanel
    {
        /// <summary>
        /// 玩家状态事件订阅令牌集合
        /// </summary>
        private readonly List<IDisposable> statEventDisposableList = new List<IDisposable>();

        /// <summary> 准心反馈订阅 </summary>
        private IDisposable attackCrosshairDisposable;

        /// <summary> 上次血量 用于判定受伤闪红 </summary>
        private float lastHealth = -1f;

        /// <summary> 残血冷色滤镜血量区间 对齐原 PlayerConfig </summary>
        [SerializeField]
        private Vector2Int criticalThreshold = new Vector2Int(10, 20);

        /// <summary> 濒死黑边血量区间 </summary>
        [SerializeField]
        private Vector2Int highHpThreshold = new Vector2Int(1, 9);

        /// <summary> 轻度理智区间 </summary>
        [SerializeField]
        private Vector2Int sanMildThreshold = new Vector2Int(40, 60);

        /// <summary> 中度理智区间 </summary>
        [SerializeField]
        private Vector2Int sanMediumThreshold = new Vector2Int(20, 39);

        /// <summary> 重度理智区间 </summary>
        [SerializeField]
        private Vector2Int sanSevereThreshold = new Vector2Int(1, 19);

        #region 状态订阅

        /// <summary>
        /// 订阅玩家状态事件
        /// </summary>
        private void SubscribeStatEvents()
        {
            SubscribeStatEvent(PlayerStatEvents.PowerChanged, OnPowerChanged);
            SubscribeStatEvent(PlayerStatEvents.WaterChanged, OnWaterChanged);
            SubscribeStatEvent(PlayerStatEvents.FoodChanged, OnFoodChanged);
            SubscribeStatEvent(PlayerStatEvents.HealthChanged, OnHealthChanged);
            SubscribeStatEvent(PlayerStatEvents.SanChanged, OnSanChanged);
        }

        /// <summary>
        /// 订阅战斗准心反馈
        /// </summary>
        private void BindCombatFeedbackEvents()
        {
            attackCrosshairDisposable = MmGlobalEventBus.GlobalBus.Subscribe(
                CombatFeedbackEvents.AttackCrosshair,
                OnAttackCrosshair);
        }

        /// <summary>
        /// 取消准心反馈订阅
        /// </summary>
        private void UnbindCombatFeedbackEvents()
        {
            attackCrosshairDisposable?.Dispose();
            attackCrosshairDisposable = null;
        }

        /// <summary>
        /// 准心命中或挥空
        /// </summary>
        private void OnAttackCrosshair(bool hitTarget)
        {
            if (hitTarget)
                PlayCrosshairHitMarker();
            else
                PlayCrosshairAttackPunch();
        }

        /// <summary>
        /// 订阅单个玩家状态事件
        /// </summary>
        /// <typeparam name="T0">第一参数类型</typeparam>
        /// <typeparam name="T1">第二参数类型</typeparam>
        /// <typeparam name="T2">第三参数类型</typeparam>
        /// <param name="eventKey">事件 Key</param>
        /// <param name="action">回调</param>
        private void SubscribeStatEvent<T0, T1, T2>(EventKey<T0, T1, T2> eventKey, Action<T0, T1, T2> action)
        {
            statEventDisposableList.Add(MmGlobalEventBus.GlobalBus.Subscribe(eventKey, action));
        }

        /// <summary>
        /// 取消订阅玩家状态事件
        /// </summary>
        private void UnsubscribeStatEvents()
        {
            for (int i = 0; i < statEventDisposableList.Count; i++)
                statEventDisposableList[i].Dispose();
            statEventDisposableList.Clear();
        }

        #endregion

        #region 生存数值

        /// <summary>
        /// 体力变化回调
        /// </summary>
        private void OnPowerChanged(float currentPower, float maxPower, bool showAnimation)
        {
            SetPowerBar(currentPower, maxPower, showAnimation);
        }

        /// <summary>
        /// 水分变化回调
        /// </summary>
        private void OnWaterChanged(float currentWater, float maxWater, bool showAnimation)
        {
            SetWaterStatus(currentWater, maxWater, showAnimation);
        }

        /// <summary>
        /// 饱食度变化回调
        /// </summary>
        private void OnFoodChanged(float currentFood, float maxFood, bool showAnimation)
        {
            SetFoodStatus(currentFood, maxFood, showAnimation);
        }

        /// <summary>
        /// 血量变化回调 刷新条与残血/濒死表现
        /// </summary>
        private void OnHealthChanged(float currentHealth, float maxHealth, bool showAnimation)
        {
            SetHealthStatus(currentHealth, maxHealth, showAnimation);

            float residual01 = EvaluateHudIntensity(
                currentHealth, criticalThreshold.x, criticalThreshold.y);
            float dying01 = EvaluateHudIntensity(
                currentHealth, highHpThreshold.x, highHpThreshold.y);
            SetHighHp(residual01, dying01);

            // 主动画改值且掉血时闪红 自然衰减 showAnimation 为 false 不闪
            bool playHit = showAnimation
                && lastHealth >= 0f
                && currentHealth < lastHealth;
            if (playHit)
                PlayHit();

            lastHealth = currentHealth;
        }

        /// <summary>
        /// 理智变化回调 刷新条与轻中重后处理
        /// </summary>
        private void OnSanChanged(float currentSan, float maxSan, bool showAnimation)
        {
            SetSanStatus(currentSan, maxSan, showAnimation);

            float mild01 = EvaluateHudIntensity(
                currentSan, sanMildThreshold.x, sanMildThreshold.y);
            float medium01 = EvaluateHudIntensity(
                currentSan, sanMediumThreshold.x, sanMediumThreshold.y);
            float severe01 = EvaluateHudIntensity(
                currentSan, sanSevereThreshold.x, sanSevereThreshold.y);
            SetSanHud(mild01, medium01, severe01);
        }

        /// <summary>
        /// 将当前值映射为区间内 0到1 强度 高于上限为0 低于下限为1
        /// </summary>
        private static float EvaluateHudIntensity(float current, int rangeMin, int rangeMax)
        {
            if (current > rangeMax)
                return 0f;
            if (current <= rangeMin)
                return 1f;
            return Mathf.InverseLerp(rangeMax, rangeMin, current);
        }

        #endregion

        #region 准心

        /// <summary>
        /// 设置准心与其动画效果
        /// </summary>
        public void SetCrosshair(ECrosshairType crosshairType, bool showAnimation)
        {
            View.CrosshairCrosshair.SetCrosshair(crosshairType, showAnimation);
        }

        /// <summary>
        /// 设置准心移动微扩
        /// </summary>
        public void SetCrosshairMoving(bool isMoving)
        {
            View.CrosshairCrosshair.SetMoving(isMoving);
        }

        /// <summary>
        /// 攻击挥空准心放大
        /// </summary>
        public void PlayCrosshairAttackPunch()
        {
            View.CrosshairCrosshair.PlayAttackPunch();
        }

        /// <summary>
        /// 攻击命中 X 准心反馈
        /// </summary>
        public void PlayCrosshairHitMarker()
        {
            View.CrosshairCrosshair.PlayHitMarker();
        }

        #endregion

        #region 生存数值条

        /// <summary>
        /// 设置体力
        /// </summary>
        /// <param name="currentPower">当前体力</param>
        /// <param name="maxPower">最大体力</param>
        /// <param name="showAnimation">是否播放缓动</param>
        public void SetPowerBar(float currentPower, float maxPower, bool showAnimation)
        {
            if (showAnimation)
                View.PowerPowerBar.SetValue(currentPower, maxPower);
            else
                View.PowerPowerBar.SetValueInstant(currentPower, maxPower);
        }

        /// <summary>
        /// 设置饱食度
        /// </summary>
        /// <param name="food">当前饱食度</param>
        /// <param name="maxFood">最大饱食度</param>
        /// <param name="showAnimation">是否播放缓动</param>
        public void SetFoodStatus(float food, float maxFood, bool showAnimation)
        {
            if (showAnimation)
                View.FoodStatus.SetValue(food, maxFood);
            else
                View.FoodStatus.SetValueInstant(food, maxFood);
        }

        /// <summary>
        /// 设置水分
        /// </summary>
        /// <param name="water">当前水分</param>
        /// <param name="maxWater">最大水分</param>
        /// <param name="showAnimation">是否播放缓动</param>
        public void SetWaterStatus(float water, float maxWater, bool showAnimation)
        {
            if (showAnimation)
                View.WaterStatus.SetValue(water, maxWater);
            else
                View.WaterStatus.SetValueInstant(water, maxWater);
        }

        /// <summary>
        /// 设置理智值
        /// </summary>
        /// <param name="san">当前理智</param>
        /// <param name="maxSan">最大理智</param>
        /// <param name="showAnimation">是否播放缓动</param>
        public void SetSanStatus(float san, float maxSan, bool showAnimation)
        {
            if (showAnimation)
                View.SanStatus.SetValue(san, maxSan);
            else
                View.SanStatus.SetValueInstant(san, maxSan);
        }

        /// <summary>
        /// 设置血量
        /// </summary>
        /// <param name="health">当前血量</param>
        /// <param name="maxHealth">最大血量</param>
        /// <param name="showAnimation">是否播放缓动</param>
        public void SetHealthStatus(float health, float maxHealth, bool showAnimation)
        {
            if (showAnimation)
                View.HealthStatus.SetValue(health, maxHealth);
            else
                View.HealthStatus.SetValueInstant(health, maxHealth);
        }

        #endregion

        #region HUD后处理

        /// <summary>
        /// 同时设置残血与濒死强度
        /// </summary>
        public void SetHighHp(float lowHp01, float critical01)
        {
            View.DamageIndicatorDamageIndicator.SetState(lowHp01, critical01);
        }

        /// <summary>
        /// 播放挨打闪红
        /// </summary>
        public void PlayHit()
        {
            View.DamageIndicatorDamageIndicator.PlayHit();
        }

        /// <summary>
        /// 同时设置轻中重理智后处理强度
        /// </summary>
        public void SetSanHud(float mild01, float medium01, float severe01)
        {
            View.SanIndicatorSanIndicator.SetState(mild01, medium01, severe01);
        }

        #endregion
    }
}
