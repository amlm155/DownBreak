using System;
using System.Collections.Generic;
using cfg.item;
using MieMieFrameWork;
using MiMieEventBus;
using UnityEngine;

namespace GAS.StateSystem
{
    /// <summary>
    /// 属性控制器 分桶管理被动属性与即时属性
    /// 即时属性变化由本类直接发布 PlayerStatEvents
    /// </summary>
    public class StatController : MonoBehaviour, IPlayerStatus
    {
        [SerializeField]
        private List<StatData> statDataList = new();

        /// <summary> 被动属性字典 </summary>
        private readonly Dictionary<string, IPassiveStat> passiveStatDict =
            new Dictionary<string, IPassiveStat>(StringComparer.OrdinalIgnoreCase);

        /// <summary> 即时属性字典 </summary>
        private readonly Dictionary<string, IImmediateStat> immediateStatDict =
            new Dictionary<string, IImmediateStat>(StringComparer.OrdinalIgnoreCase);

        /// <summary> 即时属性当前值变化回调 参数为属性名 供阈值系统按需刷新 </summary>
        public event Action<string> OnImmediateStatChanged;

        public IReadOnlyDictionary<string, IPassiveStat> PassiveStatDict => passiveStatDict;
        public IReadOnlyDictionary<string, IImmediateStat> ImmediateStatDict => immediateStatDict;

        /// <summary>
        /// 初始化属性 
        /// 区分即时属性和被动属性 加入到对应管理字典之中
        /// </summary>
        public void Init()
        {
            passiveStatDict.Clear();
            immediateStatDict.Clear();

            foreach (var data in statDataList)
            {
                if (data == null) continue;

                string key = data.name;
                if (string.IsNullOrWhiteSpace(key)) continue;

                // 同名不可跨桶重复
                if (passiveStatDict.ContainsKey(key) || immediateStatDict.ContainsKey(key))
                {
                    Debug.LogWarning($"[StatController] 属性名重复已跳过: {key}");
                    continue;
                }

                if (data.StatType == E_StatType.Immediate)
                {
                    var imStat = new ImmediateStat(data, this);
                    imStat.Initialize();
                    immediateStatDict[key] = imStat;
                }
                else
                {
                    var passiveStat = new PassiveStat(data, this);
                    passiveStat.Initialize();
                    passiveStatDict[key] = passiveStat;
                }
            }
        }

        /// <summary>
        /// 添加属性数据
        /// </summary>
        /// <param name="statData"></param>
        public void AddStatData(StatData statData)
        {
            statDataList.Add(statData);
        }

        /// <summary>
        /// 获取基础值 两类都可查
        /// </summary>
        public float GetValue(string statName)
        {
            if (immediateStatDict.TryGetValue(statName, out var imStat))
                return imStat.BaseValue;

            if (passiveStatDict.TryGetValue(statName, out var passiveStat))
                return passiveStat.BaseValue;

            return 0f;
        }

        /// <summary>
        /// 获取当前展示值 即时取Current 被动取Final
        /// </summary>
        public float GetCurrentValue(string statName)
        {
            if (immediateStatDict.TryGetValue(statName, out var imStat))
                return imStat.CurrentValue;

            if (passiveStatDict.TryGetValue(statName, out var passiveStat))
                return passiveStat.FinalValue;

            return 0f;
        }

        #region 被动属性

