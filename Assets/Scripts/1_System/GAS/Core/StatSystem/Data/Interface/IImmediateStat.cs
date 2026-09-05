using System;

namespace GAS.StateSystem
{

    /// <summary>
    /// 即时属性接口 HP MP 等资源属性
    /// </summary>
    public interface IImmediateStat
    {
        /// <summary> 基础值 </summary>
        float BaseValue { get; }

        /// <summary> 最小值 </summary>
        float MinValue { get; }

        /// <summary> 当前值 </summary>
        float CurrentValue { get; }

        /// <summary> 最大值 </summary>
        float MaxValue { get; }

        /// <summary> 属性定义的绝对硬上限 </summary>
        float HardMaxValue { get; }

        /// <summary> 每秒自动变化量 正恢复负消耗 0为停止 </summary>
        float TickPerSecond { get; }

        /// <summary> 所属控制器 </summary>
        StatController Controller { get; }

        /// <summary>
        /// 初始化
        /// </summary>
        void Initialize();

        /// <summary>
        /// 瞬时变化
        /// </summary>
        /// <returns>是否发生实际变化</returns>
        bool ChangeValue(float magnitude, E_ModifierType modifierType);

        /// <summary>
        /// 设置每秒自动变化量
        /// </summary>
        /// <param name="perSecond">每秒变化量 正恢复负消耗 0停止</param>
        void SetTickPerSecond(float perSecond);

        /// <summary>
        /// 按每秒变化量走一帧
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        /// <returns>是否发生实际变化</returns>
        bool Tick(float deltaTime);

        /// <summary>
        /// 恢复到基础值
        /// </summary>
        void Restore();

        /// <summary> 当前值变化事件 </summary>
        event Action CurValueChanged;

        /// <summary>
        /// 添加最大值修饰符
        /// </summary>
        void AddMaxModifier(StatModifier modifier);

        /// <summary>
        /// 移除指定来源的最大值修饰符
        /// </summary>
        void RemoveMaxModifiersFromSource(object source);

        /// <summary>
        /// 设置调试用当前值
        /// </summary>
        void SetCurrentValueForDebug(float value);
    }
}
