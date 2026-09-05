using MiMieEventBus;

namespace GAS.StateSystem
{
    /// <summary>
    /// 玩家生存数值对外事件 Key
    /// 由 StatController 发布 UI 订阅
    /// </summary>
    public static class PlayerStatEvents
    {
        /// <summary>
        /// 体力变化事件 参数 当前值 最大值 是否播放缓动
        /// </summary>
        public static readonly EventKey<float, float, bool> PowerChanged =
            new EventKey<float, float, bool>("PlayerState.PowerChanged");

        /// <summary>
        /// 水分变化事件 参数 当前值 最大值 是否播放缓动
        /// </summary>
        public static readonly EventKey<float, float, bool> WaterChanged =
            new EventKey<float, float, bool>("PlayerState.WaterChanged");

        /// <summary>
        /// 饱食度变化事件 参数 当前值 最大值 是否播放缓动
        /// </summary>
        public static readonly EventKey<float, float, bool> FoodChanged =
            new EventKey<float, float, bool>("PlayerState.FoodChanged");

        /// <summary>
        /// 血量变化事件 参数 当前值 最大值 是否播放缓动
        /// </summary>
        public static readonly EventKey<float, float, bool> HealthChanged =
            new EventKey<float, float, bool>("PlayerState.HealthChanged");

        /// <summary>
        /// 理智变化事件 参数 当前值 最大值 是否播放缓动
        /// </summary>
        public static readonly EventKey<float, float, bool> SanChanged =
            new EventKey<float, float, bool>("PlayerState.SanChanged");

        /// <summary>
        /// 生存 GE 应用事件 参数 GE 配置表 ID
        /// </summary>
        public static readonly EventKey<int> SurvivalEffectApplied =
            new EventKey<int>("PlayerState.SurvivalEffectApplied");

        /// <summary>
        /// 生存 GE 移除事件 参数 GE 配置表 ID
        /// </summary>
        public static readonly EventKey<int> SurvivalEffectRemoved =
            new EventKey<int>("PlayerState.SurvivalEffectRemoved");
    }
}
