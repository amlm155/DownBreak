using System;
using System.Collections.Generic;
using UnityEngine;

namespace GAS.StateSystem
{
    /// <summary>
    /// 即时属性
    /// HP、MP 等直接变化的资源属性
    /// </summary>
    [System.Serializable]
    public class ImmediateStat : IImmediateStat
    {
        //控制器
        private readonly StatController controller;
        public StatController Controller => controller;

        //原值
        private readonly float baseValue;
        public float BaseValue => baseValue;

        //基础最大值
        private readonly float baseMaxValue;

        //最小/最大值
        private readonly float minValue;
        private readonly float maxValue;
        public float MinValue => minValue;

        //当前值
        private float currentValue;
        public float CurrentValue => currentValue;
        public float MaxValue => CalculateMaxValue();
        public float HardMaxValue => maxValue;

        //每秒自动变化量
        private float tickPerSecond;
        public float TickPerSecond => tickPerSecond;

        /// <summary> 最大值修饰符列表 </summary>
        private readonly List<StatModifier> maxModifierList = new List<StatModifier>();

        //当前值变化事件
        public event Action CurValueChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ImmediateStat(StatData definition, StatController controller)
        {
            this.controller = controller;
            this.baseValue = definition.BaseValue;
            this.baseMaxValue = definition.BaseMaxValue;
            this.minValue = definition.MinValue;
            this.maxValue = definition.MaxValue;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Initialize()
        {
            currentValue = baseValue;
            CurValueChanged?.Invoke();
        }

        /// <summary>
        /// 瞬时变化（直接修改当前值）
        /// </summary>
        /// <returns>是否发生实际变化</returns>
        public virtual bool ChangeValue(float magnitude, E_ModifierType modifierType)
        {
            float previousValue = currentValue;
            float newValue = currentValue;

            switch (modifierType)
            {
                case E_ModifierType.FlatAdd:
                    newValue += magnitude;
                    break;
                case E_ModifierType.PercentageAdd:
                    newValue *= (1 + magnitude / 100f);
                    break;
                case E_ModifierType.FinalAdd:
                    newValue += magnitude;
                    break;
                case E_ModifierType.FinalPercentage:
                    newValue *= (1 + magnitude / 100f);
                    break;
            }
            currentValue = Mathf.Clamp(newValue, minValue, MaxValue);
            // 钳制后无实际变化不发事件 避免满值再吃刷空回调
            if (Mathf.Approximately(currentValue, previousValue))
                return false;

            CurValueChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 将当前值恢复到基础值
        /// </summary>
        public virtual void Restore()
        {
            currentValue = Mathf.Clamp(baseValue, minValue, MaxValue);
            CurValueChanged?.Invoke();
        }

        /// <summary>
        /// 添加最大值修饰符
        /// </summary>
        public virtual void AddMaxModifier(StatModifier modifier)
        {
            maxModifierList.Add(modifier);
            ClampCurrentValueToMax();
            CurValueChanged?.Invoke();
        }

        /// <summary>
        /// 移除指定来源的最大值修饰符
        /// </summary>
        public virtual void RemoveMaxModifiersFromSource(object source)
        {
            int removedCount = maxModifierList.RemoveAll(modifier => modifier.Source == source);
            if (removedCount <= 0)
                return;

            ClampCurrentValueToMax();
            CurValueChanged?.Invoke();
        }

        /// <summary>
        /// 设置调试用当前值
        /// 允许 GM 窗口测试到属性硬上限
        /// </summary>
        public virtual void SetCurrentValueForDebug(float value)
        {
            float previousValue = currentValue;
            currentValue = Mathf.Clamp(value, minValue, HardMaxValue);
            if (Mathf.Approximately(currentValue, previousValue))
                return;

            CurValueChanged?.Invoke();
        }

        /// <summary>
        /// 设置每秒自动变化量
        /// </summary>
        /// <param name="perSecond">每秒变化量 正恢复负消耗 0停止</param>
        public virtual void SetTickPerSecond(float perSecond)
        {
            tickPerSecond = perSecond;
        }

        /// <summary>
        /// 按每秒变化量走一帧 越界自动钳制到边界
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        /// <returns>是否发生实际变化</returns>
        public virtual bool Tick(float deltaTime)
        {
            if (Mathf.Approximately(tickPerSecond, 0f))
                return false;

            // 计算本帧变化量
            float delta = tickPerSecond * deltaTime;

            // 钳制到边界避免超出
            float newValue = Mathf.Clamp(currentValue + delta, minValue, MaxValue);

            // 未变化则不发事件
            if (Mathf.Approximately(newValue, currentValue))
                return false;

            currentValue = newValue;
            CurValueChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 计算即时属性最大值
        /// </summary>
        private float CalculateMaxValue()
        {
            float result = baseMaxValue;
            float flatAdd = 0f;
            float percentageAdd = 0f;
            float finalAdd = 0f;
            float finalPercentage = 0f;

            for (int i = 0; i < maxModifierList.Count; i++)
            {
                StatModifier modifier = maxModifierList[i];
                switch (modifier.eModifierType)
                {
                    case E_ModifierType.FlatAdd:
                        flatAdd += modifier.Value;
                        break;
                    case E_ModifierType.PercentageAdd:
                        percentageAdd += modifier.Value;
                        break;
                    case E_ModifierType.FinalAdd:
                        finalAdd += modifier.Value;
                        break;
                    case E_ModifierType.FinalPercentage:
                        finalPercentage += modifier.Value;
                        break;
                }
            }

            result += flatAdd;
            result *= 1f + percentageAdd / 100f;
            result += finalAdd;
            result *= 1f + finalPercentage / 100f;
            return Mathf.Clamp(result, minValue, maxValue);
        }

        /// <summary>
        /// 将当前值限制在最新最大值内
        /// </summary>
        private void ClampCurrentValueToMax()
        {
            currentValue = Mathf.Clamp(currentValue, minValue, MaxValue);
        }
    }
}