        /// <summary>
        /// 获取被动属性
        /// </summary>
        public IPassiveStat GetPassiveStat(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName)) return null;
            passiveStatDict.TryGetValue(statName, out var passiveStat);
            return passiveStat;
        }

        /// <summary>
        /// 添加修饰符
        /// </summary>
        public void AddModifier(string statName, StatModifier modifier)
        {
            var passiveStat = GetPassiveStat(statName);
            if (passiveStat == null || modifier == null) return;
            passiveStat.AddModifier(modifier);
        }

        /// <summary>
        /// 从来源移除修饰符
        /// </summary>
        public void RemoveModifiersFromSource(string statName, object source)
        {
            var passiveStat = GetPassiveStat(statName);
            if (passiveStat is null) return;
            passiveStat.RemoveModifiersFromSource(source);
        }

        #endregion

        #region 即时属性

        /// <summary>
        /// 获取即时属性
        /// </summary>
        public IImmediateStat GetImStat(string statName)
        {
            if (string.IsNullOrWhiteSpace(statName)) return null;
            immediateStatDict.TryGetValue(statName, out var imStat);
            return imStat;
        }

        /// <summary>
        /// 为即时属性添加最大值修饰符
        /// </summary>
        public void AddMaxModifier(string statName, StatModifier modifier)
        {
            var imStat = GetImStat(statName);
            if (imStat is null || modifier is null) return;
            imStat.AddMaxModifier(modifier);
        }

        /// <summary>
        /// 从来源移除即时属性的最大值修饰符
        /// </summary>
        public void RemoveMaxModifiersFromSource(string statName, object source)
        {
            var imStat = GetImStat(statName);
            if (imStat is null) return;
            imStat.RemoveMaxModifiersFromSource(source);
        }

        /// <summary>
        /// 对即时属性做瞬时变化 有实际变化时发布对外事件
        /// </summary>
        public void ChangeAttributeValue(string statName,
                                         float magnitude,
                                         E_ModifierType modifierType,
                                         object source = null,
                                         bool showAnimation = false)
        {
            var imStat = GetImStat(statName);
            if (imStat is null) return;
            if (!imStat.ChangeValue(magnitude, modifierType))
                return;
            PublishStatChanged(statName, imStat, showAnimation);
            OnImmediateStatChanged?.Invoke(statName);
        }

        /// <summary>
        /// 设置即时属性每秒自动变化量
        /// </summary>
        /// <param name="statName">属性名</param>
        /// <param name="perSecond">每秒变化量 正恢复负消耗 0停止</param>
        public void SetTickPerSecond(string statName, float perSecond)
        {
            var imStat = GetImStat(statName);
            if (imStat is null) return;
            imStat.SetTickPerSecond(perSecond);
        }

        /// <summary>
        /// 设置 GM 调试用即时属性当前值
        /// </summary>
        public void SetValueForDebug(string statName, float value)
        {
            var imStat = GetImStat(statName);
            if (imStat == null)
                return;

            imStat.SetCurrentValueForDebug(value);
            PublishStatChanged(statName, imStat, false);
            OnImmediateStatChanged?.Invoke(statName);
        }

        #endregion

        #region 每帧驱动

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (var pair in immediateStatDict)
            {
                if (!pair.Value.Tick(deltaTime))
                    continue;
                PublishStatChanged(pair.Key, pair.Value, false);
                OnImmediateStatChanged?.Invoke(pair.Key);
            }
        }

        #endregion

        #region 对外事件

        /// <summary>
        /// 发布玩家数值变化事件 非已知属性名则跳过
        /// </summary>
        private void PublishStatChanged(string statName, IImmediateStat imStat, bool showAnimation)
        {
            if (!TryResolveStatEvent(statName, out var eventKey))
                return;

            MmGlobalEventBus.GlobalBus.Publish(
                eventKey,
                imStat.CurrentValue,
                imStat.MaxValue,
                showAnimation);
        }

        /// <summary>
        /// 属性名映射到对外 EventKey
        /// </summary>
        private static bool TryResolveStatEvent(string statName, out EventKey<float, float, bool> eventKey)
        {
            if (string.Equals(statName, PlayerStatIds.Power, StringComparison.OrdinalIgnoreCase))
            {
                eventKey = PlayerStatEvents.PowerChanged;
                return true;
            }

            if (string.Equals(statName, PlayerStatIds.Water, StringComparison.OrdinalIgnoreCase))
            {
                eventKey = PlayerStatEvents.WaterChanged;
                return true;
            }

            if (string.Equals(statName, PlayerStatIds.Food, StringComparison.OrdinalIgnoreCase))
            {
                eventKey = PlayerStatEvents.FoodChanged;
                return true;
            }

            if (string.Equals(statName, PlayerStatIds.Health, StringComparison.OrdinalIgnoreCase))
            {
                eventKey = PlayerStatEvents.HealthChanged;
                return true;
            }

            if (string.Equals(statName, PlayerStatIds.San, StringComparison.OrdinalIgnoreCase))
            {
                eventKey = PlayerStatEvents.SanChanged;
                return true;
            }

            eventKey = default;
            return false;
        }

        #endregion

        #region IPlayerStatus 接口实现

        /// <summary>
        /// 获取属性最大值 即时属性返回 MaxValue 其余返回 0
        /// </summary>
        public float GetMaxValue(string statName)
        {
            var imStat = GetImStat(statName);
            if (imStat == null)
                return 0f;
            return imStat.MaxValue;
        }

        /// <summary>
        /// 对属性做瞬时变化
        /// </summary>
        public void ChangeValue(string statName, float delta, bool showAnimation = false)
        {
            ChangeAttributeValue(statName, delta, E_ModifierType.FlatAdd, null, showAnimation);
        }

        /// <summary>
        /// 按食物表写入饱食水分 San
        /// </summary>
        public void ApplyFoodOrWaterEffects(FoodOrWater foodTable)
        {
            if (foodTable == null)
                return;

            if (foodTable.AddFoodValue != 0)
                ChangeValue(PlayerStatIds.Food, foodTable.AddFoodValue, true);
            if (foodTable.AddWaterValue != 0)
                ChangeValue(PlayerStatIds.Water, foodTable.AddWaterValue, true);
            if (foodTable.AddSanValue != 0)
                ChangeValue(PlayerStatIds.San, foodTable.AddSanValue, true);
        }

        /// <summary>
        /// 按药品表写入血量 San
        /// </summary>
        public void ApplyMedicineEffects(Medicine medicineTable)
        {
            if (medicineTable == null)
                return;

            if (medicineTable.HealthValue != 0)
                ChangeValue(PlayerStatIds.Health, medicineTable.HealthValue, true);
            if (medicineTable.AddSanValue != 0)
                ChangeValue(PlayerStatIds.San, medicineTable.AddSanValue, true);
        }

        #endregion
    }
}
